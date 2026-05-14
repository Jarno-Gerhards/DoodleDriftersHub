const WebSocket = require("ws");

const port = process.env.PORT || 8080;
const wss = new WebSocket.Server({ port });

const rooms = new Map();

console.log("Server running on port", port);

wss.on("connection", (ws) => {
  ws.on("message", (raw) => {
    let data;

    try {
      data = JSON.parse(raw);
    } catch {
      return;
    }

    switch (data.type) {
      case "HOST_CONNECT":
        handleHostConnect(ws, data);
        break;

      case "JOIN":
        handleJoin(ws, data);
        break;

      case "PING":
        safeSend(ws, { type: "KEEP_ALIVE" });
        break;

      case "STATE":
        handleStateChange(ws, data);
        break;

      case "SUBMIT_CHAMPION":
        handleChampionSubmit(ws, data);
        break;

      case "SUBMIT_SOLUTION":
        handleSolutionSubmit(ws, data);
        break;

      case "VOTE":
        handleVote(ws, data);
        break;
    }
  });

  ws.on("close", () => {
    const room = getRoomForWs(ws);
    if (!room) return;

    if (ws === room.host) {
      broadcastRoom(room, { type: "ROOM_CLOSED" });
      rooms.delete(room.roomCode);
      console.log("Host disconnected, room closed", room.roomCode);
      return;
    }

    const playerId = room.wsToPlayerId.get(ws);
    if (playerId) {
      room.wsToPlayerId.delete(ws);
      room.players.delete(playerId);
      room.submissions.delete(playerId);
      room.votes.delete(playerId);
      room.expectedPlayers = Math.max(0, room.expectedPlayers - 1);
      maybeFinalizeAfterDisconnect(room);
      console.log("Player disconnected", playerId, "room", room.roomCode);
    }
  });
});

function handleHostConnect(ws, data) {
  const requestedCode = normalizeRoomCode(data.roomCode);
  const roomCode = requestedCode && !rooms.has(requestedCode)
    ? requestedCode
    : generateRoomCode();

  const room = createRoom(roomCode);
  room.host = ws;
  rooms.set(roomCode, room);

  ws.roomCode = roomCode;
  ws.role = "host";

  safeSend(ws, { type: "HOST_READY", roomCode });
  console.log("Host connected, room", roomCode);
}

function handleJoin(ws, data) {
  const roomCode = normalizeRoomCode(data.roomCode);
  if (!roomCode || !rooms.has(roomCode)) {
    safeSend(ws, { type: "JOIN_DENIED", reason: "ROOM_NOT_FOUND" });
    return;
  }

  const room = rooms.get(roomCode);
  const incomingId = typeof data.playerId === "string" ? data.playerId : "";
  const incomingName = typeof data.name === "string" ? data.name : "Player";

  const hasExisting = incomingId && room.players.has(incomingId);
  const playerId = hasExisting ? incomingId : generatePlayerId();
  const isNew = !hasExisting;

  room.players.set(playerId, { ws, name: incomingName });
  room.wsToPlayerId.set(ws, playerId);
  ws.playerId = playerId;
  ws.roomCode = roomCode;
  ws.role = "player";

  safeSend(ws, { type: "JOINED", playerId, roomCode });

  if (isNew && room.host) {
    safeSend(room.host, {
      type: "PLAYER_JOINED",
      playerId,
      name: incomingName
    });
  }
}

function handleStateChange(ws, data) {
  const room = getRoomForWs(ws);
  if (!room || ws !== room.host) return;
  if (typeof data.state !== "string") return;

  const normalizedState = normalizeStateName(data.state);

  if (normalizedState === "Solution") {
    resetRound(room, room.players.size);
  }

  broadcastRoom(room, { type: "STATE", state: normalizedState });
}

function handleChampionSubmit(ws, data) {
  const room = getRoomForWs(ws);
  if (!room) return;

  const playerId = resolvePlayerId(room, ws, data.playerId);
  if (!playerId) return;

  const playerName = typeof data.playerName === "string" ? data.playerName : "Player";
  const imageDataUrl = typeof data.imageDataUrl === "string" ? data.imageDataUrl : "";

  const record = room.players.get(playerId);
  if (record) {
    record.name = playerName;
  }

  if (room.host) {
    safeSend(room.host, {
      type: "CHAMPION_SUBMITTED",
      playerId,
      playerName,
      imageDataUrl
    });
  }
}

function handleSolutionSubmit(ws, data) {
  const room = getRoomForWs(ws);
  if (!room) return;

  const playerId = resolvePlayerId(room, ws, data.playerId);
  if (!playerId) return;

  const playerName = typeof data.playerName === "string" ? data.playerName : "Player";
  const description = typeof data.description === "string" ? data.description : "";
  const imageDataUrl = typeof data.imageDataUrl === "string" ? data.imageDataUrl : "";

  if (room.submissions.has(playerId)) {
    return;
  }

  room.submissions.set(playerId, {
    playerId,
    playerName,
    description,
    imageDataUrl
  });

  if (room.submissions.size >= room.expectedPlayers && room.expectedPlayers > 0) {
    openVoting(room);
  }
}

function handleVote(ws, data) {
  const room = getRoomForWs(ws);
  if (!room || !room.votingOpen) return;

  const voterPlayerId = resolvePlayerId(room, ws, data.voterPlayerId);
  const chosenPlayerId = typeof data.chosenPlayerId === "string" ? data.chosenPlayerId : null;

  if (!voterPlayerId || !chosenPlayerId) return;
  if (voterPlayerId === chosenPlayerId) return;
  if (!room.submissions.has(chosenPlayerId)) return;
  if (room.votes.has(voterPlayerId)) return;

  room.votes.set(voterPlayerId, chosenPlayerId);

  if (room.votes.size >= room.expectedPlayers && room.expectedPlayers > 0) {
    finalizeVoting(room);
  }
}

function openVoting(room) {
  if (room.votingOpen) return;
  room.votingOpen = true;

  const list = Array.from(room.submissions.values());
  broadcastRoom(room, { type: "VOTING_SUBMISSIONS", submissions: list });
  broadcastRoom(room, { type: "STATE", state: "Voting" });

  if (room.host) {
    safeSend(room.host, { type: "SOLUTION_SUBMISSIONS", submissions: list });
  }
}

function finalizeVoting(room) {
  room.votingOpen = false;

  const winner = resolveWinner(room);
  if (room.host) {
    safeSend(room.host, { type: "VOTE_RESULT", winner });
  }

  broadcastRoom(room, {
    type: "VOTE_RESULT",
    winnerPlayerId: winner ? winner.playerId : null
  });
}

function resolveWinner(room) {
  if (room.submissions.size === 0) return null;

  const voteCounts = new Map();
  room.submissions.forEach((_, playerId) => voteCounts.set(playerId, 0));
  room.votes.forEach((chosenId) => {
    if (voteCounts.has(chosenId)) {
      voteCounts.set(chosenId, voteCounts.get(chosenId) + 1);
    }
  });

  let winner = null;
  let winnerVotes = -1;
  room.submissions.forEach((submission, playerId) => {
    const count = voteCounts.get(playerId) || 0;
    if (count > winnerVotes) {
      winnerVotes = count;
      winner = submission;
    }
  });

  return winner;
}

function resolvePlayerId(room, ws, fallback) {
  if (typeof fallback === "string" && room.players.has(fallback)) {
    return fallback;
  }
  const fromWs = room.wsToPlayerId.get(ws);
  return fromWs || null;
}

function resetRound(room, expectedCount) {
  room.expectedPlayers = Math.max(0, expectedCount || 0);
  room.submissions.clear();
  room.votes.clear();
  room.votingOpen = false;
}

function maybeFinalizeAfterDisconnect(room) {
  if (!room.votingOpen && room.submissions.size >= room.expectedPlayers && room.expectedPlayers > 0) {
    openVoting(room);
  }

  if (room.votingOpen && room.votes.size >= room.expectedPlayers && room.expectedPlayers > 0) {
    finalizeVoting(room);
  }
}

function broadcastRoom(room, message) {
  const payload = JSON.stringify(message);
  room.players.forEach((player) => {
    if (player && player.ws && player.ws.readyState === WebSocket.OPEN) {
      player.ws.send(payload);
    }
  });
}

function safeSend(target, message) {
  if (!target || target.readyState !== WebSocket.OPEN) return;
  target.send(JSON.stringify(message));
}

function generateRoomCode() {
  let code = "";
  while (!code || rooms.has(code)) {
    code = String(Math.floor(10000 + Math.random() * 90000));
  }
  return code;
}

function generatePlayerId() {
  return "p" + Date.now() + Math.floor(Math.random() * 1000);
}

function normalizeRoomCode(value) {
  const raw = String(value || "").trim();
  if (!raw) return "";
  const digits = raw.replace(/[^0-9]/g, "");
  return digits.length === 5 ? digits : "";
}

function normalizeStateName(value) {
  const raw = String(value || "").trim();
  if (!raw) return "";

  const lower = raw.toLowerCase();
  if (lower === "lobby") return "Lobby";
  if (lower === "drawing") return "Drawing";
  if (lower === "solution") return "Solution";
  if (lower === "voting") return "Voting";
  return raw;
}

function createRoom(roomCode) {
  return {
    roomCode,
    host: null,
    players: new Map(),
    wsToPlayerId: new Map(),
    expectedPlayers: 0,
    votingOpen: false,
    submissions: new Map(),
    votes: new Map()
  };
}

function getRoomForWs(ws) {
  if (!ws || !ws.roomCode) return null;
  return rooms.get(ws.roomCode) || null;
}
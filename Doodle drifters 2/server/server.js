const WebSocket = require("ws");

const port = process.env.PORT || 8080;
const wss = new WebSocket.Server({ port });

let host = null;
let players = {};

console.log("Server running on port", port);

wss.on("connection", (ws) => {

  ws.on("message", (msg) => {
    let data;

    try {
      data = JSON.parse(msg);
    } catch {
      return;
    }

    switch (data.type) {

      case "HOST_CONNECT":
        host = ws;
        console.log("Host connected");
        break;

      case "JOIN":
        const id = "p" + Date.now();
        players[id] = ws;
        ws.playerId = id;

        if (host) {
          host.send(JSON.stringify({
            type: "PLAYER_JOINED",
            playerId: id,
            name: data.name
          }));
        }
        break;

      case "SUBMIT_DRAWING":
        if (host) {
          host.send(JSON.stringify({
            type: "DRAWING_SUBMITTED",
            playerId: ws.playerId,
            image: data.image
          }));
        }
        break;

      case "VOTE":
        if (host) {
          host.send(JSON.stringify({
            type: "VOTE_SUBMITTED",
            playerId: ws.playerId,
            targetId: data.drawingId
          }));
        }
        break;

      case "START_DRAW":
        broadcast({ type: "STATE", state: "DRAW" });
        break;

      case "START_VOTE":
        broadcast({ type: "STATE", state: "VOTE" });
        break;
    }
  });

  ws.on("close", () => {
    if (ws === host) {
      host = null;
      players = {};
      console.log("Host disconnected, reset");
    }
  });
});

function broadcast(msg) {
  Object.values(players).forEach(p => {
    p.send(JSON.stringify(msg));
  });
}
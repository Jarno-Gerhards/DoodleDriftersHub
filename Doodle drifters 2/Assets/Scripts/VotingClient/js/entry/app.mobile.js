import { VotingClientPanel } from "../shared/votingClientPanel.js";
import { startClientStateApp } from "../../../State Machine/Client State Machine/ClientStateApp.js";
import { createClientSocket, getClientSession, setClientSession } from "../../../State Machine/Client State Machine/ClientNetwork.js";

window.clientStateMachine = startClientStateApp({ mode: "page" });

const session = getClientSession();
if (!session.playerId) {
  const joinUrl = new URL("../../../../Client/Join Room Environment/index.html", import.meta.url).toString();
  window.location.href = joinUrl;
}

const votingGrid = document.getElementById("votingGrid");
const voteStatus = document.getElementById("voteStatus");

const localPlayerId = session.playerId || "player-1";

const panel = new VotingClientPanel({
  gridElement: votingGrid,
  statusElement: voteStatus,
  localPlayerId,
  onVoteSubmitted: (payload) => {
    clientSocket.send({
      type: "VOTE",
      voterPlayerId: payload.voterPlayerId,
      chosenPlayerId: payload.chosenPlayerId
    });
    window.dispatchEvent(new CustomEvent("voting:vote-submitted", { detail: payload }));
    console.log("[VotingClientMobile] vote submitted", payload);
  }
});

panel.setSubmissions([]);
panel.setStatus("Waiting for submissions...");

window.addEventListener("voting:update-submissions", (event) => {
  const submissions = event && event.detail ? event.detail.submissions : [];
  if (Array.isArray(submissions) && submissions.length > 0) {
    panel.setSubmissions(submissions);
  }
});

window.addEventListener("voting:result", (event) => {
  const winnerPlayerId = event && event.detail ? event.detail.winnerPlayerId : "";
  if (winnerPlayerId) {
    panel.lockWithWinner(winnerPlayerId);
  }
});

let clientSocket = null;
clientSocket = createClientSocket({
  onOpen: () => {
    const current = getClientSession();
    clientSocket.send({
      type: "JOIN",
      roomCode: sessionStorage.getItem("dd-room-code") || "",
      playerId: current.playerId,
      name: current.playerName
    });
  },
  onMessage: (msg) => {
    if (!msg) return;
    if (msg.type === "JOINED") {
      setClientSession({ playerId: msg.playerId });
    }
    if (msg.type === "STATE" && msg.state) {
      window.clientStateMachine.switchState(msg.state);
    }
    if (msg.type === "VOTING_SUBMISSIONS" && Array.isArray(msg.submissions)) {
      panel.setSubmissions(msg.submissions);
    }
    if (msg.type === "VOTE_RESULT" && msg.winnerPlayerId) {
      panel.lockWithWinner(msg.winnerPlayerId);
    }
  }
});

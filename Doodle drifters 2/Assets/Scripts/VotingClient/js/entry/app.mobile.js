import { VotingClientPanel } from "../shared/votingClientPanel.js";
import { startClientStateApp } from "../../../State Machine/Client State Machine/ClientStateApp.js";

window.clientStateMachine = startClientStateApp({ mode: "page" });

const votingGrid = document.getElementById("votingGrid");
const voteStatus = document.getElementById("voteStatus");

const params = new URLSearchParams(window.location.search);
const localPlayerId = params.get("playerId") || "player-1";

const panel = new VotingClientPanel({
  gridElement: votingGrid,
  statusElement: voteStatus,
  localPlayerId,
  onVoteSubmitted: (payload) => {
    window.dispatchEvent(new CustomEvent("voting:vote-submitted", { detail: payload }));
    console.log("[VotingClientMobile] vote submitted", payload);
  }
});

function getDemoSubmissions() {
  return [
    { playerId: "player-1", playerName: "Player 1", imageDataUrl: "" },
    { playerId: "player-2", playerName: "Player 2", imageDataUrl: "" },
    { playerId: "player-3", playerName: "Player 3", imageDataUrl: "" },
    { playerId: "player-4", playerName: "Player 4", imageDataUrl: "" },
    { playerId: "player-5", playerName: "Player 5", imageDataUrl: "" },
    { playerId: "player-6", playerName: "Player 6", imageDataUrl: "" },
    { playerId: "player-7", playerName: "Player 7", imageDataUrl: "" },
    { playerId: "player-8", playerName: "Player 8", imageDataUrl: "" },
    { playerId: "player-9", playerName: "Player 9", imageDataUrl: "" }
  ];
}

function getInitialSubmissions() {
  const incoming = window.__VOTING_SUBMISSIONS__;
  if (Array.isArray(incoming) && incoming.length > 0) {
    return incoming;
  }

  return getDemoSubmissions();
}

panel.setSubmissions(getInitialSubmissions());

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

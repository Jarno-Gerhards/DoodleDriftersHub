import { initMobileDrawingScreen } from "../shared/mobileDrawingScreen.js";
import { SolutionSubmitBridge } from "../../../VotingClient/js/shared/solutionSubmitBridge.js";
import { startClientStateApp } from "../../../State Machine/Client State Machine/ClientStateApp.js";
import { createClientSocket, getClientSession, setClientSession } from "../../../State Machine/Client State Machine/ClientNetwork.js";

window.clientStateMachine = startClientStateApp({ mode: "page" });

const session = getClientSession();
if (!session.playerId) {
  const joinUrl = new URL("../../../../Client/Join Room Environment/index.html", import.meta.url).toString();
  window.location.href = joinUrl;
}

const { drawer, textInput, submitBtn } = initMobileDrawingScreen({
  textInputId: "solutionDescription"
});

const statusLine = document.createElement("p");
statusLine.style.marginTop = "6px";
statusLine.style.textAlign = "center";
statusLine.style.color = "#f1d133";
const header = document.querySelector(".app-header");
if (header) {
  header.appendChild(statusLine);
}

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
  }
});

const bridge = new SolutionSubmitBridge({
  drawer,
  descriptionInput: textInput,
  submitButton: submitBtn,
  fallbackDescription: "Something useful",
  fallbackPlayerName: "Unknown Hero",
  onSubmit: (payload) => {
    const current = getClientSession();
    clientSocket.send({
      type: "SUBMIT_SOLUTION",
      playerId: current.playerId,
      playerName: current.playerName || payload.playerName,
      description: payload.description,
      imageDataUrl: payload.imageDataUrl
    });

    window.dispatchEvent(new CustomEvent("solution:submitted", { detail: payload }));
    console.log("[PhoneSolutionSubmit] submitted", payload.playerName);
    setWaiting("Submitted! Waiting for other players...");
  }
});

const query = new URLSearchParams(window.location.search);
const playerId = query.get("playerId");
const playerName = query.get("playerName");
if (playerId) {
  bridge.setLocalPlayer(playerId, playerName || "Unknown Hero");
}

bridge.init();

function setWaiting(message) {
  statusLine.textContent = message || "Waiting...";
  if (submitBtn) {
    submitBtn.disabled = true;
  }
}

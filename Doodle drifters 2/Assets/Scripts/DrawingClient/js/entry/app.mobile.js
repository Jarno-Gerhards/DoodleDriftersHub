import { initMobileDrawingScreen } from "../shared/mobileDrawingScreen.js";
import { createSubmissionPayload, dispatchSubmission } from "../core/gameLogic.js";
import { startClientStateApp } from "../../../State Machine/Client State Machine/ClientStateApp.js";
import { createClientSocket, getClientSession, setClientSession } from "../../../State Machine/Client State Machine/ClientNetwork.js";

window.clientStateMachine = startClientStateApp({ mode: "page" });

const session = getClientSession();
if (!session.playerId) {
  const joinUrl = new URL("../../../../Client/Join Room Environment/index.html", import.meta.url).toString();
  window.location.href = joinUrl;
}

const fallbackName = "Unnamed Champion";

const { drawer, textInput: championNameInput, submitBtn } = initMobileDrawingScreen({
  textInputId: "championName"
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

submitBtn.addEventListener("click", () => {
  const payload = createSubmissionPayload({
    drawer,
    championNameInput: championNameInput ? championNameInput.value : "",
    fallbackName
  });

  setClientSession({ playerName: payload.championName });
  const current = getClientSession();
  clientSocket.send({
    type: "SUBMIT_CHAMPION",
    playerId: current.playerId,
    playerName: payload.championName,
    imageDataUrl: payload.imageDataUrl
  });

  dispatchSubmission(payload);
  console.log("[PhoneDrawingSubmit] submitted", payload.championName);
  setWaiting("Submitted! Waiting for host...");
});

function setWaiting(message) {
  statusLine.textContent = message || "Waiting...";
  if (submitBtn) {
    submitBtn.disabled = true;
  }
}

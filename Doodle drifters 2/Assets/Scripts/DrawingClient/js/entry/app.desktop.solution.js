import { createDrawer } from "../core/drawingEngine.js";
import { DrawingUIController } from "../shared/drawingUIController.js";
import { SolutionSubmitBridge } from "../../../VotingClient/js/shared/solutionSubmitBridge.js";
import { startClientStateApp } from "../../../State Machine/Client State Machine/ClientStateApp.js";
import { createClientSocket, getClientSession, setClientSession } from "../../../State Machine/Client State Machine/ClientNetwork.js";

window.clientStateMachine = startClientStateApp({ mode: "page" });

const session = getClientSession();
if (!session.playerId) {
  const joinUrl = new URL("../../../../Client/Join Room Environment/index.html", import.meta.url).toString();
  window.location.href = joinUrl;
}

const canvas = document.getElementById("drawCanvas");
const toolButtons = document.querySelectorAll(".tool-btn");
const colorButtons = document.querySelectorAll(".color-btn");
const brushSizeSlider = document.getElementById("brushSize");
const brushSizeLabel = document.getElementById("brushSizeLabel");
const undoBtn = document.getElementById("undoBtn");
const clearBtn = document.getElementById("clearBtn");
const activeColorPreview = document.getElementById("activeColorPreview");
const solutionDescription = document.getElementById("solutionDescription");
const submitBtn = document.getElementById("submitBtn");
const sizePills = document.querySelectorAll(".size-pill");

const statusLine = document.createElement("p");
statusLine.style.marginTop = "8px";
statusLine.style.color = "#f1d133";
const header = document.querySelector(".title-wrap");
if (header) {
  header.appendChild(statusLine);
}

const drawer = createDrawer({
  canvas,
  textureWidth: 1280,
  textureHeight: 840,
  brushSize: 5,
  maxUndoSteps: 20
});

const ui = new DrawingUIController({
  drawer,
  toolButtons,
  colorButtons,
  brushSizeSlider,
  brushSizeLabel,
  undoButton: undoBtn,
  clearButton: clearBtn,
  activeColorPreview
});
ui.init();

sizePills.forEach((pill) => {
  pill.addEventListener("click", () => {
    const value = pill.getAttribute("data-size") || "0.1";
    brushSizeSlider.value = value;
    brushSizeSlider.dispatchEvent(new Event("input", { bubbles: true }));
  });
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
  }
});

function setWaiting(message) {
  statusLine.textContent = message || "Waiting...";
  if (submitBtn) {
    submitBtn.disabled = true;
  }
}

const bridge = new SolutionSubmitBridge({
  drawer,
  descriptionInput: solutionDescription,
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
    console.log("[DesktopSolutionSubmit] submitted", payload.playerName);
    setWaiting("Submitted! Waiting for other players...");
  }
});

bridge.init();
import { createDrawer } from "../core/drawingEngine.js";
import { DrawingUIController } from "../shared/drawingUIController.js";
import { DrawingSubmitBridge } from "../shared/drawingSubmitBridge.js";
import { HostScreenSimulator } from "../shared/hostScreenSimulator.js";
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
const championName = document.getElementById("championName");
const submitBtn = document.getElementById("submitBtn");
const hostGallery = document.getElementById("hostGallery");
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

const host = hostGallery
  ? new HostScreenSimulator({
      container: hostGallery,
      maxPortraits: 8
    })
  : null;

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

const submitBridge = new DrawingSubmitBridge({
  drawer,
  championNameInput: championName,
  submitButton: submitBtn,
  fallbackName: "Unnamed Champion",
  onSubmit: (payload) => {
    setClientSession({ playerName: payload.championName });
    const current = getClientSession();
    clientSocket.send({
      type: "SUBMIT_CHAMPION",
      playerId: current.playerId,
      playerName: payload.championName,
      imageDataUrl: payload.imageDataUrl
    });

    if (host) {
      host.addDrawing(payload.imageDataUrl, payload.championName);
    }

    setWaiting("Submitted! Waiting for host...");
  }
});
submitBridge.init();

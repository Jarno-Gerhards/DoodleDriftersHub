import { createDrawer } from "../core/drawingEngine.js";
import { DrawingUIController } from "../shared/drawingUIController.js";
import { SolutionSubmitBridge } from "../../../VotingClient/js/shared/solutionSubmitBridge.js";
import { startClientStateApp } from "../../../State Machine/Client State Machine/ClientStateApp.js";

window.clientStateMachine = startClientStateApp({ mode: "page" });

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

const bridge = new SolutionSubmitBridge({
  drawer,
  descriptionInput: solutionDescription,
  submitButton: submitBtn,
  fallbackDescription: "Something useful",
  fallbackPlayerName: "Unknown Hero",
  onSubmit: (payload) => {
    window.dispatchEvent(new CustomEvent("solution:submitted", { detail: payload }));
    console.log("[DesktopSolutionSubmit] submitted", payload.playerName);
  }
});

const query = new URLSearchParams(window.location.search);
const playerId = query.get("playerId");
const playerName = query.get("playerName");
if (playerId) {
  bridge.setLocalPlayer(playerId, playerName || "Unknown Hero");
}

bridge.init();
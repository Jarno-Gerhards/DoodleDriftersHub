import { createDrawer } from "../core/drawingEngine.js";
import { DrawingUIController } from "../shared/drawingUIController.js";
import { DrawingSubmitBridge } from "../shared/drawingSubmitBridge.js";
import { HostScreenSimulator } from "../shared/hostScreenSimulator.js";
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
const championName = document.getElementById("championName");
const submitBtn = document.getElementById("submitBtn");
const hostGallery = document.getElementById("hostGallery");
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

const host = hostGallery
  ? new HostScreenSimulator({
      container: hostGallery,
      maxPortraits: 8
    })
  : null;

const UNITY_DRAWING_ENDPOINT = "http://localhost:8085/drawing";

const submitBridge = new DrawingSubmitBridge({
  drawer,
  championNameInput: championName,
  submitButton: submitBtn,
  fallbackName: "Unnamed Champion",
  onSubmit: async (payload) => {
    try {
      const response = await fetch(UNITY_DRAWING_ENDPOINT, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        console.error("Unity rejected drawing submit:", await response.text());
      } else {
        console.log("Sent drawing to Unity:", payload.championName);
      }
    } catch (error) {
      console.error("Could not reach Unity localhost receiver:", error);
    }
  }
});
submitBridge.init();

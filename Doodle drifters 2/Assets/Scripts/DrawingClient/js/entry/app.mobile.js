import { createDrawer } from "../core/drawingEngine.js";
import { createSubmissionPayload, dispatchSubmission } from "../core/gameLogic.js";

const canvas = document.getElementById("drawCanvas");
const undoBtn = document.getElementById("undoBtn");
const submitBtn = document.getElementById("submitBtn");
const championNameInput = document.getElementById("championName");

const toolToggle = document.getElementById("toolToggle");
const brushToggle = document.getElementById("brushToggle");

const toolCurrentIcon = document.getElementById("toolCurrentIcon");
const activeColorPreview = document.getElementById("activeColorPreview");

const popupBackdrop = document.getElementById("popupBackdrop");
const toolPopup = document.getElementById("toolPopup");
const brushPopup = document.getElementById("brushPopup");

const brushSlider = document.getElementById("brushSize");

const fallbackName = "Unnamed Champion";

const drawer = createDrawer({
  canvas,
  textureWidth: 600,
  textureHeight: 820,
  brushSize: 5,
  maxUndoSteps: 20
});

let openPopup = null;

function setTool(toolName, icon) {
  drawer.setTool(toolName);
  toolCurrentIcon.textContent = icon;
}

function setColor(hex) {
  drawer.setColor(hex);
  activeColorPreview.style.background = hex;
}

function updateBrushUiFromSlider() {
  const value = Number(brushSlider.value);
  drawer.setBrushSizeNormalized(value);
}

function hideAllPopups() {
  popupBackdrop.hidden = true;
  toolPopup.hidden = true;
  brushPopup.hidden = true;

  toolToggle.setAttribute("aria-expanded", "false");
  brushToggle.setAttribute("aria-expanded", "false");

  openPopup = null;
}

function showPopup(type) {
  hideAllPopups();

  popupBackdrop.hidden = false;

  if (type === "tools") {
    toolPopup.hidden = false;
    toolToggle.setAttribute("aria-expanded", "true");
    openPopup = "tools";
    return;
  }

  if (type === "brush") {
    brushPopup.hidden = false;
    brushToggle.setAttribute("aria-expanded", "true");
    openPopup = "brush";
  }
}

function togglePopup(type) {
  if (openPopup === type) {
    hideAllPopups();
    return;
  }
  showPopup(type);
}

undoBtn.addEventListener("click", () => drawer.undo());

submitBtn.addEventListener("click", () => {
  const payload = createSubmissionPayload({
    drawer,
    championNameInput: championNameInput ? championNameInput.value : "",
    fallbackName
  });

  dispatchSubmission(payload);
  console.log("[PhoneDrawingSubmit] submitted", payload.championName);
});

toolToggle.addEventListener("click", () => togglePopup("tools"));
brushToggle.addEventListener("click", () => togglePopup("brush"));
popupBackdrop.addEventListener("click", hideAllPopups);

document.querySelectorAll(".popup-tool").forEach((btn) => {
  btn.addEventListener("click", () => {
    const toolName = btn.getAttribute("data-tool") || "pencil";
    const icon = btn.getAttribute("data-icon") || "✏";
    setTool(toolName, icon);
    hideAllPopups();
  });
});

document.querySelectorAll(".brush-choice").forEach((btn) => {
  btn.addEventListener("click", () => {
    const value = btn.getAttribute("data-size") || "0.1";
    brushSlider.value = value;
    updateBrushUiFromSlider();
    hideAllPopups();
  });
});

brushSlider.addEventListener("input", updateBrushUiFromSlider);

document.querySelectorAll(".inline-color").forEach((btn) => {
  btn.addEventListener("click", () => {
    const hex = btn.getAttribute("data-color") || "#000000";
    setColor(hex);
  });
});

window.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    hideAllPopups();
  }
});

setTool("pencil", "✏");
setColor("#000000");
updateBrushUiFromSlider();
hideAllPopups();

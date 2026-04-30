import { createDrawer } from "../core/drawingEngine.js";

function requiredElement(id) {
  const element = document.getElementById(id);
  if (!element) {
    throw new Error("[MobileDrawingScreen] Missing required element: #" + id);
  }
  return element;
}

export function initMobileDrawingScreen(options = {}) {
  const {
    textInputId = "championName",
    textureWidth = 600,
    textureHeight = 820,
    initialBrushSize = 5,
    defaultTool = "pencil",
    defaultToolIcon = "✏",
    defaultColor = "#000000"
  } = options;

  const canvas = requiredElement("drawCanvas");
  const undoBtn = requiredElement("undoBtn");
  const submitBtn = requiredElement("submitBtn");
  const toolToggle = requiredElement("toolToggle");
  const brushToggle = requiredElement("brushToggle");
  const toolCurrentIcon = requiredElement("toolCurrentIcon");
  const activeColorPreview = requiredElement("activeColorPreview");
  const popupBackdrop = requiredElement("popupBackdrop");
  const toolPopup = requiredElement("toolPopup");
  const brushPopup = requiredElement("brushPopup");
  const brushSlider = requiredElement("brushSize");
  const textInput = requiredElement(textInputId);

  const drawer = createDrawer({
    canvas,
    textureWidth,
    textureHeight,
    brushSize: initialBrushSize,
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
  toolToggle.addEventListener("click", () => togglePopup("tools"));
  brushToggle.addEventListener("click", () => togglePopup("brush"));
  popupBackdrop.addEventListener("click", hideAllPopups);

  document.querySelectorAll(".popup-tool").forEach((btn) => {
    btn.addEventListener("click", () => {
      const toolName = btn.getAttribute("data-tool") || defaultTool;
      const icon = btn.getAttribute("data-icon") || defaultToolIcon;
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
      const hex = btn.getAttribute("data-color") || defaultColor;
      setColor(hex);
    });
  });

  window.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      hideAllPopups();
    }
  });

  setTool(defaultTool, defaultToolIcon);
  setColor(defaultColor);
  updateBrushUiFromSlider();
  hideAllPopups();

  return {
    drawer,
    textInput,
    submitBtn
  };
}

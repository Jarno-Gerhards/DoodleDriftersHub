export class DrawingUIController {
  constructor(options) {
    const {
      drawer,
      toolButtons = [],
      colorButtons = [],
      brushSizeSlider = null,
      brushSizeLabel = null,
      undoButton = null,
      clearButton = null,
      activeColorPreview = null,
      selectedButtonClass = "selected"
    } = options || {};

    if (!drawer) {
      throw new Error("[DrawingUIController] drawer is required");
    }

    this.drawer = drawer;
    this.toolButtons = Array.from(toolButtons);
    this.colorButtons = Array.from(colorButtons);
    this.brushSizeSlider = brushSizeSlider;
    this.brushSizeLabel = brushSizeLabel;
    this.undoButton = undoButton;
    this.clearButton = clearButton;
    this.activeColorPreview = activeColorPreview;
    this.selectedButtonClass = selectedButtonClass;
  }

  init() {
    this.bindToolButtons();
    this.bindColorButtons();
    this.bindBrushSlider();
    this.bindCanvasActions();

    this.selectTool("pencil");
    this.selectColor("#000000");
  }

  bindToolButtons() {
    this.toolButtons.forEach((btn) => {
      const tool = btn.dataset.tool;
      btn.addEventListener("click", () => {
        this.selectTool(tool);
      });
    });
  }

  bindColorButtons() {
    this.colorButtons.forEach((btn) => {
      const color = btn.dataset.color;
      btn.addEventListener("click", () => {
        this.selectColor(color);
      });
    });
  }

  bindBrushSlider() {
    if (!this.brushSizeSlider) {
      return;
    }

    this.brushSizeSlider.min = "0";
    this.brushSizeSlider.max = "1";
    this.brushSizeSlider.step = "0.01";
    if (!this.brushSizeSlider.value) {
      this.brushSizeSlider.value = "0.1";
    }

    const onInput = () => {
      const value = Number(this.brushSizeSlider.value);
      this.drawer.setBrushSizeNormalized(value);
      const size = Math.round(1 + value * 39);

      if (this.brushSizeLabel) {
        this.brushSizeLabel.textContent = String(size);
      }
    };

    this.brushSizeSlider.addEventListener("input", onInput);
    onInput();
  }

  bindCanvasActions() {
    if (this.undoButton) {
      this.undoButton.addEventListener("click", () => this.drawer.undo());
    }
    if (this.clearButton) {
      this.clearButton.addEventListener("click", () => this.drawer.clearCanvas());
    }
  }

  selectTool(toolName) {
    this.drawer.setTool(toolName);

    this.toolButtons.forEach((btn) => {
      const isSelected = btn.dataset.tool === toolName;
      btn.classList.toggle(this.selectedButtonClass, isSelected);
    });
  }

  selectColor(color) {
    this.drawer.setColor(color);
    if (this.activeColorPreview) {
      this.activeColorPreview.style.background = color;
    }
  }
}

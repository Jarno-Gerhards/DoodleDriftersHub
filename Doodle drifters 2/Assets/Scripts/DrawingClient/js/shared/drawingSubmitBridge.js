export class DrawingSubmitBridge {
  constructor(options) {
    const {
      drawer,
      championNameInput,
      submitButton,
      fallbackName = "Unnamed Champion",
      onSubmit = null
    } = options || {};

    if (!drawer) {
      throw new Error("[DrawingSubmitBridge] drawer is required");
    }

    this.drawer = drawer;
    this.championNameInput = championNameInput || null;
    this.submitButton = submitButton || null;
    this.fallbackName = fallbackName;
    this.onSubmitCallback = onSubmit;
  }

  init() {
    if (!this.submitButton) {
      return;
    }

    this.submitButton.addEventListener("click", () => {
      const payload = this.onSubmit();
      if (payload) {
        console.log("[DrawingSubmitBridge] Submitted:", payload.championName);
      }
    });
  }

  onSubmit() {
    const rawName = this.championNameInput ? this.championNameInput.value : "";
    const championName = rawName && rawName.trim().length > 0
      ? rawName.trim()
      : this.fallbackName;

    const imageDataUrl = this.drawer.getTextureWithTransparentBackground();

    const payload = {
      championName,
      imageDataUrl,
      submittedAt: new Date().toISOString()
    };

    if (typeof this.onSubmitCallback === "function") {
      this.onSubmitCallback(payload);
    } else {
      window.dispatchEvent(new CustomEvent("drawing:submitted", { detail: payload }));
    }

    return payload;
  }
}

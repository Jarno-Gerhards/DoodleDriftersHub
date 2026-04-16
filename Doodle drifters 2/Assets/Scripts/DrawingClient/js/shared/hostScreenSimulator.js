export class HostScreenSimulator {
  constructor(options) {
    const {
      container,
      maxPortraits = 8
    } = options || {};

    if (!container) {
      throw new Error("[HostScreenSimulator] container is required");
    }

    this.container = container;
    this.maxPortraits = maxPortraits;
    this.count = 0;
  }

  addDrawing(imageDataUrl, playerName = "Player") {
    if (this.count >= this.maxPortraits) {
      console.warn("[HostScreenSimulator] Max submissions reached:", this.maxPortraits);
      return false;
    }

    const figure = document.createElement("figure");
    figure.className = "host-portrait";

    const img = document.createElement("img");
    img.src = imageDataUrl;
    img.alt = "Champion drawing of " + playerName;

    const caption = document.createElement("figcaption");
    caption.textContent = playerName;

    figure.appendChild(img);
    figure.appendChild(caption);
    this.container.appendChild(figure);

    this.count += 1;
    return true;
  }

  clearPortraits() {
    this.container.innerHTML = "";
    this.count = 0;
  }
}

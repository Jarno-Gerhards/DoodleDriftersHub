const TOOL_ENUM = {
  pencil: 0,
  eraser: 1,
  fill: 2,
  line: 3,
  rectangle: 4,
  circle: 5
};

export class DoodleDrawerPro {
  constructor(options) {
    const {
      canvas,
      textureWidth = 640,
      textureHeight = 420,
      brushSize = 5,
      maxUndoSteps = 20
    } = options || {};

    if (!canvas) {
      throw new Error("[DoodleDrawerPro] canvas is required");
    }

    this.canvas = canvas;
    this.ctx = canvas.getContext("2d", { willReadFrequently: true });
    this.textureWidth = textureWidth;
    this.textureHeight = textureHeight;
    this.brushSize = Math.max(1, Math.min(40, brushSize));
    this.maxUndoSteps = maxUndoSteps;

    this.canvas.width = this.textureWidth;
    this.canvas.height = this.textureHeight;

    this.currentColor = this.hexToColor32("#000000");
    this.currentTool = TOOL_ENUM.pencil;

    this.strokeStarted = false;
    this.shapeBaseSnapshot = null;
    this.shapeStartX = 0;
    this.shapeStartY = 0;

    this.isPointerDown = false;
    this.activePointerId = null;

    this.undoStack = [];

    this.pixelBuffer = new Uint8ClampedArray(this.textureWidth * this.textureHeight * 4);
    this.fillWhite();
    this.bindPointerEvents();
  }

  setTool(tool) {
    if (typeof tool === "number") {
      this.currentTool = Math.max(0, Math.min(5, tool));
      return;
    }

    const key = String(tool || "").toLowerCase();
    if (TOOL_ENUM[key] === undefined) {
      throw new Error("[DoodleDrawerPro] invalid tool: " + tool);
    }
    this.currentTool = TOOL_ENUM[key];
  }

  setToolByIndex(index) {
    this.setTool(index);
  }

  setColor(color) {
    this.currentColor = this.parseColor(color);
  }

  setBrushSize(size) {
    this.brushSize = Math.max(1, Math.min(40, Math.round(size)));
  }

  setBrushSizeNormalized(t) {
    const clamped = Math.max(0, Math.min(1, Number(t) || 0));
    this.brushSize = Math.round(1 + clamped * 39);
  }

  undo() {
    if (this.undoStack.length === 0) {
      return;
    }
    this.pixelBuffer = this.undoStack.pop();
    this.commitBuffer();
  }

  clearCanvas() {
    this.fillWhite();
    this.undoStack = [];
  }

  getCanvas() {
    return this.canvas;
  }

  getDataURL() {
    return this.canvas.toDataURL("image/png");
  }

  getTextureWithTransparentBackground(whiteThreshold = 245) {
    const tmp = document.createElement("canvas");
    tmp.width = this.textureWidth;
    tmp.height = this.textureHeight;
    const tmpCtx = tmp.getContext("2d", { willReadFrequently: true });

    const clone = new Uint8ClampedArray(this.pixelBuffer);
    for (let i = 0; i < clone.length; i += 4) {
      const r = clone[i];
      const g = clone[i + 1];
      const b = clone[i + 2];

      const isWhiteLike = r >= whiteThreshold && g >= whiteThreshold && b >= whiteThreshold;
      clone[i + 3] = isWhiteLike ? 0 : 255;
    }

    tmpCtx.putImageData(new ImageData(clone, this.textureWidth, this.textureHeight), 0, 0);
    return tmp.toDataURL("image/png");
  }

  bindPointerEvents() {
    this.canvas.addEventListener("pointerdown", (e) => this.handlePointerDown(e));
    this.canvas.addEventListener("pointermove", (e) => this.handlePointerMove(e));
    this.canvas.addEventListener("pointerup", (e) => this.handlePointerUp(e));
    this.canvas.addEventListener("pointercancel", (e) => this.handlePointerUp(e));
    this.canvas.addEventListener("pointerleave", (e) => {
      if (!this.isPointerDown) {
        this.strokeStarted = false;
        this.shapeBaseSnapshot = null;
      }
    });
  }

  handlePointerDown(e) {
    e.preventDefault();

    this.isPointerDown = true;
    this.activePointerId = e.pointerId;
    this.canvas.setPointerCapture(e.pointerId);

    const { x, y } = this.eventToPixel(e);
    this.tryDrawAt(x, y, true);
  }

  handlePointerMove(e) {
    if (!this.isPointerDown || e.pointerId !== this.activePointerId) {
      return;
    }
    e.preventDefault();

    const { x, y } = this.eventToPixel(e);
    this.tryDrawAt(x, y, false);
  }

  handlePointerUp(e) {
    if (e.pointerId !== this.activePointerId) {
      return;
    }
    this.isPointerDown = false;
    this.activePointerId = null;
    this.strokeStarted = false;
    this.shapeBaseSnapshot = null;
  }

  tryDrawAt(px, py, isStartEvent) {
    if (!this.strokeStarted || isStartEvent) {
      this.pushUndo();
      this.strokeStarted = true;

      if (this.currentTool === TOOL_ENUM.fill) {
        this.floodFill(px, py, this.currentColor);
        this.strokeStarted = false;
        return;
      }

      if (this.isShapeTool(this.currentTool)) {
        this.shapeBaseSnapshot = new Uint8ClampedArray(this.pixelBuffer);
        this.shapeStartX = px;
        this.shapeStartY = py;
        return;
      }
    }

    if (this.currentTool === TOOL_ENUM.pencil) {
      this.drawCircleOnBuffer(this.pixelBuffer, px, py, this.brushSize, this.currentColor);
      this.commitBuffer();
      return;
    }

    if (this.currentTool === TOOL_ENUM.eraser) {
      this.drawCircleOnBuffer(this.pixelBuffer, px, py, this.brushSize, [255, 255, 255, 255]);
      this.commitBuffer();
      return;
    }

    if (this.isShapeTool(this.currentTool) && this.shapeBaseSnapshot) {
      this.drawShapePreview(px, py);
    }
  }

  drawShapePreview(endX, endY) {
    this.pixelBuffer.set(this.shapeBaseSnapshot);

    if (this.currentTool === TOOL_ENUM.line) {
      this.drawLineOnBuffer(
        this.pixelBuffer,
        this.shapeStartX,
        this.shapeStartY,
        endX,
        endY,
        this.brushSize,
        this.currentColor
      );
    } else if (this.currentTool === TOOL_ENUM.rectangle) {
      this.drawRectangleOnBuffer(
        this.pixelBuffer,
        this.shapeStartX,
        this.shapeStartY,
        endX,
        endY,
        this.brushSize,
        this.currentColor
      );
    } else if (this.currentTool === TOOL_ENUM.circle) {
      this.drawEllipseOnBuffer(
        this.pixelBuffer,
        this.shapeStartX,
        this.shapeStartY,
        endX,
        endY,
        this.brushSize,
        this.currentColor
      );
    }

    this.commitBuffer();
  }

  commitBuffer() {
    const imageData = new ImageData(this.pixelBuffer, this.textureWidth, this.textureHeight);
    this.ctx.putImageData(imageData, 0, 0);
  }

  setPixelInBuffer(buf, x, y, color32) {
    if (x < 0 || x >= this.textureWidth || y < 0 || y >= this.textureHeight) {
      return;
    }

    const idx = (y * this.textureWidth + x) * 4;
    buf[idx] = color32[0];
    buf[idx + 1] = color32[1];
    buf[idx + 2] = color32[2];
    buf[idx + 3] = color32[3];
  }

  drawCircleOnBuffer(buf, cx, cy, radius, color32) {
    const r2 = radius * radius;
    for (let dy = -radius; dy <= radius; dy++) {
      for (let dx = -radius; dx <= radius; dx++) {
        if (dx * dx + dy * dy > r2) {
          continue;
        }
        this.setPixelInBuffer(buf, cx + dx, cy + dy, color32);
      }
    }
  }

  paintThickOnBuffer(buf, cx, cy, half, color32) {
    for (let dy = -half; dy <= half; dy++) {
      for (let dx = -half; dx <= half; dx++) {
        this.setPixelInBuffer(buf, cx + dx, cy + dy, color32);
      }
    }
  }

  drawLineOnBuffer(buf, x0, y0, x1, y1, thickness, color32) {
    let dx = Math.abs(x1 - x0);
    const sx = x0 < x1 ? 1 : -1;
    let dy = Math.abs(y1 - y0);
    const sy = y0 < y1 ? 1 : -1;
    let err = dx - dy;
    const half = Math.max(1, Math.floor(thickness / 2));

    while (true) {
      this.paintThickOnBuffer(buf, x0, y0, half, color32);
      if (x0 === x1 && y0 === y1) {
        break;
      }

      const e2 = 2 * err;
      if (e2 > -dy) {
        err -= dy;
        x0 += sx;
      }
      if (e2 < dx) {
        err += dx;
        y0 += sy;
      }
    }
  }

  drawRectangleOnBuffer(buf, x0, y0, x1, y1, thickness, color32) {
    const minX = Math.min(x0, x1);
    const maxX = Math.max(x0, x1);
    const minY = Math.min(y0, y1);
    const maxY = Math.max(y0, y1);

    this.drawLineOnBuffer(buf, minX, minY, maxX, minY, thickness, color32);
    this.drawLineOnBuffer(buf, minX, maxY, maxX, maxY, thickness, color32);
    this.drawLineOnBuffer(buf, minX, minY, minX, maxY, thickness, color32);
    this.drawLineOnBuffer(buf, maxX, minY, maxX, maxY, thickness, color32);
  }

  drawEllipseOnBuffer(buf, x0, y0, x1, y1, thickness, color32) {
    const cx = Math.floor((x0 + x1) / 2);
    const cy = Math.floor((y0 + y1) / 2);
    const rx = Math.floor(Math.abs(x1 - x0) / 2);
    const ry = Math.floor(Math.abs(y1 - y0) / 2);

    if (rx === 0 && ry === 0) {
      this.paintThickOnBuffer(buf, cx, cy, thickness, color32);
      return;
    }
    if (rx === 0) {
      this.drawLineOnBuffer(buf, cx, cy - ry, cx, cy + ry, thickness, color32);
      return;
    }
    if (ry === 0) {
      this.drawLineOnBuffer(buf, cx - rx, cy, cx + rx, cy, thickness, color32);
      return;
    }

    const half = Math.max(1, Math.floor(thickness / 2));

    const plot = (ex, ey) => {
      this.paintThickOnBuffer(buf, cx + ex, cy + ey, half, color32);
      this.paintThickOnBuffer(buf, cx - ex, cy + ey, half, color32);
      this.paintThickOnBuffer(buf, cx + ex, cy - ey, half, color32);
      this.paintThickOnBuffer(buf, cx - ex, cy - ey, half, color32);
    };

    const rx2 = rx * rx;
    const ry2 = ry * ry;
    let x = 0;
    let y = ry;
    let px = 0;
    let py = 2 * rx2 * y;

    let p = Math.round(ry2 - rx2 * ry + 0.25 * rx2);
    while (px < py) {
      plot(x, y);
      x += 1;
      px += 2 * ry2;
      if (p < 0) {
        p += ry2 + px;
      } else {
        y -= 1;
        py -= 2 * rx2;
        p += ry2 + px - py;
      }
    }

    p = Math.round(ry2 * (x + 0.5) * (x + 0.5) + rx2 * (y - 1) * (y - 1) - rx2 * ry2);
    while (y >= 0) {
      plot(x, y);
      y -= 1;
      py -= 2 * rx2;
      if (p > 0) {
        p += rx2 - py;
      } else {
        x += 1;
        px += 2 * ry2;
        p += rx2 - py + px;
      }
    }
  }

  floodFill(startX, startY, fillColor32) {
    const target = this.getPixelColor32(this.pixelBuffer, startX, startY);
    if (this.colorEquals(target, fillColor32)) {
      return;
    }

    const queue = [startY * this.textureWidth + startX];
    let head = 0;

    while (head < queue.length) {
      const idx = queue[head++];
      const pixelIndex = idx * 4;

      const current = [
        this.pixelBuffer[pixelIndex],
        this.pixelBuffer[pixelIndex + 1],
        this.pixelBuffer[pixelIndex + 2],
        this.pixelBuffer[pixelIndex + 3]
      ];

      if (!this.colorEquals(current, target)) {
        continue;
      }

      this.pixelBuffer[pixelIndex] = fillColor32[0];
      this.pixelBuffer[pixelIndex + 1] = fillColor32[1];
      this.pixelBuffer[pixelIndex + 2] = fillColor32[2];
      this.pixelBuffer[pixelIndex + 3] = fillColor32[3];

      const x = idx % this.textureWidth;
      const y = Math.floor(idx / this.textureWidth);

      if (x > 0) queue.push(idx - 1);
      if (x < this.textureWidth - 1) queue.push(idx + 1);
      if (y > 0) queue.push(idx - this.textureWidth);
      if (y < this.textureHeight - 1) queue.push(idx + this.textureWidth);
    }

    this.commitBuffer();
  }

  getPixelColor32(buf, x, y) {
    const idx = (y * this.textureWidth + x) * 4;
    return [buf[idx], buf[idx + 1], buf[idx + 2], buf[idx + 3]];
  }

  colorEquals(a, b) {
    return a[0] === b[0] && a[1] === b[1] && a[2] === b[2] && a[3] === b[3];
  }

  pushUndo() {
    if (this.undoStack.length >= this.maxUndoSteps) {
      this.undoStack.shift();
    }
    this.undoStack.push(new Uint8ClampedArray(this.pixelBuffer));
  }

  isShapeTool(tool) {
    return tool === TOOL_ENUM.line || tool === TOOL_ENUM.rectangle || tool === TOOL_ENUM.circle;
  }

  fillWhite() {
    for (let i = 0; i < this.pixelBuffer.length; i += 4) {
      this.pixelBuffer[i] = 255;
      this.pixelBuffer[i + 1] = 255;
      this.pixelBuffer[i + 2] = 255;
      this.pixelBuffer[i + 3] = 255;
    }
    this.commitBuffer();
  }

  eventToPixel(e) {
    const rect = this.canvas.getBoundingClientRect();
    const nx = (e.clientX - rect.left) / rect.width;
    const ny = (e.clientY - rect.top) / rect.height;

    const x = Math.max(0, Math.min(this.textureWidth - 1, Math.round(nx * this.textureWidth)));
    const y = Math.max(0, Math.min(this.textureHeight - 1, Math.round(ny * this.textureHeight)));
    return { x, y };
  }

  parseColor(color) {
    if (Array.isArray(color)) {
      return [
        this.clampByte(color[0]),
        this.clampByte(color[1]),
        this.clampByte(color[2]),
        this.clampByte(color.length > 3 ? color[3] : 255)
      ];
    }

    if (typeof color === "string") {
      return this.hexToColor32(color);
    }

    return [0, 0, 0, 255];
  }

  hexToColor32(hex) {
    const normalized = String(hex || "#000000").trim();
    const value = normalized.startsWith("#") ? normalized.slice(1) : normalized;

    if (value.length === 3) {
      const r = parseInt(value[0] + value[0], 16);
      const g = parseInt(value[1] + value[1], 16);
      const b = parseInt(value[2] + value[2], 16);
      return [r, g, b, 255];
    }

    if (value.length === 6 || value.length === 8) {
      const r = parseInt(value.slice(0, 2), 16);
      const g = parseInt(value.slice(2, 4), 16);
      const b = parseInt(value.slice(4, 6), 16);
      const a = value.length === 8 ? parseInt(value.slice(6, 8), 16) : 255;
      return [r, g, b, a];
    }

    return [0, 0, 0, 255];
  }

  clampByte(n) {
    return Math.max(0, Math.min(255, Number(n) || 0));
  }
}

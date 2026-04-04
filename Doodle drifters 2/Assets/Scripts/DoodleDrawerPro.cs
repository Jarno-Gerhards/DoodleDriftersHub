using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Advanced drawing component for the "Draw Your Champion" client screen.
///
/// Performance: alle tekenoperaties werken op een Color32[] buffer in geheugen.
/// SetPixels32 + Apply wordt maar één keer per frame aangeroepen — geen losse SetPixel calls.
///
/// Tools:
///   - Pencil    : vrijhand tekenen
///   - Eraser    : vrijhand wissen
///   - Fill      : flood-fill
///   - Line      : rechte lijn (live preview)
///   - Rectangle : rechthoek outline (live preview)
///   - Circle    : ellipse outline (live preview)
/// </summary>
public class DoodleDrawerPro : MonoBehaviour
{
    [Header("Canvas")]
    public RawImage drawArea;
    public int textureSize = 512;

    [Header("Brush")]
    [Range(1, 40)] public int brushSize = 5;

    [Header("Undo")]
    public int maxUndoSteps = 20;

    // ── Tool enum ─────────────────────────────────────────────────────────────

    public enum DrawTool { Pencil, Eraser, Fill, Line, Rectangle, Circle }

    // ── Runtime state ─────────────────────────────────────────────────────────

    private Texture2D drawTexture;
    private Color32[] pixelBuffer;        // live buffer — altijd in sync met drawTexture
    private Color     currentColor  = Color.black;
    private DrawTool  currentTool   = DrawTool.Pencil;
    private bool      strokeStarted = false;

    // Shape tools
    private Color32[] shapeBaseSnapshot;  // snapshot van canvas bij begin van sleep
    private int       shapeStartX, shapeStartY;

    private readonly Stack<Color32[]> undoStack = new Stack<Color32[]>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        drawTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        pixelBuffer = new Color32[textureSize * textureSize];
        FillWhite();
        drawArea.texture = drawTexture;
    }

    private void Update()
    {
        if (Pointer.current == null) return;

        bool pointerDown = Pointer.current.press.isPressed;

        if (pointerDown)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            if (IsBlockedByOtherUI(screenPos)) { strokeStarted = false; return; }

            Vector2 localPos;
            if (!GetLocalPos(out localPos)) { strokeStarted = false; return; }

            Rect rect = drawArea.rectTransform.rect;
            int px = Mathf.Clamp(LocalToPixel(localPos.x, rect.xMin, rect.xMax), 0, textureSize - 1);
            int py = Mathf.Clamp(LocalToPixel(localPos.y, rect.yMin, rect.yMax), 0, textureSize - 1);

            if (!strokeStarted)
            {
                PushUndo();
                strokeStarted = true;

                if (currentTool == DrawTool.Fill)
                {
                    FloodFill(px, py, currentColor);
                    strokeStarted = false;
                    return;
                }

                if (IsShapeTool(currentTool))
                {
                    shapeBaseSnapshot = (Color32[])pixelBuffer.Clone();
                    shapeStartX = px;
                    shapeStartY = py;
                    return;
                }
            }

            if (currentTool == DrawTool.Pencil)
            {
                DrawCircleOnBuffer(pixelBuffer, px, py, brushSize, currentColor);
                CommitBuffer();
            }
            else if (currentTool == DrawTool.Eraser)
            {
                DrawCircleOnBuffer(pixelBuffer, px, py, brushSize, Color.white);
                CommitBuffer();
            }
            else if (IsShapeTool(currentTool) && shapeBaseSnapshot != null)
            {
                DrawShapePreview(px, py);
            }
        }
        else
        {
            strokeStarted     = false;
            shapeBaseSnapshot = null;
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void SetTool(DrawTool tool) => currentTool = tool;

    /// <summary>0=Pencil 1=Eraser 2=Fill 3=Line 4=Rectangle 5=Circle</summary>
    public void SetTool(int toolIndex) => currentTool = (DrawTool)toolIndex;

    public void SetColor(Color color) => currentColor = color;

    public void SetBrushSize(int size) => brushSize = Mathf.Clamp(size, 1, 40);

    public void SetBrushSizeNormalized(float t)
        => brushSize = Mathf.RoundToInt(Mathf.Lerp(1f, 40f, Mathf.Clamp01(t)));

    public void Undo()
    {
        if (undoStack.Count == 0) { Debug.Log("[DoodleDrawerPro] Nothing to undo."); return; }
        pixelBuffer = undoStack.Pop();
        CommitBuffer();
    }

    public void ClearCanvas()
    {
        if (drawTexture == null) return;
        FillWhite();
        undoStack.Clear();
    }

    public Texture2D GetTexture() => drawTexture;

    public Texture2D GetTextureWithTransparentBackground(byte whiteThreshold = 245)
    {
        if (drawTexture == null) return null;

        Texture2D clone = new Texture2D(drawTexture.width, drawTexture.height, TextureFormat.RGBA32, false);
        clone.filterMode = drawTexture.filterMode;
        clone.wrapMode   = drawTexture.wrapMode;

        Color32[] pixels = (Color32[])pixelBuffer.Clone();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 p = pixels[i];
            bool isWhiteLike = p.r >= whiteThreshold && p.g >= whiteThreshold && p.b >= whiteThreshold;
            p.a      = isWhiteLike ? (byte)0 : (byte)255;
            pixels[i] = p;
        }

        clone.SetPixels32(pixels);
        clone.Apply();
        return clone;
    }

    // =========================================================================
    // Shape preview — herstel snapshot + teken shape op tijdelijke buffer
    // =========================================================================

    private void DrawShapePreview(int endX, int endY)
    {
        // Kopieer de snapshot naar de live buffer
        System.Array.Copy(shapeBaseSnapshot, pixelBuffer, pixelBuffer.Length);

        switch (currentTool)
        {
            case DrawTool.Line:
                DrawLineOnBuffer(pixelBuffer, shapeStartX, shapeStartY, endX, endY, brushSize, currentColor);
                break;
            case DrawTool.Rectangle:
                DrawRectangleOnBuffer(pixelBuffer, shapeStartX, shapeStartY, endX, endY, brushSize, currentColor);
                break;
            case DrawTool.Circle:
                DrawEllipseOnBuffer(pixelBuffer, shapeStartX, shapeStartY, endX, endY, brushSize, currentColor);
                break;
        }

        CommitBuffer();
    }

    // =========================================================================
    // Buffer helpers
    // =========================================================================

    /// <summary>Schrijft pixelBuffer naar de texture — één keer per tekenoperatie.</summary>
    private void CommitBuffer()
    {
        drawTexture.SetPixels32(pixelBuffer);
        drawTexture.Apply();
    }

    /// <summary>Zet een pixel in de buffer (geen texture write).</summary>
    private void SetPixelInBuffer(Color32[] buf, int x, int y, Color32 color)
    {
        if (x < 0 || x >= textureSize || y < 0 || y >= textureSize) return;
        buf[y * textureSize + x] = color;
    }

    // =========================================================================
    // Drawing primitives — allemaal op Color32[] buffer, geen SetPixel
    // =========================================================================

    private void DrawCircleOnBuffer(Color32[] buf, int cx, int cy, int radius, Color color)
    {
        Color32 c32 = color;
        int r2 = radius * radius;
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            if (dx * dx + dy * dy > r2) continue;
            SetPixelInBuffer(buf, cx + dx, cy + dy, c32);
        }
    }

    private void DrawLineOnBuffer(Color32[] buf, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        Color32 c32  = color;
        int dx  = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy  = Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int half = Mathf.Max(1, thickness / 2);

        while (true)
        {
            PaintThickOnBuffer(buf, x0, y0, half, c32);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 <  dx) { err += dx; y0 += sy; }
        }
    }

    private void DrawRectangleOnBuffer(Color32[] buf, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        int minX = Mathf.Min(x0, x1), maxX = Mathf.Max(x0, x1);
        int minY = Mathf.Min(y0, y1), maxY = Mathf.Max(y0, y1);

        DrawLineOnBuffer(buf, minX, minY, maxX, minY, thickness, color); // onder
        DrawLineOnBuffer(buf, minX, maxY, maxX, maxY, thickness, color); // boven
        DrawLineOnBuffer(buf, minX, minY, minX, maxY, thickness, color); // links
        DrawLineOnBuffer(buf, maxX, minY, maxX, maxY, thickness, color); // rechts
    }

    private void DrawEllipseOnBuffer(Color32[] buf, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        Color32 c32 = color;
        int cx = (x0 + x1) / 2;
        int cy = (y0 + y1) / 2;
        int rx = Mathf.Abs(x1 - x0) / 2;
        int ry = Mathf.Abs(y1 - y0) / 2;

        if (rx == 0 && ry == 0) { PaintThickOnBuffer(buf, cx, cy, thickness, c32); return; }
        if (rx == 0) { DrawLineOnBuffer(buf, cx, cy - ry, cx, cy + ry, thickness, color); return; }
        if (ry == 0) { DrawLineOnBuffer(buf, cx - rx, cy, cx + rx, cy, thickness, color); return; }

        int half = Mathf.Max(1, thickness / 2);

        void Plot(long ex, long ey)
        {
            PaintThickOnBuffer(buf, cx + (int)ex, cy + (int)ey, half, c32);
            PaintThickOnBuffer(buf, cx - (int)ex, cy + (int)ey, half, c32);
            PaintThickOnBuffer(buf, cx + (int)ex, cy - (int)ey, half, c32);
            PaintThickOnBuffer(buf, cx - (int)ex, cy - (int)ey, half, c32);
        }

        long rx2 = (long)rx * rx;
        long ry2 = (long)ry * ry;
        long x = 0, y = ry;
        long px = 0, py = 2 * rx2 * y;

        // Regio 1
        long p = (long)(ry2 - rx2 * ry + 0.25 * rx2);
        while (px < py)
        {
            Plot(x, y);
            x++; px += 2 * ry2;
            if (p < 0) p += ry2 + px;
            else { y--; py -= 2 * rx2; p += ry2 + px - py; }
        }

        // Regio 2
        p = (long)(ry2 * (x + 0.5) * (x + 0.5) + rx2 * (y - 1) * (y - 1) - rx2 * ry2);
        while (y >= 0)
        {
            Plot(x, y);
            y--; py -= 2 * rx2;
            if (p > 0) p += rx2 - py;
            else { x++; px += 2 * ry2; p += rx2 - py + px; }
        }
    }

    /// <summary>Schildert een dik punt op de buffer — geen texture writes.</summary>
    private void PaintThickOnBuffer(Color32[] buf, int cx, int cy, int half, Color32 color)
    {
        for (int dy = -half; dy <= half; dy++)
        for (int dx = -half; dx <= half; dx++)
            SetPixelInBuffer(buf, cx + dx, cy + dy, color);
    }

    // =========================================================================
    // Fill white
    // =========================================================================

    private void FillWhite()
    {
        Color32 white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < pixelBuffer.Length; i++)
            pixelBuffer[i] = white;
        CommitBuffer();
    }

    // =========================================================================
    // Flood fill
    // =========================================================================

    private void FloodFill(int startX, int startY, Color fillColor)
    {
        Color32 fill32 = fillColor;
        Color32 target = pixelBuffer[startY * textureSize + startX];
        if (ColorEquals(target, fill32)) return;

        Queue<int> queue = new Queue<int>();
        queue.Enqueue(startY * textureSize + startX);

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            if (!ColorEquals(pixelBuffer[idx], target)) continue;
            pixelBuffer[idx] = fill32;

            int x = idx % textureSize;
            int y = idx / textureSize;

            if (x > 0)             queue.Enqueue(idx - 1);
            if (x < textureSize-1) queue.Enqueue(idx + 1);
            if (y > 0)             queue.Enqueue(idx - textureSize);
            if (y < textureSize-1) queue.Enqueue(idx + textureSize);
        }

        CommitBuffer();
    }

    private static bool ColorEquals(Color32 a, Color32 b)
        => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

    // =========================================================================
    // Undo
    // =========================================================================

    private void PushUndo()
    {
        if (undoStack.Count >= maxUndoSteps)
        {
            var temp = new Stack<Color32[]>(undoStack);
            undoStack.Clear();
            bool droppedOldest = false;
            foreach (var item in temp)
            {
                if (!droppedOldest) { droppedOldest = true; continue; }
                undoStack.Push(item);
            }
        }
        undoStack.Push((Color32[])pixelBuffer.Clone());
    }

    // =========================================================================
    // Input helpers
    // =========================================================================

    private static bool IsShapeTool(DrawTool t)
        => t == DrawTool.Line || t == DrawTool.Rectangle || t == DrawTool.Circle;

    private bool GetLocalPos(out Vector2 localPos)
    {
        Vector2 screenPos = Pointer.current.position.ReadValue();
        Camera uiCam = null;

        var canvas = drawArea.canvas;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        if (!RectTransformUtility.RectangleContainsScreenPoint(drawArea.rectTransform, screenPos, uiCam))
        {
            localPos = default;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawArea.rectTransform, screenPos, uiCam, out localPos);
    }

    private bool IsBlockedByOtherUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, hits);

        if (hits.Count == 0) return false;
        var topHit = hits[0].gameObject;
        return topHit != null && topHit != drawArea.gameObject;
    }

    private int LocalToPixel(float local, float min, float max)
        => Mathf.RoundToInt(Mathf.InverseLerp(min, max, local) * textureSize);
}
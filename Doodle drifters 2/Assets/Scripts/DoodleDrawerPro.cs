using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Advanced drawing component for the "Draw Your Champion" client screen.
///
/// Features:
///   - Pencil, Eraser, Fill (flood-fill) tools
///   - Configurable brush / eraser size
///   - Color picker support (call SetColor from UI buttons)
///   - Multi-step Undo (configurable history depth)
///   - ClearCanvas()
///   - GetTexture() — compatible with DoodleInference
/// </summary>
public class DoodleDrawerPro : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Canvas")]
    public RawImage drawArea;
    public int textureSize = 512;

    [Header("Brush")]
    [Range(1, 40)] public int brushSize = 5;

    [Header("Undo")]
    public int maxUndoSteps = 20;

    public enum DrawTool { Pencil, Eraser, Fill }

    private Texture2D drawTexture;
    private Color     currentColor  = Color.black;
    private DrawTool  currentTool   = DrawTool.Pencil;
    private bool      strokeStarted = false;

    private readonly Stack<Color32[]> undoStack = new Stack<Color32[]>();


    private void Start()
    {
        drawTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
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
            }

            if (currentTool == DrawTool.Pencil)
                DrawCircle(px, py, brushSize, currentColor);
            else if (currentTool == DrawTool.Eraser)
                DrawCircle(px, py, brushSize, Color.white);
        }
        else
        {
            strokeStarted = false;
        }
    }

    public void SetTool(DrawTool tool) => currentTool = tool;

    /// <summary>For UI buttons: 0 = Pencil, 1 = Eraser, 2 = Fill</summary>
    public void SetTool(int toolIndex) => currentTool = (DrawTool)toolIndex;

    public void SetColor(Color color) => currentColor = color;

    public void SetBrushSize(int size) => brushSize = Mathf.Clamp(size, 1, 40);

    /// <summary>Maps a 0-1 slider value to 1-40 px brush size.</summary>
    public void SetBrushSizeNormalized(float t)
    {
        brushSize = Mathf.RoundToInt(Mathf.Lerp(1f, 40f, Mathf.Clamp01(t)));
    }

    /// <summary>Undo the last stroke or fill. Called by the Undo button.</summary>
    public void Undo()
    {
        if (undoStack.Count == 0)
        {
            Debug.Log("[DoodleDrawerPro] Nothing to undo.");
            return;
        }

        Color32[] snapshot = undoStack.Pop();
        drawTexture.SetPixels32(snapshot);
        drawTexture.Apply();
        Debug.Log($"[DoodleDrawerPro] Undo applied. Steps remaining: {undoStack.Count}");
    }

    /// <summary>Wipes the canvas white and clears undo history.</summary>
    public void ClearCanvas()
    {
        if (drawTexture == null) return;
        FillWhite();
        undoStack.Clear();
    }

    public Texture2D GetTexture() => drawTexture;

    /// <summary>
    /// Returns a cloned texture where near-white pixels become transparent.
    /// Useful for submit/export while keeping the editor canvas white.
    /// </summary>
    public Texture2D GetTextureWithTransparentBackground(byte whiteThreshold = 245)
    {
        if (drawTexture == null) return null;

        Texture2D clone = new Texture2D(drawTexture.width, drawTexture.height, TextureFormat.RGBA32, false);
        clone.filterMode = drawTexture.filterMode;
        clone.wrapMode = drawTexture.wrapMode;

        Color32[] pixels = drawTexture.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 p = pixels[i];
            bool isWhiteLike = p.r >= whiteThreshold && p.g >= whiteThreshold && p.b >= whiteThreshold;
            p.a = isWhiteLike ? (byte)0 : (byte)255;
            pixels[i] = p;
        }

        clone.SetPixels32(pixels);
        clone.Apply();
        return clone;
    }

    private void DrawCircle(int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;

        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            if (dx * dx + dy * dy > r2) continue;
            int nx = cx + dx, ny = cy + dy;
            if (nx < 0 || nx >= textureSize || ny < 0 || ny >= textureSize) continue;
            drawTexture.SetPixel(nx, ny, color);
        }

        drawTexture.Apply();
    }

    private void FillWhite()
    {
        Color32[] white = new Color32[textureSize * textureSize];
        for (int i = 0; i < white.Length; i++)
            white[i] = new Color32(255, 255, 255, 255);
        drawTexture.SetPixels32(white);
        drawTexture.Apply();
    }

    private void FloodFill(int startX, int startY, Color fillColor)
    {
        Color32 fill32 = fillColor;
        Color32 target = drawTexture.GetPixel(startX, startY);

        if (ColorEquals(target, fill32)) return;

        Color32[]  pixels = drawTexture.GetPixels32();
        Queue<int> queue  = new Queue<int>();
        queue.Enqueue(startY * textureSize + startX);

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            if (!ColorEquals(pixels[idx], target)) continue;

            pixels[idx] = fill32;

            int x = idx % textureSize;
            int y = idx / textureSize;

            if (x > 0)             queue.Enqueue(idx - 1);
            if (x < textureSize-1) queue.Enqueue(idx + 1);
            if (y > 0)             queue.Enqueue(idx - textureSize);
            if (y < textureSize-1) queue.Enqueue(idx + textureSize);
        }

        drawTexture.SetPixels32(pixels);
        drawTexture.Apply();
    }

    private static bool ColorEquals(Color32 a, Color32 b)
        => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

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

        undoStack.Push(drawTexture.GetPixels32());
    }

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

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, hits);

        if (hits.Count == 0) return false;

        var topHit = hits[0].gameObject;
        return topHit != null && topHit != drawArea.gameObject;
    }

    private int LocalToPixel(float local, float min, float max)
        => Mathf.RoundToInt(Mathf.InverseLerp(min, max, local) * textureSize);
}
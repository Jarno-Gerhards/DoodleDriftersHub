using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Touch/mouse drawing canvas using Texture2D.
/// Equivalent to DrawingCanvas.js (Paper.js-based).
/// Attach to a RawImage UI element that will serve as the drawing surface.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class DrawingCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Settings")]
    public int TextureWidth = 512;
    public int TextureHeight = 512;
    public int BrushSize = 5;
    public float MinDistance = 3f;

    [Header("Colors")]
    public Color CurrentColor = Color.black;
    public Color BackgroundColor = new Color(0, 0, 0, 0); // Transparent

    [Header("Color Picker (optional)")]
    public GameObject ColorPickerPanel;
    public Button ColorPickerToggle;

    private Texture2D _texture;
    private RawImage _rawImage;
    private bool _interactable = true;
    private Vector2 _lastPoint;
    private bool _drawing;

    // Undo history: store a copy of the texture after each stroke
    private List<Color[]> _history = new List<Color[]>();
    private Color[] _currentStrokeStart;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        CreateTexture();
        Clear();

        if (ColorPickerToggle != null && ColorPickerPanel != null)
        {
            ColorPickerToggle.onClick.AddListener(() =>
                ColorPickerPanel.SetActive(!ColorPickerPanel.activeSelf));
        }
    }

    private void CreateTexture()
    {
        _texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
        _texture.filterMode = FilterMode.Bilinear;
        _rawImage.texture = _texture;
    }

    // ============================================================
    // Public API
    // ============================================================

    public void Clear()
    {
        Color[] pixels = new Color[TextureWidth * TextureHeight];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = BackgroundColor;
        _texture.SetPixels(pixels);
        _texture.Apply();
        _history.Clear();
    }

    public void Undo()
    {
        if (_history.Count > 0)
        {
            _history.RemoveAt(_history.Count - 1);

            if (_history.Count > 0)
            {
                _texture.SetPixels(_history[_history.Count - 1]);
            }
            else
            {
                // Restore to blank
                Color[] blank = new Color[TextureWidth * TextureHeight];
                for (int i = 0; i < blank.Length; i++)
                    blank[i] = BackgroundColor;
                _texture.SetPixels(blank);
            }
            _texture.Apply();
        }
    }

    public void SetColor(Color color)
    {
        CurrentColor = color;
    }

    public void SetInteractable(bool interactable)
    {
        _interactable = interactable;
    }

    /// <summary>
    /// Export the drawing as a base64-encoded PNG data URL.
    /// If addWhiteBackground is true, composites on white before exporting.
    /// </summary>
    public string ExportImage(bool addWhiteBackground = false)
    {
        Texture2D exportTex;

        if (addWhiteBackground)
        {
            exportTex = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            Color[] srcPixels = _texture.GetPixels();
            Color[] dstPixels = new Color[srcPixels.Length];

            for (int i = 0; i < srcPixels.Length; i++)
            {
                Color src = srcPixels[i];
                // Alpha-blend onto white
                dstPixels[i] = new Color(
                    src.r * src.a + 1f * (1f - src.a),
                    src.g * src.a + 1f * (1f - src.a),
                    src.b * src.a + 1f * (1f - src.a),
                    1f
                );
            }
            exportTex.SetPixels(dstPixels);
            exportTex.Apply();
        }
        else
        {
            exportTex = _texture;
        }

        byte[] pngData = exportTex.EncodeToPNG();

        if (addWhiteBackground)
            Destroy(exportTex);

        string base64 = System.Convert.ToBase64String(pngData);
        return "data:image/png;base64," + base64;
    }

    // ============================================================
    // Input Handling
    // ============================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_interactable) return;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rawImage.rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            return;

        Vector2 texCoord = LocalPointToTextureCoord(localPoint);

        // Save current state for undo
        _currentStrokeStart = _texture.GetPixels();

        _lastPoint = texCoord;
        _drawing = true;
        DrawCircle((int)texCoord.x, (int)texCoord.y, BrushSize, CurrentColor);
        _texture.Apply();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_interactable || !_drawing) return;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rawImage.rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            return;

        Vector2 texCoord = LocalPointToTextureCoord(localPoint);

        float dist = Vector2.Distance(_lastPoint, texCoord);
        if (dist < MinDistance) return;

        // Draw line from last point to current
        DrawLine((int)_lastPoint.x, (int)_lastPoint.y, (int)texCoord.x, (int)texCoord.y, BrushSize, CurrentColor);
        _lastPoint = texCoord;
        _texture.Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_drawing) return;
        _drawing = false;

        // Save snapshot for undo
        _history.Add(_texture.GetPixels());
    }

    // ============================================================
    // Drawing Primitives
    // ============================================================

    private Vector2 LocalPointToTextureCoord(Vector2 localPoint)
    {
        Rect rect = _rawImage.rectTransform.rect;
        float normalizedX = (localPoint.x - rect.x) / rect.width;
        float normalizedY = (localPoint.y - rect.y) / rect.height;

        float texX = normalizedX * TextureWidth;
        float texY = normalizedY * TextureHeight;

        return new Vector2(
            Mathf.Clamp(texX, 0, TextureWidth - 1),
            Mathf.Clamp(texY, 0, TextureHeight - 1)
        );
    }

    private void DrawCircle(int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= r2)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < TextureWidth && py >= 0 && py < TextureHeight)
                    {
                        _texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    private void DrawLine(int x0, int y0, int x1, int y1, int brushSize, Color color)
    {
        // Bresenham's line algorithm with brush
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawCircle(x0, y0, brushSize, color);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    // ============================================================
    // Color Picker Helpers
    // ============================================================

    /// <summary>
    /// Called by color option buttons in the UI.
    /// Pass the hex color string (e.g., "#FF0000").
    /// </summary>
    public void SetColorFromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            CurrentColor = color;
        }

        if (ColorPickerPanel != null)
            ColorPickerPanel.SetActive(false);
    }
}

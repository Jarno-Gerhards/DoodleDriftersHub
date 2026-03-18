using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DoodleDrawer : MonoBehaviour
{
    public RawImage drawArea;
    public int textureSize = 512;
    public int brushSize = 3;

    Texture2D drawTexture;

    void Start()
    {
        drawTexture = new Texture2D(textureSize, textureSize);
        ClearCanvas();

        drawArea.texture = drawTexture;
    }

void Update()
    {
        if (Pointer.current == null) return;
        if (!Pointer.current.press.isPressed) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector2 localPos;

        Camera uiCam = null;
        var canvas = drawArea.canvas;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawArea.rectTransform, screenPos, uiCam, out localPos))
            return;

        Rect rect = drawArea.rectTransform.rect;

        float x = Mathf.InverseLerp(rect.xMin, rect.xMax, localPos.x);
        float y = Mathf.InverseLerp(rect.yMin, rect.yMax, localPos.y);

        int px = Mathf.Clamp((int)(x * textureSize), 0, textureSize - 1);
        int py = Mathf.Clamp((int)(y * textureSize), 0, textureSize - 1);

        Draw(px, py);
    }

    void Draw(int x, int y)
    {
        for (int i = -brushSize; i <= brushSize; i++)
        {
            for (int j = -brushSize; j <= brushSize; j++)
            {
                int dx = x + i;
                int dy = y + j;

                if (dx >= 0 && dx < textureSize && dy >= 0 && dy < textureSize)
                    drawTexture.SetPixel(dx, dy, Color.black);
            }
        }

        drawTexture.Apply();
    }

    public Texture2D GetTexture()
    {
        return drawTexture;
    }

    public void ClearCanvas()
    {
        Color[] colors = new Color[textureSize * textureSize];

        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;

        drawTexture.SetPixels(colors);
        drawTexture.Apply();
    }
}
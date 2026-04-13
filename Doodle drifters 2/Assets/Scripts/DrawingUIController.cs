using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Wires the full "Draw Your Champion" client screen UI to DoodleDrawerPro.
///
/// Tools: Pencil, Eraser, Fill, Line, Rectangle, Circle
/// Assign all references in the Inspector.
/// </summary>
public class DrawingUIController : MonoBehaviour
{
    [Header("Drawer")]
    [SerializeField] private DoodleDrawerPro drawer;

    [Header("Tool Buttons")]
    [SerializeField] private Button pencilButton;
    [SerializeField] private Button eraserButton;
    [SerializeField] private Button fillButton;
    [SerializeField] private Button lineButton;
    [SerializeField] private Button rectangleButton;
    [SerializeField] private Button circleButton;

    [Header("Tool Button Colors")]
    [SerializeField] private Color selectedButtonColor   = new Color(0.25f, 0.55f, 1f);
    [SerializeField] private Color deselectedButtonColor = Color.white;

    [Header("Color Palette Buttons")]
    [Tooltip("Each button's Image.color is used as the drawing color.")]
    [SerializeField] private Button[] colorButtons;

    [Header("Brush Size")]
    [SerializeField] private Slider          brushSizeSlider;
    [SerializeField] private TextMeshProUGUI brushSizeLabel;

    [Header("Canvas Actions")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button clearButton;

    [Header("Active Color Preview (optional)")]
    [SerializeField] private Image activeColorPreview;

    private Button[] allToolButtons;

    private void Start()
    {
        allToolButtons = new[]
        {
            pencilButton, eraserButton, fillButton,
            lineButton, rectangleButton, circleButton
        };

        pencilButton?   .onClick.AddListener(() => SelectTool(DoodleDrawerPro.DrawTool.Pencil,    pencilButton));
        eraserButton?   .onClick.AddListener(() => SelectTool(DoodleDrawerPro.DrawTool.Eraser,    eraserButton));
        fillButton?     .onClick.AddListener(() => SelectTool(DoodleDrawerPro.DrawTool.Fill,      fillButton));
        lineButton?     .onClick.AddListener(() => SelectTool(DoodleDrawerPro.DrawTool.Line,      lineButton));
        rectangleButton?.onClick.AddListener(() => SelectTool(DoodleDrawerPro.DrawTool.Rectangle, rectangleButton));
        circleButton?   .onClick.AddListener(() => SelectTool(DoodleDrawerPro.DrawTool.Circle,    circleButton));

        foreach (Button btn in colorButtons)
        {
            if (btn == null) continue;
            Color c = btn.GetComponent<Image>().color;
            btn.onClick.AddListener(() => SelectColor(c));
        }

        if (brushSizeSlider != null)
        {
            brushSizeSlider.minValue = 0f;
            brushSizeSlider.maxValue = 1f;
            brushSizeSlider.value    = 0.1f;
            brushSizeSlider.onValueChanged.AddListener(OnBrushSliderChanged);
            OnBrushSliderChanged(brushSizeSlider.value);
        }

        undoButton? .onClick.AddListener(drawer.Undo);
        clearButton?.onClick.AddListener(drawer.ClearCanvas);

        SelectTool(DoodleDrawerPro.DrawTool.Pencil, pencilButton);
        SelectColor(Color.black);
    }

    private void SelectTool(DoodleDrawerPro.DrawTool tool, Button button)
    {
        drawer.SetTool(tool);
        HighlightToolButton(button);
    }

    private void SelectColor(Color color)
    {
        drawer.SetColor(color);
        if (activeColorPreview != null)
            activeColorPreview.color = color;
    }

    private void HighlightToolButton(Button selected)
    {
        foreach (Button btn in allToolButtons)
            SetButtonColor(btn, deselectedButtonColor);

        SetButtonColor(selected, selectedButtonColor);
    }

    private static void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private void OnBrushSliderChanged(float value)
    {
        drawer.SetBrushSizeNormalized(value);
        int size = Mathf.RoundToInt(Mathf.Lerp(1f, 40f, value));
        if (brushSizeLabel != null)
            brushSizeLabel.text = size.ToString();
    }
}
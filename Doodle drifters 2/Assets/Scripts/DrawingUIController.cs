using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Wires the entire "Draw Your Champion" client screen UI to DoodleDrawerPro.
///
/// Assign all references in the Inspector.
/// See the Unity Setup Guide for the full hierarchy.
/// </summary>
public class DrawingUIController : MonoBehaviour
{

    [Header("Drawer")]
    [SerializeField] private DoodleDrawerPro drawer;

    [Header("Tool Buttons")]
    [SerializeField] private Button pencilButton;
    [SerializeField] private Button eraserButton;
    [SerializeField] private Button fillButton;

    [Header("Tool Button Selected Colors")]
    [SerializeField] private Color selectedButtonColor   = new Color(0.25f, 0.55f, 1f);
    [SerializeField] private Color deselectedButtonColor = Color.white;

    [Header("Color Palette Buttons")]
    [Tooltip("Assign one Button per color swatch. Each button's Image color = the draw color.")]
    [SerializeField] private Button[] colorButtons;


    [Header("Brush Size")]
    [SerializeField] private Slider      brushSizeSlider;
    [SerializeField] private TextMeshProUGUI brushSizeLabel;   // optional — shows current size

    // ── Canvas actions ────────────────────────────────────────────────────────

    [Header("Canvas Actions")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button submitButton;

    // ── Active color indicator ────────────────────────────────────────────────

    [Header("Active Color Preview (optional)")]
    [SerializeField] private Image activeColorPreview;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private Button currentToolButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Tool buttons
        pencilButton?.onClick.AddListener(() => SelectTool(DoodleDrawerPro.DrawTool.Pencil, pencilButton));
        eraserButton?.onClick.AddListener(() => SelectTool(DoodleDrawerPro.DrawTool.Eraser, eraserButton));
        fillButton?.onClick.AddListener(()   => SelectTool(DoodleDrawerPro.DrawTool.Fill,   fillButton));

        // Color palette
        foreach (Button btn in colorButtons)
        {
            if (btn == null) continue;
            Color c = btn.GetComponent<Image>().color;
            btn.onClick.AddListener(() => SelectColor(c));
        }

        // Brush size slider
        if (brushSizeSlider != null)
        {
            brushSizeSlider.minValue = 0f;
            brushSizeSlider.maxValue = 1f;
            brushSizeSlider.value    = 0.1f;   // default ~5 px
            brushSizeSlider.onValueChanged.AddListener(OnBrushSliderChanged);
            OnBrushSliderChanged(brushSizeSlider.value);
        }

        undoButton?.onClick.AddListener(drawer.Undo);
        clearButton?.onClick.AddListener(drawer.ClearCanvas);

        // Default tool = Pencil
        SelectTool(DoodleDrawerPro.DrawTool.Pencil, pencilButton);

        // Default color = black
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
        // Reset all tool buttons
        SetButtonColor(pencilButton, deselectedButtonColor);
        SetButtonColor(eraserButton, deselectedButtonColor);
        SetButtonColor(fillButton,   deselectedButtonColor);

        // Highlight selected
        SetButtonColor(selected, selectedButtonColor);
        currentToolButton = selected;
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

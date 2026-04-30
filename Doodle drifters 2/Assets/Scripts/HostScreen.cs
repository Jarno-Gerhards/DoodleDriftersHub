using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Host screen — "The party is assembling"
///
/// Shows all submitted client drawings side by side in the center.
/// No limit on the number of drawings.
///
/// Inspector hook-up:
///   - drawingContainer : the HorizontalLayoutGroup where portraits are added
///   - portraitPrefab   : prefab of a single portrait slot (see setup guide)
///   - assemblingText   : the "The party is assembling" TextMeshPro (optional)
///
/// Other scripts call HostScreen.AddDrawing(texture, playerName).
/// DrawingSubmitBridge.cs does this automatically from the client Submit button.
/// </summary>
public class HostScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform            drawingContainer;
    [SerializeField] private GameObject           portraitPrefab;
    [SerializeField] private TextMeshProUGUI      assemblingText;

    [Header("Settings")]
    [SerializeField] private string assemblingMessage = "The party is assembling...";
    [SerializeField] private Vector2 portraitSize     = new Vector2(180f, 180f);
    [SerializeField] private int maxPortraits          = 8;

    private readonly List<GameObject> portraits = new List<GameObject>();

    public static HostScreen Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (assemblingText != null)
            assemblingText.text = assemblingMessage;
    }

    /// <summary>
    /// Add a drawing to the host screen.
    /// Call this from DrawingSubmitBridge (or directly) when a client submits.
    /// </summary>
    /// <param name="drawing">The Texture2D from DoodleDrawerPro.GetTexture()</param>
    /// <param name="playerName">Optional player label shown under the portrait</param>
    public void AddDrawing(Texture2D drawing, string playerName = "Player")
    {
        if (portraitPrefab == null)
        {
            Debug.LogError("[HostScreen] portraitPrefab is not assigned!");
            return;
        }

        if (portraits.Count >= maxPortraits)
        {
            Debug.LogWarning($"[HostScreen] Max submissions reached ({maxPortraits}). Ignoring drawing from '{playerName}'.");
            return;
        }

        GameObject portrait = Instantiate(portraitPrefab, drawingContainer);
        portraits.Add(portrait);

        RectTransform rt = portrait.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = portraitSize;
        }

        RawImage img = portrait.GetComponentInChildren<RawImage>();
        if (img != null)
        {
            Texture2D clone = CloneTexture(drawing);
            img.texture = clone;
        }
        else
        {
            Debug.LogWarning("[HostScreen] Portrait prefab has no RawImage child.");
        }

        TextMeshProUGUI label = portrait.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = playerName;

        Debug.Log($"[HostScreen] Added portrait for '{playerName}'. Total: {portraits.Count}");
    }

    /// <summary>Remove all portraits from the host screen (e.g. between rooms).</summary>
    public void ClearPortraits()
    {
        foreach (GameObject p in portraits)
            Destroy(p);
        portraits.Clear();
    }

    private Texture2D CloneTexture(Texture2D source)
    {
        Texture2D clone = new Texture2D(source.width, source.height, source.format, false);
        clone.SetPixels32(source.GetPixels32());
        clone.Apply();
        return clone;
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Host screen — "The party is assembling"
///
/// Toont alle ingezonden tekeningen van clients naast elkaar in het midden.
/// Geen limiet aan het aantal tekeningen.
///
/// Inspector hook-up:
///   - drawingContainer : de HorizontalLayoutGroup waar de portraits in komen
///   - portraitPrefab   : prefab van één portrait slot (zie setup guide)
///   - assemblingText   : de "The party is assembling" TextMeshPro (optioneel)
///
/// Andere scripts roepen HostScreen.AddDrawing(texture, playerName) aan.
/// DrawingSubmitBridge.cs doet dit automatisch vanuit de client Submit knop.
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

    // Runtime list of all added portraits
    private readonly List<GameObject> portraits = new List<GameObject>();

    // ── Singleton so DrawingSubmitBridge can find it easily ───────────────────
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

    // =========================================================================
    // Public API
    // =========================================================================

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

        GameObject portrait = Instantiate(portraitPrefab, drawingContainer);
        portraits.Add(portrait);

        // Set portrait size
        RectTransform rt = portrait.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = portraitSize;
        }

        // Find the RawImage inside the prefab and assign the drawing texture
        RawImage img = portrait.GetComponentInChildren<RawImage>();
        if (img != null)
        {
            // Clone the texture so it isn't affected if the canvas is cleared
            Texture2D clone = CloneTexture(drawing);
            img.texture = clone;
        }
        else
        {
            Debug.LogWarning("[HostScreen] Portrait prefab has no RawImage child.");
        }

        // Find the label TMP and set player name
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

    // =========================================================================
    // Helpers
    // =========================================================================

    private Texture2D CloneTexture(Texture2D source)
    {
        Texture2D clone = new Texture2D(source.width, source.height, source.format, false);
        clone.SetPixels32(source.GetPixels32());
        clone.Apply();
        return clone;
    }
}
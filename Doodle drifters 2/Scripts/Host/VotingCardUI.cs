using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component attached to each voting card in the host's voting display.
/// Handles showing drawing info and animating vote results.
/// </summary>
public class VotingCardUI : MonoBehaviour
{
    [Header("UI References (auto-found if null)")]
    public TMP_Text NumberText;
    public RawImage DrawingImage;
    public TMP_Text DescriptionText;
    public TMP_Text PlayerNameText;
    public TMP_Text VoteCountText;
    public TMP_Text VoteLabelText;

    public string PlayerId { get; private set; }

    public void Setup(int number, DrawingData drawing)
    {
        PlayerId = drawing.PlayerId;

        if (NumberText != null)
            NumberText.text = number.ToString();

        if (DrawingImage != null && !string.IsNullOrEmpty(drawing.ImageBase64))
        {
            Texture2D tex = Base64ToTexture(drawing.ImageBase64);
            if (tex != null)
                DrawingImage.texture = tex;
        }

        if (DescriptionText != null)
            DescriptionText.text = !string.IsNullOrEmpty(drawing.Item) ? drawing.Item : "Unknown";

        // Hide results UI initially
        if (PlayerNameText != null) PlayerNameText.gameObject.SetActive(false);
        if (VoteCountText != null) VoteCountText.gameObject.SetActive(false);
        if (VoteLabelText != null) VoteLabelText.gameObject.SetActive(false);
    }

    public void ShowResults(string playerName, int finalVotes, float animationInterval)
    {
        if (PlayerNameText != null)
        {
            PlayerNameText.text = playerName;
            PlayerNameText.gameObject.SetActive(true);
        }

        if (VoteCountText != null)
        {
            VoteCountText.gameObject.SetActive(true);
            VoteCountText.text = "0";
        }

        if (VoteLabelText != null)
        {
            VoteLabelText.text = "votes";
            VoteLabelText.gameObject.SetActive(true);
        }

        StartCoroutine(AnimateVoteCount(finalVotes, animationInterval));
    }

    private IEnumerator AnimateVoteCount(int target, float interval)
    {
        yield return new WaitForSeconds(0.5f);

        int current = 0;
        while (current < target)
        {
            current++;
            if (VoteCountText != null)
                VoteCountText.text = current.ToString();
            yield return new WaitForSeconds(interval);
        }
    }

    private Texture2D Base64ToTexture(string base64)
    {
        try
        {
            // Strip data URI prefix if present
            string data = base64;
            int commaIdx = data.IndexOf(',');
            if (commaIdx >= 0)
                data = data.Substring(commaIdx + 1);

            byte[] bytes = System.Convert.FromBase64String(data);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            return tex;
        }
        catch
        {
            return null;
        }
    }
}

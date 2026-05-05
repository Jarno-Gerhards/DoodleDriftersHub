using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class BookUIController : MonoBehaviour
{
    public Texture2D closedSprite;
    public Texture2D openSprite;
    public Texture2D openFlipSprite;
    public Texture2D openingSprite;

    private VisualElement bookImage;
    private Label pageText;

    private enum BookState
    {
        Closed,
        Opening,
        Open,
        Flipping
    }

    private BookState currentState = BookState.Closed;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        bookImage = root.Q<VisualElement>("bookImage");
        pageText = root.Q<Label>("pageText");

        SetBookImage(closedSprite);

        SetupTextInteractions();
    }

    void SetupTextInteractions()
    {
        // Hover → slightly smaller
        pageText.RegisterCallback<MouseEnterEvent>(_ =>
        {
            pageText.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1));
        });

        pageText.RegisterCallback<MouseLeaveEvent>(_ =>
        {
            pageText.style.scale = new Scale(Vector3.one);
        });

        // Click
        pageText.RegisterCallback<ClickEvent>(_ =>
        {
            StartCoroutine(HandleClick());
        });
    }

    IEnumerator HandleClick()
    {
        // Make it larger briefly
        pageText.style.scale = new Scale(new Vector3(1.2f, 1.2f, 1));

        yield return new WaitForSeconds(0.2f);

        pageText.style.scale = new Scale(Vector3.one);

        // Decide animation based on state
        if (currentState == BookState.Closed)
        {
            yield return StartCoroutine(OpenBook());
        }
        else if (currentState == BookState.Open)
        {
            yield return StartCoroutine(FlipPage());
        }
    }

    IEnumerator OpenBook()
    {
        currentState = BookState.Opening;

        SetBookImage(openingSprite);

        yield return new WaitForSeconds(0.2f); // adjust timing

        SetBookImage(openSprite);

        currentState = BookState.Open;
    }

    IEnumerator FlipPage()
    {
        currentState = BookState.Flipping;

        SetBookImage(openFlipSprite);

        yield return new WaitForSeconds(0.3f); // short animation

        SetBookImage(openSprite);

        currentState = BookState.Open;
    }

    void SetBookImage(Texture2D tex)
    {
        bookImage.style.backgroundImage = new StyleBackground(tex);
    }
}
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
    private Label nextPageText;
    private Label titleText;

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
        nextPageText = root.Q<Label>("nextPageText");
        titleText = root.Q<Label>("titleText");

        SetBookImage(closedSprite);

        // Hide next page text at start
        nextPageText.style.display = DisplayStyle.None;

        SetupTextInteractions();
    }

    void SetupTextInteractions()
    {
        // Original text interactions
        pageText.RegisterCallback<MouseEnterEvent>(_ =>
        {
            pageText.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1));
        });

        pageText.RegisterCallback<MouseLeaveEvent>(_ =>
        {
            pageText.style.scale = new Scale(Vector3.one);
        });

        pageText.RegisterCallback<ClickEvent>(_ =>
        {
            StartCoroutine(HandleClick());
        });

        // Next page interactions
        nextPageText.RegisterCallback<MouseEnterEvent>(_ =>
        {
            nextPageText.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1));
        });

        nextPageText.RegisterCallback<MouseLeaveEvent>(_ =>
        {
            nextPageText.style.scale = new Scale(Vector3.one);
        });

        nextPageText.RegisterCallback<ClickEvent>(_ =>
        {
            if (currentState == BookState.Open)
            {
                StartCoroutine(FlipPage());
            }
        });
    }

    IEnumerator HandleClick()
    {
        pageText.style.scale = new Scale(new Vector3(1.2f, 1.2f, 1));

        yield return new WaitForSeconds(0.2f);

        pageText.style.scale = new Scale(Vector3.one);

        if (currentState == BookState.Closed)
        {
            yield return StartCoroutine(OpenBook());
        }
    }

    IEnumerator OpenBook()
    {
        currentState = BookState.Opening;

                // Hide original text
        pageText.style.display = DisplayStyle.None;
        titleText.style.display = DisplayStyle.None;

        SetBookImage(openingSprite);

        yield return new WaitForSeconds(0.3f);

        SetBookImage(openSprite);

        currentState = BookState.Open;

        // Show next page text
        nextPageText.style.display = DisplayStyle.Flex;
        nextPageText.text = "Next Page";
    }

    IEnumerator FlipPage()
    {
        currentState = BookState.Flipping;

        SetBookImage(openFlipSprite);

        nextPageText.style.display = DisplayStyle.None;

        yield return new WaitForSeconds(0.3f);

        SetBookImage(openSprite);

        nextPageText.style.display = DisplayStyle.Flex;
        nextPageText.text = "Next Page";

        currentState = BookState.Open;
    }

    void SetBookImage(Texture2D tex)
    {
        bookImage.style.backgroundImage = new StyleBackground(tex);
    }
}
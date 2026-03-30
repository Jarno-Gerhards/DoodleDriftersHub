using UnityEngine;
using UnityEngine.UIElements;

public class ButtonTemplate : MonoBehaviour
{
    private UIDocument _document;

    private Button _button;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();

        if (_document == null)
        {
            Debug.LogError("UIDocument component is missing.");
            return;
        }

        _button = _document.rootVisualElement.Q<Button>("Button");

        if (_button == null)
        {
            Debug.LogError("Button with name 'Button' was not found in the UI document.");
            return;
        }

        _button.RegisterCallback<ClickEvent>(OnButtonClick);
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.UnregisterCallback<ClickEvent>(OnButtonClick);
        }
    }

    private void OnButtonClick(ClickEvent evt)
    {
        Debug.Log("Button clicked!");
    }

}

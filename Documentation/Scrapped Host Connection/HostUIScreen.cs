using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class HostUIScreen : MonoBehaviour
{
    [SerializeField] private StateMachine.GameStateType state;
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject rootOverride;
    [SerializeField] private bool hideOnAwake = true;

    private IHostUIScreenHooks[] hooks;

    public StateMachine.GameStateType State => state;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        hooks = GetComponents<IHostUIScreenHooks>();

        if (hideOnAwake)
        {
            Hide();
        }
    }

    public void Show()
    {
        SetVisible(true);
        EnsureHooks();
        for (int i = 0; i < hooks.Length; i++)
        {
            hooks[i].OnShow();
        }
    }

    public void Hide()
    {
        SetVisible(false);
        EnsureHooks();
        for (int i = 0; i < hooks.Length; i++)
        {
            hooks[i].OnHide();
        }
    }

    private void EnsureHooks()
    {
        if (hooks == null)
        {
            hooks = GetComponents<IHostUIScreenHooks>();
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (uiDocument != null)
        {
            uiDocument.enabled = isVisible;
        }

        if (canvas != null)
        {
            canvas.enabled = isVisible;
        }

        if (uiDocument == null && canvas == null)
        {
            GameObject target = rootOverride != null ? rootOverride : gameObject;
            target.SetActive(isVisible);
        }
    }
}

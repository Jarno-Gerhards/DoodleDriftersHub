using System.Collections.Generic;
using UnityEngine;

public class HostUIRouter : MonoBehaviour
{
    [SerializeField] private StateMachine stateMachine;
    [SerializeField] private bool autoDiscoverScreens = true;

    private readonly Dictionary<StateMachine.GameStateType, List<HostUIScreen>> screensByState = new();

    private void Awake()
    {
        if (stateMachine == null)
        {
            stateMachine = FindFirstObjectByType<StateMachine>();
        }

        if (stateMachine == null)
        {
            Debug.LogError("[HostUIRouter] StateMachine not found in scene.");
            enabled = false;
            return;
        }

        if (autoDiscoverScreens)
        {
            HostUIScreen[] screens = FindObjectsOfType<HostUIScreen>(true);
            for (int i = 0; i < screens.Length; i++)
            {
                RegisterScreen(screens[i]);
            }
        }
    }

    private void OnEnable()
    {
        if (stateMachine != null)
        {
            stateMachine.StateChanged += OnStateChanged;
        }
    }

    private void OnDisable()
    {
        if (stateMachine != null)
        {
            stateMachine.StateChanged -= OnStateChanged;
        }
    }

    private void Start()
    {
        ShowState(stateMachine.CurrentStateType);
    }

    public void RegisterScreen(HostUIScreen screen)
    {
        if (screen == null)
        {
            return;
        }

        if (!screensByState.TryGetValue(screen.State, out List<HostUIScreen> list))
        {
            list = new List<HostUIScreen>();
            screensByState.Add(screen.State, list);
        }

        if (!list.Contains(screen))
        {
            list.Add(screen);
        }

        screen.Hide();
    }

    private void OnStateChanged(StateMachine.GameStateType previous, StateMachine.GameStateType next)
    {
        ShowState(next);
    }

    private void ShowState(StateMachine.GameStateType state)
    {
        foreach (KeyValuePair<StateMachine.GameStateType, List<HostUIScreen>> entry in screensByState)
        {
            List<HostUIScreen> screens = entry.Value;
            for (int i = 0; i < screens.Count; i++)
            {
                HostUIScreen screen = screens[i];
                if (screen == null)
                {
                    continue;
                }

                if (entry.Key == state)
                {
                    screen.Show();
                }
                else
                {
                    screen.Hide();
                }
            }
        }
    }
}

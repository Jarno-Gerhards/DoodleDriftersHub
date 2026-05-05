using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public enum GameStateType
    {
        Lobby,
        Drawing,
        Voting
        //Add more types as neccesary
    }

    public event Action<GameStateType, GameStateType> StateChanged;

    public GameStateType CurrentStateType => currentStateType;

    private Dictionary<GameStateType, IState> states;

    void InitializeStates()
    {
        states = new Dictionary<GameStateType, IState>
        { // change the actual states when they are created
            { GameStateType.Lobby, new LobbyState() },
            { GameStateType.Drawing, new testState() },
            { GameStateType.Voting, new testState() },
        };
    }

    private Dictionary<GameStateType, List<GameStateType>> validTransitions =
        new Dictionary<GameStateType, List<GameStateType>>
    {
    { GameStateType.Lobby, new List<GameStateType> { GameStateType.Drawing } },
    { GameStateType.Drawing, new List<GameStateType> { GameStateType.Voting } },
    { GameStateType.Voting, new List<GameStateType> { GameStateType.Drawing } }
    };

    IState currentState;
    private GameContext context;
    private GameStateType currentStateType;

    void Awake()
    {
        context = new GameContext(this);
        InitializeStates();
    }

    void Start()
    {
        SwitchState(GameStateType.Lobby);
    }

    void Update()
    {
        currentState?.Update(context);
    }

    void FixedUpdate()
    {
        currentState?.FixedUpdate(context);
    }

    public void SwitchState(GameStateType newState)
    {
        // if (validTransitions[currentStateType].Contains(newState))
        // {
        //     Debug.LogWarning("invalid transition");
        //     return;
        // } uncomment when state transitions are fleshed out

        if (currentState != null && newState == currentStateType)
        {
            return;
        }
        GameStateType previousState = currentStateType;
        currentStateType = newState;
        SetState(states[newState]);
        StateChanged?.Invoke(previousState, currentStateType);
    }

    public void SetState(IState newState)
    {
        currentState?.Exit(context);
        currentState = newState;
        currentState?.Enter(context);
    }
}
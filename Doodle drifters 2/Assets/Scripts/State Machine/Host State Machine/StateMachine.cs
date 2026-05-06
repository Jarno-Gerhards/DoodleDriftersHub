using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    private WebSocketClient network;

    public enum GameStateType
    {
        Lobby,
        NewScene,
        Think,
        Drawing,
        Voting,//
        Solution
        //Add more types as neccesary
    }

    private HashSet<GameStateType> networkStates = new HashSet<GameStateType> // Only states the network needs to know about
    {
        GameStateType.Lobby,
        GameStateType.Drawing,
        GameStateType.Voting,
        GameStateType.Solution
    };

    private Dictionary<GameStateType, IState> states;

    void InitializeStates()
    {
        states = new Dictionary<GameStateType, IState>
        { // change the actual states when they are created
            { GameStateType.Lobby, new LobbyState() },
           // { GameStateType.NewScene, new NewSceneState() },
            { GameStateType.Think, new testState() },
           // { GameStateType.Drawing, new DrawingState() },
            //{ GameStateType.Voting, new VotingState() },
            //{ GameStateType.Solution, new SolutionState() },
        };
    }

    private Dictionary<GameStateType, List<GameStateType>> validTransitions =
        new Dictionary<GameStateType, List<GameStateType>>
    {
    { GameStateType.Lobby, new List<GameStateType> { GameStateType.NewScene } },
    { GameStateType.NewScene, new List<GameStateType> { GameStateType.Think } },
    { GameStateType.Think, new List<GameStateType> { GameStateType.Drawing } },
    { GameStateType.Drawing, new List<GameStateType> { GameStateType.Voting } },
    { GameStateType.Voting, new List<GameStateType> { GameStateType.Solution } },
    { GameStateType.Solution, new List<GameStateType> { GameStateType.Lobby } }
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
        Debug.Log(newState);
        currentStateType = newState;
        SetState(states[newState]);

        BroadcastState(newState);
        Debug.Log("NETWORK REF: " + network);
        Debug.Log("NETWORK NULL? " + (network == null));
        Debug.Log("INSTANCE ID: " + GetInstanceID());
    }

    public void SetState(IState newState)
    {
        currentState?.Exit(context);
        currentState = newState;
        currentState?.Enter(context);
    }

    public void SetNetwork(WebSocketClient ws)
    {
        Debug.Log("ASSIGNED WS INSTANCE: " + ws.GetInstanceID());
        network = ws;
    }

    private void BroadcastState(GameStateType state)
    {
        if (network == null) return;

        if (!networkStates.Contains(state)) return;

        network.Send("{\"type\":\"STATE\",\"state\":\"" + state.ToString() + "\"}");
    }
}
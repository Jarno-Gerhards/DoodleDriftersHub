using UnityEngine;

public class GameContext
{
    public StateMachine stateMachine;
    public Texture2D latestDrawing;
    public string latestTitle;
    //plug systems in later

    public GameContext(StateMachine sm)
    {
        stateMachine = sm;
    }
}
using UnityEngine;

public class LobbyState : IState
{
    private GameObject lobbyUI;
    public LobbyState()
    {
        //lobbyUI = GameObject.Find("HostLobbyScreen");
    }

    public void Enter(GameContext context)
    {
        //lobbyUI.SetActive(true);
    }

    public void Exit(GameContext context)
    {
        //lobbyUI.SetActive(false);
    }

    public void Update(GameContext context)
    {

    }

    public void FixedUpdate(GameContext context)
    {

    }
}
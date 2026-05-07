using UnityEngine;

public class LobbyState : IState
{
    private GameObject lobbyUI;
    private HostLobbyScript lobbyScript;
    public LobbyState()
    {
        lobbyUI = GameObject.Find("HostLobbyScreen");
        lobbyScript = lobbyUI.GetComponent<HostLobbyScript>();
    }

    public void Enter(GameContext context)
    {
        lobbyUI.SetActive(true);
    }

    public void Exit(GameContext context)
    {
        lobbyUI.SetActive(false);
    }

    public void Update(GameContext context)
    {
        
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}
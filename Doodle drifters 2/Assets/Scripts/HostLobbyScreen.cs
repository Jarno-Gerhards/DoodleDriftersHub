using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class HostLobbyScript : MonoBehaviour
{
    private UIDocument document;
    private Label roomCode;
    private int roomCodeNumber = 12345;
    private Label playerName;
    private VisualElement playerAvatar;
    public Texture2D testImage;
    private TextField encounters;
    private EventCallback<ChangeEvent<string>> encounterCallback;
    private char minEncounters = '2';
    private char maxEncounters = '9';
    private Button startGameButton;
    private int playerCount = 0;
    private void Awake()
    {
        document = GetComponent<UIDocument>();
        StartCoroutine(TextFunc());
    }

    private void Start()
    {
        roomCodeNumber = Random.Range(10000, 99999);
        roomCode = document.rootVisualElement.Q<Label>("RoomCode");
        roomCode.text = $"Room Code: {roomCodeNumber}";
        encounters = document.rootVisualElement.Q<TextField>("EncounterInput");
        startGameButton = document.rootVisualElement.Q<Button>("StartGame");
        startGameButton.clicked += OnStartClick;
        encounterCallback = HandleEncounterCount;
        encounters.RegisterValueChangedCallback(encounterCallback);
    }

    public void AddPlayer(string name, Texture2D avatar)
    {
        playerName = document.rootVisualElement.Q<Label>($"player{playerCount + 1}Name");
        playerAvatar = document.rootVisualElement.Q<VisualElement>($"player{playerCount + 1}Avatar");
        playerName.text = name;
        playerAvatar.style.backgroundImage = avatar;
        playerCount++;
    }

    private IEnumerator TextFunc()
    {
        yield return new WaitForSeconds(1);
        AddPlayer("Player " + (playerCount + 1), testImage);
        if (playerCount <= 5)
        {
            StartCoroutine(TextFunc());
        }
    }

    private void OnStartClick()
    {
        if (playerCount < 2)
        {
            Debug.LogWarning("At least 2 players are required to start the game.");
            return;
        }
        if (string.IsNullOrEmpty(encounters.text))
        {
            Debug.LogWarning("Please enter the number of encounters before starting the game.");
            return;
        }
        Debug.Log("Start Game button clicked!");
    }

    private void HandleEncounterCount(ChangeEvent<string> evt)
    {
        string input = evt.newValue;
        if (string.IsNullOrEmpty(input))
        {
            return;
        }
        char c = input[0];
        if (c < minEncounters || c > maxEncounters)
        {
            encounters.SetValueWithoutNotify(evt.previousValue);
        }
    }

    private void OnDisable()
    {
        startGameButton.clicked -= OnStartClick;
        encounters.UnregisterValueChangedCallback(encounterCallback);
    }
}

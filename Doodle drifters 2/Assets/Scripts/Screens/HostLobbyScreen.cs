using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class HostLobbyScript : MonoBehaviour
{
    public StateMachine stateMachine;
    [SerializeField] private WebSocketClient networkClient;
    private UIDocument document;
    private Label roomCode;
    private string roomCodeValue = "-----";
    private Label playerName;
    private VisualElement playerAvatar;
    public Texture2D testImage;
    [SerializeField] private bool enableTestPlayers = false;
    private TextField encounters;
    private EventCallback<ChangeEvent<string>> encounterCallback;
    private char minEncounters = '2';
    private char maxEncounters = '9';
    private Button startGameButton;
    private int playerCount = 0;
    private void Awake()
    {
        document = GetComponent<UIDocument>();
        if (document == null)
        {
            document = GetComponentInChildren<UIDocument>(true);
        }
        if (networkClient == null)
        {
            networkClient = FindFirstObjectByType<WebSocketClient>();
        }
        if (enableTestPlayers)
        {
            StartCoroutine(TestFunc());
        }
    }

    private void OnEnable()
    {
        EnsureRoomCodeLabel();
    }

    private void Start()
    {
        EnsureRoomCodeLabel();
        encounters = document.rootVisualElement.Q<TextField>("EncounterInput");
        startGameButton = document.rootVisualElement.Q<Button>("StartGame");
        startGameButton.clicked += OnStartClick;
        encounterCallback = HandleEncounterCount;
        encounters.RegisterValueChangedCallback(encounterCallback);
    }

    public void AddPlayer(string name, Texture2D avatar) // Voegt een speler toe met naam en avatar met auto increment
    {
        playerName = document.rootVisualElement.Q<Label>($"player{playerCount + 1}Name");
        playerAvatar = document.rootVisualElement.Q<VisualElement>($"player{playerCount + 1}Avatar");
        playerName.text = name;
        playerAvatar.style.backgroundImage = avatar;
        playerCount++;
    }

    public void SetRoomCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;

        roomCodeValue = code.Trim();
        EnsureRoomCodeLabel();
    }

    private void EnsureRoomCodeLabel()
    {
        if (document == null)
        {
            document = GetComponent<UIDocument>();
            if (document == null)
            {
                document = GetComponentInChildren<UIDocument>(true);
            }
        }

        if (roomCode == null)
        {
            TryResolveRoomCodeLabel();
        }

        if (roomCode == null)
        {
            Debug.LogWarning("[HostLobbyScript] RoomCode label not found.");
            return;
        }

        roomCode.text = $"Room Code: {roomCodeValue}";
    }

    private void TryResolveRoomCodeLabel()
    {
        if (document != null)
        {
            roomCode = document.rootVisualElement.Q<Label>("RoomCode");
            if (roomCode != null) return;
        }

        UIDocument[] docs = FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < docs.Length; i++)
        {
            UIDocument doc = docs[i];
            if (doc == null) continue;

            Label label = doc.rootVisualElement.Q<Label>("RoomCode");
            if (label != null)
            {
                document = doc;
                roomCode = label;
                return;
            }
        }
    }

    private IEnumerator TestFunc() // Tijdelijke functie om speler toevoegen te laten
    {
        yield return new WaitForSeconds(1);
        AddPlayer("Player " + (playerCount + 1), testImage);
        if (playerCount <= 5)
        {
            StartCoroutine(TestFunc());
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
        stateMachine.SwitchState(StateMachine.GameStateType.NewScene);
        if (networkClient != null)
        {
            networkClient.SendState("Solution");
        }
    }

    private void HandleEncounterCount(ChangeEvent<string> evt) // Zorgt er voor dat alleen cijfers tussen min en max encounters kunnen worden ingevoerd
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

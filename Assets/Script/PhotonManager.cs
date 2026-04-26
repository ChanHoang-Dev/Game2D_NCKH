using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;

    [Header("UI References")]
    public GameObject lobbyPanel;
    public GameObject roomPanel;

    public GameObject SelectionCharacterPanel;
    public TMP_InputField roomNameInput;
    public TMP_InputField playerNameInput;
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerListText;
    public Button startGameButton;

    public GameObject notifi_BossLeave;

    public TextMeshProUGUI textNotifi_BossLeave;


    [Header("Game Settings")]
    public int minPlayers = 2;
    public int maxPlayers = 10;
    public GameObject[] playerPrefab;

    public int selectedCharacterIndex = 0; // Biến lưu chỉ số nhân vật đã chọn

    public bool isPendingCreateRoom = false; // Biến để kiểm tra nếu đang trong quá trình tạo phòng

    public bool isPendingJoinRoom = false;

    [Header("Spawn Settings")]
    public Vector2 mapMinBounds = new Vector2(-20, -20);
    public Vector2 mapMaxBounds = new Vector2(20, 20);
    public float spawnSafeDistance = 3f; // Khoảng cách an toàn giữa các spawn point

    private bool isSpawned = false;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true; // Đảm bảo tất cả client cùng load scene khi master client load
        PhotonNetwork.ConnectUsingSettings();// Kết nối đến Photon Master Server

        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        UpdateUIBasedOnScene();
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateUIBasedOnScene();

        // Spawn player if in game scene and in a room
        if (scene.name != "LobbyGame" && PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    private void UpdateUIBasedOnScene()
    {
        // Check if we're in lobby scene
        bool isLobbyScene = SceneManager.GetActiveScene().name == "LobbyGame";

        if (lobbyPanel != null)
            lobbyPanel.SetActive(isLobbyScene && !PhotonNetwork.InRoom);

        if (roomPanel != null)
            roomPanel.SetActive(isLobbyScene && PhotonNetwork.InRoom);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
    }
    public void ContinueToCharacterSelection()
    {
        CharacterSelection characterSelection = SelectionCharacterPanel.GetComponent<CharacterSelection>();
        if (characterSelection != null)
        {
            selectedCharacterIndex = characterSelection.currentIndex; // Lưu chỉ số nhân vật đã chọn
        }
        SelectionCharacterPanel.SetActive(false);
        if (isPendingCreateRoom)
        {
            CreateRoom();
            isPendingCreateRoom = false;
        }
        else if (isPendingJoinRoom)
        {
            JoinRoom();
            isPendingJoinRoom = false;
        }
    }
    public void PrepareCreateRoom()
    {
        isPendingCreateRoom = true;
        isPendingJoinRoom = false;
        SelectionCharacterPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }
    public void PrepareJoinRoom()
    {
        isPendingJoinRoom = true;
        isPendingCreateRoom = false;
        SelectionCharacterPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }
    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            Debug.LogWarning("Room name is empty!");
            return;
        }

        if (!string.IsNullOrEmpty(playerNameInput.text))
        {
            PhotonNetwork.NickName = playerNameInput.text;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)maxPlayers;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            Debug.LogWarning("Room name is empty!");
            return;
        }

        if (!string.IsNullOrEmpty(playerNameInput.text))
        {
            PhotonNetwork.NickName = playerNameInput.text;
        }

        PhotonNetwork.JoinRoom(roomNameInput.text);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        roomNameText.text = "Room: " + PhotonNetwork.CurrentRoom.Name;

        UpdatePlayerList();

        // Only master client can start the game
        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.gameObject.SetActive(true);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        if(playerListText == null) return;
        if(SceneManager.GetActiveScene().name != "LobbyGame") return;
        playerListText.text = "Players (" + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers + "):\n";

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string role = player.IsMasterClient ? " [Host]" : "[Player]";
            playerListText.text += player.NickName + role + "\n";
        }

        // Enable start button if minimum players reached and user is master
        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount >= minPlayers;
        }
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            startGameButton.interactable = false;
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < minPlayers)
        {
            Debug.LogWarning("Not enough players!");
            return;
        }

        // Close the room so no one else can join
        PhotonNetwork.CurrentRoom.IsOpen = false;

        // Load the game scene for all players
        PhotonNetwork.LoadLevel("GameScene"); // Thay "GameScene" bằng tên scene game của bạn
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
        isSpawned = false; // Reset trạng thái spawn khi rời phòng
        if(SceneManager.GetActiveScene().name != "LobbyGame")
        {
            PhotonNetwork.LoadLevel("LobbyGame"); // Trở về lobby nếu đang ở trong game scene
        }
    }

    private void SpawnPlayer()
    {
        if (isSpawned) return;
        isSpawned = true;
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned!");
            return;
        }
        int index = Mathf.Clamp(selectedCharacterIndex, 0, playerPrefab.Length - 1);
        GameObject prefabToSpawn = playerPrefab[index];
        // Generate random spawn position within map bounds
        Vector3 spawnPosition = GetRandomSpawnPosition();

        GameObject player = PhotonNetwork.Instantiate(prefabToSpawn.name, spawnPosition, Quaternion.identity);

        if (player.GetComponent<PhotonView>().IsMine)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
            {
                cam.SetTarget(player.transform);
            }
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Generate random position within map bounds
        float randomX = Random.Range(mapMinBounds.x, mapMaxBounds.x);
        float randomY = Random.Range(mapMinBounds.y, mapMaxBounds.y);

        return new Vector3(randomX, randomY, 0);
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        bool isLoobyScene = SceneManager.GetActiveScene().name == "LobbyGame";
        if (isLoobyScene && !PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ShowNotificationAndLeave("Host has left the room. You will return to the lobby.")); // Trở về lobby sau 5 giây
        }
        else
        {
            UpdatePlayerList();
        }
    }
    private IEnumerator ShowNotificationAndLeave(string message)
    {
        if (notifi_BossLeave != null && textNotifi_BossLeave != null)
        {
            textNotifi_BossLeave.text = message;
            notifi_BossLeave.SetActive(true);
        }

        yield return new WaitForSeconds(5f); // Chờ 5 giây trước khi trở về lobby
        PhotonNetwork.LeaveRoom();
        if(notifi_BossLeave != null)
        {
            notifi_BossLeave.SetActive(false);
        }
    }
}
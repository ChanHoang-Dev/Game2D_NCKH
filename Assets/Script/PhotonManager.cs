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

    [Header("UI - Character Selection Scene")]
    public GameObject selectPanel;       // Panel chọn nhân vật (hiện đầu tiên)
    public GameObject createRoomPanel;   // Panel nhập tên + ID khi bấm "Tạo phòng"
    public GameObject joinRoomPanel;     // Panel nhập tên + ID khi bấm "Vào phòng"

    [Header("Create Room Panel Fields")]
    public TMP_InputField createPlayerNameInput;
    public TMP_InputField createRoomIdInput;

    [Header("Join Room Panel Fields")]
    public TMP_InputField joinPlayerNameInput;
    public TMP_InputField joinRoomIdInput;

    [Header("UI - Lobby Scene (phòng chờ) - KHÔNG gán tay ở đây")]
    [Tooltip("Các field này được LobbyRoomUI.cs (đặt trong scene LobbyGame) tự đăng ký lúc runtime, vì PhotonManager và object UI nằm khác scene.")]
    private TextMeshProUGUI peopleNumberText; // "2/10" - số người hiện tại / tối đa
    private TextMeshProUGUI roomIdText;       // "Room: ABC123"
    private Button playButton;                // Nút Play - chỉ chủ phòng (Master Client) mới thấy

    public GameObject notifi_BossLeave;
    public TextMeshProUGUI textNotifi_BossLeave;

    [Header("Game Settings")]
    public int minPlayers = 2;
    public int maxPlayers = 10;
    public GameObject[] playerPrefab;

    public int selectedCharacterIndex = 0; // Nhân vật đã chọn ở SelectPanel

    // Lưu tạm thông tin để dùng sau khi kết nối Photon xong
    private string pendingPlayerName;
    private string pendingRoomId;
    private bool isPendingCreateRoom = false;
    private bool isPendingJoinRoom = false;

    [Header("Spawn Settings - GameScene")]
    public Vector2 mapMinBounds = new Vector2(-20, -20);
    public Vector2 mapMaxBounds = new Vector2(20, 20);
    public float spawnSafeDistance = 3f;

    [Header("Spawn Settings - LobbyGame (phòng chờ)")]
    public Vector2 lobbySpawnAreaMin = new Vector2(-5, -5);
    public Vector2 lobbySpawnAreaMax = new Vector2(5, 5);

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
        // Không auto-connect nữa. Chỉ connect khi người chơi bấm Tạo/Vào phòng.
        PhotonNetwork.AutomaticallySyncScene = true;

        SceneManager.sceneLoaded += OnSceneLoaded;

        UpdateUIBasedOnScene();

        // Nếu đang ở scene chọn nhân vật, mở sẵn SelectPanel
        if (selectPanel != null && SceneManager.GetActiveScene().name == "SelectedCharacter")
        {
            ShowSelectPanel();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(
        $"[SCENE LOADED] " +
        $"Name={PhotonNetwork.NickName}, " +
        $"Actor={PhotonNetwork.LocalPlayer.ActorNumber}, " +
        $"Scene={scene.name}, " +
        $"InRoom={PhotonNetwork.InRoom}"
        );
        UpdateUIBasedOnScene();

        if (scene.name == "LobbyGame")
        {
            SetupLobbyRoomUI();

            StartCoroutine(WaitForRoomAndSpawn()); // Delay để đảm bảo UI đã được đăng ký
        }
        else if (scene.name == "GameScene" && PhotonNetwork.InRoom)
        {
            // Sang GameScene thật thì spawn lại (map khác, cần spawn point khác)
            isSpawned = false;
            StartCoroutine(WaitForRoomAndSpawn());
        }
    }
    private IEnumerator WaitForRoomAndSpawn()
    {
        Debug.Log("[SPAWN] Đang chờ vào Room...");

        float timeout = 10f;
        float timer = 0f;

        while (!PhotonNetwork.InRoom && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[SPAWN] Timeout - vẫn chưa vào Room!");
            yield break;
        }

        Debug.Log(
            $"[SPAWN] Đã vào Room: {PhotonNetwork.CurrentRoom.Name}"
        );

        // Đợi thêm 1 frame để scene ổn định
        yield return null;

        SpawnPlayer();
    }

    private void UpdateUIBasedOnScene()
    {
        // Không còn dùng lobbyPanel/roomPanel riêng - scene LobbyGame chỉ có
        // 1 bộ UI phòng chờ (peopleNumberText, roomIdText, playButton),
        // được cập nhật trong SetupLobbyRoomUI() khi scene LobbyGame load xong.
    }

    // ================= CHỌN NHÂN VẬT (SelectPanel) =================

    public void ShowSelectPanel()
    {
        if (selectPanel != null) selectPanel.SetActive(true);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
    }

    // Gọi khi người chơi click chọn 1 nhân vật trong SelectPanel (ví dụ từ CharacterSelection.cs)
    public void SetSelectedCharacter(int index)
    {
        selectedCharacterIndex = index;
    }

    // Nút "Tạo phòng" trong SelectPanel
    public void OnClickOpenCreateRoomPanel()
    {
        if (selectPanel != null) selectPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(true);
    }

    // Nút "Vào phòng" trong SelectPanel
    public void OnClickOpenJoinRoomPanel()
    {
        if (selectPanel != null) selectPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(true);
    }

    // Nút "Hủy" trong CreateRoomPanel hoặc JoinRoomPanel
    public void OnClickCancelPanel()
    {
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        ShowSelectPanel();
    }

    // ================= NÚT "TẠO" TRONG CREATE ROOM PANEL =================
    public void OnClickConfirmCreateRoom()
    {
        if (createPlayerNameInput == null || string.IsNullOrEmpty(createPlayerNameInput.text))
        {
            Debug.LogWarning("Chưa nhập tên người chơi!");
            return;
        }
        if (createRoomIdInput == null || string.IsNullOrEmpty(createRoomIdInput.text))
        {
            Debug.LogWarning("Chưa nhập ID phòng!");
            return;
        }

        pendingPlayerName = createPlayerNameInput.text;
        pendingRoomId = createRoomIdInput.text;
        isPendingCreateRoom = true;
        isPendingJoinRoom = false;

        ConnectAndProceed();
    }

    // ================= NÚT "VÀO" TRONG JOIN ROOM PANEL =================
    public void OnClickConfirmJoinRoom()
    {
        if (joinPlayerNameInput == null || string.IsNullOrEmpty(joinPlayerNameInput.text))
        {
            Debug.LogWarning("Chưa nhập tên người chơi!");
            return;
        }
        if (joinRoomIdInput == null || string.IsNullOrEmpty(joinRoomIdInput.text))
        {
            Debug.LogWarning("Chưa nhập ID phòng!");
            return;
        }

        pendingPlayerName = joinPlayerNameInput.text;
        pendingRoomId = joinRoomIdInput.text;
        isPendingJoinRoom = true;
        isPendingCreateRoom = false;

        ConnectAndProceed();
    }

    // Bắt đầu kết nối Photon. Việc tạo/vào phòng thực sự sẽ chạy ở OnConnectedToMaster()
    private void ConnectAndProceed()
    {
        PhotonNetwork.NickName = pendingPlayerName;
        // Nếu đã kết nối sẵn thì không cần connect lại, mà chạy luôn ProceedCreateOrJoin()
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.JoinedLobby)
        {
            // Đã kết nối sẵn từ trước thì chạy luôn
            ProceedCreateOrJoin();
            return;
        }
        // Nếu chưa kết nối thì connect trước, rồi tạo/vào phòng ở OnConnectedToMaster()
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.ConnectedToMasterServer)
        {
            PhotonNetwork.JoinLobby();
            return;
        }
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Disconnected
        || PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.PeerCreated)
        {
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        Debug.LogWarning("Đang ở trạng thái trung gian, chưa xử lý: " + PhotonNetwork.NetworkClientState);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");

        if (isPendingCreateRoom || isPendingJoinRoom)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");

        if (isPendingCreateRoom || isPendingJoinRoom)
        {
            ProceedCreateOrJoin();
        }
    }

    private void ProceedCreateOrJoin()
    {
        if (isPendingCreateRoom)
        {
            CreateRoom();
        }
        else if (isPendingJoinRoom)
        {
            JoinRoom();
        }
    }

    private void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)maxPlayers;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom(pendingRoomId, roomOptions);
    }

    private void JoinRoom()
    {
        PhotonNetwork.JoinRoom(pendingRoomId);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Tạo phòng thất bại: " + message);
        isPendingCreateRoom = false;
        isPendingJoinRoom = false;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Vào phòng thất bại: " + message);
        isPendingCreateRoom = false;
        isPendingJoinRoom = false;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);

        isPendingCreateRoom = false;
        isPendingJoinRoom = false;
        isSpawned = false; 

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("LobbyGame");
    }

    public void RegisterLobbyRoomUI(TextMeshProUGUI peopleNumber, TextMeshProUGUI roomId, Button play)
    {
        peopleNumberText = peopleNumber;
        roomIdText = roomId;
        playButton = play;

        if (PhotonNetwork.InRoom)
        {
            SetupLobbyRoomUI();
        }
    }
    private void SetupLobbyRoomUI()
    {
        if (roomIdText != null)
            roomIdText.text = "Room: " + PhotonNetwork.CurrentRoom.Name;

        UpdatePlayerCountUI();

        if (playButton != null)
        {
            playButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerCountUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCountUI();
    }

    private void UpdatePlayerCountUI()
    {
        if (SceneManager.GetActiveScene().name != "LobbyGame") return;

        if (peopleNumberText != null)
        {
            peopleNumberText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
        }
        if (PhotonNetwork.IsMasterClient && playButton != null)
        {
            playButton.gameObject.SetActive(true);
            playButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount >= minPlayers;
        }
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return; 
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < minPlayers)
        {
            Debug.LogWarning("Not enough players!");
            return;
        }

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel("GameScene");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        isSpawned = false;

        if (SceneManager.GetActiveScene().name == "LobbyGame" || SceneManager.GetActiveScene().name == "GameScene")
        {
            SceneManager.LoadScene("SelectedCharacter"); 
        }
    }

    private void SpawnPlayer()
    {
        Debug.Log(
        $"[SPAWN CHECK] " +
        $"Name={PhotonNetwork.NickName}, " +
        $"Actor={PhotonNetwork.LocalPlayer.ActorNumber}, " +
        $"InRoom={PhotonNetwork.InRoom}, " +
        $"Ready={PhotonNetwork.IsConnectedAndReady}, " +
        $"Scene={SceneManager.GetActiveScene().name}, " +
        $"isSpawned={isSpawned}"
        );
        if (isSpawned) return;
        if(!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[SPAWN] Chưa ở trong Room!");
            return;
        }
        if (playerPrefab == null || playerPrefab.Length == 0)
        {
            Debug.LogError("Player Prefab is not assigned!");
            return;
        }
        int index = Mathf.Clamp(selectedCharacterIndex, 0, playerPrefab.Length - 1);
        GameObject prefabToSpawn = playerPrefab[index];

        bool isLobbyScene = SceneManager.GetActiveScene().name == "LobbyGame";
        Vector3 spawnPosition = isLobbyScene ? GetLobbySpawnPosition() : GetRandomSpawnPosition();

        GameObject player = PhotonNetwork.Instantiate(prefabToSpawn.name, spawnPosition, Quaternion.identity);
        isSpawned = true;
        Debug.Log(
        $"[SPAWN SUCCESS] {PhotonNetwork.NickName} " +
        $"spawned {player.name}"
        );

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
        float randomX = Random.Range(mapMinBounds.x, mapMaxBounds.x);
        float randomY = Random.Range(mapMinBounds.y, mapMaxBounds.y);

        return new Vector3(randomX, randomY, 0);
    }

    private Vector3 GetLobbySpawnPosition()
    {
        float randomX = Random.Range(lobbySpawnAreaMin.x, lobbySpawnAreaMax.x);
        float randomY = Random.Range(lobbySpawnAreaMin.y, lobbySpawnAreaMax.y);

        return new Vector3(randomX, randomY, 0);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        bool isLobbyScene = SceneManager.GetActiveScene().name == "LobbyGame";
        if (isLobbyScene && !PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ShowNotificationAndLeave("Host has left the room. You will return to the lobby."));
        }
        else
        {
            UpdatePlayerCountUI();
        }
    }

    private IEnumerator ShowNotificationAndLeave(string message)
    {
        if (notifi_BossLeave != null && textNotifi_BossLeave != null)
        {
            textNotifi_BossLeave.text = message;
            notifi_BossLeave.SetActive(true);
        }

        yield return new WaitForSeconds(5f);
        PhotonNetwork.LeaveRoom();
        if (notifi_BossLeave != null)
        {
            notifi_BossLeave.SetActive(false);
        }
    }
}
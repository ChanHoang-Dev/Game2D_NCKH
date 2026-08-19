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
    [Tooltip("Các field này được LobbyGameUI.cs (đặt trong scene LobbyGame) tự đăng ký lúc runtime, vì PhotonManager và object UI nằm khác scene.")]
    private TextMeshProUGUI peopleNumberText; // "2/10" - số người hiện tại / tối đa
    private TextMeshProUGUI roomIdText;       // "Room: ABC123"
    private Button playButton;                // Nút Play - chỉ chủ phòng (Master Client) mới thấy

    // Đánh dấu bộ UI phòng chờ hiện tại đã được đăng ký hay chưa, dùng để tránh
    // OnSceneLoaded setup UI khi field còn null (race condition với LobbyGameUI.Start()).
    private bool lobbyUIRegistered = false;

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
        if (SceneManager.GetActiveScene().name == "SelectedCharacter")
        {
            RebindSelectedCharacterSceneReferences();
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
            // Reset field UI cũ + cờ đăng ký. UI thật sự sẽ do LobbyGameUI.Start() gọi
            // RegisterLobbyRoomUI() đăng ký lại - tại đây KHÔNG gọi SetupLobbyRoomUI() nữa
            // vì field peopleNumberText/roomIdText/playButton chắc chắn đang null lúc này
            // (object UI của scene LobbyGame chưa chắc đã chạy Start() xong), gọi sớm sẽ set
            // vào field null -> không có tác dụng gì, khiến UI hiện giá trị mặc định sai
            // (text "Id" cứng, nút Play hiện sai cho non-host, số người sai).
            peopleNumberText = null;
            roomIdText = null;
            playButton = null;
            lobbyUIRegistered = false;

            StartCoroutine(WaitForRoomAndSpawn()); // Delay để đảm bảo UI đã được đăng ký
        }
        else if (scene.name == "GameScene" && PhotonNetwork.InRoom)
        {
            // Sang GameScene thật thì spawn lại (map khác, cần spawn point khác)
            lobbyUIRegistered = false;
            isSpawned = false;
            StartCoroutine(WaitForRoomAndSpawn());
        }
        else
        {
            lobbyUIRegistered = false;

            if (scene.name == "SelectedCharacter")
            {
                // QUAN TRỌNG: PhotonManager là singleton DontDestroyOnLoad, nhưng selectPanel /
                // createRoomPanel / joinRoomPanel được kéo tay trong Inspector, trỏ tới các object
                // CỤ THỂ của lần load scene "SelectedCharacter" đầu tiên. Mỗi khi scene này bị
                // unload rồi load lại (ví dụ sau khi rời phòng), Unity tạo ra INSTANCE MỚI của các
                // object đó, còn field cũ trong PhotonManager vẫn trỏ tới object đã bị destroy.
                // Gọi SetActive() trên object đã destroy sẽ không có tác dụng gì (đây chính là lý do
                // nút "Tạo phòng"/"Vào phòng" bấm không phản ứng gì sau khi quay lại scene này lần 2).
                // => Phải tìm lại reference mới mỗi lần scene được load.
                RebindSelectedCharacterSceneReferences();
                ShowSelectPanel();
            }
        }
    }

    [Header("SelectedCharacter Scene - Tên GameObject để tự tìm lại reference sau mỗi lần load scene")]
    [Tooltip("Điền đúng tên GameObject trong Hierarchy của scene SelectedCharacter. Dùng để PhotonManager tự GameObject.Find lại panel sau khi scene bị load lại (rời phòng quay về), vì reference cũ trong Inspector sẽ bị mất khi scene unload.")]
    public string selectPanelObjectName = "SelectPanel";
    public string createRoomPanelObjectName = "CreateRoomPanel";
    public string joinRoomPanelObjectName = "JoinRoomPanel";
    public string createPlayerNameInputObjectName = "CreatePlayerNameInput";
    public string createRoomIdInputObjectName = "CreateRoomIdInput";
    public string joinPlayerNameInputObjectName = "JoinPlayerNameInput";
    public string joinRoomIdInputObjectName = "JoinRoomIdInput";

    [Tooltip("Tên GameObject của 2 nút mở panel Tạo phòng / Vào phòng trong SelectPanel. KHÔNG gán OnClick() cho 2 nút này bằng tay trong Inspector - PhotonManager sẽ tự gán bằng code mỗi khi scene load lại, vì gán tay sẽ bị Missing sau khi rời phòng quay về (do object bị tạo lại nhưng OnClick binding cũ trỏ vào instance PhotonManager đã bị 'di cư' sang persistent scene).")]
    public string createRoomButtonObjectName = "tạo phòng";
    public string joinRoomButtonObjectName = "vào phòng";

    public string confirmCreateRoomButtonObjectName = " ";
    public string confirmJoinRoomButtonObjectName = " ";
    public string cancelButtonObjectName = "";

    private void RebindSelectedCharacterSceneReferences()
    {
        selectPanel = FindInActiveSceneByName(selectPanelObjectName);
        createRoomPanel = FindInActiveSceneByName(createRoomPanelObjectName);
        joinRoomPanel = FindInActiveSceneByName(joinRoomPanelObjectName);

        GameObject createNameObj = FindInActiveSceneByName(createPlayerNameInputObjectName);
        GameObject createIdObj = FindInActiveSceneByName(createRoomIdInputObjectName);
        GameObject joinNameObj = FindInActiveSceneByName(joinPlayerNameInputObjectName);
        GameObject joinIdObj = FindInActiveSceneByName(joinRoomIdInputObjectName);

        if (createNameObj != null) createPlayerNameInput = createNameObj.GetComponent<TMP_InputField>();
        if (createIdObj != null) createRoomIdInput = createIdObj.GetComponent<TMP_InputField>();
        if (joinNameObj != null) joinPlayerNameInput = joinNameObj.GetComponent<TMP_InputField>();
        if (joinIdObj != null) joinRoomIdInput = joinIdObj.GetComponent<TMP_InputField>();

        if (selectPanel == null) Debug.LogError($"[PhotonManager] Không tìm thấy GameObject tên '{selectPanelObjectName}' trong scene SelectedCharacter!");
        if (createRoomPanel == null) Debug.LogError($"[PhotonManager] Không tìm thấy GameObject tên '{createRoomPanelObjectName}' trong scene SelectedCharacter!");
        if (joinRoomPanel == null) Debug.LogError($"[PhotonManager] Không tìm thấy GameObject tên '{joinRoomPanelObjectName}' trong scene SelectedCharacter!");

        // Gán OnClick cho 2 nút "Tạo phòng" / "Vào phòng" BẰNG CODE, không dựa vào binding
        // Inspector đã bake sẵn trong file scene. Lý do: object nút này bị Unity destroy và tạo
        // lại mới mỗi khi scene SelectedCharacter load lại (sau khi rời phòng), còn OnClick binding
        // cũ trong file scene trỏ tới PhotonManager instance lúc thiết kế - instance đó tuy vẫn
        // đang sống (nhờ DontDestroyOnLoad) nhưng không còn nằm trong scene SelectedCharacter nữa,
        // nên Unity không resolve lại được -> OnClick hiện "Missing", bấm không có phản ứng gì.
        // Gán bằng code luôn dùng đúng Instance đang chạy nên không bao giờ bị mất kết nối.
        GameObject createBtnObj = FindInActiveSceneByName(createRoomButtonObjectName);
        GameObject joinBtnObj = FindInActiveSceneByName(joinRoomButtonObjectName);

        GameObject confirmCreateBtnObj = FindInActiveSceneByName(confirmCreateRoomButtonObjectName);
        GameObject confirmJoinBtnObj = FindInActiveSceneByName(confirmJoinRoomButtonObjectName);

        GameObject CancelPanel = FindInActiveSceneByName(cancelButtonObjectName);

        if (createBtnObj != null)
        {
            Button createBtn = createBtnObj.GetComponent<Button>();
            if (createBtn != null)
            {
                createBtn.onClick.RemoveAllListeners(); // tránh add trùng listener nếu rebind nhiều lần
                createBtn.onClick.AddListener(OnClickOpenCreateRoomPanel);
            }
        }
        else
        {
            Debug.LogError($"[PhotonManager] Không tìm thấy nút tên '{createRoomButtonObjectName}' trong scene SelectedCharacter!");
        }

        if (joinBtnObj != null)
        {
            Button joinBtn = joinBtnObj.GetComponent<Button>();
            if (joinBtn != null)
            {
                joinBtn.onClick.RemoveAllListeners();
                joinBtn.onClick.AddListener(OnClickOpenJoinRoomPanel);
            }
        }
        else
        {
            Debug.LogError($"[PhotonManager] Không tìm thấy nút tên '{joinRoomButtonObjectName}' trong scene SelectedCharacter!");
        }
        if (confirmCreateBtnObj != null)
        {
            Button confirmCreateBtn = confirmCreateBtnObj.GetComponent<Button>();
            if (confirmCreateBtn != null)
            {
                confirmCreateBtn.onClick.RemoveAllListeners(); // tránh add trùng listener nếu rebind nhiều lần
                confirmCreateBtn.onClick.AddListener(OnClickConfirmCreateRoom);
            }
        }
        else
        {
            Debug.LogError($"[PhotonManager] Không tìm thấy nút tên '{confirmCreateRoomButtonObjectName}' trong scene SelectedCharacter!");
        }
        if (confirmJoinBtnObj != null)
        {
            Button confirmJoinBtn = confirmJoinBtnObj.GetComponent<Button>();
            if (confirmJoinBtn != null)
            {
                confirmJoinBtn.onClick.RemoveAllListeners();
                confirmJoinBtn.onClick.AddListener(OnClickConfirmJoinRoom);
            }
        }
        else
        {
            Debug.LogError($"[PhotonManager] Không tìm thấy nút tên '{confirmJoinRoomButtonObjectName}' trong scene SelectedCharacter!");
        }
        if (CancelPanel!= null)
        {
            Button cancelBtn = CancelPanel.GetComponent<Button>();
            if (cancelBtn != null)
            {
                cancelBtn.onClick.RemoveAllListeners();
                cancelBtn.onClick.AddListener(OnClickCancelPanel);
            }
        }
        else
        {
            Debug.LogError($"[PhotonManager] Không tìm thấy nút tên '{cancelButtonObjectName}' trong scene SelectedCharacter!");
        }
    }

    // Tìm GameObject theo tên trong scene hiện tại, kể cả khi nó đang bị inactive
    // (GameObject.Find thường KHÔNG tìm được object inactive, nên phải duyệt thủ công).
    private GameObject FindInActiveSceneByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            Transform found = FindChildRecursive(root.transform, objectName);
            if (found != null) return found.gameObject;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }

        return null;
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

    // Được LobbyGameUI.Start() gọi ngay khi scene LobbyGame load xong, trên MỌI client
    // (host lẫn client thường). Đây là nơi DUY NHẤT khởi tạo UI phòng chờ.
    public void RegisterLobbyRoomUI(TextMeshProUGUI peopleNumber, TextMeshProUGUI roomId, Button play)
    {
        peopleNumberText = peopleNumber;
        roomIdText = roomId;
        playButton = play;
        lobbyUIRegistered = true;

        // Dừng coroutine chờ cũ (nếu có) để tránh set UI 2 lần chồng nhau
        StopAllCoroutines_LobbyUIWait();

        if (PhotonNetwork.InRoom)
        {
            SetupLobbyRoomUI();
        }
        else
        {
            // Trường hợp UI đăng ký xong nhưng client chưa kịp InRoom == true
            // (thường xảy ra ở client thường do độ trễ mạng khi AutomaticallySyncScene
            // tự load scene theo host). Thay vì bỏ qua im lặng như code cũ, chủ động
            // đợi cho tới khi InRoom == true rồi mới setup, đảm bảo UI luôn được set đúng.
            lobbyUIWaitCoroutine = StartCoroutine(WaitInRoomThenSetupLobbyUI());
        }
    }

    private Coroutine lobbyUIWaitCoroutine;

    private void StopAllCoroutines_LobbyUIWait()
    {
        if (lobbyUIWaitCoroutine != null)
        {
            StopCoroutine(lobbyUIWaitCoroutine);
            lobbyUIWaitCoroutine = null;
        }
    }

    private IEnumerator WaitInRoomThenSetupLobbyUI()
    {
        float timeout = 10f;
        float timer = 0f;

        while (!PhotonNetwork.InRoom && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        lobbyUIWaitCoroutine = null;

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[LOBBY UI] Timeout chờ InRoom để setup UI phòng chờ!");
            yield break;
        }

        SetupLobbyRoomUI();
    }

    private void SetupLobbyRoomUI()
    {
        if (!lobbyUIRegistered) return; // Field có thể còn null nếu chưa đăng ký, tránh set nhầm

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
        if (!lobbyUIRegistered) return; // Field có thể còn null nếu UI chưa đăng ký xong

        if (peopleNumberText != null)
        {
            peopleNumberText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
        }
        if (playButton != null)
        {
            playButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            if (PhotonNetwork.IsMasterClient)
            {
                playButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount >= minPlayers;
            }
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
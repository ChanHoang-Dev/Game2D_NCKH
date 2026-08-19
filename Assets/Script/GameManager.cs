using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Cainos.PixelArtTopDown_Basic
{
    public class GameManager : MonoBehaviourPunCallbacks
    {
        public static GameManager Instance;

        [Header("Game Settings")]
        public float gameDuration = 120f;
        public float waitingTime = 5f; // Thời gian chờ tất cả người chơi vào
        public Light2D globalLight;

        [Header("UI")]
        public GameObject TimeAndCountCanvas;
        public GameObject endGameCanvas;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI ghostCountText;
        public TextMeshProUGUI humanCountText;
        [SerializeField] private Button returnSelectionSceneBtn;

        [Header("Host Left After Game End")]
        [Tooltip("Số giây đếm ngược trước khi tự động rời phòng nếu host rời sau khi game đã kết thúc.")]
        public float hostLeftReturnDelay = 10f;

        private double gameStartTime; // Thời gian bắt đầu game (PhotonNetwork.Time)
        private bool gameStarted = false;
        private bool waitingForPlayers = true;
        private float currentTime;
        private bool gameEnded = false;

        private List<PlayerController> allPlayers = new List<PlayerController>();
        private int ghostCount = 0;
        private int humanCount = 0;

        private Coroutine gameCoroutine;
        private Coroutine hostLeftCountdownCoroutine;
        private string baseResultText = ""; // Nội dung thắng/thua gốc, dùng để ghép thêm dòng countdown

        // Đánh dấu master client ĐÃ từng bị switch (host cũ đã rời) TRƯỚC KHI biết game đã kết thúc
        // hay chưa. Lý do cần cờ độc lập này: EndGameRPC là RPC async, còn OnMasterClientSwitched là
        // event của chính Photon room - thứ tự 2 sự kiện này tới trên từng máy KHÔNG được đảm bảo cố
        // định. Nếu chỉ dựa vào "OnMasterClientSwitched chạy trong lúc gameEnded == true" để bắt đầu
        // countdown, client nào nhận được switch event TRƯỚC KHI EndGameRPC kịp tới sẽ bỏ lỡ hoàn
        // toàn countdown (lúc switch chạy, gameEnded vẫn false -> rơi vào nhánh "game chưa kết thúc").
        // Cờ này ghi nhận sự kiện switch xảy ra bất kể lúc đó gameEnded là gì, để EndGameRPC có thể
        // tự kiểm tra lại và bắt đầu countdown ngay khi nó chạy, dù chạy sau switch bao lâu.
        private bool masterSwitchedWhileNotEndedYet = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            TimeAndCountCanvas.SetActive(true);
            endGameCanvas.SetActive(false);

            // Set global light to dark
            if (globalLight != null)
            {
                globalLight.intensity = 0f; // Tối đen
            }

            // Only master client starts the game countdown
            if (PhotonNetwork.IsMasterClient)
            {
                // Wait for all players to load, then start game
                gameCoroutine = StartCoroutine(WaitAndStartGame());
            }
            returnSelectionSceneBtn.onClick.AddListener(ReturnToLobby);
        }

        private void Update()
        {
            if (gameEnded) return;

            // Show waiting message
            if (waitingForPlayers)
            {
                timeText.text = "Waiting for players...";
                return;
            }

            // Calculate current time based on PhotonNetwork.Time (synchronized)
            if (gameStarted)
            {
                double elapsedTime = PhotonNetwork.Time - gameStartTime;
                currentTime = gameDuration - (float)elapsedTime;

                // Update timer
                if (currentTime > 0)
                {
                    UpdateTimeText(currentTime);
                }
                else
                {
                    currentTime = 0;
                    UpdateTimeText(currentTime);

                    // Only master client calls EndGame to sync
                    if (PhotonNetwork.IsMasterClient)
                    {
                        EndGame();
                    }
                }
            }
        }

        void UpdateTimeText(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time - minutes * 60);
            timeText.text = string.Format("{0:0}:{1:00}", minutes, seconds);

        }

        public void RegisterPlayer(PlayerController player)
        {
            if (!allPlayers.Contains(player))
            {
                allPlayers.Add(player);
            }
            UpdatePlayerCounts();
        }

        public void UnregisterPlayer(PlayerController player)
        {
            if (allPlayers.Contains(player))
            {
                allPlayers.Remove(player);
            }
            UpdatePlayerCounts();
        }


        [PunRPC]
        public void SelectRandomGhost()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            // Wait for all players to be registered
            StartCoroutine(WaitAndSelectGhost());
        }

        private IEnumerator WaitAndStartGame()
        {
            // Wait for all players to spawn and register
            yield return new WaitForSeconds(waitingTime);

            if (!PhotonNetwork.IsMasterClient) yield break;// Chỉ master client mới thực hiện việc chọn ma và bắt đầu game
            // Select random ghost
            yield return StartCoroutine(WaitAndSelectGhost());

            if (!PhotonNetwork.IsMasterClient) yield break;// Chỉ master client mới thực hiện việc chọn ma và bắt đầu game

            // Sync game start time using Photon's network time
            gameStartTime = PhotonNetwork.Time;

            // Send RPC to all clients to start the game
            photonView.RPC("StartGameCountdown", RpcTarget.AllBuffered, gameStartTime);
        }

        [PunRPC]
        private void StartGameCountdown(double startTime)
        {
            gameStartTime = startTime;
            gameStarted = true;
            waitingForPlayers = false;

            Debug.Log("Game started at network time: " + startTime);
        }

        private IEnumerator WaitAndSelectGhost()
        {
            yield return new WaitForSeconds(1f);

            // Find all player objects in the scene
            PlayerController[] players = FindObjectsOfType<PlayerController>();

            if (players.Length > 0)
            {
                int randomIndex = Random.Range(0, players.Length);
                PlayerController selectedGhost = players[randomIndex];

                // Use RPC to set ghost on all clients
                selectedGhost.photonView.RPC("SetAsGhost", RpcTarget.AllBuffered);
            }
        }

        public void UpdatePlayerCounts()
        {
            ghostCount = 0;
            humanCount = 0; // Chỉ đếm người còn sống (chưa bị bắt)

            foreach (PlayerController player in allPlayers)
            {
                if (player.IsGhost)
                    ghostCount++;
                else if (!player.IsEliminated) // Chỉ đếm người chưa bị loại
                    humanCount++;
            }

            if (ghostCountText != null)
                ghostCountText.text = "Ghosts: " + ghostCount;

            if (humanCountText != null)
                humanCountText.text = "Humans Alive: " + humanCount; // Số người còn sống

            // Check win condition
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            if (gameEnded) return;// Nếu game đã kết thúc, không kiểm tra điều kiện thắng thua nữa
            if(!gameStarted) return;// Nếu game chưa bắt đầu, không kiểm tra điều kiện thắng thua

            int totalPlayers = ghostCount + humanCount;
            if (totalPlayers == 0) return;

            // If all humans are eliminated (only ghosts remain), ghosts win
            if (humanCount == 0 && ghostCount > 0)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    EndGame();
                }
            }
            else if (ghostCount == 0 && humanCount > 0)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    EndGame();
                }
            }
        }

        private void EndGame()
        {
            if (gameEnded) return;

            // Use RPC to end game for all clients
            photonView.RPC("EndGameRPC", RpcTarget.AllBuffered);
        }

        [PunRPC]
        private void EndGameRPC()
        {
            if (gameEnded) return;
            gameEnded = true;

            endGameCanvas.SetActive(true);
            TimeAndCountCanvas.SetActive(false);

            // Tính lại số lượng ghost/human còn sống NGAY tại thời điểm này (không dùng biến
            // ghostCount/humanCount cũ vì chúng có thể chưa kịp cập nhật đồng bộ trên từng client
            // do EndGameRPC là RPC async - đây chính là nguyên nhân gây sai kết quả trước đây).
            int aliveGhosts = 0;
            int aliveHumans = 0;
            foreach (PlayerController player in allPlayers)
            {
                if (player.IsGhost)
                    aliveGhosts++;
                else if (!player.IsEliminated)
                    aliveHumans++;
            }

            // Xác định phe thắng DUY NHẤT MỘT LẦN, luật rõ ràng không mơ hồ:
            // - Ghost thắng nếu không còn human nào sống sót (bắt hết người trước khi hết giờ).
            // - Human thắng trong MỌI trường hợp khác còn lại (hết giờ mà vẫn còn ít nhất 1 human sống,
            //   hoặc ghost đã bị loại hết - dù luật hiện tại ghost không bị loại nên case này hiếm xảy ra).
            bool ghostWins = (aliveHumans == 0);

            // Determine winner cho local player
            PlayerController localPlayer = null;
            foreach (PlayerController player in allPlayers)
            {
                if (player.photonView.IsMine)
                {
                    localPlayer = player;
                    break;
                }
            }

            if (localPlayer != null)
            {
                if (localPlayer.IsGhost)
                {
                    // Ma thắng khi bắt hết người; ma thua khi hết giờ mà vẫn còn người sống.
                    resultText.text = ghostWins ? "You Win!" : "You Lose!";
                }
                else
                {
                    // Người bị bắt (bị loại) luôn luôn thua, bất kể phe nào thắng chung.
                    // Người còn sống khi game kết thúc = thắng (vì ghostWins chỉ true khi
                    // aliveHumans == 0, nên nếu người này còn sống thì chắc chắn ghostWins == false).
                    resultText.text = localPlayer.IsEliminated ? "You Lose!" : "You Win!";
                }
            }

            // Lưu lại nội dung thắng/thua gốc để dùng khi ghép dòng countdown (nếu host rời sau đó)
            baseResultText = resultText.text;

            // Disable all player movement
            foreach (PlayerController player in allPlayers)
            {
                if (player.photonView.IsMine)
                {
                    player.enabled = false;
                }
            }

            // Xử lý trường hợp master client đã bị switch (host cũ rời) TRƯỚC KHI RPC EndGameRPC
            // kịp chạy trên máy này. Nếu không kiểm tra ở đây, client này sẽ không bao giờ tự bắt
            // đầu countdown, vì OnMasterClientSwitched của nó đã chạy từ trước lúc gameEnded == false,
            // và sẽ không có lần switch nào khác xảy ra nữa để kích hoạt lại.
            if (masterSwitchedWhileNotEndedYet)
            {
                TryStartHostLeftCountdown();
            }
        }

        // Bắt đầu coroutine đếm ngược 10s rồi rời phòng, dùng chung cho cả 2 trường hợp:
        // 1) OnMasterClientSwitched chạy trong lúc gameEnded đã true (trường hợp thông thường).
        // 2) EndGameRPC chạy sau khi phát hiện switch đã xảy ra từ trước (trường hợp race condition).
        private void TryStartHostLeftCountdown()
        {
            if (hostLeftCountdownCoroutine == null && PhotonNetwork.InRoom)
            {
                hostLeftCountdownCoroutine = StartCoroutine(HostLeftAfterGameEndCountdown());
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            foreach (PlayerController player in allPlayers)
            {
                if (player.photonView.Owner == otherPlayer)
                {
                    // Không gọi PhotonNetwork.Destroy() ngay tại đây. Lý do: OnPlayerLeftRoom và
                    // OnMasterClientSwitched có thể tới gần như cùng lúc nhưng KHÔNG đảm bảo thứ tự,
                    // và ngay cả khi PhotonNetwork.IsMasterClient (cờ phía client) đã báo true, server
                    // có thể vẫn chưa hoàn tất xác nhận quyền MasterClient cho thao tác network-remove
                    // này, dẫn tới lỗi "Client is neither owner nor MasterClient taking over...".
                    // Trì hoãn 1 frame để quyền MasterClient kịp ổn định trước khi thử Destroy.
                    if (player != null && player.photonView != null)
                    {
                        StartCoroutine(DestroyLeftPlayerNextFrame(player.gameObject));
                    }
                    break;
                }
            }
        }

        private IEnumerator DestroyLeftPlayerNextFrame(GameObject playerObj)
        {
            yield return null; // đợi 1 frame để PhotonNetwork.IsMasterClient / quyền sở hữu ổn định

            if (playerObj == null) yield break; // object có thể đã bị hủy bởi client khác trong lúc chờ

            if (!PhotonNetwork.IsMasterClient) yield break; // không còn là master nữa thì không hủy

            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if (pv == null) yield break;

            // Vẫn có thể thất bại trong trường hợp hiếm (ví dụ chính máy này cũng đang trong quá
            // trình rời phòng). Bọc try-catch để không log lỗi ồn ào, không ảnh hưởng luồng game -
            // object rác này sẽ được Photon tự dọn khi phòng đóng hoặc do client khác thử lại.
            try
            {
                PhotonNetwork.Destroy(playerObj);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GameManager] Không thể destroy player object đã rời phòng (sẽ được dọn sau): " + e.Message);
            }
        }
        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log("Master client switched to: " + newMasterClient.NickName);

            // Trường hợp game ĐÃ kết thúc (đang ở màn hình thắng/thua) và host cũ vừa rời:
            // tất cả client còn lại (kể cả host mới) sẽ đếm ngược rồi tự rời phòng,
            // vì trận đã xong nên không cần tiếp tục giữ phòng.
            if (gameEnded)
            {
                TryStartHostLeftCountdown();
                return;
            }

            // gameEnded vẫn đang false tại đây, nhưng có thể EndGameRPC đã được gửi đi và đang
            // trên đường tới (chỉ chưa được xử lý xong trên máy này). Đánh dấu lại để EndGameRPC,
            // khi nó thực sự chạy, biết rằng switch đã xảy ra và cần bắt đầu countdown ngay lập tức
            // thay vì chờ một OnMasterClientSwitched khác (có thể không bao giờ tới nữa).
            masterSwitchedWhileNotEndedYet = true;

            // Trường hợp game CHƯA kết thúc: Photon đã tự chuyển master client,
            // ở đây chỉ cần đảm bảo logic game (đếm giờ/chọn ma) tiếp tục chạy đúng
            // trên master client mới, tránh crash hoặc đứng game.
            if (PhotonNetwork.IsMasterClient)
            {
                if (!gameStarted)
                {
                    if (gameCoroutine != null)
                    {
                        StopCoroutine(gameCoroutine);// Dừng coroutine cũ nếu nó đang chạy
                    }
                    gameCoroutine = StartCoroutine(WaitAndStartGame());
                }// Bắt đầu lại quá trình chờ và chọn ma nếu game chưa bắt đầu
                else
                {
                    UpdatePlayerCounts();// Nếu game đã bắt đầu, cập nhật lại số lượng người chơi để kiểm tra điều kiện thắng thua (trường hợp chủ phòng rời đi giữa chừng)
                }
            }
        }

        private IEnumerator HostLeftAfterGameEndCountdown()
        {
            float remaining = hostLeftReturnDelay;

            while (remaining > 0f)
            {
                // Nếu trong lúc đếm ngược mà mình đã rời phòng rồi (ví dụ tự bấm nút Back),
                // hoặc client không còn kết nối, thì dừng ngay, không cố LeaveRoom() lần nữa.
                if (!PhotonNetwork.InRoom)
                {
                    hostLeftCountdownCoroutine = null;
                    yield break;
                }

                if (resultText != null)
                {
                    string suffix = "\nHost left. Returning to menu in " + Mathf.CeilToInt(remaining) + "s...";
                    resultText.text = string.IsNullOrEmpty(baseResultText) ? suffix.TrimStart('\n') : baseResultText + suffix;
                }

                yield return new WaitForSeconds(1f);
                remaining -= 1f;
            }

            hostLeftCountdownCoroutine = null;

            // Guard cuối cùng: chỉ gọi LeaveRoom() nếu vẫn thật sự đang ở trong phòng.
            // Đây là nguyên nhân của lỗi "Operation LeaveRoom (254) not called..." trước đây -
            // coroutine chạy hết 10s nhưng client đã tự rời phòng từ trước (do bấm Back, hoặc
            // do một luồng khác cũng gọi LeaveRoom), dẫn tới gọi LeaveRoom() lần 2 khi client
            // đang ở trạng thái "Leaving" hoặc đã "Disconnected".
            if (PhotonNetwork.InRoom)
            {
                if (resultText != null && !string.IsNullOrEmpty(baseResultText))
                {
                    resultText.text = baseResultText + "\nReturning to menu...";
                }

                PhotonNetwork.LeaveRoom();
            }
        }

        public void ReturnToLobby()
        {
            // Nếu đang có coroutine đếm ngược do host rời, hủy nó trước khi tự rời phòng bằng nút Back,
            // để tránh việc coroutine gọi LeaveRoom() thêm 1 lần nữa sau khi mình đã rời rồi.
            if (hostLeftCountdownCoroutine != null)
            {
                StopCoroutine(hostLeftCountdownCoroutine);
                hostLeftCountdownCoroutine = null;
            }

            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }
    }
}
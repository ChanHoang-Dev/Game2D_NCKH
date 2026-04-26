using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.Rendering.Universal;

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

        private double gameStartTime; // Thời gian bắt đầu game (PhotonNetwork.Time)
        private bool gameStarted = false;
        private bool waitingForPlayers = true;
        private float currentTime;
        private bool gameEnded = false;

        private List<PlayerController> allPlayers = new List<PlayerController>();
        private int ghostCount = 0;
        private int humanCount = 0;

        private Coroutine gameCoroutine;

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

            // Determine winner
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
                // Nếu bị loại = thua
                if (localPlayer.IsEliminated)
                {
                    resultText.text = "You Lose!";
                }
                // Nếu là ma và còn người sống =0  ma thắng
                else if (localPlayer.IsGhost && humanCount == 0)
                {
                    resultText.text = "You Win!";
                }
                // Nếu là người và hết giờ mà còn sống = người thắng
                else if (!localPlayer.IsGhost && humanCount > 0)
                {
                    resultText.text = "You Win!";
                }
                // Các trường hợp khác
                else if (ghostCount > humanCount)
                {
                    resultText.text = localPlayer.IsGhost ? "You Win!" : "You Lose!";
                }
                else if (humanCount > ghostCount)
                {
                    resultText.text = !localPlayer.IsGhost && !localPlayer.IsEliminated ? "You Win!" : "You Lose!";
                }
                else
                {
                    resultText.text = "Draw!";
                }
            }

            // Disable all player movement
            foreach (PlayerController player in allPlayers)
            {
                if (player.photonView.IsMine)
                {
                    player.enabled = false;
                }
            }
        }
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            PlayerController leftPlayer = null;
            foreach (PlayerController player in allPlayers)
            {
                if (player.photonView.Owner == otherPlayer)
                {
                    if (PhotonNetwork.IsMasterClient && player.photonView != null)
                    {
                        PhotonNetwork.Destroy(player.gameObject);
                    }
                    break;
                }
            }
        }
        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log("Master client switched to: " + newMasterClient.NickName);

            if (PhotonNetwork.IsMasterClient)
            {
                // If the new master client is the local player, check if the game has already started
                if (!gameStarted)
                {
                    if (gameCoroutine != null)
                    {
                        StopCoroutine(gameCoroutine);// Dừng coroutine cũ nếu nó đang chạy
                    }
                    StartCoroutine(WaitAndStartGame());
                }// Bắt đầu lại quá trình chờ và chọn ma nếu game chưa bắt đầu
                else if (!gameEnded)
                {
                    UpdatePlayerCounts();// Nếu game đã bắt đầu, cập nhật lại số lượng người chơi để kiểm tra điều kiện thắng thua (trường hợp chủ phòng rời đi giữa chừng)
                }
            }
        }
        public void ReturnToLobby()
        {
            PhotonNetwork.LeaveRoom();
        }
    }
}
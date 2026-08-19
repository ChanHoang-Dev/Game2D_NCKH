using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyGameUI : MonoBehaviour
{
    [Header("Kéo 4 object trong scene LobbyGame vào đây")]
    public TextMeshProUGUI peopleNumberText; 
    public TextMeshProUGUI roomIdText;       
    public Button playButton;          

    public Button backButton;      

    private void Start()
    {
        if (PhotonManager.Instance == null)
        {
            Debug.LogError("Không tìm thấy PhotonManager.Instance! Kiểm tra scene đầu tiên có PhotonManager chưa.");
            return;
        }
        if (peopleNumberText == null) Debug.LogError("[LobbyGameUI] Chưa gán peopleNumberText trong Inspector!");
        if (roomIdText == null) Debug.LogError("[LobbyGameUI] Chưa gán roomIdText trong Inspector!");
        if (playButton == null) Debug.LogError("[LobbyGameUI] Chưa gán playButton trong Inspector!");

        PhotonManager.Instance.RegisterLobbyRoomUI(peopleNumberText, roomIdText, playButton);

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PhotonManager.Instance.StartGame);
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(PhotonManager.Instance.LeaveRoom);
        }
    }
}
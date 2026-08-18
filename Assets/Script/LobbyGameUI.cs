using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyGameUI : MonoBehaviour
{
    [Header("Kéo 3 object trong scene LobbyGame vào đây")]
    public TextMeshProUGUI peopleNumberText; 
    public TextMeshProUGUI roomIdText;       
    public Button playButton;                

    private void Start()
    {
        if (PhotonManager.Instance == null)
        {
            Debug.LogError("Không tìm thấy PhotonManager.Instance! Kiểm tra scene đầu tiên có PhotonManager chưa.");
            return;
        }

        // Đăng ký ngược UI này vào PhotonManager
        PhotonManager.Instance.RegisterLobbyRoomUI(peopleNumberText, roomIdText, playButton);

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PhotonManager.Instance.StartGame);
        }
    }
}
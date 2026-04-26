using System;
using Cainos.PixelArtTopDown_Basic;
using UnityEngine;
using UnityEngine.UI;

public class UiManagerButton : MonoBehaviour
{
    public Button createRoombtn;
    public Button joinRoombtn;

    public Button startGamebtn;

    public Button leaveGamebtn;

    public Button continueBtn;

    public Button select_RightBtn;
    public Button select_LeftBtn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (createRoombtn != null)
        {
            createRoombtn.onClick.RemoveAllListeners();
            createRoombtn.onClick.AddListener(OnCreateRoomButtonClicked);
        }
        if (joinRoombtn != null)
        {
            joinRoombtn.onClick.RemoveAllListeners();
            joinRoombtn.onClick.AddListener(OnJoinRoomButtonClicked);
        }
        if (continueBtn != null)
        {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(OnContinueButtonClicked);
        }
        if (startGamebtn != null)
        {
            startGamebtn.onClick.RemoveAllListeners();
            startGamebtn.onClick.AddListener(OnStartGameButtonClicked);
        }
        if (leaveGamebtn != null)
        {
            leaveGamebtn.onClick.RemoveAllListeners();
            leaveGamebtn.onClick.AddListener(OnLeaveGameButtonClicked);
        }
        if (select_RightBtn != null)
        {
            select_RightBtn.onClick.RemoveAllListeners();
            select_RightBtn.onClick.AddListener(OnSelectRightButtonClicked);
        }
        if (select_LeftBtn != null)
        {
            select_LeftBtn.onClick.RemoveAllListeners();
            select_LeftBtn.onClick.AddListener(OnSelectLeftButtonClicked);
        }
    }

    private void OnSelectLeftButtonClicked()
    {
        CharacterSelection characterSelection = FindObjectOfType<CharacterSelection>();
        if (characterSelection != null)
        {
            characterSelection.Previous();
        }
    }

    private void OnSelectRightButtonClicked()
    {
        CharacterSelection characterSelection = FindObjectOfType<CharacterSelection>();
        if (characterSelection != null)
        {
            characterSelection.Next();
        }
    }

    private void OnLeaveGameButtonClicked()
    {
        if (PhotonManager.Instance != null)
        {
            PhotonManager.Instance.LeaveRoom();
        }
    }

    private void OnStartGameButtonClicked()
    {
        if (PhotonManager.Instance != null)
        {
            PhotonManager.Instance.StartGame();
        }
    }

    private void OnJoinRoomButtonClicked()
    {
        if (PhotonManager.Instance != null)
            PhotonManager.Instance.PrepareJoinRoom();
    }

    private void OnCreateRoomButtonClicked()
    {
        if (PhotonManager.Instance != null)
            PhotonManager.Instance.PrepareCreateRoom();
    }
    private void OnContinueButtonClicked()
    {
        if (PhotonManager.Instance != null)
        {
            PhotonManager.Instance.ContinueToCharacterSelection();
        }
    }
    // Update is called once per frame
    void Update()
    {

    }
}

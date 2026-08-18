using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharacterItem : MonoBehaviour
{
    [Header("Character Info")]
    public int characterIndex; // Index của nhân vật này, phải khớp với thứ tự trong PhotonManager.playerPrefab
    public Sprite characterSprite; // Ảnh nhân vật, dùng để hiển thị lên preview bên phải

    [Header("Reference")]
    public CharacterSelection characterSelection; // Kéo object quản lý chung (CharacterSelection) vào đây

    [Header("Highlight khi được chọn (không bắt buộc)")]
    public GameObject selectedHighlight; // Ví dụ viền/khung sáng quanh ô, ẩn mặc định

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClickSelect);

        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(false);
        }
    }

    private void OnClickSelect()
    {
        if (characterSelection == null)
        {
            Debug.LogWarning("CharacterItem chưa gán CharacterSelection!");
            return;
        }

        characterSelection.SelectCharacter(characterIndex, characterSprite);

        // Bỏ highlight tất cả các ô khác, chỉ bật ô này
        RefreshAllHighlights();
    }

    private void RefreshAllHighlights()
    {
        // Tìm tất cả CharacterItem trong scene (kể cả đang inactive nếu cần) để tắt highlight cũ
        CharacterItem[] allItems = FindObjectsByType<CharacterItem>(FindObjectsSortMode.None);
        foreach (CharacterItem item in allItems)
        {
            if (item.selectedHighlight != null)
            {
                item.selectedHighlight.SetActive(item == this);
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [Header("Preview")]
    public Image previewImage; // Ô trống bên phải, hiển thị sprite to của nhân vật đã chọn

    public int currentIndex = -1; // Chưa chọn nhân vật nào lúc đầu

    [Header("Optional")]
    public bool hidePreviewWhenNoneSelected = true;

    private void Start()
    {
        if (hidePreviewWhenNoneSelected && previewImage != null && currentIndex < 0)
        {
            previewImage.enabled = false; // Ẩn ô preview cho tới khi chọn nhân vật
        }
    }

    // Gọi từ CharacterItem khi người chơi click vào 1 nhân vật trong danh sách
    public void SelectCharacter(int index, Sprite characterSprite)
    {
        currentIndex = index;

        if (previewImage != null)
        {
            previewImage.sprite = characterSprite;
            previewImage.enabled = true;
            previewImage.preserveAspect = true;
        }

        // Lưu luôn vào PhotonManager để dùng lúc spawn player
        if (PhotonManager.Instance != null)
        {
            PhotonManager.Instance.SetSelectedCharacter(index);
        }
    }
}
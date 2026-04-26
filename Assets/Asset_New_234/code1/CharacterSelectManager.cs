using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("UI Preview")]
    public Image previewImage;

    [Header("Character Data")]
    public Sprite[] characterSprites;

    [Header("Highlight")]
    public GameObject[] highlights;

    private int currentIndex = -1;

    public void SelectCharacter(int index)
    {
        // Đổi ảnh preview
        previewImage.sprite = characterSprites[index];

        // Tắt hết highlight
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].SetActive(false);
        }

        // Bật highlight cho tướng đang chọn
        highlights[index].SetActive(true);

        currentIndex = index;

        Debug.Log("Selected character: " + index);
    }
}
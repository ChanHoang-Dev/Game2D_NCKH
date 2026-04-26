using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public Image fadeImage; 
    public float fadeDuration = 1f; 

    public GameObject CreditsPanel;

    // --- DÒNG MỚI THÊM VÀO ĐÂY ---
    public GameObject settingPanel; // Biến này để nhận cái bảng Setting từ Unity

    public void PlayGame()
    {
        StartCoroutine(FadeAndLoadScene("GamePlay")); 
    }

    IEnumerator FadeAndLoadScene(string sceneName)
    {
        float elapsedTime = 0f;
        Color c = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsedTime / fadeDuration); 
            fadeImage.color = c;
            yield return null; 
        }

        SceneManager.LoadScene(sceneName); 
    }

    public void QuitGame()
    {
        Debug.Log("Đã thoát Game!"); 
        Application.Quit(); 
    }
    public void OpenCredits()
    {
        CreditsPanel.SetActive(true); 
    }
    public void CloseCredits()
    {
        CreditsPanel.SetActive(false); 
    }
    // --- 2 HÀM MỚI ĐỂ BẬT/TẮT SETTING ---
    public void OpenSetting()
    {
        settingPanel.SetActive(true); // Hiện bảng Setting lên
    }

    public void CloseSetting()
    {
        settingPanel.SetActive(false); // Giấu bảng Setting đi
    }
}
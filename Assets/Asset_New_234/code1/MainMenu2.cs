using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu2 : MonoBehaviour
{
    public GameObject taophongPanel;

    public GameObject vaophongPanel;

    public void StartGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu");
    }



    public void OpenTaophongPanel()
    {
        taophongPanel.SetActive(true);
    }

    public void CloseTaophongPanel()
    {
        taophongPanel.SetActive(false);
    }

    public void OpenVaophongPanel()
    {
        vaophongPanel.SetActive(true);
    }

    public void CloseVaophongPanel()
    {
        vaophongPanel.SetActive(false);
    }

}

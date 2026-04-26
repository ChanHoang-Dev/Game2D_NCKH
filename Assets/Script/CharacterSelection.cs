using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [Header("UI")]
    public Image[] chars;
    public Button select_LeftBtn;
    public Button select_RightBtn;


    public int currentIndex = 0;

    Vector2 bigSize = new Vector2(200,200);
    Vector2 smallSize = new Vector2(100,100);

    Vector2 leftHidePos = new Vector2(-200,0);
    Vector2 centerPos = new Vector2(0,0);
    Vector2 rightPos = new Vector2(200,0);

    Color bigColor = Color.white;
    Color smallColor = new Color(1,1,1,0.4f);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateView();
    }

    public void Next()
    {
        if(currentIndex < chars.Length - 1)
        {
            currentIndex++;
            UpdateView();
        }
    }
    public void Previous()
    {
        if(currentIndex > 0)
        {
            currentIndex--;
            UpdateView();
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    private void UpdateView()
    {
        for(int i=0;i<chars.Length;i++)
        {
            if(i<currentIndex)
            {
                chars[i].rectTransform.anchoredPosition = leftHidePos;
                chars[i].gameObject.SetActive(false);
            }
            else if(i==currentIndex)
            {
                chars[i].rectTransform.sizeDelta = bigSize;
                chars[i].color = bigColor;
                chars[i].gameObject.SetActive(true);
                chars[i].rectTransform.anchoredPosition = centerPos;
            }
            else if(i == currentIndex+1)
            {
                chars[i].rectTransform.sizeDelta = smallSize;
                chars[i].color = smallColor;
                chars[i].gameObject.SetActive(true);
                chars[i].rectTransform.anchoredPosition = rightPos;
            }
            else
            {
                chars[i].gameObject.SetActive(false);
            }
        }
        select_LeftBtn.gameObject.SetActive(currentIndex > 0);
        select_RightBtn.gameObject.SetActive(currentIndex < chars.Length - 1);
    }
}

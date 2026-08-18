using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [Header("Scale Setting")]
    public float scaleUp = 1.2f;
    public float scaleDown = 0.8f ;

    public float duration = 0.5f ;

    public Ease easeType = Ease.InOutSine; 

    public Vector3 originalScale;

    public Sequence sequence;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalScale = transform.localScale; // Store the original scale of the GameObject
        PlayPulse(); // Start the pulsing animation
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void PlayPulse()
    {
        sequence = DOTween.Sequence(); 
        sequence.Append(transform.DOScale(originalScale * scaleUp, duration).SetEase(easeType))
                .Append(transform.DOScale(originalScale * scaleDown, duration).SetEase(easeType))
                .SetLoops(-1, LoopType.Restart); 
    }
    private void OnDisable() {
        if (sequence != null)
        {
            sequence.Kill(); 
        }
        transform.localScale = originalScale; 
    }
    public void OnClickStartGame()
    {
        sequence.Kill();
        transform.localScale = originalScale; 
        SceneManager.LoadScene("SelectedCharacter"); 
    }
}

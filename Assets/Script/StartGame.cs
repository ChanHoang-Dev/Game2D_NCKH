using UnityEngine;
using DG.Tweening;

public class StartGame : MonoBehaviour
{
    [Header("Scale Setting")]
    public float scaleUp = 1.2f;
    public float scaleDown = 0.8f ;

    public float duration = 0.5f ;

    public Ease easeType = Ease.InOutSine; // Tweening function for smooth scaling

    public Vector3 originalScale;

    public Sequence sequence;// Sequence to manage the scaling animation
    [Header("Ui Setting")]
    [SerializeField] private GameObject startGameUI; // Reference to the start game UI GameObject

    [SerializeField] private GameObject UiInteractable; // Reference to the UI Interactable GameObject

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
        sequence = DOTween.Sequence(); // Create a new sequence for the scaling animation
        sequence.Append(transform.DOScale(originalScale * scaleUp, duration).SetEase(easeType))
                .Append(transform.DOScale(originalScale * scaleDown, duration).SetEase(easeType))
                .SetLoops(-1, LoopType.Restart); // Loop the sequence indefinitely
    }
    private void OnDisable() {
        if (sequence != null)
        {
            sequence.Kill(); // Stop the animation when the GameObject is disabled
        }
        transform.localScale = originalScale; // Reset the scale to the original value
    }
    public void OnClickStartGame()
    {
        sequence.Kill(); // Stop the pulsing animation when the start game button is clicked
        transform.localScale = originalScale; // Reset the scale to the original value
        startGameUI.SetActive(false); // Hide the start game UI
        UiInteractable.SetActive(true); // Show the UI Interactable GameObject
    }
}

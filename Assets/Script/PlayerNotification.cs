using UnityEngine;
using TMPro;
using DG.Tweening;  

public class PlayerNotification : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private CanvasGroup canvasGroup; // CanvasGroup để điều khiển độ

    [SerializeField] private RectTransform panelText; // Panel chứa text để áp dụng hiệu ứng

    [Header("Animation Settings")]
    public float dropHeight = 100f; // Chiều cao rơi
    public float riseHeight = 100f; // Chiều cao nâng lên
    public float duration = 0.5f; // Thời gian cho mỗi giai đoạn (rơi, nâng lên, mờ dần)

    public float displayDuration = 3f; // Thời gian hiển thị thông báo trước khi mờ dần

    private Vector3 basePos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        basePos = panelText.localPosition; // Lưu vị trí ban đầu của panel
        panelText.gameObject.SetActive(false); // Ẩn panel khi bắt đầu
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void  ShowNotification(string message)
    {
        notificationText.text = message;
        panelText.gameObject.SetActive(true); // Hiển thị panel

        panelText.localPosition = basePos + Vector3.up * dropHeight; // Đặt vị trí ban đầu ở trên cao   
        canvasGroup.alpha = 1f; // Đảm bảo thông báo bắt đầu với độ mờ hoàn toàn

        DOTween.Kill(panelText); // Dừng tất cả tweens hiện tại trên panelText

        Sequence sequence = DOTween.Sequence();
        sequence.Append(panelText.DOLocalMove(basePos, duration).SetEase(Ease.OutBounce)); // Rơi xuống

        sequence.AppendInterval(displayDuration); // Giữ nguyên vị trí trong một khoảng thời gian

        sequence.Append(panelText.DOLocalMove(basePos+ Vector3.up * riseHeight, duration).SetEase(Ease.InQuad)); // Nâng lên

        sequence.Join(canvasGroup.DOFade(0f, duration)); // Mờ dần đồng thời với việc nâng lên

        sequence.OnComplete(() => 
        {
                panelText.gameObject.SetActive(false);
                canvasGroup.alpha = 1f; // Reset độ mờ để sẵn sàng cho lần hiển thị tiếp theo
                panelText.localPosition = basePos; // Reset vị trí để sẵn sàng cho lần hiển thị tiếp theo
        }); // Ẩn panel sau khi hoàn thành
    }
}

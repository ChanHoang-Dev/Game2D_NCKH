using UnityEngine;

public class GhostMovement : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 5f; // Tốc độ trôi của "con ma"

    // Giả sử ban đầu nhân vật đang quay trái (LeftIdle)
    private bool isFacingRight = false;

    void Update()
    {
        // 1. Lấy phím bấm (A, D, W, S)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // 2. Chỉ cập nhật hướng quay mặt khi bấm A hoặc D (Trái/Phải)
        // Nếu chỉ bấm W hoặc S (moveY khác 0, moveX = 0), đoạn if này bị bỏ qua -> Giữ nguyên hướng cũ!
        if (moveX > 0)
        {
            isFacingRight = true;
        }
        else if (moveX < 0)
        {
            isFacingRight = false;
        }

        // Cập nhật Animator để đổi giữa LeftIdle và RightIdle
        animator.SetBool("IsFacingRight", isFacingRight);

        // 3. Code làm nhân vật trôi đi lờ lờ
        Vector2 movement = new Vector2(moveX, moveY).normalized;
        // Xóa dòng cũ này đi:
        // transform.Translate(movement * moveSpeed * Time.deltaTime);

        // Cách 1: Thêm Space.World vào
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);

        // HOẶC Cách 2 (Khuyên dùng cho 2D): Cộng thẳng vào position, không bao giờ lo bị ngược hướng
        // transform.position += (Vector3)movement * moveSpeed * Time.deltaTime;
    }
}
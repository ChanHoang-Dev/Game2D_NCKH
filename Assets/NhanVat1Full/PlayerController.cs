using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Thông số di chuyển")]
    public float moveSpeed = 5f;

    // Khai báo các component
    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 movement;

    void Start()
    {
        // Lấy các component đã gắn trên nhân vật
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Lấy giá trị trục ngang và dọc (WASD hoặc phím mũi tên)
        // Dùng GetAxisRaw để di chuyển giật cục, chuẩn game 2D (trả về đúng -1, 0, 1)
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // 2. Gửi giá trị tốc độ vào Animator
        // sqrMagnitude trả về bình phương độ dài vector, dùng để kiểm tra > 0 rất tối ưu
        anim.SetFloat("Speed", movement.sqrMagnitude);

        // 3. Gửi giá trị hướng (X, Y) vào Animator
        // MẸO: Chỉ cập nhật X và Y khi người chơi thực sự bấm phím (movement != 0)
        // Nhờ vậy, khi buông phím, nhân vật vẫn giữ nguyên hướng nhìn cuối cùng!
        if (movement.x != 0 || movement.y != 0)
        {
            anim.SetFloat("X", movement.x);
            anim.SetFloat("Y", movement.y);
        }
    }

    void FixedUpdate()
    {
        // 4. Xử lý vật lý (Di chuyển nhân vật)
        // movement.normalized giúp nhân vật đi chéo không bị nhanh hơn đi thẳng
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Tốc độ di chuyển
    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        // Gắn kết Rigidbody2D vào script
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Nhận tín hiệu từ bàn phím (W,A,S,D hoặc 4 phím mũi tên)
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        // Lệnh di chuyển bằng vật lý (.normalized giúp nhân vật đi chéo không bị nhanh hơn đi thẳng)
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}
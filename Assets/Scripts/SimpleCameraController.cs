using UnityEngine;

/// <summary>
/// Di chuyển camera đơn giản để test trên WebGL.
/// WASD = di chuyển
/// Chuột phải giữ + kéo = xoay nhìn
/// Scroll = zoom in/out
/// Q/E = lên xuống
/// </summary>
public class SimpleCameraController : MonoBehaviour
{
    [Header("=== TỐC ĐỘ ===")]
    public float moveSpeed = 2f;
    public float lookSpeed = 2f;
    public float scrollSpeed = 3f;
    public float fastMultiplier = 3f; // Giữ Shift để đi nhanh

    private float rotX = 0f;
    private float rotY = 0f;

    void Start()
    {
        // Lấy góc nhìn ban đầu
        rotX = transform.eulerAngles.x;
        rotY = transform.eulerAngles.y;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleScroll();
    }

    void HandleMovement()
    {
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= fastMultiplier;

        // WASD di chuyển
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        Vector3 move = transform.right * h + transform.forward * v;
        transform.position += move * speed * Time.deltaTime;

        // Q/E lên xuống
        if (Input.GetKey(KeyCode.Q))
            transform.position -= Vector3.up * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            transform.position += Vector3.up * speed * Time.deltaTime;
    }

    void HandleLook()
    {
        // Chuột PHẢI giữ để xoay nhìn
        if (Input.GetMouseButton(1))
        {
            rotY += Input.GetAxis("Mouse X") * lookSpeed;
            rotX -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotX = Mathf.Clamp(rotX, -89f, 89f);
            transform.rotation = Quaternion.Euler(rotX, rotY, 0f);
        }
    }

    void HandleScroll()
    {
        // Scroll zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position += transform.forward * scroll * scrollSpeed;
    }
}

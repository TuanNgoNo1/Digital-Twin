using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateSubmarineBlades : MonoBehaviour
{
    // Set rotate to equal false
    public bool rotate = false;

    // List of Rotatable Objects
    public List<GameObject> rotatableObjects;

    // Object Rotation Speed (Sẽ được cập nhật từ PLCController)
    public float rotationSpeed = 100;

    // Rotation direction: 1 for Thuan (clockwise), -1 for Nguoc (counter-clockwise)
    public float rotationDirection = 1f;

    // Number of rotations to perform
    public float soVongCanQuay = 1000f;
    private float soVongDaQuay = 0f;

    // Public getters để đọc trạng thái
    public float GetSoVongDaQuay() => soVongDaQuay;
    public float GetRotationSpeed() => rotationSpeed;
    public float GetRotationDirection() => rotationDirection;
    public bool GetIsRotating() => rotate;

    // Reference to PLCController
    private PLCController plcController;

    private void Start()
    {
        // Find PLCController in the scene
        plcController = FindObjectOfType<PLCController>();

        // Khởi tạo trạng thái ban đầu
        rotate = false;
        soVongDaQuay = 0f;
    }

    private void FixedUpdate()
    {
        // Nếu không ở trạng thái xoay thì không làm gì
        if (!rotate) return;

        // Xoay các đối tượng trong danh sách
        foreach (GameObject rotatableObject in rotatableObjects)
        {
            if (rotatableObject != null)
            {
                // Chú ý: rotationSpeed ở đây được PLCController cập nhật liên tục qua Coroutine
                // Sử dụng FixedUpdate kết hợp Time.fixedDeltaTime để quay đồng bộ vật lý
                rotatableObject.transform.Rotate(Vector3.forward * rotationSpeed * rotationDirection * Time.fixedDeltaTime);
            }
        }

        // Theo dõi tiến độ xoay
        TrackRotations();
    }

    private void TrackRotations()
    {
        // Tính toán góc quay đã thực hiện trong frame này
        float anglePerFrame = rotationSpeed * Time.fixedDeltaTime;
        soVongDaQuay += Mathf.Abs(anglePerFrame) / 360f;

        // Nếu đạt đủ số vòng yêu cầu thì dừng lại
        if (soVongDaQuay >= soVongCanQuay)
        {
            rotate = false;
            soVongDaQuay = 0f;

            // Thông báo cho PLC dừng motor nếu cần thiết
            if (plcController != null)
            {
                plcController.StopDongCo();
            }
        }
    }

    // Set rotation direction dựa trên trạng thái bit từ PLC
    public void SetRotationDirection(bool isThuan)
    {
        rotationDirection = isThuan ? 1f : -1f;
    }

    // Thiết lập số vòng cần quay từ thanh ghi PLC
    public void SetNumberOfRotations(float soVong)
    {
        soVongCanQuay = Mathf.Max(soVong, 0.01f);
        soVongDaQuay = 0f;
    }

    // Bật/Tắt xoay (Dùng cho Button hoặc PLC gọi)
    public void RotateObject()
    {
        rotate = !rotate;
        if (rotate)
        {
            soVongDaQuay = 0f;
        }
    }

    // Overload để chỉ định chính xác trạng thái bật/tắt
    public void RotateObject(bool enable)
    {
        rotate = enable;
        if (rotate)
        {
            soVongDaQuay = 0f;
        }
    }
}
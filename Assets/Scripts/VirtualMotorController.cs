using UnityEngine;

/// <summary>
/// Điều khiển motor ảo (không cần PLC thật).
/// Mô phỏng quay motor, tốc độ, chiều quay, số vòng, góc.
/// </summary>
public class VirtualMotorController : MonoBehaviour
{
    public static VirtualMotorController Instance;

    [Header("=== Model 3D Motor ===")]
    public Transform motorRotor;  // Gán Shaft hoặc Sproket để quay

    [Header("=== Thông số Motor ===")]
    public float currentSpeed = 0f;      // RPM hiện tại
    public float targetSpeed = 100f;     // RPM mục tiêu
    public bool isRunning = false;
    public bool isForward = true;        // true = Thuận, false = Ngược

    [Header("=== Đặt vị trí ===")]
    public float targetRotations = 0f;   // Số vòng cần quay
    public float targetAngle = 0f;       // Góc cần quay (độ)

    [Header("=== Trạng thái đo ===")]
    public float totalRotations = 0f;    // Tổng số vòng đã quay
    public float currentAngle = 0f;      // Góc hiện tại (độ)

    [Header("=== Giới hạn ===")]
    public float maxSpeed = 3000f;
    public float speedStep = 10f;
    public float accelerationRate = 200f; // RPM/giây - tốc độ tăng/giảm mượt

    // Events
    public System.Action<string> OnStatusChanged;
    public System.Action OnMotorStarted;
    public System.Action OnMotorStopped;

    private float rotationAccumulator = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!isRunning)
        {
            // Giảm tốc mượt về 0 khi dừng
            if (currentSpeed > 0)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, accelerationRate * Time.deltaTime);
                RotateMotor();
            }
            return;
        }

        // Tăng/giảm tốc mượt đến target
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);
        RotateMotor();

        // Kiểm tra đã đạt mục tiêu chưa (nếu có đặt)
        if (targetRotations > 0 && totalRotations >= targetRotations)
        {
            Stop();
            LogStatus("✅ Đã quay đủ {0:F1} vòng!", totalRotations);
        }
        if (targetAngle > 0 && currentAngle >= targetAngle)
        {
            Stop();
            LogStatus("✅ Đã quay đủ {0:F1}°!", currentAngle);
        }
    }

    void RotateMotor()
    {
        if (currentSpeed <= 0) return;

        // Tính góc quay mỗi frame: RPM -> degrees/sec = RPM * 6
        float degreesPerSecond = currentSpeed * 6f;
        float rotationThisFrame = degreesPerSecond * Time.deltaTime;
        // Match virtual Rotor_Main direction with the real motor.
        float direction = isForward ? -1f : 1f;

        // Quay model 3D
        if (motorRotor != null)
        {
            motorRotor.Rotate(Vector3.forward, rotationThisFrame * direction, Space.Self);
        }

        // Cập nhật thống kê
        rotationAccumulator += rotationThisFrame;
        currentAngle += rotationThisFrame;
        totalRotations = rotationAccumulator / 360f;
    }

    // =====================================================
    // PUBLIC API - Gọi từ HMI
    // =====================================================

    public void StartMotor()
    {
        isRunning = true;
        OnMotorStarted?.Invoke();
        LogStatus("▶ Motor START - {0} - Tốc độ: {1} RPM", isForward ? "Thuận" : "Ngược", targetSpeed);
    }

    public void Stop()
    {
        isRunning = false;
        OnMotorStopped?.Invoke();
        LogStatus("⏹ Motor STOP - Đã quay: {0:F1} vòng", totalRotations);
    }

    public void SetForward()
    {
        isForward = true;
        LogStatus("↻ Chiều: THUẬN");
    }

    public void SetReverse()
    {
        isForward = false;
        LogStatus("↺ Chiều: NGƯỢC");
    }

    public void IncreaseSpeed()
    {
        targetSpeed = Mathf.Min(targetSpeed + speedStep, maxSpeed);
        LogStatus("⬆ Tốc độ: {0} RPM", targetSpeed);
    }

    public void DecreaseSpeed()
    {
        targetSpeed = Mathf.Max(targetSpeed - speedStep, 0);
        LogStatus("⬇ Tốc độ: {0} RPM", targetSpeed);
    }

    public void SetSpeed(float rpm)
    {
        targetSpeed = Mathf.Clamp(rpm, 0, maxSpeed);
        LogStatus("⚡ Đặt tốc độ: {0} RPM", targetSpeed);
    }

    public void SetTargetRotations(float rotations)
    {
        targetRotations = rotations;
        targetAngle = 0; // Chỉ dùng 1 mode
        LogStatus("🔄 Đặt số vòng: {0}", targetRotations);
    }

    public void SetTargetAngle(float angle)
    {
        targetAngle = angle;
        targetRotations = 0; // Chỉ dùng 1 mode
        LogStatus("📐 Đặt góc: {0}°", targetAngle);
    }

    public void ResetAll()
    {
        Stop();
        currentSpeed = 0;
        totalRotations = 0;
        currentAngle = 0;
        rotationAccumulator = 0;
        targetRotations = 0;
        targetAngle = 0;
        targetSpeed = 100;
        isForward = true;
        LogStatus("🔄 RESET hoàn tất");
    }

    private void LogStatus(string format, params object[] args)
    {
        string msg = string.Format(format, args);
        Debug.Log($"<color=cyan>[Motor] {msg}</color>");
        OnStatusChanged?.Invoke(msg);
    }
}

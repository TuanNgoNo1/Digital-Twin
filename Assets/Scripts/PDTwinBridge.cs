using UnityEngine;
using System.Runtime.InteropServices;

public static class PDTwinBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SubmitResult(float score, string dataJson);
#endif

    private static bool submitted;

    /// <summary>
    /// Gửi điểm về P-DTwin (chỉ gọi 1 lần). Score 0.0–10.0.
    /// </summary>
    public static void Submit(float score, string dataJson)
    {
        if (submitted)
        {
            Debug.LogWarning("[PDTwinBridge] Already submitted, ignoring.");
            return;
        }
        submitted = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        SubmitResult(Mathf.Clamp(score, 0f, 10f), dataJson);
#else
        Debug.Log($"[PDTwinBridge][DEV] Score: {score}, Data: {dataJson}");
#endif
    }
}

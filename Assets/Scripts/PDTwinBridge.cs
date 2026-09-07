using UnityEngine;
using System.Runtime.InteropServices;

public static class PDTwinBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SubmitResult(float score, string details);

    [DllImport("__Internal")]
    private static extern void ReportProgressResult(float score);
#endif

    private static bool submitted;
    private static bool hasProgress;
    private static float lastProgress;

    public static bool IsSubmitted => submitted;

    /// <summary>
    /// Gui diem tam ve P-DTwin. Co the goi nhieu lan truoc khi nop diem cuoi.
    /// </summary>
    public static void ReportProgress(float score)
    {
        if (submitted)
            return;

        score = SanitizeScore(score);
        if (hasProgress && Mathf.Approximately(lastProgress, score))
            return;

        hasProgress = true;
        lastProgress = score;

#if UNITY_WEBGL && !UNITY_EDITOR
        ReportProgressResult(score);
#else
        Debug.Log($"[PDTwinBridge][DEV] Progress: {score:F2}");
#endif
    }

    /// <summary>
    /// Gui diem cuoi ve P-DTwin dung mot lan. Score nam trong 0.0-10.0.
    /// </summary>
    public static void Submit(float score, string details)
    {
        if (submitted)
        {
            Debug.LogWarning("[PDTwinBridge] Already submitted, ignoring.");
            return;
        }

        score = SanitizeScore(score);
        details ??= string.Empty;
        submitted = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        SubmitResult(score, details);
#else
        Debug.Log($"[PDTwinBridge][DEV] Final score: {score:F2}, Details: {details}");
#endif
    }

    private static float SanitizeScore(float score)
    {
        if (float.IsNaN(score) || float.IsInfinity(score))
            return 0f;
        return Mathf.Clamp(score, 0f, 10f);
    }

#if UNITY_EDITOR
    public static void ResetForEditorTesting()
    {
        submitted = false;
        hasProgress = false;
        lastProgress = 0f;
    }
#endif
}

using System.Collections;
using UnityEngine;

public class SCORMProgressHandler : MonoBehaviour
{
    public int currentLevel = 1;

    private void OnEnable()
    {
        if (SCORMManager.Instance != null)
        {
            SCORMManager.Instance.ReportLevelProgress(currentLevel);
            Debug.Log($"Reported progress to SCORM.");
        }
        else
        {
            Debug.LogWarning("SCORMManager instance not found!");
        }       
    }
}
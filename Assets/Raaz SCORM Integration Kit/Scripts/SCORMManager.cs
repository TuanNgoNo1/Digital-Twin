using UnityEngine;
using TMPro;

public class SCORMManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI studentInfoText;
    public TMP_InputField scoreInputField;

    public static SCORMManager Instance { get; private set; }
    // Global values we can access anywhere
    public string StudentId { get; private set; }
    public string StudentName { get; private set; }

    private float _highestScoreReached = 0f;

    private int maxLevels = 2;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); //  keep SCORMManager alive across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Immediately mark course as incomplete
        SetCompletion("incomplete");
        RequestStudentInfo(); // Ask parent page for student info at start
    }

    // ---------- SCORM → LMS communication via postMessage ----------

    public void InitSCORM()
    {
        Application.ExternalEval("parent.postMessage('initSCORM', '*');");
    }

    public void SetCompletion(string status)
    {
        Application.ExternalEval($"parent.postMessage('setStatus:{status}', '*');");
    }

    public void SetScore(float score)
    {
        Application.ExternalEval($"parent.postMessage('setScore:{score}', '*');");
    }

    public void SetScore()
    {
        Application.ExternalEval($"parent.postMessage('setScore:{scoreInputField.text}', '*');");
    }

    public void FinishSCORM()
    {
        Application.ExternalEval("parent.postMessage('markFinished', '*');");
    }

    public void RequestStudentInfo()
    {
        Application.ExternalEval("parent.postMessage('requestStudentInfo', '*');");
        Debug.Log("Requesting student info");
    }

    // ---------- LMS → Unity communication (receiving info) ----------

    // Called from JS when student info comes back
    public void OnReceiveStudentInfo(string json)
    {
        var data = JsonUtility.FromJson<StudentInfo>(json);
        Debug.Log($"SCORM Student GET → ID: {data.id}, Name: {data.name}");

        //Save globally
        StudentId = data.id;
        StudentName = data.name;
        Debug.Log($"SCORM Student SET in Unity → ID: {StudentId}, Name: {StudentName}");
        if (studentInfoText != null)
        {
            studentInfoText.text = $"Learner Name: {StudentName}\nLearner ID: {StudentId}";
        }
        Debug.Log("Received student info");
    }
    // ---------- New: Progress Reporting ----------

    public void ReportLevelProgress(int levelsCompleted)
    {
        if (levelsCompleted < 0) levelsCompleted = 0;
        if (levelsCompleted > 2) levelsCompleted = 2;      

        float completionPercent = (levelsCompleted / 2f) * 100f;
        
        if(completionPercent <= _highestScoreReached)
            return;
        _highestScoreReached = completionPercent;
        // Send score/progress to LMS
        Application.ExternalEval($"parent.postMessage('setScore:{completionPercent:F0}', '*');");

        // Bookmark progress (so LMS can resume from last level)
        Application.ExternalEval($"parent.postMessage('setLocation:{levelsCompleted}', '*');");

        // Update status
        if (levelsCompleted >= 2)
            Application.ExternalEval("parent.postMessage('setStatus:passed', '*');");
        else
            Application.ExternalEval("parent.postMessage('setStatus:incomplete', '*');");

        Debug.Log($"SCORM Progress → Levels: {levelsCompleted}/2, Completion: {completionPercent}%");
    }
    // ---------- Helper Classes ----------

    [System.Serializable]
    private class StudentInfo
    {
        public string type;
        public string id;
        public string name;
    }
}

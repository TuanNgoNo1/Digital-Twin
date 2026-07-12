using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class StartScreenController : MonoBehaviour
{
    [SerializeField] private GameObject practicePanel;
    [SerializeField] private GameObject introductionPanel;
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private string practiceSceneName = "Sy_scene";

    private void Awake()
    {
        ShowPractice();
    }

    public void ShowPractice()
    {
        SetScreenState(practicePanel);
    }

    public void ShowIntroduction()
    {
        SetScreenState(introductionPanel);
    }

    public void ShowGuide()
    {
        SetScreenState(guidePanel);
    }

    public void LoadPracticeScene()
    {
        SceneManager.LoadScene(practiceSceneName);
    }

    private void SetScreenState(GameObject activePanel)
    {
        SetPanelActive(practicePanel, activePanel);
        SetPanelActive(introductionPanel, activePanel);
        SetPanelActive(guidePanel, activePanel);
    }

    private static void SetPanelActive(GameObject panel, GameObject activePanel)
    {
        if (panel != null)
            panel.SetActive(panel == activePanel);
    }
}

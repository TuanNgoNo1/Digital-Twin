using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class IntroductionPanelController : MonoBehaviour
{
    [SerializeField] private GameObject[] pages;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageIndicatorText;
    [SerializeField] private bool resetToFirstPageOnEnable = true;
    [SerializeField] private UnityEvent onCompleted;

    private int currentPageIndex;

    public UnityEvent Completed => onCompleted;

    private void Awake()
    {
        HookButtons();
        ShowPage(0);
    }

    private void OnEnable()
    {
        if (resetToFirstPageOnEnable)
        {
            ShowPage(0);
        }
    }

    public void ShowNextPage()
    {
        if (pages != null && pages.Length > 0 && currentPageIndex >= pages.Length - 1)
        {
            onCompleted?.Invoke();
            return;
        }

        ShowPage(currentPageIndex + 1);
    }

    public void ShowPreviousPage()
    {
        ShowPage(currentPageIndex - 1);
    }

    public void ShowPage(int pageIndex)
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        currentPageIndex = Mathf.Clamp(pageIndex, 0, pages.Length - 1);

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == currentPageIndex);
            }
        }

        UpdateNavigationState();
    }

    private void HookButtons()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(ShowPreviousPage);
            previousButton.onClick.AddListener(ShowPreviousPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPage);
            nextButton.onClick.AddListener(ShowNextPage);
        }
    }

    private void UpdateNavigationState()
    {
        int pageCount = pages?.Length ?? 0;

        if (previousButton != null)
        {
            previousButton.interactable = currentPageIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = pageCount > 0;
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = pageCount > 0 ? $"{currentPageIndex + 1}/{pageCount}" : string.Empty;
        }
    }
}

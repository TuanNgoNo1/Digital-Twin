using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PDTwin.RuntimeUI
{
    public sealed class PDTwinStatusBar : MonoBehaviour
    {
        private sealed class StatusCell
        {
            public Image dot;
            public TextMeshProUGUI label;
            public TextMeshProUGUI value;
            public TextMeshProUGUI detail;
        }

        private readonly Dictionary<string, StatusCell> cells = new Dictionary<string, StatusCell>(StringComparer.OrdinalIgnoreCase);
        private TextMeshProUGUI scoreValue;
        private TextMeshProUGUI scoreCaption;
        private TextMeshProUGUI scoreDiscText;
        private Image scoreDisc;
        private Button scoreButton;
        private GameObject transitionBanner;
        private Image transitionBackground;
        private Image transitionDot;
        private TextMeshProUGUI transitionTitle;
        private TextMeshProUGUI transitionDetail;
        private Coroutine transitionHideRoutine;

        public static PDTwinStatusBar Build(Transform parent, Action scoreClicked)
        {
            GameObject bar = PDTwinRuntimeUiFactory.CreateRect("PTITStatusBar", parent, PDTwinRuntimeTheme.Surface);
            RectTransform barRect = bar.GetComponent<RectTransform>();
            // Leave enough vertical room for readable Vietnamese labels. The
            // runtime layout guard moves the lesson navigator below this bar.
            PDTwinRuntimeUiFactory.AnchorTop(barRect, 70f);

            Shadow shadow = bar.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.07f, 0.10f, 0.16f, 0.16f);
            shadow.effectDistance = new Vector2(0f, -3f);

            GameObject accent = PDTwinRuntimeUiFactory.CreateRect("PTITAccent", bar.transform, PDTwinRuntimeTheme.PtitRed);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = Vector2.one;
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.offsetMin = new Vector2(0f, -3f);
            accentRect.offsetMax = Vector2.zero;

            GameObject rowObject = new GameObject("StatusRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(bar.transform, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            PDTwinRuntimeUiFactory.Stretch(rowRect, 10f, 8f, 10f, 7f);
            HorizontalLayoutGroup row = rowObject.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            PDTwinStatusBar component = bar.AddComponent<PDTwinStatusBar>();
            component.BuildBrand(rowObject.transform);
            component.BuildStatusCell(rowObject.transform, "COM3", "COM3 / GATEWAY");
            component.BuildStatusCell(rowObject.transform, "PLC", "PLC FX3U");
            component.BuildScore(rowObject.transform, scoreClicked);
            component.BuildTransitionBanner();
            return component;
        }

        public void SetStatus(string key, string label, string value, string detail, RuntimeHealth health)
        {
            if (!cells.TryGetValue(key ?? string.Empty, out StatusCell cell))
                return;

            if (!string.IsNullOrWhiteSpace(label))
                cell.label.text = label;
            cell.value.text = value ?? string.Empty;
            cell.detail.text = detail ?? string.Empty;
            cell.dot.color = PDTwinRuntimeTheme.HealthColor(health);
            cell.value.color = health == RuntimeHealth.Offline ? PDTwinRuntimeTheme.Danger : PDTwinRuntimeTheme.Ink;
        }

        public void SetScore(float score, bool submitted)
        {
            if (scoreValue == null)
                return;
            scoreValue.text = score.ToString("F2") + " / 10";
            scoreCaption.text = submitted ? "Đã nộp • xem chi tiết" : "Điểm tạm • xem chi tiết";
            scoreDiscText.text = score.ToString("F1");
            scoreDisc.fillAmount = Mathf.Clamp01(score / 10f);
            scoreDisc.color = submitted ? PDTwinRuntimeTheme.Success : PDTwinRuntimeTheme.PtitRed;
        }

        public void SetScoreInteractable(bool value)
        {
            if (scoreButton != null)
                scoreButton.interactable = value;
        }

        public void SetTransitionBanner(bool visible, string title, string detail, RuntimeHealth health)
        {
            if (transitionBanner == null)
                return;
            if (transitionHideRoutine != null)
            {
                StopCoroutine(transitionHideRoutine);
                transitionHideRoutine = null;
            }
            transitionBanner.SetActive(visible);
            if (!visible)
                return;

            transitionTitle.text = title ?? string.Empty;
            transitionDetail.text = detail ?? string.Empty;
            transitionDot.color = PDTwinRuntimeTheme.HealthColor(health);
            transitionBackground.color = health == RuntimeHealth.Online
                ? new Color(0.90f, 0.98f, 0.94f, 0.98f)
                : health == RuntimeHealth.Offline
                    ? new Color(1f, 0.91f, 0.92f, 0.98f)
                    : new Color(1f, 0.96f, 0.84f, 0.98f);
            transitionTitle.color = health == RuntimeHealth.Offline
                ? PDTwinRuntimeTheme.Danger : PDTwinRuntimeTheme.Ink;
        }

        public void ShowTransitionResult(string title, string detail, RuntimeHealth health, float seconds = 2.5f)
        {
            SetTransitionBanner(true, title, detail, health);
            transitionHideRoutine = StartCoroutine(HideTransitionBannerAfter(seconds));
        }

        private IEnumerator HideTransitionBannerAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, seconds));
            transitionBanner.SetActive(false);
            transitionHideRoutine = null;
        }

        private void BuildTransitionBanner()
        {
            transitionBanner = PDTwinRuntimeUiFactory.CreateRect(
                "ModeTransitionBanner", transform, new Color(1f, 0.96f, 0.84f, 0.98f), true);
            transitionBackground = transitionBanner.GetComponent<Image>();
            RectTransform rect = transitionBanner.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(-24f, 46f);

            transitionDot = PDTwinRuntimeUiFactory.CreateRect(
                "StatusDot", transitionBanner.transform, PDTwinRuntimeTheme.HealthColor(RuntimeHealth.Warning), true).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(transitionDot);
            RectTransform dotRect = transitionDot.rectTransform;
            dotRect.anchorMin = new Vector2(0f, 0.5f);
            dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.pivot = new Vector2(0f, 0.5f);
            dotRect.anchoredPosition = new Vector2(15f, 0f);
            dotRect.sizeDelta = new Vector2(12f, 12f);

            transitionTitle = PDTwinRuntimeUiFactory.CreateText(
                "Title", transitionBanner.transform, "ĐANG CHUYỂN CHẾ ĐỘ", 14f,
                PDTwinRuntimeTheme.Ink, FontStyles.Bold);
            RectTransform titleRect = transitionTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.42f);
            titleRect.anchorMax = new Vector2(0.48f, 1f);
            titleRect.offsetMin = new Vector2(36f, 0f);
            titleRect.offsetMax = new Vector2(0f, -3f);

            transitionDetail = PDTwinRuntimeUiFactory.CreateText(
                "Detail", transitionBanner.transform, string.Empty, 12.5f,
                PDTwinRuntimeTheme.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            RectTransform detailRect = transitionDetail.rectTransform;
            detailRect.anchorMin = new Vector2(0.45f, 0f);
            detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = new Vector2(0f, 3f);
            detailRect.offsetMax = new Vector2(-16f, 0f);

            transitionBanner.transform.SetAsLastSibling();
            transitionBanner.SetActive(false);
        }

        private void BuildBrand(Transform parent)
        {
            GameObject brand = PDTwinRuntimeUiFactory.CreateRect("PTITBrand", parent, PDTwinRuntimeTheme.PtitRedSoft, true);
            LayoutElement layout = brand.AddComponent<LayoutElement>();
            layout.preferredWidth = 150f;
            layout.minWidth = 138f;

            TextMeshProUGUI mark = PDTwinRuntimeUiFactory.CreateText("Mark", brand.transform, "PTIT", 24f, PDTwinRuntimeTheme.PtitRed, FontStyles.Bold);
            RectTransform markRect = mark.rectTransform;
            markRect.anchorMin = new Vector2(0f, 0.34f);
            markRect.anchorMax = new Vector2(1f, 1f);
            markRect.offsetMin = new Vector2(12f, 0f);
            markRect.offsetMax = new Vector2(-8f, -3f);

            TextMeshProUGUI caption = PDTwinRuntimeUiFactory.CreateText("Caption", brand.transform, "DIGITAL TWIN LAB", 9.5f, PDTwinRuntimeTheme.PtitRedDark, FontStyles.Bold);
            RectTransform captionRect = caption.rectTransform;
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = new Vector2(1f, 0.42f);
            captionRect.offsetMin = new Vector2(12f, 4f);
            captionRect.offsetMax = new Vector2(-8f, 0f);
        }

        private void BuildStatusCell(Transform parent, string key, string defaultLabel)
        {
            GameObject root = PDTwinRuntimeUiFactory.CreateRect("Status_" + key, parent, PDTwinRuntimeTheme.SurfaceMuted, true);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.minWidth = 190f;
            layout.flexibleWidth = 1f;

            Image dot = PDTwinRuntimeUiFactory.CreateRect("Dot", root.transform, PDTwinRuntimeTheme.HealthColor(RuntimeHealth.Unknown), true).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(dot);
            RectTransform dotRect = dot.rectTransform;
            dotRect.anchorMin = new Vector2(0f, 1f);
            dotRect.anchorMax = new Vector2(0f, 1f);
            dotRect.pivot = new Vector2(0f, 1f);
            // Align the centre of the dot with the first-line status label.
            dotRect.anchoredPosition = new Vector2(13f, -6.5f);
            dotRect.sizeDelta = new Vector2(11f, 11f);

            TextMeshProUGUI label = PDTwinRuntimeUiFactory.CreateText("Label", root.transform, defaultLabel, 11.5f, PDTwinRuntimeTheme.Muted, FontStyles.Bold);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.66f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(28f, 0f);
            labelRect.offsetMax = new Vector2(-8f, -5f);

            TextMeshProUGUI value = PDTwinRuntimeUiFactory.CreateText("Value", root.transform, "ĐANG KHỞI TẠO", 17f, PDTwinRuntimeTheme.Ink, FontStyles.Bold);
            RectTransform valueRect = value.rectTransform;
            valueRect.anchorMin = new Vector2(0f, 0.27f);
            valueRect.anchorMax = new Vector2(1f, 0.72f);
            valueRect.offsetMin = new Vector2(12f, 0f);
            valueRect.offsetMax = new Vector2(-8f, 0f);

            TextMeshProUGUI detail = PDTwinRuntimeUiFactory.CreateText("Detail", root.transform, "Đang kiểm tra kết nối...", 10.5f, PDTwinRuntimeTheme.Muted);
            RectTransform detailRect = detail.rectTransform;
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = new Vector2(1f, 0.34f);
            detailRect.offsetMin = new Vector2(12f, 3f);
            detailRect.offsetMax = new Vector2(-8f, 0f);

            cells[key] = new StatusCell { dot = dot, label = label, value = value, detail = detail };
        }

        private void BuildScore(Transform parent, Action scoreClicked)
        {
            scoreButton = PDTwinRuntimeUiFactory.CreateButton("ScoreSummaryButton", parent, string.Empty, PDTwinRuntimeTheme.PtitRedSoft, PDTwinRuntimeTheme.Ink);
            LayoutElement layout = scoreButton.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 250f;
            layout.minWidth = 225f;

            Transform generatedLabel = scoreButton.transform.Find("Label");
            if (generatedLabel != null)
                generatedLabel.gameObject.SetActive(false);

            Image scoreTrack = PDTwinRuntimeUiFactory.CreateRect("ScoreTrack", scoreButton.transform, PDTwinRuntimeTheme.ProgressTrack).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(scoreTrack);
            RectTransform discRect = scoreTrack.rectTransform;
            discRect.anchorMin = new Vector2(0f, 0.5f);
            discRect.anchorMax = new Vector2(0f, 0.5f);
            discRect.pivot = new Vector2(0f, 0.5f);
            discRect.anchoredPosition = new Vector2(11f, 0f);
            discRect.sizeDelta = new Vector2(46f, 46f);

            scoreDisc = PDTwinRuntimeUiFactory.CreateRect("ScoreProgress", scoreTrack.transform, PDTwinRuntimeTheme.PtitRed).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(scoreDisc);
            PDTwinRuntimeUiFactory.Stretch(scoreDisc.rectTransform);
            scoreDisc.type = Image.Type.Filled;
            scoreDisc.fillMethod = Image.FillMethod.Radial360;
            scoreDisc.fillOrigin = (int)Image.Origin360.Top;
            scoreDisc.fillClockwise = true;
            scoreDisc.fillAmount = 0f;

            Image scoreInner = PDTwinRuntimeUiFactory.CreateRect("ScoreInner", scoreTrack.transform, PDTwinRuntimeTheme.PtitRedSoft).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(scoreInner);
            PDTwinRuntimeUiFactory.Stretch(scoreInner.rectTransform, 5f, 5f, 5f, 5f);

            scoreDiscText = PDTwinRuntimeUiFactory.CreateText("DiscText", scoreInner.transform, "0.0", 12.5f, PDTwinRuntimeTheme.PtitRedDark, FontStyles.Bold, TextAlignmentOptions.Center);
            PDTwinRuntimeUiFactory.Stretch(scoreDiscText.rectTransform);

            scoreCaption = PDTwinRuntimeUiFactory.CreateText("Caption", scoreButton.transform, "Điểm tạm • xem chi tiết", 10.5f, PDTwinRuntimeTheme.PtitRedDark, FontStyles.Bold);
            RectTransform captionRect = scoreCaption.rectTransform;
            captionRect.anchorMin = new Vector2(0f, 0.52f);
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(66f, 0f);
            captionRect.offsetMax = new Vector2(-22f, -3f);

            scoreValue = PDTwinRuntimeUiFactory.CreateText("Value", scoreButton.transform, "0.00 / 10", 21f, PDTwinRuntimeTheme.Ink, FontStyles.Bold);
            RectTransform valueRect = scoreValue.rectTransform;
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = new Vector2(1f, 0.62f);
            valueRect.offsetMin = new Vector2(66f, 3f);
            valueRect.offsetMax = new Vector2(-22f, 0f);

            TextMeshProUGUI arrow = PDTwinRuntimeUiFactory.CreateText("Arrow", scoreButton.transform, "›", 22f, PDTwinRuntimeTheme.PtitRed, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = Vector2.one;
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.offsetMin = new Vector2(-24f, 0f);
            arrowRect.offsetMax = new Vector2(-4f, 0f);

            scoreButton.onClick.AddListener(() => scoreClicked?.Invoke());
        }
    }
}

namespace PDTwin.RuntimeUI
{
    /// <summary>
    /// Adapts runtime-only UI created by CircuitManager without editing the
    /// lesson scene, prefabs, wiring logic, or CircuitManager itself.
    /// </summary>
    public sealed class Bai1RuntimeLayoutGuard : MonoBehaviour
    {
        private const float SearchInterval = 0.2f;
        private const float NavigationTop = 72f;

        private bool navigationAdjusted;
        private bool bottomMaskDisabled;
        private float nextSearchAt;

        public static Bai1RuntimeLayoutGuard Attach(GameObject host)
        {
            Bai1RuntimeLayoutGuard guard = host.GetComponent<Bai1RuntimeLayoutGuard>();
            return guard != null ? guard : host.AddComponent<Bai1RuntimeLayoutGuard>();
        }

        private void Update()
        {
            if (navigationAdjusted && bottomMaskDisabled)
            {
                enabled = false;
                return;
            }

            if (Time.unscaledTime < nextSearchAt)
                return;

            nextSearchAt = Time.unscaledTime + SearchInterval;
            AdjustNavigation();
            DisableBottomMask();
        }

        private void AdjustNavigation()
        {
            GameObject canvas = GameObject.Find("StepNavigation_Canvas");
            if (canvas == null)
                return;

            RectTransform navigation = canvas.transform.Find("StepNavigationBar") as RectTransform;
            if (navigation == null)
                return;

            Vector2 position = navigation.anchoredPosition;
            navigation.anchoredPosition = new Vector2(position.x, -NavigationTop);
            navigationAdjusted = true;
        }

        private void DisableBottomMask()
        {
            GameObject canvas = GameObject.Find("PracticalWorkspaceMasks_Canvas");
            if (canvas == null)
                return;

            Transform bottomMask = canvas.transform.Find("BottomMask");
            if (bottomMask == null)
                return;

            bottomMask.gameObject.SetActive(false);
            bottomMaskDisabled = true;
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PDTwin.RuntimeUI;

namespace PDTwin.Point
{
    /// <summary>
    /// Runtime-only PTIT score UI. This never edits the lesson scene, prefabs,
    /// wire objects, sockets, or PLC scripts.
    /// </summary>
    public sealed class PointRuntimePanel : MonoBehaviour
    {
        private sealed class OverviewRow
        {
            public Image card;
            public Image dot;
            public TextMeshProUGUI title;
            public TextMeshProUGUI evidence;
            public TextMeshProUGUI state;
            public TextMeshProUGUI score;
            public Button button;
            public Outline outline;
        }

        private sealed class EvidenceRow
        {
            public Image badge;
            public TextMeshProUGUI marker;
            public TextMeshProUGUI label;
            public TextMeshProUGUI value;
        }

        private const float DrawerWidth = 570f;
        private readonly OverviewRow[] overviewRows = new OverviewRow[3];
        private readonly TextMeshProUGUI[] chipLabels = new TextMeshProUGUI[4];
        private readonly TextMeshProUGUI[] chipValues = new TextMeshProUGUI[4];
        private readonly EvidenceRow[] evidenceRows = new EvidenceRow[4];

        private PDTwinStatusBar statusBar;
        private GameObject drawerOverlay;
        private RectTransform drawerRect;
        private GameObject confirmModal;
        private TextMeshProUGUI submissionState;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI scoreCaption;
        private TextMeshProUGUI scoreRingText;
        private Image scoreRingFill;
        private TextMeshProUGUI testEyebrow;
        private TextMeshProUGUI testTitle;
        private TextMeshProUGUI testBadge;
        private Image testBadgeSurface;
        private TextMeshProUGUI statusText;
        private Image statusCard;
        private TextMeshProUGUI attemptText;
        private Button primaryButton;
        private Button submitButton;
        private float lastCanvasWidth = -1f;

        public event Action PrimaryRequested;
        public event Action SubmitRequested;
        public event Action<int> ReviewTestRequested;

        public static PointRuntimePanel Create(string title)
        {
            GameObject root = new GameObject(
                "PDTwinPointRuntime_Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5250;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            PointRuntimePanel panel = root.AddComponent<PointRuntimePanel>();
            panel.statusBar = PDTwinStatusBar.Build(root.transform, panel.ToggleDrawer);
            panel.BuildDrawer(title);

            Bai1RuntimeStatusProvider.Attach(root, panel.statusBar);
            Bai1RuntimeLayoutGuard.Attach(root);
            return panel;
        }

        private void LateUpdate()
        {
            RectTransform canvasRect = transform as RectTransform;
            if (canvasRect == null || drawerRect == null)
                return;

            float width = canvasRect.rect.width;
            if (Mathf.Abs(width - lastCanvasWidth) < 0.5f)
                return;

            lastCanvasWidth = width;
            drawerRect.offsetMin = new Vector2(-Mathf.Min(DrawerWidth, Mathf.Max(320f, width)), 0f);
        }

        private void BuildDrawer(string title)
        {
            drawerOverlay = new GameObject("ScoreDrawerOverlay", typeof(RectTransform));
            drawerOverlay.transform.SetParent(transform, false);
            PDTwinRuntimeUiFactory.Stretch(drawerOverlay.GetComponent<RectTransform>());

            GameObject scrim = PDTwinRuntimeUiFactory.CreateRect("Scrim", drawerOverlay.transform, PDTwinRuntimeTheme.Scrim);
            PDTwinRuntimeUiFactory.Stretch(scrim.GetComponent<RectTransform>());
            Button scrimButton = scrim.AddComponent<Button>();
            scrimButton.targetGraphic = scrim.GetComponent<Image>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(CloseDrawer);

            GameObject drawer = PDTwinRuntimeUiFactory.CreateRect("ScoreDrawer", drawerOverlay.transform, PDTwinRuntimeTheme.Surface);
            drawerRect = drawer.GetComponent<RectTransform>();
            drawerRect.anchorMin = new Vector2(1f, 0f);
            drawerRect.anchorMax = Vector2.one;
            drawerRect.pivot = new Vector2(1f, 0.5f);
            drawerRect.offsetMin = new Vector2(-DrawerWidth, 0f);
            drawerRect.offsetMax = Vector2.zero;
            Shadow drawerShadow = drawer.AddComponent<Shadow>();
            drawerShadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
            drawerShadow.effectDistance = new Vector2(-10f, 0f);

            BuildHeader(drawer.transform, title);
            BuildBody(drawer.transform);
            BuildFooter(drawer.transform);
            BuildConfirmation(drawerOverlay.transform);
            drawerOverlay.SetActive(false);
        }

        private void BuildHeader(Transform parent, string title)
        {
            GameObject header = PDTwinRuntimeUiFactory.CreateRect("Header", parent, PDTwinRuntimeTheme.PtitRed);
            PDTwinRuntimeUiFactory.AnchorTop(header.GetComponent<RectTransform>(), 102f);

            TextMeshProUGUI eyebrow = PDTwinRuntimeUiFactory.CreateText(
                "Eyebrow", header.transform, "PTIT • ĐÁNH GIÁ THỰC HÀNH", 13f,
                new Color(1f, 1f, 1f, 0.84f), FontStyles.Bold);
            PlaceStretch(eyebrow.rectTransform, 24f, 55f, 70f, 17f);

            TextMeshProUGUI heading = PDTwinRuntimeUiFactory.CreateText(
                "Heading", header.transform,
                string.IsNullOrWhiteSpace(title) ? "BÀI THỰC HÀNH 1 • CHI TIẾT ĐIỂM" : title,
                25f, Color.white, FontStyles.Bold);
            PlaceStretch(heading.rectTransform, 24f, 17f, 70f, 40f);

            Button close = PDTwinRuntimeUiFactory.CreateButton(
                "CloseButton", header.transform, "×", new Color(1f, 1f, 1f, 0.18f), Color.white, 29f);
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-18f, 0f);
            closeRect.sizeDelta = new Vector2(46f, 46f);
            close.onClick.AddListener(CloseDrawer);
        }

        private void BuildBody(Transform parent)
        {
            GameObject viewport = PDTwinRuntimeUiFactory.CreateRect("BodyViewport", parent, PDTwinRuntimeTheme.SurfaceMuted);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(0f, 90f);
            viewportRect.offsetMax = new Vector2(0f, -102f);
            viewport.AddComponent<RectMask2D>();

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
            scroll.viewport = viewportRect;

            GameObject body = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            body.transform.SetParent(viewport.transform, false);
            RectTransform bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = Vector2.one;
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
            scroll.content = bodyRect;

            ContentSizeFitter fitter = body.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup layout = body.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(21, 21, 18, 24);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            BuildSubmissionPill(body.transform);
            BuildScoreCard(body.transform);
            MakeSectionLabel(body.transform, "TỔNG QUAN THÀNH PHẦN ĐIỂM");
            for (int i = 0; i < overviewRows.Length; i++)
                overviewRows[i] = BuildOverviewRow(body.transform, i);
            MakeSectionLabel(body.transform, "TEST ĐANG HIỂN THỊ");
            BuildTestCard(body.transform);
        }

        private void BuildSubmissionPill(Transform parent)
        {
            GameObject pill = CreateLayoutCard("SubmissionPill", parent, PDTwinRuntimeTheme.PtitRedSoft, 38f);
            submissionState = PDTwinRuntimeUiFactory.CreateText(
                "State", pill.transform, "●  Điểm tạm • chưa nộp bài", 13f,
                PDTwinRuntimeTheme.PtitRedDark, FontStyles.Bold);
            PDTwinRuntimeUiFactory.Stretch(submissionState.rectTransform, 12f, 5f, 12f, 5f);
        }

        private void BuildScoreCard(Transform parent)
        {
            GameObject card = CreateLayoutCard("ScoreCard", parent, PDTwinRuntimeTheme.Surface, 118f);
            Outline border = card.AddComponent<Outline>();
            border.effectColor = PDTwinRuntimeTheme.ProgressTrack;
            border.effectDistance = new Vector2(1f, -1f);

            Image track = PDTwinRuntimeUiFactory.CreateRect("RingTrack", card.transform, PDTwinRuntimeTheme.ProgressTrack).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(track);
            Place(track.rectTransform, 17f, 20f, 78f, 78f);

            scoreRingFill = PDTwinRuntimeUiFactory.CreateRect("RingProgress", track.transform, PDTwinRuntimeTheme.PtitRed).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(scoreRingFill);
            PDTwinRuntimeUiFactory.Stretch(scoreRingFill.rectTransform);
            scoreRingFill.type = Image.Type.Filled;
            scoreRingFill.fillMethod = Image.FillMethod.Radial360;
            scoreRingFill.fillOrigin = (int)Image.Origin360.Top;
            scoreRingFill.fillClockwise = true;
            scoreRingFill.fillAmount = 0f;

            Image inner = PDTwinRuntimeUiFactory.CreateRect("RingInner", track.transform, PDTwinRuntimeTheme.Surface).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(inner);
            PDTwinRuntimeUiFactory.Stretch(inner.rectTransform, 7f, 7f, 7f, 7f);
            scoreRingText = PDTwinRuntimeUiFactory.CreateText("RingText", inner.transform, "0.0", 21f, PDTwinRuntimeTheme.PtitRedDark, FontStyles.Bold, TextAlignmentOptions.Center);
            PDTwinRuntimeUiFactory.Stretch(scoreRingText.rectTransform);

            scoreText = PDTwinRuntimeUiFactory.CreateText("Score", card.transform, "0.00 / 10", 29f, PDTwinRuntimeTheme.Ink, FontStyles.Bold);
            PlaceStretch(scoreText.rectTransform, 112f, 52f, 18f, 42f);
            scoreCaption = PDTwinRuntimeUiFactory.CreateText("Caption", card.transform, "Điểm tốt nhất đã được hệ thống xác nhận", 13f, PDTwinRuntimeTheme.Muted);
            PlaceStretch(scoreCaption.rectTransform, 112f, 20f, 18f, 31f);
        }

        private OverviewRow BuildOverviewRow(Transform parent, int rowIndex)
        {
            GameObject card = CreateLayoutCard("Overview_" + rowIndex, parent, PDTwinRuntimeTheme.Surface, 78f);
            Button button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            int capturedIndex = rowIndex;
            button.onClick.AddListener(() =>
            {
                if (capturedIndex > 0)
                    ReviewTestRequested?.Invoke(capturedIndex - 1);
            });

            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = PDTwinRuntimeTheme.PtitRed;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.enabled = false;

            Image dot = PDTwinRuntimeUiFactory.CreateRect("Dot", card.transform, PDTwinRuntimeTheme.Disabled).GetComponent<Image>();
            PDTwinRuntimeUiFactory.SetCircle(dot);
            RectTransform dotRect = dot.rectTransform;
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.pivot = new Vector2(0f, 0.5f);
            dotRect.anchoredPosition = new Vector2(15f, 0f);
            dotRect.sizeDelta = new Vector2(13f, 13f);

            TextMeshProUGUI title = PDTwinRuntimeUiFactory.CreateText("Title", card.transform, "Thành phần điểm", 15f, PDTwinRuntimeTheme.Ink, FontStyles.Bold);
            Place(title.rectTransform, 39f, 38f, 280f, 25f);
            TextMeshProUGUI evidence = PDTwinRuntimeUiFactory.CreateText("Evidence", card.transform, "Chưa có dữ liệu", 12.5f, PDTwinRuntimeTheme.Muted);
            PlaceStretch(evidence.rectTransform, 39f, 10f, 155f, 29f);
            TextMeshProUGUI state = PDTwinRuntimeUiFactory.CreateText("State", card.transform, "CHƯA CHẤM", 11f, PDTwinRuntimeTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Right);
            PlaceRight(state.rectTransform, 16f, 42f, 125f, 22f);
            TextMeshProUGUI score = PDTwinRuntimeUiFactory.CreateText("Score", card.transform, "0.00 / 0.00", 15f, PDTwinRuntimeTheme.Ink, FontStyles.Bold, TextAlignmentOptions.Right);
            PlaceRight(score.rectTransform, 16f, 11f, 145f, 28f);

            return new OverviewRow { card = card.GetComponent<Image>(), dot = dot, title = title, evidence = evidence, state = state, score = score, button = button, outline = outline };
        }

        private void BuildTestCard(Transform parent)
        {
            const float cardHeight = 590f;
            GameObject card = CreateLayoutCard("TestCard", parent, PDTwinRuntimeTheme.Surface, cardHeight);

            testEyebrow = PDTwinRuntimeUiFactory.CreateText("Eyebrow", card.transform, "TEST 1 • CHẾ ĐỘ SỐ VÒNG", 12f, PDTwinRuntimeTheme.PtitRedDark, FontStyles.Bold);
            PlaceStretch(testEyebrow.rectTransform, 17f, 546f, 110f, 22f);
            testTitle = PDTwinRuntimeUiFactory.CreateText("Title", card.transform, "Quay thuận 1 vòng ở 10 RPM", 19f, PDTwinRuntimeTheme.Ink, FontStyles.Bold);
            PlaceStretch(testTitle.rectTransform, 17f, 511f, 110f, 34f);

            testBadgeSurface = PDTwinRuntimeUiFactory.CreateRect("Badge", card.transform, PDTwinRuntimeTheme.WarningSoft, true).GetComponent<Image>();
            PlaceRight(testBadgeSurface.rectTransform, 16f, 522f, 92f, 38f);
            testBadge = PDTwinRuntimeUiFactory.CreateText("Label", testBadgeSurface.transform, "SẴN SÀNG", 11f, PDTwinRuntimeTheme.Warning, FontStyles.Bold, TextAlignmentOptions.Center);
            PDTwinRuntimeUiFactory.Stretch(testBadge.rectTransform, 5f, 4f, 5f, 4f);

            GameObject chipRow = new GameObject("Targets", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            chipRow.transform.SetParent(card.transform, false);
            RectTransform chipRowRect = chipRow.GetComponent<RectTransform>();
            chipRowRect.anchorMin = new Vector2(0f, 1f);
            chipRowRect.anchorMax = new Vector2(1f, 1f);
            chipRowRect.pivot = new Vector2(0.5f, 1f);
            chipRowRect.offsetMin = new Vector2(16f, -166f);
            chipRowRect.offsetMax = new Vector2(-16f, -82f);
            HorizontalLayoutGroup chipLayout = chipRow.GetComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 8f;
            chipLayout.childControlWidth = true;
            chipLayout.childControlHeight = true;
            chipLayout.childForceExpandWidth = true;
            chipLayout.childForceExpandHeight = true;

            for (int i = 0; i < 4; i++)
            {
                GameObject chip = PDTwinRuntimeUiFactory.CreateRect("Target_" + i, chipRow.transform, PDTwinRuntimeTheme.SurfaceMuted, true);
                chipLabels[i] = PDTwinRuntimeUiFactory.CreateText("Label", chip.transform, "MỤC TIÊU", 10f, PDTwinRuntimeTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
                PlaceStretch(chipLabels[i].rectTransform, 5f, 43f, 5f, 20f);
                chipValues[i] = PDTwinRuntimeUiFactory.CreateText("Value", chip.transform, "—", 14f, PDTwinRuntimeTheme.Ink, FontStyles.Bold, TextAlignmentOptions.Center);
                PlaceStretch(chipValues[i].rectTransform, 5f, 13f, 5f, 30f);
            }

            BuildEvidenceTable(card.transform);

            statusCard = PDTwinRuntimeUiFactory.CreateRect("EvaluationStatus", card.transform, PDTwinRuntimeTheme.WarningSoft, true).GetComponent<Image>();
            RectTransform statusRect = statusCard.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.offsetMin = new Vector2(16f, -471f);
            statusRect.offsetMax = new Vector2(-16f, -383f);
            statusText = PDTwinRuntimeUiFactory.CreateText("Status", statusCard.transform, "Hệ thống đang chuẩn bị dữ liệu chấm.", 13.5f, PDTwinRuntimeTheme.Warning, FontStyles.Bold);
            PDTwinRuntimeUiFactory.Stretch(statusText.rectTransform, 14f, 10f, 14f, 10f);

            GameObject attemptCard = PDTwinRuntimeUiFactory.CreateRect("AttemptSummary", card.transform, PDTwinRuntimeTheme.SurfaceMuted, true);
            RectTransform attemptRect = attemptCard.GetComponent<RectTransform>();
            attemptRect.anchorMin = new Vector2(0f, 1f);
            attemptRect.anchorMax = new Vector2(1f, 1f);
            attemptRect.pivot = new Vector2(0.5f, 1f);
            attemptRect.offsetMin = new Vector2(16f, -573f);
            attemptRect.offsetMax = new Vector2(-16f, -484f);
            TextMeshProUGUI label = PDTwinRuntimeUiFactory.CreateText("Label", attemptCard.transform, "TRẠNG THÁI LƯỢT CHẤM", 10.5f, PDTwinRuntimeTheme.Muted, FontStyles.Bold);
            PlaceStretch(label.rectTransform, 13f, 50f, 13f, 20f);
            attemptText = PDTwinRuntimeUiFactory.CreateText("Summary", attemptCard.transform, "Có tối đa 3 lượt nếu chưa đạt.", 13f, PDTwinRuntimeTheme.Ink);
            PlaceStretch(attemptText.rectTransform, 13f, 12f, 13f, 37f);
        }

        private void BuildEvidenceTable(Transform parent)
        {
            GameObject table = PDTwinRuntimeUiFactory.CreateRect("TelemetryEvidence", parent, PDTwinRuntimeTheme.Surface, true);
            RectTransform tableRect = table.GetComponent<RectTransform>();
            tableRect.anchorMin = new Vector2(0f, 1f);
            tableRect.anchorMax = new Vector2(1f, 1f);
            tableRect.pivot = new Vector2(0.5f, 1f);
            tableRect.offsetMin = new Vector2(16f, -370f);
            tableRect.offsetMax = new Vector2(-16f, -178f);
            Outline outline = table.AddComponent<Outline>();
            outline.effectColor = PDTwinRuntimeTheme.Border;
            outline.effectDistance = new Vector2(1f, -1f);

            string[] defaults = { "Gateway & telemetry", "Chuỗi vận hành", "RPM thực", "Encoder khi dừng" };
            for (int i = 0; i < evidenceRows.Length; i++)
            {
                GameObject row = PDTwinRuntimeUiFactory.CreateRect("Evidence_" + i, table.transform,
                    i % 2 == 0 ? PDTwinRuntimeTheme.Surface : PDTwinRuntimeTheme.SurfaceMuted);
                RectTransform rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = Vector2.one;
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.offsetMin = new Vector2(0f, -(i + 1) * 48f);
                rowRect.offsetMax = new Vector2(0f, -i * 48f);

                Image badge = PDTwinRuntimeUiFactory.CreateRect("Badge", row.transform,
                    PDTwinRuntimeTheme.HealthColor(RuntimeHealth.Unknown)).GetComponent<Image>();
                PDTwinRuntimeUiFactory.SetCircle(badge);
                RectTransform badgeRect = badge.rectTransform;
                badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0f, 0.5f);
                badgeRect.pivot = new Vector2(0f, 0.5f);
                badgeRect.anchoredPosition = new Vector2(12f, 0f);
                badgeRect.sizeDelta = new Vector2(24f, 24f);

                TextMeshProUGUI marker = PDTwinRuntimeUiFactory.CreateText("Marker", badge.transform, "·", 14f,
                    Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
                PDTwinRuntimeUiFactory.Stretch(marker.rectTransform);

                TextMeshProUGUI label = PDTwinRuntimeUiFactory.CreateText("Label", row.transform, defaults[i], 13.5f,
                    PDTwinRuntimeTheme.Ink, FontStyles.Normal);
                PlaceStretch(label.rectTransform, 48f, 11f, 330f, 27f);

                TextMeshProUGUI value = PDTwinRuntimeUiFactory.CreateText("Value", row.transform, "Chưa có dữ liệu", 12.5f,
                    PDTwinRuntimeTheme.Ink, FontStyles.Bold, TextAlignmentOptions.Right);
                PlaceRight(value.rectTransform, 13f, 9f, 300f, 30f);
                evidenceRows[i] = new EvidenceRow { badge = badge, marker = marker, label = label, value = value };
            }
        }

        private void BuildFooter(Transform parent)
        {
            GameObject footer = PDTwinRuntimeUiFactory.CreateRect("Footer", parent, PDTwinRuntimeTheme.Surface);
            PDTwinRuntimeUiFactory.AnchorBottom(footer.GetComponent<RectTransform>(), 90f);
            GameObject divider = PDTwinRuntimeUiFactory.CreateRect("Divider", footer.transform, PDTwinRuntimeTheme.Border);
            PDTwinRuntimeUiFactory.AnchorTop(divider.GetComponent<RectTransform>(), 1f);

            GameObject row = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(footer.transform, false);
            PDTwinRuntimeUiFactory.Stretch(row.GetComponent<RectTransform>(), 20f, 18f, 20f, 17f);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            primaryButton = PDTwinRuntimeUiFactory.CreateButton("PrimaryAction", row.transform, "BẮT ĐẦU CHẤM TEST 1", PDTwinRuntimeTheme.PtitRed, Color.white, 13.5f);
            submitButton = PDTwinRuntimeUiFactory.CreateButton("SubmitAction", row.transform, "XEM LẠI VÀ NỘP", PDTwinRuntimeTheme.SurfaceMuted, PDTwinRuntimeTheme.Muted, 13.5f);
            primaryButton.onClick.AddListener(() => PrimaryRequested?.Invoke());
            submitButton.onClick.AddListener(() => confirmModal.SetActive(true));
        }

        private void BuildConfirmation(Transform parent)
        {
            confirmModal = PDTwinRuntimeUiFactory.CreateRect("SubmitConfirmation", parent, new Color(0.04f, 0.06f, 0.10f, 0.72f));
            PDTwinRuntimeUiFactory.Stretch(confirmModal.GetComponent<RectTransform>());
            GameObject card = PDTwinRuntimeUiFactory.CreateRect("Card", confirmModal.transform, PDTwinRuntimeTheme.Surface, true);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(440f, 250f);

            TextMeshProUGUI title = PDTwinRuntimeUiFactory.CreateText("Title", card.transform, "Xác nhận nộp bài?", 24f, PDTwinRuntimeTheme.Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            PDTwinRuntimeUiFactory.AnchorTop(title.rectTransform, 42f, 22f, 22f, 27f);
            TextMeshProUGUI note = PDTwinRuntimeUiFactory.CreateText("Note", card.transform, "Điểm tốt nhất đã lưu sẽ được ghi nhận chính thức và khóa bài.", 15f, PDTwinRuntimeTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center);
            PDTwinRuntimeUiFactory.AnchorTop(note.rectTransform, 62f, 34f, 34f, 80f);

            Button cancel = PDTwinRuntimeUiFactory.CreateButton("Cancel", card.transform, "XEM LẠI", PDTwinRuntimeTheme.SurfaceMuted, PDTwinRuntimeTheme.Ink, 14f);
            RectTransform cancelRect = cancel.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.offsetMin = new Vector2(24f, 25f);
            cancelRect.offsetMax = new Vector2(-6f, 78f);
            Button confirm = PDTwinRuntimeUiFactory.CreateButton("Confirm", card.transform, "XÁC NHẬN NỘP", PDTwinRuntimeTheme.PtitRed, Color.white, 14f);
            RectTransform confirmRect = confirm.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(1f, 0f);
            confirmRect.offsetMin = new Vector2(6f, 25f);
            confirmRect.offsetMax = new Vector2(-24f, 78f);

            cancel.onClick.AddListener(() => confirmModal.SetActive(false));
            confirm.onClick.AddListener(() =>
            {
                confirmModal.SetActive(false);
                SubmitRequested?.Invoke();
            });
            confirmModal.SetActive(false);
        }

        public void SetSubmissionState(bool submitted, bool autoSubmitted = false)
        {
            if (submissionState == null)
                return;
            submissionState.text = submitted
                ? autoSubmitted ? "●  Hết thời gian • đã tự động nộp bài" : "●  Đã nộp • điểm chính thức"
                : "●  Điểm tạm • chưa nộp bài";
            submissionState.color = submitted ? PDTwinRuntimeTheme.Success : PDTwinRuntimeTheme.PtitRedDark;
        }

        public void SetScore(float score, bool submitted)
        {
            float safeScore = Mathf.Clamp(score, 0f, 10f);
            scoreText.text = safeScore.ToString("F2") + " / 10";
            scoreRingText.text = safeScore.ToString("F1");
            scoreRingFill.fillAmount = safeScore / 10f;
            scoreRingFill.color = submitted ? PDTwinRuntimeTheme.Success : PDTwinRuntimeTheme.PtitRed;
            scoreCaption.text = submitted ? "Điểm chính thức đã khóa" : "Điểm tốt nhất đã được hệ thống xác nhận";
            statusBar?.SetScore(safeScore, submitted);
            SetSubmissionState(submitted);
        }

        public void SetOverview(PointCriterionView[] items)
        {
            for (int i = 0; i < overviewRows.Length; i++)
            {
                OverviewRow row = overviewRows[i];
                bool visible = items != null && i < items.Length && items[i] != null;
                row.card.gameObject.SetActive(visible);
                if (!visible)
                    continue;
                PointCriterionView item = items[i];
                row.title.text = item.title ?? string.Empty;
                row.evidence.text = item.evidence ?? string.Empty;
                row.state.text = item.state ?? string.Empty;
                row.score.text = $"{item.earned:F2} / {item.maximum:F2}";
                row.dot.color = PDTwinRuntimeTheme.HealthColor(item.health);
                row.state.color = PDTwinRuntimeTheme.HealthColor(item.health);
                row.card.color = item.selected ? PDTwinRuntimeTheme.PtitRedSoft : PDTwinRuntimeTheme.Surface;
                row.outline.enabled = item.selected;
                row.button.interactable = item.interactive;
            }
        }

        public void SetTestHeader(string eyebrow, string title, string badge, RuntimeHealth health)
        {
            testEyebrow.text = eyebrow ?? string.Empty;
            testTitle.text = title ?? string.Empty;
            testBadge.text = badge ?? string.Empty;
            testBadge.color = PDTwinRuntimeTheme.HealthColor(health);
            testBadgeSurface.color = PDTwinRuntimeTheme.HealthSurface(health);
        }

        public void SetTargetChips(string[] labels, string[] values)
        {
            for (int i = 0; i < chipLabels.Length; i++)
            {
                chipLabels[i].text = labels != null && i < labels.Length ? labels[i] ?? string.Empty : string.Empty;
                chipValues[i].text = values != null && i < values.Length ? values[i] ?? string.Empty : "—";
            }
        }

        public void SetEvidenceRows(string[] labels, string[] values, RuntimeHealth[] health)
        {
            for (int i = 0; i < evidenceRows.Length; i++)
            {
                EvidenceRow row = evidenceRows[i];
                RuntimeHealth rowHealth = health != null && i < health.Length
                    ? health[i]
                    : RuntimeHealth.Unknown;
                if (labels != null && i < labels.Length && !string.IsNullOrWhiteSpace(labels[i]))
                    row.label.text = labels[i];
                row.value.text = values != null && i < values.Length ? values[i] ?? "—" : "—";
                row.value.color = rowHealth == RuntimeHealth.Offline
                    ? PDTwinRuntimeTheme.Danger
                    : rowHealth == RuntimeHealth.Online ? PDTwinRuntimeTheme.Success : PDTwinRuntimeTheme.Ink;
                row.badge.color = PDTwinRuntimeTheme.HealthColor(rowHealth);
                row.marker.text = rowHealth == RuntimeHealth.Online ? "✓"
                    : rowHealth == RuntimeHealth.Offline ? "!"
                    : rowHealth == RuntimeHealth.Warning ? "•" : "·";
            }
        }

        public void SetStatus(string value, RuntimeHealth health = RuntimeHealth.Warning)
        {
            statusText.text = string.IsNullOrWhiteSpace(value) ? "Chưa có dữ liệu chấm." : value;
            statusText.color = PDTwinRuntimeTheme.HealthColor(health);
            statusCard.color = PDTwinRuntimeTheme.HealthSurface(health);
        }

        public void SetAttemptSummary(string value)
        {
            attemptText.text = string.IsNullOrWhiteSpace(value) ? "Có tối đa 3 lượt nếu chưa đạt." : value;
        }

        public void SetActions(string primaryLabel, bool primaryEnabled, bool submitEnabled)
        {
            SetButtonAppearance(primaryButton, primaryLabel, primaryEnabled ? PDTwinRuntimeTheme.PtitRed : PDTwinRuntimeTheme.SurfaceMuted, primaryEnabled ? Color.white : PDTwinRuntimeTheme.Muted);
            primaryButton.interactable = primaryEnabled;
            SetButtonAppearance(submitButton, "XEM LẠI VÀ NỘP", submitEnabled ? PDTwinRuntimeTheme.PtitRed : PDTwinRuntimeTheme.SurfaceMuted, submitEnabled ? Color.white : PDTwinRuntimeTheme.Muted);
            submitButton.interactable = submitEnabled;
        }

        public void LockAfterSubmit(bool autoSubmitted)
        {
            SetSubmissionState(true, autoSubmitted);
            SetActions("ĐÃ KHÓA BÀI", false, false);
        }

        private void ToggleDrawer()
        {
            if (drawerOverlay != null)
                drawerOverlay.SetActive(!drawerOverlay.activeSelf);
        }

        private void CloseDrawer()
        {
            if (drawerOverlay != null)
                drawerOverlay.SetActive(false);
        }

        private static GameObject CreateLayoutCard(string name, Transform parent, Color color, float height)
        {
            GameObject card = PDTwinRuntimeUiFactory.CreateRect(name, parent, color, true);
            LayoutElement element = card.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return card;
        }

        private static TextMeshProUGUI MakeSectionLabel(Transform parent, string value)
        {
            TextMeshProUGUI label = PDTwinRuntimeUiFactory.CreateText("SectionLabel", parent, value, 11f, PDTwinRuntimeTheme.Muted, FontStyles.Bold);
            LayoutElement element = label.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 24f;
            element.minHeight = 24f;
            return label;
        }

        private static void SetButtonAppearance(Button button, string label, Color background, Color foreground)
        {
            if (button == null)
                return;
            if (button.targetGraphic is Image image)
                image.color = background;
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label ?? string.Empty;
                text.color = foreground;
            }
        }

        private static void Place(RectTransform rect, float left, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceRight(RectTransform rect, float right, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-right, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceStretch(RectTransform rect, float left, float bottom, float right, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }
    }
}

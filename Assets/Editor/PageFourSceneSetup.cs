using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PageFourSceneSetup
{
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity", OpenSceneMode.Single);
        BuildOrUpdate(scene);
    }

    private static void SetupWhenPossible()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
        const string path = "Assets/Scenes/StartScene.unity";
        Scene scene = SceneManager.GetSceneByPath(path);
        bool closeAfter = !scene.isLoaded;
        if (closeAfter)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
        BuildOrUpdate(scene);
        if (closeAfter)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void BuildOrUpdate(Scene scene)
    {
        GameObject page = FindInScene(scene, "Trang 4");
        if (page == null)
        {
            return;
        }

        Transform content = page.transform.Find("PageFourContent");
        Transform model = page.transform.Find("PageFourModel");
        if (model == null)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d_Thay_Tien_1.fbx");
            GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset, page.transform) as GameObject;
            modelInstance.name = "PageFourModel";
            Transform pageTwoModel = FindInScene(scene, "3d_Thay_Tien_1")?.transform;
            if (pageTwoModel != null)
            {
                modelInstance.transform.localPosition = pageTwoModel.localPosition;
                modelInstance.transform.localRotation = pageTwoModel.localRotation;
                modelInstance.transform.localScale = pageTwoModel.localScale;
            }
            model = modelInstance.transform;
        }

        Button playButton;
        RectTransform cursor;
        if (content == null)
        {
            RectTransform pageRect = page.transform as RectTransform;
            RectTransform contentRect = CreateRect(pageRect, "PageFourContent", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            content = contentRect;
            CreateText(contentRect, "PageFourTitle", "Hướng dẫn thao tác cắm dây", 48f,
                new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 76f), TextAlignmentOptions.Center);
            playButton = CreateButton(contentRect, "PlayButton", "▶  Play", new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(180f, 60f));
            cursor = CreateHandCursor(contentRect);
        }
        else
        {
            playButton = content.Find("PlayButton")?.GetComponent<Button>();
            cursor = content.Find("CursorObject") as RectTransform;
        }

        Texture2D handIcons = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HandIcons.png");
        RawImage cursorImage = cursor != null ? cursor.GetComponentInChildren<RawImage>(true) : null;
        if (cursor != null && cursorImage == null)
        {
            TextMeshProUGUI oldText = cursor.GetComponent<TextMeshProUGUI>();
            if (oldText != null)
            {
                oldText.enabled = false;
            }
            GameObject handImage = new GameObject("HandImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            handImage.transform.SetParent(cursor, false);
            handImage.layer = 5;
            RectTransform handRect = handImage.GetComponent<RectTransform>();
            handRect.anchorMin = Vector2.zero;
            handRect.anchorMax = Vector2.one;
            handRect.offsetMin = Vector2.zero;
            handRect.offsetMax = Vector2.zero;
            cursorImage = handImage.GetComponent<RawImage>();
        }
        if (cursorImage != null)
        {
            cursor.sizeDelta = new Vector2(36f, 36f);
            cursorImage.texture = handIcons;
            cursorImage.uvRect = new Rect(0f, 0f, 0.5f, 1f);
            cursorImage.color = Color.black;
            cursorImage.raycastTarget = false;
        }
        PageFourWiringTutorialController controller = page.GetComponent<PageFourWiringTutorialController>();
        if (controller == null)
        {
            controller = page.AddComponent<PageFourWiringTutorialController>();
        }
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("modelRoot").objectReferenceValue = model;
        serialized.FindProperty("modelPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d_Thay_Tien_1.fbx");
        serialized.FindProperty("playButton").objectReferenceValue = playButton;
        serialized.FindProperty("cursorObject").objectReferenceValue = cursor;
        serialized.FindProperty("cursorImage").objectReferenceValue = cursorImage;
        serialized.FindProperty("handIconsTexture").objectReferenceValue = handIcons;
        serialized.FindProperty("jack35Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jack 3.5mm.fbx");
        serialized.FindProperty("wireMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Red_wire.mat");
        serialized.FindProperty("jackBodyMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WirePlugOverlay/WirePlugOverlay_Red.mat");
        serialized.FindProperty("moveDuration").floatValue = 1.35f;
        serialized.FindProperty("cursorApproachDuration").floatValue = 1f;
        serialized.FindProperty("cursorTransferDuration").floatValue = 0.8f;
        serialized.FindProperty("cursorReleaseDuration").floatValue = 1f;
        serialized.FindProperty("socketANormalized").vector2Value = new Vector2(0.332f, 0.295f);
        serialized.FindProperty("socketBNormalized").vector2Value = new Vector2(0.716f, 0.322f);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scene.path);
        AssetDatabase.SaveAssets();
        Debug.Log("[PageFourSceneSetup] Đã tạo hướng dẫn cắm dây cho Trang 4.");
    }

    private static RectTransform CreateRect(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize,
        Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.black;
        return text;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        gameObject.GetComponent<Image>().color = Color.white;
        Outline outline = gameObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.1f, 0.13f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        TextMeshProUGUI text = CreateText(rect, "Label", label, 30f, new Vector2(0.5f, 0.5f), Vector2.zero,
            size - new Vector2(16f, 8f), TextAlignmentOptions.Center);
        text.raycastTarget = false;
        return gameObject.GetComponent<Button>();
    }

    private static RectTransform CreateHandCursor(RectTransform parent)
    {
        GameObject gameObject = new GameObject("CursorObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(36f, 36f);
        return rect;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }
        }
        return null;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class SceneAuthoredWiringBoardSetup
{
    private const string ScenePath = "Assets/Scenes/Sy_scene.unity";
    private const string TexturePath = "Assets/Resources/NewServoWiringBoard.png";
    private const string MaterialPath = "Assets/Materials/NewServoWiringBoard_2D.mat";
    private const string RootName = "SceneAuthoredWiringBoard2D";
    private const float JackDiameterPixels = 14f;
    private const float SceneBoardScaleMultiplier = 1.7f;

    private static readonly Dictionary<string, Vector2> SocketPixels =
        new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase)
        {
            { "24VDC", new Vector2(116f, 228f) }, { "GND_24V", new Vector2(246f, 228f) },
            { "5VDC", new Vector2(396f, 228f) }, { "GND_5V", new Vector2(523f, 228f) },
            { "X4", new Vector2(137f, 410.5f) }, { "X3", new Vector2(137f, 473.5f) },
            { "X2", new Vector2(137f, 537.5f) }, { "X1", new Vector2(137f, 600.5f) },
            { "X0", new Vector2(137f, 665.5f) }, { "SS", new Vector2(137f, 730.5f) },
            { "Y4", new Vector2(409f, 418.5f) }, { "Y3", new Vector2(409f, 486f) },
            { "Y2", new Vector2(408.5f, 554f) }, { "Y1", new Vector2(408.65f, 624.13f) },
            { "Y0", new Vector2(408.51f, 689.15f) }, { "+V4", new Vector2(537.5f, 427f) },
            { "+V3", new Vector2(537.5f, 502f) }, { "+V2", new Vector2(537.5f, 578.5f) },
            { "+V1", new Vector2(539.2f, 656.6f) }, { "+V0", new Vector2(538.6f, 731.8f) },
            { "Pin11", new Vector2(678f, 338f) }, { "Pin12", new Vector2(678f, 400f) },
            { "Pin9", new Vector2(678f, 460f) }, { "Pin10", new Vector2(678f, 524.5f) },
            { "Pin15", new Vector2(678f, 611f) }, { "Pin16", new Vector2(678f, 672f) },
            { "Pin13", new Vector2(678.85f, 724.9f) }, { "Pin14", new Vector2(678.75f, 782.2f) },
            { "oA", new Vector2(863.5f, 377f) }, { "oB", new Vector2(865.5f, 458.5f) },
            { "oC", new Vector2(865.5f, 535f) }, { "Motor_A", new Vector2(1087f, 562f) },
            { "Motor_B", new Vector2(1087f, 621f) }, { "Motor_C", new Vector2(1087f, 680f) },
            { "Enc_A", new Vector2(1130f, 802f) }, { "Enc_B", new Vector2(1130.5f, 885.5f) }
        };

    [MenuItem("Tools/Digital Twin/Build scene-authored 2D wiring board")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = FindInScene<Camera>(scene);
        GameObject legacyBoard = FindByName(scene, "Board");
        GameObject socketsObject = FindByName(scene, "Sockets");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (camera == null || legacyBoard == null || socketsObject == null || texture == null)
            throw new InvalidOperationException("Sy_scene requires Main Camera, Board, Sockets and NewServoWiringBoard.png.");

        Renderer legacyRenderer = legacyBoard.GetComponent<Renderer>();
        if (legacyRenderer == null)
            throw new InvalidOperationException("Board does not have a Renderer.");

        Dictionary<string, (Vector3 position, Quaternion rotation)> operationPoses =
            CaptureOperationPoses(scene);
        GameObject oldRoot = FindByName(scene, RootName);
        if (oldRoot != null)
            UnityEngine.Object.DestroyImmediate(oldRoot);

        GameObject root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        SceneAuthoredWiringBoard authored = root.AddComponent<SceneAuthoredWiringBoard>();
        authored.socketsRoot = socketsObject.transform;

        Bounds bounds = legacyRenderer.bounds;
        Vector3 right = camera.transform.right.normalized;
        Vector3 up = camera.transform.up.normalized;
        Vector3 normal = -camera.transform.forward.normalized;
        float availableWidth = ProjectedSize(bounds, right);
        float availableHeight = ProjectedSize(bounds, up);
        float aspect = texture.width / (float)texture.height;
        float width = availableWidth * 0.92f;
        float height = width / aspect;
        if (height > availableHeight * 0.92f)
        {
            height = availableHeight * 0.92f;
            width = height * aspect;
        }

        // The legacy renderer only occupied a small part of the new white
        // workspace. Author the replacement board at a useful inspection size;
        // CircuitManager performs the final responsive fit to ModelPanel.
        width *= SceneBoardScaleMultiplier;
        height *= SceneBoardScaleMultiplier;

        Vector3 center = bounds.center - up * (availableHeight * 0.07f) + normal * 0.025f;
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        surface.name = "BoardSurface2D";
        surface.transform.SetParent(root.transform, true);
        surface.transform.SetPositionAndRotation(center, camera.transform.rotation);
        surface.transform.localScale = new Vector3(width, height, 1f);
        UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());
        MeshRenderer surfaceRenderer = surface.GetComponent<MeshRenderer>();
        surfaceRenderer.sharedMaterial = GetOrCreateMaterial(texture);
        surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
        surfaceRenderer.receiveShadows = false;
        authored.boardRenderer = surfaceRenderer;

        GameObject jackRoot = new GameObject("JackVisuals");
        jackRoot.transform.SetParent(root.transform, false);
        authored.jackVisualRoot = jackRoot;

        SocketPoint[] sockets = socketsObject.GetComponentsInChildren<SocketPoint>(true);
        foreach (SocketPoint socket in sockets.OrderBy(value => value.socketID, StringComparer.OrdinalIgnoreCase))
        {
            if (!SocketPixels.TryGetValue(socket.socketID, out Vector2 pixel))
            {
                Debug.LogWarning($"[SceneAuthoredWiringBoardSetup] No board coordinate for socket {socket.socketID}.");
                continue;
            }

            (Vector3 operationPosition, Quaternion operationRotation) = operationPoses.TryGetValue(socket.socketID, out var pose)
                ? pose
                : (socket.transform.position, socket.transform.rotation);
            float horizontal = pixel.x / texture.width - 0.5f;
            float vertical = 0.5f - pixel.y / texture.height;
            Vector3 wiringPosition = surface.transform.TransformPoint(
                new Vector3(horizontal, vertical, 0f)) + normal * 0.01f;
            Quaternion wiringRotation = surface.transform.rotation;
            socket.transform.SetPositionAndRotation(wiringPosition, wiringRotation);

            authored.sockets.Add(socket);
            authored.operationPositions.Add(operationPosition);
            authored.operationRotations.Add(operationRotation);
            authored.wiringPositions.Add(wiringPosition);
            authored.wiringRotations.Add(wiringRotation);
            CreateJackVisual(socket, jackRoot.transform, camera);
        }

        HashSet<string> interactiveSocketIds = new HashSet<string>(
            sockets.Select(socket => socket.socketID),
            StringComparer.OrdinalIgnoreCase);
        GameObject obstacleRoot = new GameObject("NonInteractiveSocketObstacles");
        obstacleRoot.transform.SetParent(surface.transform, false);
        foreach (KeyValuePair<string, Vector2> entry in SocketPixels)
        {
            if (interactiveSocketIds.Contains(entry.Key))
                continue;

            float horizontal = entry.Value.x / texture.width - 0.5f;
            float vertical = 0.5f - entry.Value.y / texture.height;
            Vector3 position = surface.transform.TransformPoint(
                new Vector3(horizontal, vertical, 0f)) + normal * 0.012f;
            GameObject obstacle = new GameObject($"AvoidSocket_{entry.Key}");
            obstacle.transform.SetParent(surface.transform, true);
            obstacle.transform.position = position;
            obstacle.AddComponent<WireRoutingObstacle>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SceneAuthoredWiringBoardSetup] Built persistent 2D board with {authored.sockets.Count} existing sockets.");
    }

    private static Dictionary<string, (Vector3, Quaternion)> CaptureOperationPoses(Scene scene)
    {
        Dictionary<string, (Vector3, Quaternion)> result = new Dictionary<string, (Vector3, Quaternion)>(StringComparer.OrdinalIgnoreCase);
        SceneAuthoredWiringBoard previous = FindInScene<SceneAuthoredWiringBoard>(scene);
        foreach (SocketPoint socket in FindAllInScene<SocketPoint>(scene))
        {
            if (previous != null && previous.TryGetPoses(socket, out Vector3 position, out Quaternion rotation, out _, out _))
                result[socket.socketID] = (position, rotation);
            else
                result[socket.socketID] = (socket.transform.position, socket.transform.rotation);
        }
        return result;
    }

    private static Material GetOrCreateMaterial(Texture2D texture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            material = new Material(shader) { name = "NewServoWiringBoard_2D" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateJackVisual(SocketPoint socket, Transform parent, Camera camera)
    {
        string color = GetJackColor(socket);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/NewWiring/JackConnector_{color}.fbx");
        if (prefab == null) return;
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        visual.name = $"Jack_{socket.socketID}_{color}";
        visual.transform.SetPositionAndRotation(
            socket.transform.position,
            Quaternion.LookRotation(
                (camera.transform.position - socket.transform.position).normalized,
                camera.transform.up));
        if (TryGetBounds(visual, out Bounds bounds))
        {
            float diameter = Mathf.Max(ProjectedSize(bounds, camera.transform.right), ProjectedSize(bounds, camera.transform.up));
            float target = ScreenPixelsToWorld(camera, socket.transform.position, JackDiameterPixels);
            if (diameter > 0.000001f && target > 0f) visual.transform.localScale *= target / diameter;

            // Align in screen space because the jack protrudes toward the
            // camera; matching only its board-plane position causes parallax.
            if (TryGetBounds(visual, out Bounds scaledBounds))
            {
                Vector3 socketScreen = camera.WorldToScreenPoint(socket.transform.position);
                Vector3 visualScreen = camera.WorldToScreenPoint(scaledBounds.center);
                Vector3 alignedCenter = camera.ScreenToWorldPoint(
                    new Vector3(socketScreen.x, socketScreen.y, visualScreen.z));
                visual.transform.position += alignedCenter - scaledBounds.center;
            }
        }
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static string GetJackColor(SocketPoint socket)
    {
        switch (socket.acceptColor)
        {
            case WireColor.Red: return "Red";
            case WireColor.Black: return "Black";
            case WireColor.Yellow: return "Yellow";
            case WireColor.Green: return "Green";
            case WireColor.Blue: return "Blue";
        }
        string id = socket.socketID ?? string.Empty;
        if (id.IndexOf("GND", StringComparison.OrdinalIgnoreCase) >= 0 || new[] { "Pin10", "Pin12", "Pin14", "oA", "Motor_A" }.Contains(id, StringComparer.OrdinalIgnoreCase)) return "Black";
        if (new[] { "24VDC", "5VDC", "oB", "Motor_B" }.Contains(id, StringComparer.OrdinalIgnoreCase) || id.StartsWith("+V", StringComparison.OrdinalIgnoreCase)) return "Red";
        return "Yellow";
    }

    private static float ProjectedSize(Bounds bounds, Vector3 axis) =>
        Mathf.Abs(axis.x) * bounds.size.x + Mathf.Abs(axis.y) * bounds.size.y + Mathf.Abs(axis.z) * bounds.size.z;

    private static float ScreenPixelsToWorld(Camera camera, Vector3 position, float pixels)
    {
        Vector3 screen = camera.WorldToScreenPoint(position);
        if (screen.z <= camera.nearClipPlane) return 0f;
        return Vector3.Distance(camera.ScreenToWorldPoint(screen), camera.ScreenToWorldPoint(new Vector3(screen.x + pixels, screen.y, screen.z)));
    }

    private static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) { bounds = default; return false; }

        bounds = default;
        bool initialized = false;
        foreach (Renderer renderer in renderers)
        {
            Bounds local = renderer.localBounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = local.center + Vector3.Scale(
                    local.extents,
                    new Vector3(x, y, z));
                Vector3 worldCorner = renderer.transform.TransformPoint(corner);
                if (!initialized)
                {
                    bounds = new Bounds(worldCorner, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(worldCorner);
                }
            }
        }

        return initialized;
    }

    private static GameObject FindByName(Scene scene, string name) =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).FirstOrDefault(value => value.name == name)?.gameObject;

    private static T FindInScene<T>(Scene scene) where T : Component => FindAllInScene<T>(scene).FirstOrDefault();

    private static IEnumerable<T> FindAllInScene<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
}

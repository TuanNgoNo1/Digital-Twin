using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WiringModelsSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Sy_scene.unity";

    private static readonly string[] ConnectedWirePaths =
    {
        "Assets/3D_Thay_Tien_Wires/Wires 1/Wire_Head_Red(5VDC-+V0).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 1/Wire_Head_Red(5VDC-+V1).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 1/Wire_Head_Yellow(Y0-Pin11).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 1/Wire_Head_Yellow(Y1-Pin9).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 1/Wire_Head_Black(GND_5V-Pin10).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 1/Wire_Head_Black(GND_5V-Pin12).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 2/Wire_Head_Red(24V-SS).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 2/Wire_Head_Yellow(Enc_A-X4).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 2/Wire_Head_Yellow(Enc_B-X3).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 2/Wire_Head_Yellow(Pin_13-X0).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 2/Wire_Head_Yellow(Pin_15-X1).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 2/Wire_Head_Black(Pin14-GND_5V).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 3/Wire_Head_Black(oA-Motor_A).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 3/Wire_Head_Red(oB-Motor_B).obj",
        "Assets/3D_Thay_Tien_Wires/Wires 3/Wire_Head_Yellow(oC-Motor_C).obj"
    };

    [MenuItem("Tools/Digital Twin/Assign connected wire models")]
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeAfterSetup = !scene.isLoaded;
        if (closeAfterSetup)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        CircuitManager manager = FindInScene(scene, "CircuitManager")?.GetComponent<CircuitManager>();
        if (manager == null)
            throw new System.InvalidOperationException("Could not find CircuitManager in Sy_scene.");

        SerializedObject serialized = new SerializedObject(manager);
        SerializedProperty prefabs = serialized.FindProperty("connectedWirePrefabs");
        prefabs.arraySize = ConnectedWirePaths.Length;

        for (int i = 0; i < ConnectedWirePaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConnectedWirePaths[i]);
            if (prefab == null)
                throw new System.InvalidOperationException($"Could not load connected wire model: {ConnectedWirePaths[i]}");

            prefabs.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        if (closeAfterSetup)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log($"[WiringModelsSceneSetup] Assigned {ConnectedWirePaths.Length} connected wire models.");
    }

    [MenuItem("Tools/Digital Twin/Apply new servo wiring board")]
    public static void ApplyNewServoWiringBoard()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeAfterSetup = !scene.isLoaded;
        if (closeAfterSetup)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        if (FindSocket(scene, "Motor_C") != null)
        {
            if (closeAfterSetup)
                EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[WiringModelsSceneSetup] New servo wiring board is already applied.");
            return;
        }

        // The motor terminal block is top-to-bottom A, B, C.
        RenameSocket(scene, "Motor_A", "Motor_C");
        RenameSocket(scene, "Motor_S", "Motor_A");
        SetSocketColor(scene, "Motor_A", WireColor.Red);
        SetSocketColor(scene, "Motor_B", WireColor.Yellow);
        SetSocketColor(scene, "Motor_C", WireColor.Black);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        if (closeAfterSetup)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log("[WiringModelsSceneSetup] Applied the new servo wiring board socket layout.");
    }

    private static void RenameSocket(Scene scene, string currentId, string newId)
    {
        SocketPoint socket = FindSocket(scene, currentId);
        if (socket == null)
            throw new System.InvalidOperationException($"Could not find socket {currentId}.");

        socket.socketID = newId;
        socket.name = newId;
    }

    private static void SetSocketColor(Scene scene, string id, WireColor color)
    {
        SocketPoint socket = FindSocket(scene, id);
        if (socket == null)
            throw new System.InvalidOperationException($"Could not find socket {id}.");

        socket.acceptColor = color;
    }

    private static SocketPoint FindSocket(Scene scene, string socketId)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SocketPoint socket in root.GetComponentsInChildren<SocketPoint>(true))
            {
                if (socket.socketID == socketId)
                    return socket;
            }
        }

        return null;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child.gameObject;
            }
        }

        return null;
    }
}

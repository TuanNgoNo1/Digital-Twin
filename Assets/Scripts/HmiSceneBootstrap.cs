using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class HmiSceneBootstrap : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null;

        PLCController_v2 plcController = PLCController_v2.Instance != null
            ? PLCController_v2.Instance
            : FindFirstObjectByType<PLCController_v2>();

        if (plcController == null)
        {
            EnsureEventSystem();

            GameObject plcObject = new GameObject("PLC_Manager_HMI");
            plcController = plcObject.AddComponent<PLCController_v2>();
            plcController.enableControlCameraLayout = false;
            plcController.showWireLabels = false;
            plcController.canvasHmiScale = 1f;

            foreach (GameObject sceneObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (sceneObject.scene == gameObject.scene && sceneObject.name == "HMI_Screen")
                {
                    sceneObject.transform.localScale = Vector3.one;
                    break;
                }
            }
        }

        plcController.SetRuntimeHmiVisible(true);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
}

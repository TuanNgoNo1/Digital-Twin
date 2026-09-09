using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-authored presentation data for the flat wiring board. SocketPoint
/// components remain on the existing Sockets hierarchy; this component only
/// stores their operation/wiring poses and the persistent visual objects.
/// </summary>
public sealed class SceneAuthoredWiringBoard : MonoBehaviour
{
    public Renderer boardRenderer;
    public Transform socketsRoot;
    public GameObject jackVisualRoot;
    public List<SocketPoint> sockets = new List<SocketPoint>();
    public List<Vector3> operationPositions = new List<Vector3>();
    public List<Quaternion> operationRotations = new List<Quaternion>();
    public List<Vector3> wiringPositions = new List<Vector3>();
    public List<Quaternion> wiringRotations = new List<Quaternion>();

    public bool TryGetPoses(
        SocketPoint socket,
        out Vector3 operationPosition,
        out Quaternion operationRotation,
        out Vector3 wiringPosition,
        out Quaternion wiringRotation)
    {
        int index = sockets.IndexOf(socket);
        bool valid = index >= 0 &&
            index < operationPositions.Count && index < operationRotations.Count &&
            index < wiringPositions.Count && index < wiringRotations.Count;
        if (valid)
        {
            operationPosition = operationPositions[index];
            operationRotation = operationRotations[index];
            wiringPosition = wiringPositions[index];
            wiringRotation = wiringRotations[index];
            return true;
        }

        operationPosition = wiringPosition = socket != null ? socket.transform.position : Vector3.zero;
        operationRotation = wiringRotation = socket != null ? socket.transform.rotation : Quaternion.identity;
        return false;
    }
}

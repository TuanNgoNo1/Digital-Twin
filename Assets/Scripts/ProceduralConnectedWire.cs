using UnityEngine;
using UnityEngine.Rendering;

public sealed class ProceduralConnectedWire : MonoBehaviour
{
    private const int PathSegments = 24;
    private const int RadialSegments = 10;

    private WireBody sourceWire;
    private Color wireColor;
    private float radius;
    private Mesh wireMesh;
    private Material wireMaterial;

    public void Configure(WireBody wire, Color color)
    {
        sourceWire = wire;
        wireColor = color;
        radius = Mathf.Max(0.0011f, wire != null ? wire.wireWidth * 0.22f : 0.0011f);
        EnsureRenderer();
    }

    public bool Rebuild()
    {
        if (sourceWire == null || sourceWire.plugA == null || sourceWire.plugB == null)
            return false;

        EnsureRenderer();

        Vector3 start = sourceWire.plugA.transform.position;
        Vector3 end = sourceWire.plugB.transform.position;
        if ((end - start).sqrMagnitude < 0.000001f)
            return false;

        Camera camera = Camera.main;
        Vector3 surfaceNormal = camera != null
            ? -camera.transform.forward.normalized
            : -Vector3.forward;
        float surfaceOffset = Mathf.Clamp(radius * 0.8f, 0.0015f, 0.0045f);
        start += surfaceNormal * surfaceOffset;
        end += surfaceNormal * surfaceOffset;

        Vector3 routeRight = camera != null ? camera.transform.right.normalized : Vector3.right;
        Vector3 routeUp = camera != null ? camera.transform.up.normalized : Vector3.up;
        Vector3 delta = end - start;
        float horizontalDistance = Vector3.Dot(delta, routeRight);
        float verticalDistance = Vector3.Dot(delta, routeUp);

        Vector3 bendA;
        Vector3 bendB;
        if (Mathf.Abs(horizontalDistance) >= Mathf.Abs(verticalDistance))
        {
            Vector3 halfHorizontal = routeRight * (horizontalDistance * 0.5f);
            bendA = start + halfHorizontal;
            bendB = end - halfHorizontal;
        }
        else
        {
            Vector3 halfVertical = routeUp * (verticalDistance * 0.5f);
            bendA = start + halfVertical;
            bendB = end - halfVertical;
        }

        int ringCount = PathSegments + 1;
        int capStartIndex = ringCount * RadialSegments;
        Vector3[] vertices = new Vector3[capStartIndex + 2];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[PathSegments * RadialSegments * 6 + RadialSegments * 6];

        Vector3[] centers = new Vector3[ringCount];
        for (int pathIndex = 0; pathIndex < ringCount; pathIndex++)
        {
            float t = pathIndex / (float)PathSegments;
            centers[pathIndex] = EvaluateRightAnglePath(start, bendA, bendB, end, t);
        }

        for (int pathIndex = 0; pathIndex < ringCount; pathIndex++)
        {
            Vector3 previous = centers[Mathf.Max(0, pathIndex - 1)];
            Vector3 next = centers[Mathf.Min(ringCount - 1, pathIndex + 1)];
            Vector3 tangent = (next - previous).normalized;
            Vector3 ringRight = Vector3.Cross(tangent, surfaceNormal).normalized;
            if (ringRight.sqrMagnitude < 0.001f)
                ringRight = Vector3.Cross(tangent, Vector3.up).normalized;
            Vector3 ringUp = Vector3.Cross(ringRight, tangent).normalized;

            float ringRadius = GetRingRadius(pathIndex, ringCount);
            for (int radialIndex = 0; radialIndex < RadialSegments; radialIndex++)
            {
                float angle = radialIndex / (float)RadialSegments * Mathf.PI * 2f;
                Vector3 radial = Mathf.Cos(angle) * ringRight + Mathf.Sin(angle) * ringUp;
                int vertexIndex = pathIndex * RadialSegments + radialIndex;
                vertices[vertexIndex] = transform.InverseTransformPoint(
                    centers[pathIndex] + radial * ringRadius);
                normals[vertexIndex] = transform.InverseTransformDirection(radial).normalized;
                uvs[vertexIndex] = new Vector2(
                    radialIndex / (float)RadialSegments,
                    pathIndex / (float)PathSegments);
            }
        }

        int triangleIndex = 0;
        for (int pathIndex = 0; pathIndex < PathSegments; pathIndex++)
        {
            int currentRing = pathIndex * RadialSegments;
            int nextRing = (pathIndex + 1) * RadialSegments;
            for (int radialIndex = 0; radialIndex < RadialSegments; radialIndex++)
            {
                int nextRadial = (radialIndex + 1) % RadialSegments;
                triangles[triangleIndex++] = currentRing + radialIndex;
                triangles[triangleIndex++] = nextRing + radialIndex;
                triangles[triangleIndex++] = nextRing + nextRadial;
                triangles[triangleIndex++] = currentRing + radialIndex;
                triangles[triangleIndex++] = nextRing + nextRadial;
                triangles[triangleIndex++] = currentRing + nextRadial;
            }
        }

        vertices[capStartIndex] = transform.InverseTransformPoint(centers[0]);
        vertices[capStartIndex + 1] = transform.InverseTransformPoint(centers[ringCount - 1]);
        normals[capStartIndex] = transform.InverseTransformDirection(
            (centers[0] - centers[1]).normalized);
        normals[capStartIndex + 1] = transform.InverseTransformDirection(
            (centers[ringCount - 1] - centers[ringCount - 2]).normalized);

        int lastRingStart = (ringCount - 1) * RadialSegments;
        for (int radialIndex = 0; radialIndex < RadialSegments; radialIndex++)
        {
            int nextRadial = (radialIndex + 1) % RadialSegments;
            triangles[triangleIndex++] = capStartIndex;
            triangles[triangleIndex++] = nextRadial;
            triangles[triangleIndex++] = radialIndex;
            triangles[triangleIndex++] = capStartIndex + 1;
            triangles[triangleIndex++] = lastRingStart + radialIndex;
            triangles[triangleIndex++] = lastRingStart + nextRadial;
        }

        wireMesh.Clear();
        wireMesh.vertices = vertices;
        wireMesh.normals = normals;
        wireMesh.uv = uvs;
        wireMesh.triangles = triangles;
        wireMesh.RecalculateBounds();
        return true;
    }

    private void EnsureRenderer()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (wireMesh == null)
        {
            wireMesh = new Mesh { name = name + "_Mesh" };
            wireMesh.indexFormat = IndexFormat.UInt16;
            meshFilter.sharedMesh = wireMesh;
        }

        if (wireMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            wireMaterial = new Material(shader) { name = name + "_Material" };
            if (wireMaterial.HasProperty("_BaseColor"))
                wireMaterial.SetColor("_BaseColor", wireColor);
            if (wireMaterial.HasProperty("_Color"))
                wireMaterial.SetColor("_Color", wireColor);
            if (wireMaterial.HasProperty("_Smoothness"))
                wireMaterial.SetFloat("_Smoothness", 0.58f);
            if (wireMaterial.HasProperty("_Metallic"))
                wireMaterial.SetFloat("_Metallic", 0.08f);
            wireMaterial.renderQueue = 2450;
            meshRenderer.sharedMaterial = wireMaterial;
        }

        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private float GetRingRadius(int pathIndex, int ringCount)
    {
        bool terminalCollar = pathIndex <= 2 || pathIndex >= ringCount - 3;
        return terminalCollar ? radius * 1.38f : radius;
    }

    private static Vector3 EvaluateRightAnglePath(
        Vector3 start,
        Vector3 bendA,
        Vector3 bendB,
        Vector3 end,
        float t)
    {
        Vector3[] points = { start, bendA, bendB, end };
        float totalLength = 0f;
        for (int i = 0; i < points.Length - 1; i++)
            totalLength += Vector3.Distance(points[i], points[i + 1]);

        if (totalLength <= 0.000001f)
            return Vector3.Lerp(start, end, t);

        float remaining = Mathf.Clamp01(t) * totalLength;
        for (int i = 0; i < points.Length - 1; i++)
        {
            float segmentLength = Vector3.Distance(points[i], points[i + 1]);
            if (segmentLength <= 0.000001f)
                continue;

            if (remaining <= segmentLength)
                return Vector3.Lerp(points[i], points[i + 1], remaining / segmentLength);

            remaining -= segmentLength;
        }

        return end;
    }

    private void OnDestroy()
    {
        if (wireMesh != null)
            Destroy(wireMesh);
        if (wireMaterial != null)
            Destroy(wireMaterial);
    }
}

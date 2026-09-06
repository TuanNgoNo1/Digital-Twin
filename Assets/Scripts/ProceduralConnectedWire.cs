using UnityEngine;
using UnityEngine.Rendering;

public sealed class ProceduralConnectedWire : MonoBehaviour
{
    private const int PathSegments = 32;
    private const int RadialSegments = 12;

    private WireBody sourceWire;
    private Color wireColor;
    private float radius;
    private Mesh wireMesh;
    private Material wireMaterial;
    private Material metalMaterial;
    private GameObject plugASleeve;
    private GameObject plugAElbow;
    private GameObject plugAStrainRelief;
    private GameObject plugAMetalCollar;
    private GameObject plugBSleeve;
    private GameObject plugBElbow;
    private GameObject plugBStrainRelief;
    private GameObject plugBMetalCollar;

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

        Vector3 socketStart = sourceWire.plugA.transform.position;
        Vector3 socketEnd = sourceWire.plugB.transform.position;
        if ((socketEnd - socketStart).sqrMagnitude < 0.000001f)
            return false;

        Camera camera = Camera.main;
        Vector3 surfaceNormal = camera != null
            ? -camera.transform.forward.normalized
            : -Vector3.forward;
        float plugRise = Mathf.Clamp(radius * 4.2f, 0.004f, 0.012f);
        Vector3 start = socketStart + surfaceNormal * plugRise;
        Vector3 end = socketEnd + surfaceNormal * plugRise;

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
        UpdatePlugVisuals(
            socketStart,
            socketEnd,
            start,
            end,
            centers[1] - centers[0],
            centers[ringCount - 2] - centers[ringCount - 1],
            surfaceNormal,
            plugRise);
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

    private void UpdatePlugVisuals(
        Vector3 socketStart,
        Vector3 socketEnd,
        Vector3 routeStart,
        Vector3 routeEnd,
        Vector3 startTangent,
        Vector3 endTangent,
        Vector3 surfaceNormal,
        float plugRise)
    {
        EnsureMetalMaterial();
        UpdatePlugVisual(
            ref plugASleeve,
            ref plugAElbow,
            ref plugAStrainRelief,
            ref plugAMetalCollar,
            "A",
            socketStart,
            routeStart,
            startTangent.normalized,
            surfaceNormal,
            plugRise);
        UpdatePlugVisual(
            ref plugBSleeve,
            ref plugBElbow,
            ref plugBStrainRelief,
            ref plugBMetalCollar,
            "B",
            socketEnd,
            routeEnd,
            endTangent.normalized,
            surfaceNormal,
            plugRise);
    }

    private void UpdatePlugVisual(
        ref GameObject sleeve,
        ref GameObject elbow,
        ref GameObject strainRelief,
        ref GameObject metalCollar,
        string suffix,
        Vector3 socketPosition,
        Vector3 routePosition,
        Vector3 routeDirection,
        Vector3 surfaceNormal,
        float plugRise)
    {
        if (routeDirection.sqrMagnitude < 0.001f)
            routeDirection = Vector3.right;

        sleeve = EnsurePrimitive(sleeve, PrimitiveType.Cylinder, "CablePlug_" + suffix + "_Sleeve", wireMaterial);
        elbow = EnsurePrimitive(elbow, PrimitiveType.Sphere, "CablePlug_" + suffix + "_Elbow", wireMaterial);
        strainRelief = EnsurePrimitive(
            strainRelief,
            PrimitiveType.Cylinder,
            "CablePlug_" + suffix + "_StrainRelief",
            wireMaterial);
        metalCollar = EnsurePrimitive(
            metalCollar,
            PrimitiveType.Cylinder,
            "CablePlug_" + suffix + "_MetalCollar",
            metalMaterial);

        float sleeveRadius = radius * 2.25f;
        SetCylinder(
            sleeve.transform,
            Vector3.Lerp(socketPosition, routePosition, 0.53f),
            surfaceNormal,
            sleeveRadius,
            plugRise * 0.92f);

        elbow.transform.position = routePosition;
        elbow.transform.rotation = Quaternion.identity;
        elbow.transform.localScale = Vector3.one * (sleeveRadius * 2f);

        float reliefLength = radius * 5.2f;
        SetCylinder(
            strainRelief.transform,
            routePosition + routeDirection * (reliefLength * 0.5f),
            routeDirection,
            radius * 1.55f,
            reliefLength);

        SetCylinder(
            metalCollar.transform,
            socketPosition + surfaceNormal * (radius * 0.35f),
            surfaceNormal,
            radius * 2.55f,
            radius * 0.7f);
    }

    private GameObject EnsurePrimitive(
        GameObject value,
        PrimitiveType primitiveType,
        string objectName,
        Material material)
    {
        if (value == null)
        {
            value = GameObject.CreatePrimitive(primitiveType);
            value.name = objectName;
            value.transform.SetParent(transform, true);
            Collider collider = value.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                DestroyOwnedObject(collider);
            }
        }

        MeshRenderer renderer = value.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return value;
    }

    private static void SetCylinder(
        Transform cylinder,
        Vector3 position,
        Vector3 axis,
        float cylinderRadius,
        float length)
    {
        Vector3 direction = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;
        cylinder.position = position;
        cylinder.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        // Unity's primitive cylinder is two units tall with a radius of 0.5.
        cylinder.localScale = new Vector3(cylinderRadius * 2f, length * 0.5f, cylinderRadius * 2f);
    }

    private void EnsureMetalMaterial()
    {
        if (metalMaterial != null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        metalMaterial = new Material(shader) { name = name + "_PlugMetal" };
        Color metalColor = new Color(0.68f, 0.72f, 0.76f, 1f);
        if (metalMaterial.HasProperty("_BaseColor"))
            metalMaterial.SetColor("_BaseColor", metalColor);
        if (metalMaterial.HasProperty("_Color"))
            metalMaterial.SetColor("_Color", metalColor);
        if (metalMaterial.HasProperty("_Smoothness"))
            metalMaterial.SetFloat("_Smoothness", 0.82f);
        if (metalMaterial.HasProperty("_Metallic"))
            metalMaterial.SetFloat("_Metallic", 0.78f);
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
            DestroyOwnedObject(wireMesh);
        if (wireMaterial != null)
            DestroyOwnedObject(wireMaterial);
        if (metalMaterial != null)
            DestroyOwnedObject(metalMaterial);
    }

    private static void DestroyOwnedObject(Object value)
    {
        if (value == null)
            return;

        if (Application.isPlaying)
            Destroy(value);
        else
            DestroyImmediate(value);
    }
}

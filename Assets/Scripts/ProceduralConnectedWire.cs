using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ProceduralConnectedWire : MonoBehaviour
{
    private const int PathSegments = 32;
    private const int RadialSegments = 12;
    private const float SocketClearancePixels = 30f;
    private const float RouteLaneSpacingPixels = 18f;
    private const int RouteLaneSearchCount = 18;

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

        Vector3[] routePoints = BuildSocketAvoidingRoute(start, end, camera);

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
            centers[pathIndex] = EvaluatePolyline(routePoints, t);
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

    private Vector3[] BuildSocketAvoidingRoute(Vector3 start, Vector3 end, Camera camera)
    {
        if (camera == null)
            return new[] { start, Vector3.Lerp(start, end, 0.5f), end };

        Vector3 startScreen3 = camera.WorldToScreenPoint(start);
        Vector3 endScreen3 = camera.WorldToScreenPoint(end);
        Vector2 startScreen = startScreen3;
        Vector2 endScreen = endScreen3;
        float routeDepth = (startScreen3.z + endScreen3.z) * 0.5f;

        List<Vector2> obstacles = new List<Vector2>();
        SocketPoint socketA = sourceWire.plugA.connectedSocket;
        SocketPoint socketB = sourceWire.plugB.connectedSocket;
        foreach (SocketPoint socket in FindObjectsByType<SocketPoint>(
                     // Jack visuals stay visible for the entire board even when
                     // a legacy step socket group is inactive. Include those
                     // sockets so the route avoids what the player actually sees.
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (socket == null || socket == socketA || socket == socketB)
                continue;
            Vector3 screen = camera.WorldToScreenPoint(socket.transform.position);
            if (screen.z > camera.nearClipPlane)
                obstacles.Add(screen);
        }

        foreach (WireRoutingObstacle obstacle in FindObjectsByType<WireRoutingObstacle>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (obstacle == null)
                continue;
            Vector3 screen = camera.WorldToScreenPoint(obstacle.transform.position);
            if (screen.z > camera.nearClipPlane)
                obstacles.Add(screen);
        }

        List<Vector2[]> candidates = new List<Vector2[]>();
        float middleX = (startScreen.x + endScreen.x) * 0.5f;
        float middleY = (startScreen.y + endScreen.y) * 0.5f;
        AddLaneCandidates(candidates, startScreen, endScreen, middleX, middleY);
        for (int lane = 1; lane <= RouteLaneSearchCount; lane++)
        {
            float offset = lane * RouteLaneSpacingPixels;
            AddLaneCandidates(candidates, startScreen, endScreen, middleX + offset, middleY + offset);
            AddLaneCandidates(candidates, startScreen, endScreen, middleX - offset, middleY - offset);
        }

        Vector2[] best = candidates[0];
        float bestScore = float.PositiveInfinity;
        foreach (Vector2[] candidate in candidates)
        {
            float score = ScoreRoute(candidate, obstacles);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        Vector3[] worldRoute = new Vector3[best.Length];
        for (int i = 0; i < best.Length; i++)
        {
            if (i == 0) worldRoute[i] = start;
            else if (i == best.Length - 1) worldRoute[i] = end;
            else worldRoute[i] = camera.ScreenToWorldPoint(new Vector3(best[i].x, best[i].y, routeDepth));
        }
        return worldRoute;
    }

    private static void AddLaneCandidates(
        List<Vector2[]> candidates,
        Vector2 start,
        Vector2 end,
        float verticalLaneX,
        float horizontalLaneY)
    {
        candidates.Add(new[] { start, new Vector2(verticalLaneX, start.y), new Vector2(verticalLaneX, end.y), end });
        candidates.Add(new[] { start, new Vector2(start.x, horizontalLaneY), new Vector2(end.x, horizontalLaneY), end });
    }

    private static float ScoreRoute(Vector2[] route, List<Vector2> obstacles)
    {
        float score = 0f;
        float clearanceSquared = SocketClearancePixels * SocketClearancePixels;
        for (int segment = 0; segment < route.Length - 1; segment++)
        {
            Vector2 a = route[segment];
            Vector2 b = route[segment + 1];
            float length = Vector2.Distance(a, b);
            score += length;
            foreach (Vector2 obstacle in obstacles)
            {
                float distanceSquared = DistanceToSegmentSquared(obstacle, a, b);
                if (distanceSquared < clearanceSquared)
                    score += 1000000f + (clearanceSquared - distanceSquared) * 1000f;
            }
        }
        return score;
    }

    private static float DistanceToSegmentSquared(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.0001f)
            return (point - a).sqrMagnitude;
        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
        return (point - (a + segment * t)).sqrMagnitude;
    }

    private static Vector3 EvaluatePolyline(Vector3[] points, float t)
    {
        float totalLength = 0f;
        for (int i = 0; i < points.Length - 1; i++)
            totalLength += Vector3.Distance(points[i], points[i + 1]);

        if (totalLength <= 0.000001f)
            return Vector3.Lerp(points[0], points[points.Length - 1], t);

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

        return points[points.Length - 1];
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

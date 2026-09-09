using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ProceduralConnectedWire : MonoBehaviour
{
    private const float BoardPixelWidth = 1448f;
    private const float BoardPixelHeight = 1086f;
    private const int PathSegments = 192;
    private const int RadialSegments = 32;
    private const int CornerSegments = 18;
    private const float CornerRoundness = 0.28f;
    private const float SocketClearancePixels = 30f;
    private const float RouteLaneSpacingPixels = 18f;
    private const int RouteLaneSearchCount = 18;

    private WireBody sourceWire;
    private Color wireColor;
    private float radius;
    private Mesh wireMesh;
    private Material wireMaterial;
    private Material highlightMaterial;
    private Material metalMaterial;
    private LineRenderer glossHighlight;
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
        // Make the connected cable visibly fuller than the thin interaction
        // line while keeping enough clearance between nearby terminals.
        radius = Mathf.Max(0.00165f, wire != null ? wire.wireWidth * 0.33f : 0.00165f);
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

        Vector3[] routePoints = BuildRoundedRoute(
            BuildSocketAvoidingRoute(start, end, camera),
            GetCornerRoundness());

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
        UpdateGlossHighlight(centers, surfaceNormal);
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
                wireMaterial.SetFloat("_Smoothness", 1f);
            if (wireMaterial.HasProperty("_Glossiness"))
                wireMaterial.SetFloat("_Glossiness", 1f);
            if (wireMaterial.HasProperty("_GlossMapScale"))
                wireMaterial.SetFloat("_GlossMapScale", 1f);
            if (wireMaterial.HasProperty("_Metallic"))
                wireMaterial.SetFloat("_Metallic", 0f);
            if (wireMaterial.HasProperty("_SpecColor"))
                wireMaterial.SetColor("_SpecColor", new Color(0.18f, 0.18f, 0.18f, 1f));
            if (wireMaterial.HasProperty("_ClearCoatMask"))
            {
                wireMaterial.SetFloat("_ClearCoatMask", 1f);
                wireMaterial.EnableKeyword("_CLEARCOAT");
            }
            if (wireMaterial.HasProperty("_ClearCoatSmoothness"))
                wireMaterial.SetFloat("_ClearCoatSmoothness", 1f);
            if (wireMaterial.HasProperty("_CoatMask"))
                wireMaterial.SetFloat("_CoatMask", 1f);
            if (wireMaterial.HasProperty("_CoatSmoothness"))
                wireMaterial.SetFloat("_CoatSmoothness", 1f);
            if (wireMaterial.HasProperty("_SpecularHighlights"))
                wireMaterial.SetFloat("_SpecularHighlights", 1f);
            if (wireMaterial.HasProperty("_EnvironmentReflections"))
                wireMaterial.SetFloat("_EnvironmentReflections", 1f);
            if (wireMaterial.HasProperty("_EmissionColor"))
            {
                wireMaterial.SetColor("_EmissionColor", wireColor * 0.045f);
                wireMaterial.EnableKeyword("_EMISSION");
            }
            wireMaterial.renderQueue = 2450;
            meshRenderer.sharedMaterial = wireMaterial;
        }

        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void UpdateGlossHighlight(Vector3[] centers, Vector3 surfaceNormal)
    {
        if (centers == null || centers.Length < 2)
            return;

        if (glossHighlight == null)
        {
            GameObject highlightObject = new GameObject("CableGlossHighlight");
            highlightObject.transform.SetParent(transform, false);
            glossHighlight = highlightObject.AddComponent<LineRenderer>();
            glossHighlight.useWorldSpace = true;
            glossHighlight.alignment = LineAlignment.View;
            glossHighlight.textureMode = LineTextureMode.Stretch;
            glossHighlight.numCapVertices = 8;
            glossHighlight.numCornerVertices = 12;
            glossHighlight.shadowCastingMode = ShadowCastingMode.Off;
            glossHighlight.receiveShadows = false;
        }

        if (highlightMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            highlightMaterial = new Material(shader) { name = name + "_GlossHighlight" };
            if (highlightMaterial.HasProperty("_Surface"))
                highlightMaterial.SetFloat("_Surface", 1f);
            if (highlightMaterial.HasProperty("_ZWrite"))
                highlightMaterial.SetFloat("_ZWrite", 0f);
            if (highlightMaterial.HasProperty("_SrcBlend"))
                highlightMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (highlightMaterial.HasProperty("_DstBlend"))
                highlightMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (highlightMaterial.HasProperty("_BaseColor"))
                highlightMaterial.SetColor("_BaseColor", Color.white);
            if (highlightMaterial.HasProperty("_Color"))
                highlightMaterial.SetColor("_Color", Color.white);
            highlightMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            highlightMaterial.renderQueue = 3100;
            glossHighlight.sharedMaterial = highlightMaterial;
        }

        Vector3[] highlightPoints = new Vector3[centers.Length];
        float highlightLift = radius * 1.035f;
        for (int i = 0; i < centers.Length; i++)
            highlightPoints[i] = centers[i] + surfaceNormal * highlightLift;

        glossHighlight.positionCount = highlightPoints.Length;
        glossHighlight.SetPositions(highlightPoints);
        glossHighlight.widthMultiplier = radius * 0.24f;
        Color reflectionColor = Color.Lerp(wireColor, Color.white, 0.72f);
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(reflectionColor, 0f),
                new GradientColorKey(Color.white, 0.42f),
                new GradientColorKey(reflectionColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.42f, 0.035f),
                new GradientAlphaKey(0.34f, 0.965f),
                new GradientAlphaKey(0f, 1f)
            });
        glossHighlight.colorGradient = gradient;
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
            metalMaterial.SetFloat("_Smoothness", 0.98f);
        if (metalMaterial.HasProperty("_Metallic"))
            metalMaterial.SetFloat("_Metallic", 0.94f);
        if (metalMaterial.HasProperty("_SpecColor"))
            metalMaterial.SetColor("_SpecColor", Color.white);
        if (metalMaterial.HasProperty("_SpecularHighlights"))
            metalMaterial.SetFloat("_SpecularHighlights", 1f);
        if (metalMaterial.HasProperty("_EnvironmentReflections"))
            metalMaterial.SetFloat("_EnvironmentReflections", 1f);
    }

    private float GetRingRadius(int pathIndex, int ringCount)
    {
        bool terminalCollar = pathIndex <= 2 || pathIndex >= ringCount - 3;
        return terminalCollar ? radius * 1.5f : radius;
    }

    private Vector3[] BuildSocketAvoidingRoute(Vector3 start, Vector3 end, Camera camera)
    {
        if (camera == null)
            return new[] { start, Vector3.Lerp(start, end, 0.5f), end };

        if (TryBuildReferenceBoardRoute(start, end, camera, out Vector3[] referenceRoute))
            return referenceRoute;

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

    private bool TryBuildReferenceBoardRoute(
        Vector3 start,
        Vector3 end,
        Camera camera,
        out Vector3[] route)
    {
        route = null;
        if (sourceWire == null ||
            !TryGetReferenceRoutePixels(
                sourceWire.correctSocketA,
                sourceWire.correctSocketB,
                out Vector2[] routePixels))
        {
            return false;
        }

        GameObject boardSurfaceObject = GameObject.Find("NewServoWiringBoardSurface");
        if (boardSurfaceObject == null)
            boardSurfaceObject = GameObject.Find("BoardSurface2D");
        if (boardSurfaceObject == null)
            return false;

        Transform boardSurface = boardSurfaceObject.transform;
        Vector3 surfaceNormal = -camera.transform.forward.normalized;
        float startDepth = Vector3.Dot(start - boardSurface.position, surfaceNormal);
        float endDepth = Vector3.Dot(end - boardSurface.position, surfaceNormal);
        float wireDepth = (startDepth + endDepth) * 0.5f;

        route = new Vector3[routePixels.Length + 2];
        route[0] = start;
        for (int index = 0; index < routePixels.Length; index++)
        {
            Vector2 pixel = routePixels[index];
            Vector3 pointOnBoard = boardSurface.TransformPoint(new Vector3(
                pixel.x / BoardPixelWidth - 0.5f,
                0.5f - pixel.y / BoardPixelHeight,
                0f));
            route[index + 1] = pointOnBoard + surfaceNormal * wireDepth;
        }
        route[route.Length - 1] = end;
        return true;
    }

    private static bool TryGetReferenceRoutePixels(
        string socketA,
        string socketB,
        out Vector2[] routePixels)
    {
        routePixels = null;

        // Step 1: use the clear gap under the supply blocks, the lane between
        // PLC and driver, and the clear strip below the PLC. This keeps the
        // long runs away from the printed terminal names.
        if (IsOrderedPair(socketA, socketB, "5VDC", "+V0"))
            routePixels = Pixels(458, 234.6f, 458, 756, 566, 756, 566, 727.6f);
        else if (IsOrderedPair(socketA, socketB, "5VDC", "+V1"))
            routePixels = Pixels(474, 234.6f, 474, 683, 566, 683, 566, 654f);
        else if (IsOrderedPair(socketA, socketB, "Y0", "Pin11"))
            routePixels = Pixels(585, 685.8f, 585, 338);
        else if (IsOrderedPair(socketA, socketB, "Y1", "Pin9"))
            routePixels = Pixels(362, 622.2f, 362, 820, 607, 820, 607, 492, 672, 492);
        else if (IsOrderedPair(socketA, socketB, "GND_5V", "Pin10"))
            routePixels = Pixels(570, 234.6f, 570, 557, 672, 557);
        else if (IsOrderedPair(socketA, socketB, "GND_5V", "Pin12"))
            routePixels = Pixels(586, 234.6f, 586, 432, 672, 432);

        // Step 2: encoder runs use the lower outside corridor, while the
        // remaining control wires stay in the clean PLC/driver separation.
        else if (IsOrderedPair(socketA, socketB, "24VDC", "SS"))
            routePixels = Pixels(45, 234.6f, 45, 770, 142.4f, 770);
        else if (IsOrderedPair(socketA, socketB, "Enc_A", "X3"))
            routePixels = Pixels(1045, 796.3f, 1045, 900, 335, 900, 335, 474.8f);
        else if (IsOrderedPair(socketA, socketB, "Enc_B", "X4"))
            routePixels = Pixels(1070, 878f, 1070, 920, 317, 920, 317, 413.2f);
        else if (IsOrderedPair(socketA, socketB, "Pin15", "X0"))
            routePixels = Pixels(672, 642, 600, 642, 600, 795, 350, 795, 350, 666);
        else if (IsOrderedPair(socketA, socketB, "Pin13", "X1"))
            routePixels = Pixels(672, 752, 585, 752, 585, 812, 365, 812, 365, 601);
        else if (IsOrderedPair(socketA, socketB, "Pin14", "GND_5V"))
            routePixels = Pixels(672.5f, 812, 615, 812, 615, 234.6f);

        // Step 3: the three motor phases fan out through separate vertical
        // lanes beside the driver/HMI, then enter each motor jack from the
        // left so the socket labels remain unobstructed.
        else if (IsOrderedPair(socketA, socketB, "oA", "Motor_A"))
            routePixels = Pixels(970, 380.4f, 970, 562.1f);
        else if (IsOrderedPair(socketA, socketB, "oB", "Motor_B"))
            routePixels = Pixels(855, 488, 945, 488, 945, 620);
        else if (IsOrderedPair(socketA, socketB, "oC", "Motor_C"))
            routePixels = Pixels(855, 562, 920, 562, 920, 677);

        return routePixels != null;
    }

    private static bool IsOrderedPair(
        string socketA,
        string socketB,
        string expectedA,
        string expectedB)
    {
        return string.Equals(socketA, expectedA, System.StringComparison.OrdinalIgnoreCase) &&
            string.Equals(socketB, expectedB, System.StringComparison.OrdinalIgnoreCase);
    }

    private static Vector2[] Pixels(params float[] coordinates)
    {
        Vector2[] points = new Vector2[coordinates.Length / 2];
        for (int index = 0; index < points.Length; index++)
            points[index] = new Vector2(coordinates[index * 2], coordinates[index * 2 + 1]);
        return points;
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

    private float GetCornerRoundness()
    {
        if (sourceWire == null)
            return CornerRoundness;

        if (IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "Y0", "Pin11"))
            return 0.08f;

        if (IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "Pin14", "GND_5V"))
            return 0.34f;

        bool squareReferenceRoute =
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "5VDC", "+V0") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "5VDC", "+V1") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "Y0", "Pin11") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "Y1", "Pin9") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "GND_5V", "Pin10") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "GND_5V", "Pin12") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "24VDC", "SS") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "Enc_A", "X3") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "Enc_B", "X4") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "oA", "Motor_A") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "oB", "Motor_B") ||
            IsOrderedPair(sourceWire.correctSocketA, sourceWire.correctSocketB, "oC", "Motor_C");
        return squareReferenceRoute ? 0.22f : CornerRoundness;
    }

    private static Vector3[] BuildRoundedRoute(Vector3[] points, float cornerRoundness)
    {
        if (points == null || points.Length < 3)
            return points;

        List<Vector3> rounded = new List<Vector3> { points[0] };
        for (int pointIndex = 1; pointIndex < points.Length - 1; pointIndex++)
        {
            Vector3 previous = points[pointIndex - 1];
            Vector3 corner = points[pointIndex];
            Vector3 next = points[pointIndex + 1];
            Vector3 incoming = corner - previous;
            Vector3 outgoing = next - corner;
            float incomingLength = incoming.magnitude;
            float outgoingLength = outgoing.magnitude;

            if (incomingLength <= 0.000001f || outgoingLength <= 0.000001f ||
                Vector3.Dot(incoming / incomingLength, outgoing / outgoingLength) > 0.999f)
            {
                rounded.Add(corner);
                continue;
            }

            float trimDistance = Mathf.Min(incomingLength, outgoingLength) * cornerRoundness;
            Vector3 entry = corner - incoming / incomingLength * trimDistance;
            Vector3 exit = corner + outgoing / outgoingLength * trimDistance;
            rounded.Add(entry);

            for (int segment = 1; segment <= CornerSegments; segment++)
            {
                float t = segment / (float)CornerSegments;
                float oneMinusT = 1f - t;
                rounded.Add(
                    oneMinusT * oneMinusT * entry +
                    2f * oneMinusT * t * corner +
                    t * t * exit);
            }
        }

        rounded.Add(points[points.Length - 1]);
        return rounded.ToArray();
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
        if (highlightMaterial != null)
            DestroyOwnedObject(highlightMaterial);
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

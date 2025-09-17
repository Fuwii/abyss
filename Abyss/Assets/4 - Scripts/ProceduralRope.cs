using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralRope : MonoBehaviour
{
    [Tooltip("Anchor + все сегменты по порядку (их world positions будут использованы)")]
    public List<Transform> ropeSegments;

    [Range(3, 64)] public int sides = 8;
    public float radius = 0.1f;
    [Min(1)] public int smoothPerSegment = 4;
    public float uvScale = 1f; // уменьшает/увеличивает V координату для текстуры

    private Mesh mesh;

    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "ProceduralRopeMesh";
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        if (ropeSegments == null || ropeSegments.Count < 2) return;
        GenerateMesh();
    }

    void GenerateMesh()
    {
        // 1) Собираем мирные позиции
        List<Vector3> worldPoints = new List<Vector3>(ropeSegments.Count);
        foreach (var t in ropeSegments) worldPoints.Add(t.position);

        // 2) Catmull-Rom сглаживание (результат — в world space)
        List<Vector3> smoothWorld = new List<Vector3>();
        for (int i = 0; i < worldPoints.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? worldPoints[i - 1] : worldPoints[i];
            Vector3 p1 = worldPoints[i];
            Vector3 p2 = worldPoints[i + 1];
            Vector3 p3 = i < worldPoints.Count - 2 ? worldPoints[i + 2] : worldPoints[i + 1];

            for (int j = 0; j < smoothPerSegment; j++)
            {
                float t = j / (float)smoothPerSegment;
                smoothWorld.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        smoothWorld.Add(worldPoints[worldPoints.Count - 1]);

        // 3) Вычисляем расстояния вдоль кривой (для UV V)
        int ringCount = smoothWorld.Count;
        float[] dist = new float[ringCount];
        float total = 0f;
        for (int i = 1; i < ringCount; i++)
        {
            total += Vector3.Distance(smoothWorld[i - 1], smoothWorld[i]);
            dist[i] = total;
        }

        // 4) Переводим в локальные координаты объекта (Mesh в локальных координатах)
        List<Vector3> localPoints = new List<Vector3>(ringCount);
        for (int i = 0; i < ringCount; i++)
            localPoints.Add(transform.InverseTransformPoint(smoothWorld[i]));

        // 5) Строим трубу с устойчивой рамкой (normal/binormal) — предотвращает скручивание
        BuildTube(localPoints, dist);
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    void BuildTube(List<Vector3> points, float[] dist)
    {
        int ringCount = points.Count;
        if (ringCount < 2) return;

        // +2 вершины для центров крышек
        int vertCount = ringCount * sides + 2;
        Vector3[] verts = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        List<int> tris = new List<int>((ringCount - 1) * sides * 6 + sides * 2 * 3);

        // начальный тангент
        Vector3 prevTangent = (points[1] - points[0]).normalized;
        Vector3 referenceUp = Vector3.up;
        Vector3 binormal = Vector3.Cross(prevTangent, referenceUp);
        if (binormal.sqrMagnitude < 1e-6f) binormal = Vector3.Cross(prevTangent, Vector3.right);
        binormal.Normalize();

        float uvScaleCurrent = uvScale * ropeSegments.Count;

        for (int i = 0; i < ringCount; i++)
        {
            Vector3 tangent;
            if (i < ringCount - 1) tangent = (points[i + 1] - points[i]).normalized;
            else tangent = (points[i] - points[i - 1]).normalized;

            if (tangent.sqrMagnitude < 1e-6f) tangent = prevTangent;

            Vector3 normal = Vector3.Cross(binormal, tangent).normalized;
            if (normal.sqrMagnitude < 1e-6f)
            {
                normal = Vector3.Cross(tangent, Vector3.right).normalized;
                if (normal.sqrMagnitude < 1e-6f) normal = Vector3.Cross(tangent, Vector3.up).normalized;
            }
            binormal = Vector3.Cross(tangent, normal).normalized;

            for (int j = 0; j < sides; j++)
            {
                float angle = (j / (float)sides) * Mathf.PI * 2f;
                Vector3 localOffset = normal * Mathf.Cos(angle) * radius + binormal * Mathf.Sin(angle) * radius;
                int idx = i * sides + j;

                verts[idx] = points[i] + localOffset;
                normals[idx] = localOffset.normalized;
                uvs[idx] = new Vector2(
                    j / (float)sides,
                    (i / (float)(ringCount - 1)) * uvScaleCurrent
                );
            }

            prevTangent = tangent;
        }

        // === Добавляем центры крышек ===
        int startCenterIdx = ringCount * sides;
        int endCenterIdx = ringCount * sides + 1;
        verts[startCenterIdx] = points[0];
        verts[endCenterIdx] = points[ringCount - 1];

        normals[startCenterIdx] = -(points[1] - points[0]).normalized;
        normals[endCenterIdx] = (points[ringCount - 1] - points[ringCount - 2]).normalized;

        uvs[startCenterIdx] = new Vector2(0.5f, 0f);
        uvs[endCenterIdx] = new Vector2(0.5f, 1f);

        // боковые стенки
        for (int i = 0; i < ringCount - 1; i++)
        {
            for (int j = 0; j < sides; j++)
            {
                int curr = i * sides + j;
                int next = i * sides + (j + 1) % sides;
                int currNext = (i + 1) * sides + j;
                int nextNext = (i + 1) * sides + (j + 1) % sides;

                tris.Add(curr);
                tris.Add(next);
                tris.Add(currNext);

                tris.Add(currNext);
                tris.Add(next);
                tris.Add(nextNext);
            }
        }

        // крышка в начале
        for (int j = 0; j < sides; j++)
        {
            int curr = j;
            int next = (j + 1) % sides;
            tris.Add(startCenterIdx);
            tris.Add(next);
            tris.Add(curr);
        }

        // крышка в конце
        int ringStart = (ringCount - 1) * sides;
        for (int j = 0; j < sides; j++)
        {
            int curr = ringStart + j;
            int next = ringStart + (j + 1) % sides;
            tris.Add(endCenterIdx);
            tris.Add(curr);
            tris.Add(next);
        }

        // финализация
        mesh.Clear();
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
    }

}

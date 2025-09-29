using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Копирует всю иерархию трансформов из sourceRoot в targetRoot (по именам/структуре).
/// Предназначен для cosmetic bones: source = animated bones (Animator), target = cosmetic bones (в иерархии физического рига).
/// Важно: обновление идёт в LateUpdate() чтобы Animator уже применил позы.
/// </summary>
[ExecuteAlways]
public class CopyAnimatedChain : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("Корень анимированного поддерева (например lowerLeg.l на skinned rig)")]
    public Transform sourceRoot;

    [Tooltip("Корень целевого поддерева (куда будут копироваться трансформы). Обычно child под физической костью.")]
    public Transform targetRoot;

    [Header("Options")]
    [Tooltip("Автоматически создаёт недостающие target трансформы (GameObject) с тем же именем")]
    public bool createMissingTargets = false;

    [Tooltip("Копировать позицию")]
    public bool copyPosition = true;

    [Tooltip("Копировать ротацию")]
    public bool copyRotation = true;

    [Tooltip("Сглаживание в секундах (0 = мгновенно)")]
    [Range(0f, 0.5f)]
    public float smoothTime = 0.0f;

    [Tooltip("Игнорировать целевые трансформы, у которых есть Rigidbody (чтобы не ломать физику)")]
    public bool skipIfTargetHasRigidbody = true;

    [Tooltip("Маска по осям применяемая к локальной дельте (local euler) источника перед применением")]
    public Vector3 axisMask = Vector3.one;

    // Внутренние пары
    class Pair
    {
        public Transform src;
        public Transform srcParent;
        public Transform tgt;
        public Transform tgtParent;
        public Quaternion localRot; // локальная дельта источника (от srcParent)
        public Vector3 localPos;
        public Quaternion curRot;
        public Vector3 curPos;
        public bool skipApply;
    }

    List<Pair> pairs = new List<Pair>();
    bool isInitialized = false;

    void OnEnable()
    {
        BuildMapIfNeeded();
    }

    void OnValidate()
    {
        // rebuild in editor changes
        isInitialized = false;
    }

    void BuildMapIfNeeded()
    {
        if (isInitialized) return;
        pairs.Clear();

        if (sourceRoot == null || targetRoot == null)
        {
            Debug.LogWarning($"{name}: sourceRoot or targetRoot is null. Map not built.");
            isInitialized = true;
            return;
        }

        // Map: рекурсивно пройти sourceRoot и для каждого src найти соответствующий tgt (по имени, относительно targetRoot)
        Dictionary<Transform, Transform> map = new Dictionary<Transform, Transform>();
        // root mapping: sourceRoot -> targetRoot
        map[sourceRoot] = targetRoot;
        pairs.Add(MakePair(sourceRoot, sourceRoot.parent, targetRoot, targetRoot.parent));

        RecursivelyMapChildren(sourceRoot, targetRoot, map);

        isInitialized = true;
    }

    void RecursivelyMapChildren(Transform srcNode, Transform tgtNode, Dictionary<Transform, Transform> map)
    {
        // для каждого child src
        for (int i = 0; i < srcNode.childCount; i++)
        {
            Transform childSrc = srcNode.GetChild(i);
            // try find a child with same name under tgtNode
            Transform childTgt = null;
            if (tgtNode != null)
            {
                childTgt = tgtNode.Find(childSrc.name);
            }

            if (childTgt == null && createMissingTargets && tgtNode != null)
            {
                GameObject go = new GameObject(childSrc.name);
                childTgt = go.transform;
                childTgt.SetParent(tgtNode, false);
                // optionally copy initial transform to visually align
                childTgt.position = childSrc.position;
                childTgt.rotation = childSrc.rotation;
            }

            if (childTgt != null)
            {
                map[childSrc] = childTgt;
                pairs.Add(MakePair(childSrc, childSrc.parent, childTgt, childTgt.parent));
                RecursivelyMapChildren(childSrc, childTgt, map);
            }
            else
            {
                // Если нет соответствующего target и мы не создаём — всё равно рекурсивно не пойдём, потому что нет куда.
                // (альтернатива: можно попытаться найти в глубине, но это медленнее)
                Debug.LogWarning($"{name}: target for source '{childSrc.name}' not found under '{tgtNode?.name ?? "null"}' and createMissingTargets=false. Skipping subtree.");
            }
        }
    }

    Pair MakePair(Transform src, Transform srcParent, Transform tgt, Transform tgtParent)
    {
        Pair p = new Pair();
        p.src = src;
        p.srcParent = srcParent;
        p.tgt = tgt;
        p.tgtParent = tgtParent;

        // compute local deltas relative to srcParent
        if (srcParent != null)
        {
            p.localRot = Quaternion.Inverse(srcParent.rotation) * src.rotation;
            p.localPos = Quaternion.Inverse(srcParent.rotation) * (src.position - srcParent.position);
        }
        else
        {
            // если у source нет родителя — относительная дельта к мировым координатам
            p.localRot = src.rotation;
            p.localPos = src.position;
        }

        p.curRot = tgt.rotation;
        p.curPos = tgt.position;

        // skip apply if target has a rigidbody and option включена
        p.skipApply = skipIfTargetHasRigidbody && (p.tgt.GetComponent<Rigidbody>() != null);

        return p;
    }

    void LateUpdate()
    {
        BuildMapIfNeeded();
        if (pairs.Count == 0) return;

        float t = 1f;
        if (smoothTime > 0f) t = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.00001f, smoothTime));

        foreach (var p in pairs)
        {
            if (p.src == null || p.tgt == null) continue;
            if (p.skipApply) continue;

            // Если родитель источника существует — recompute локальные дельты на лету,
            // чтобы учесть, если source родитель анимируется отдельно (обычно так).
            if (p.srcParent != null)
            {
                p.localRot = Quaternion.Inverse(p.srcParent.rotation) * p.src.rotation;
                p.localPos = Quaternion.Inverse(p.srcParent.rotation) * (p.src.position - p.srcParent.position);
            }
            else
            {
                p.localRot = p.src.rotation;
                p.localPos = p.src.position;
            }

            // axis masking (applied to local euler of localRot)
            if (axisMask != Vector3.one)
            {
                Vector3 e = p.localRot.eulerAngles;
                e = new Vector3(
                    axisMask.x != 0f ? e.x : 0f,
                    axisMask.y != 0f ? e.y : 0f,
                    axisMask.z != 0f ? e.z : 0f
                );
                p.localRot = Quaternion.Euler(e);
            }

            // compute world target based on targetParent
            Quaternion targetWorldRot;
            Vector3 targetWorldPos;
            if (p.tgtParent != null)
            {
                targetWorldRot = p.tgtParent.rotation * p.localRot;
                targetWorldPos = p.tgtParent.position + p.tgtParent.rotation * p.localPos;
            }
            else
            {
                // если у target нет родителя — локальная дельта в мировых coords
                targetWorldRot = p.localRot;
                targetWorldPos = p.localPos;
            }

            // apply smoothing
            if (smoothTime > 0f)
            {
                if (copyRotation) p.curRot = Quaternion.Slerp(p.curRot, targetWorldRot, t);
                if (copyPosition) p.curPos = Vector3.Lerp(p.curPos, targetWorldPos, t);
            }
            else
            {
                if (copyRotation) p.curRot = targetWorldRot;
                if (copyPosition) p.curPos = targetWorldPos;
            }

            // finally apply to transform (cosmetic bones)
            if (copyRotation) p.tgt.rotation = p.curRot;
            if (copyPosition) p.tgt.position = p.curPos;
        }
    }

    // Для явной ручной пересборки (в редакторе/рантайме)
    [ContextMenu("Rebuild Map")]
    public void RebuildMapNow()
    {
        isInitialized = false;
        BuildMapIfNeeded();
    }
}

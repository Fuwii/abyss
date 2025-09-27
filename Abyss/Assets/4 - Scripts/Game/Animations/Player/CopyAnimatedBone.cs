using UnityEngine;

[ExecuteAlways]
public class CopyAnimatedBone : MonoBehaviour
{
    [Header("Source animated bone (read-only)")]
    public Transform animatedBone;          // A
    public Transform animatedParent;        // PA (если null -> animatedBone.parent)

    [Header("Target parent (physics/synced)")]
    [Tooltip("„тобы скопировать локальную позу относительно этой родительской transform'а")]
    public Transform physicsParent;         // PP

    [Header("Options")]
    public bool copyPosition = true;
    public bool copyRotation = true;
    public bool preserveStartBindOffset = false; // учитываем ли начальное смещение
    [Range(0f, 0.5f)] public float smooth = 0f;  // 0 = мгновенно, >0 плавность (в секундах)
    public Vector3 axisMask = Vector3.one;       // (1,1,1) - копировать всЄ; (1,1,0) - игнорировать Z-ось

    // внутренности
    Quaternion bindOffsetRot = Quaternion.identity;
    Vector3 bindOffsetPos = Vector3.zero;
    Quaternion currentRot;
    Vector3 currentPos;

    void Start()
    {
        if (animatedBone == null)
        {
            Debug.LogWarning($"{name}: animatedBone не установлен.");
            enabled = false;
            return;
        }
        if (animatedParent == null) animatedParent = animatedBone.parent;
        if (physicsParent == null)
        {
            Debug.LogWarning($"{name}: physicsParent не установлен. ”станови родител€, к которому прив€зан физический сустав.");
            enabled = false;
            return;
        }

        // «апомним bind offset (разницу между анимированным и физическим в момент старта),
        // чтобы при preserveStartBindOffset=true учитывать его.
        if (preserveStartBindOffset)
        {
            // байнд-оффсет: transform анимированной кости в пространстве physicsParent
            bindOffsetRot = Quaternion.Inverse(physicsParent.rotation) * animatedBone.rotation;
            bindOffsetPos = Quaternion.Inverse(physicsParent.rotation) * (animatedBone.position - physicsParent.position);
        }

        currentRot = transform.rotation;
        currentPos = transform.position;
    }

    // ќбновл€ем после Animator'а Ч LateUpdate
    void LateUpdate()
    {
        if (!animatedBone || !physicsParent) return;

        Transform PA = animatedParent ?? animatedBone.parent;
        if (PA == null) PA = animatedBone.parent;

        // 1) локальна€ дельта анимации относительно еЄ родител€ (PA)
        Quaternion localRot = Quaternion.Inverse(PA.rotation) * animatedBone.rotation;
        Vector3 localPos = Quaternion.Inverse(PA.rotation) * (animatedBone.position - PA.position);

        // 2) если хотим учитывать начальное смещение - комбинируем с bindOffset
        if (preserveStartBindOffset)
        {
            // Ќагружаем bindOffset перед применением локальной дельты
            // (вариант: можно попробовать bindOffset * localRot или localRot * bindOffset - лучше проверить в сцене)
            localRot = bindOffsetRot * localRot;
            localPos = bindOffsetPos + localPos;
        }

        // 3) маска по ос€м (примен€етс€ к локальной ротации)
        if (axisMask != Vector3.one)
        {
            Vector3 e = localRot.eulerAngles;
            e = new Vector3(
                axisMask.x != 0f ? e.x : 0f,
                axisMask.y != 0f ? e.y : 0f,
                axisMask.z != 0f ? e.z : 0f
            );
            localRot = Quaternion.Euler(e);
        }

        // 4) финальна€ мирова€ цель относительно physicsParent
        Quaternion targetWorldRot = physicsParent.rotation * localRot;
        Vector3 targetWorldPos = physicsParent.position + physicsParent.rotation * localPos;

        // 5) сглаживание
        if (smooth > 0f)
        {
            float t = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.0001f, smooth));
            if (copyRotation) currentRot = Quaternion.Slerp(currentRot, targetWorldRot, t);
            if (copyPosition) currentPos = Vector3.Lerp(currentPos, targetWorldPos, t);
        }
        else
        {
            if (copyRotation) currentRot = targetWorldRot;
            if (copyPosition) currentPos = targetWorldPos;
        }

        // 6) применение (дл€ cosmetic bones можно ставить напр€мую)
        if (copyRotation) transform.rotation = currentRot;
        if (copyPosition) transform.position = currentPos;
    }
}

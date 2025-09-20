using UnityEngine;

public class TransformRandomizable : RandomizableComponent
{
    public override RandomCategory Category => RandomCategory.Transform;

    [Header("Transform Randomization Presets")]
    public TransformVariationPreset positionPreset = TransformVariationPreset.None;
    public TransformVariationPreset rotationPreset = TransformVariationPreset.None;

    [Tooltip("Local or world")]
    public bool useLocal = true;
}
public enum TransformVariationPreset
{
    None,
    Tiny,
    Small,
    Medium,
    Large,
    Extreme
}

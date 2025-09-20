using UnityEngine;

namespace Game.Mechanics.Random.Transform
{
    public class TransformRandomizable : RandomizableComponent
    {
        public override RandomCategory Category => RandomCategory.Transform;

        [Header("Transform Randomization Presets")]
        public TransformVariationPreset positionPreset = TransformVariationPreset.None;
        public TransformVariationPreset rotationPreset = TransformVariationPreset.None;

        [Tooltip("Local or world")]
        public bool useLocal = true;
    }
}
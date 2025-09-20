using Game.Mechanics.Random.Seed;
using UnityEngine;

namespace Game.Mechanics.Random.Transform
{
    [DisallowMultipleComponent]
    public class TransformRandomizer : MonoBehaviour, IRandomizer
    {
        public RandomCategory Category => RandomCategory.Transform;

        public void Randomize(RandomizableComponent comp, ref SeededRandom rng, long worldSeed, RandomMode mode)
        {
            var tComp = comp as TransformRandomizable;
            if (tComp == null) return;

            var localRng = rng;
            if (mode == RandomMode.Independent)
            {
                var seed = SeedUtils.CombineSeed(worldSeed, tComp.persistentId, "transform");
                localRng = new SeededRandom(seed);
            }

            var target = tComp.transform;

            //Position
            var posDelta = GetRandomPositionDelta(tComp.positionPreset, ref localRng);
            if (tComp.useLocal)
                target.localPosition += posDelta;
            else
                target.position += posDelta;

            //Rotation 
            var rotDelta = GetRandomRotationDelta(tComp.rotationPreset, ref localRng);
            var deltaQuat = Quaternion.Euler(rotDelta);

            if (tComp.useLocal)
                target.localRotation *= deltaQuat;
            else
                target.rotation *= deltaQuat;
        }

        private Vector3 GetRandomPositionDelta(TransformVariationPreset preset, ref SeededRandom rng)
        {
            var range = preset switch
            {
                TransformVariationPreset.Tiny => 0.5f,
                TransformVariationPreset.Small => 1.5f,
                TransformVariationPreset.Medium => 3f,
                TransformVariationPreset.Large => 6f,
                TransformVariationPreset.Extreme => 12f,
                _ => 0f
            };

            return new Vector3(
                (float)(rng.NextDouble() * 2 - 1) * range,
                (float)(rng.NextDouble() * 2 - 1) * range,
                (float)(rng.NextDouble() * 2 - 1) * range
            );
        }

        private Vector3 GetRandomRotationDelta(TransformVariationPreset preset, ref SeededRandom rng)
        {
            var range = preset switch
            {
                TransformVariationPreset.Tiny => 2.5f,
                TransformVariationPreset.Small => 10f,
                TransformVariationPreset.Medium => 25f,
                TransformVariationPreset.Large => 60f,
                TransformVariationPreset.Extreme => 180f,
                _ => 0f
            };

            return new Vector3(
                (float)(rng.NextDouble() * 2 - 1) * range,
                (float)(rng.NextDouble() * 2 - 1) * range,
                (float)(rng.NextDouble() * 2 - 1) * range
            );
        }
    }
}

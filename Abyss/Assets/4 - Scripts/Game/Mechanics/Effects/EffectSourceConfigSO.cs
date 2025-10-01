using UnityEngine;

namespace Game.Mechanics.Effects
{
    [CreateAssetMenu(fileName = "EffectSourceConfig", menuName = "Game/Effects/EffectSourceConfig", order = 100)]
    public class EffectSourceConfigSO : ScriptableObject
    {
        public EffectType effectType = EffectType.Fire;
        public float delay = 0f; // сек до старта
        public float duration = -1f; // длительность источника (-1 = бесконечный)
        public float tickInterval = -1f; // интервал тика (<=0 = single/instant tick)
        public int stacksPerTick = 1; // сколько стаков добавляется при каждом тике
        public int initialStacks = 0; // применить сразу (игнорирует Delay)
        public bool refreshExisting = false;

        public EffectSourceConfig ToConfig()
        {
            return new EffectSourceConfig
            {
                effectType = effectType,
                delay = delay,
                duration = duration,
                tickInterval = tickInterval,
                stacksPerTick = stacksPerTick,
                initialStacks = initialStacks,
                refreshExisting = refreshExisting
            };
        }
    }
}

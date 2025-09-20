using System;

namespace Game.Mechanics.Effects
{
    [Serializable]
    public class EffectSourceConfig
    {
        public EffectType effectType;
        public float delay; // сек до старта
        public float duration = -1f; // длительность источника (-1 = бесконечный)
        public float tickInterval = -1f; // интервал тика (<=0 = single/instant tick)
        public int stacksPerTick = 1; // сколько стаков добавляется при каждом тике
        public int initialStacks; // применить сразу (игнорирует Delay)

        /// <summary>
        /// Если true и эффект не поддерживает многократных источников, существующий источник будет обновлён/рефрешнут.
        /// </summary>
        public bool refreshExisting;
    }
}
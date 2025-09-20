using System;
using Game.Mechanics.Effects;

namespace Game.Mechanics.Interactables.Consumables
{
    [Serializable]
    public class ConsumableEffectEntry
    {
        public EffectType effectType = EffectType.None;
        public EffectSourceConfig sourceConfig = new();
    }
}
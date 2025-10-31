using System;
using Game.Mechanics.Effects;
using UnityEngine;

namespace Game.Player.UI
{
    [Serializable]
    public struct EffectColor
    {
        [SerializeField] private EffectType type;
        [SerializeField] private Color color;

        public EffectType Type => type;
        public Color Color => color;
    }
}
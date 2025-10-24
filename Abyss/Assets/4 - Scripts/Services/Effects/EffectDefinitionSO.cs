using UnityEngine;
using Game.Mechanics.Effects;

[CreateAssetMenu(fileName = "EffectDefinition", menuName = "Game/Effects/EffectDefinition")]
public class EffectDefinitionSO : ScriptableObject
{
    [Tooltip("Effect enum value (must match the effect type)")]
    public EffectType effectType; 

    [Tooltip("full effect name: Game.Player.Stamina.Effects.FireEffect")]
    public string EffectTypeFullName;
    public DecaySourceConfigSO DecayConfig;
}

using UnityEngine;

[CreateAssetMenu(menuName = "Game/Effects/EffectDefinition", fileName = "EffectDefinition")]
public class EffectDefinitionSO : ScriptableObject
{
    [Tooltip("full effect name: Game.Player.Stamina.Effects.FireEffect")]
    public string EffectTypeFullName;

    public DecaySourceConfigSO DecayConfig;
}

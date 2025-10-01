using UnityEngine;

[CreateAssetMenu(fileName = "DecaySourceConfig", menuName = "Game/Effects/DecaySourceConfig", order = 100)]
public class DecaySourceConfigSO : ScriptableObject
{
    public bool Enabled = true;

    public float TickInterval = 1f;

    public int StacksPerTick = 1;

    public float TotalDuration = 0f;

    public DecaySourceConfig ToConfig()
    {
        return new DecaySourceConfig
        {
            Enabled = Enabled,
            TickInterval = TickInterval,
            StacksPerTick = StacksPerTick,
            TotalDuration = TotalDuration
        };
    }
}

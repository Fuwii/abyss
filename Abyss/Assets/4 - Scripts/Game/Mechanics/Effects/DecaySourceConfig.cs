using UnityEngine;

public class DecaySourceConfig
{
    public bool Enabled;
    public float TickInterval;    // <=0 => remove all immediately
    public int StacksPerTick;
    public float TotalDuration;   // <=0 => no time limit
}
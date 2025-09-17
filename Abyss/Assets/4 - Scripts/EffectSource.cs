using System;
using UnityEngine;
public enum EffectType
{
    None,
    Poison,
    Fire,
}
[Serializable]
public class EffectSourceConfig
{
    public EffectType EffectType;
    public float Delay = 0f; // сек до старта
    public float Duration = -1f; // длительность источника (-1 = бесконечный)
    public float TickInterval = -1f; // интервал тика (<=0 = single/instant tick)
    public int StacksPerTick = 1; // сколько стаков добавляется при каждом тике
    public int InitialStacks = 0; // применить сразу (игнорирует Delay)


    /// <summary>
    /// Если true и эффект не поддерживает многократных источников, существующий источник будет обновлён/рефрешнут.
    /// </summary>
    public bool RefreshExisting = false;
}

public class EffectSource
{
    public float Delay;
    public float RemainingDelay;
    public float Duration; // original duration (can be -1)
    public float RemainingDuration;
    public float TickInterval;
    public float TimeToNextTick;
    public int StacksPerTick;
    public bool Started;


    public void Init(EffectSourceConfig cfg)
    {
        Delay = cfg.Delay;
        RemainingDelay = cfg.Delay;
        Duration = cfg.Duration;
        RemainingDuration = cfg.Duration;
        TickInterval = cfg.TickInterval;
        TimeToNextTick = cfg.TickInterval > 0f ? cfg.TickInterval : 0f;
        StacksPerTick = Math.Max(0, cfg.StacksPerTick);
        Started = false;
    }


    public void Reset()
    {
        Delay = RemainingDelay = 0f;
        Duration = RemainingDuration = 0f;
        TickInterval = 0f;
        TimeToNextTick = 0f;
        StacksPerTick = 0;
        Started = false;
    }
}

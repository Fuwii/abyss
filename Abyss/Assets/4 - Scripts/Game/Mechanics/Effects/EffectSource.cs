using System;

namespace Game.Mechanics.Effects
{
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
            Delay = cfg.delay;
            RemainingDelay = cfg.delay;
            Duration = cfg.duration;
            RemainingDuration = cfg.duration;
            TickInterval = cfg.tickInterval;
            TimeToNextTick = cfg.tickInterval > 0f ? cfg.tickInterval : 0f;
            StacksPerTick = Math.Max(0, cfg.stacksPerTick);
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
}
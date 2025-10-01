using UnityEngine;

public class DecaySource
{
    private DecaySourceConfig _cfg;
    private float _timeToNextTick;
    private float _remainingDuration;

    public void Init(DecaySourceConfig cfg)
    {
        _cfg = cfg;
        Reset();
    }

    public void Reset()
    {
        _timeToNextTick = _cfg != null && _cfg.TickInterval > 0f ? _cfg.TickInterval : 0f;
        _remainingDuration = _cfg != null && _cfg.TotalDuration > 0f ? _cfg.TotalDuration : float.PositiveInfinity;
    }
    public int Update(float dt)
    {
        if (_cfg == null || !_cfg.Enabled) return 0;

        if (_cfg.TickInterval <= 0f)
            return int.MaxValue;

        int totalToRemove = 0;
        _timeToNextTick -= dt;
        while (_timeToNextTick <= 0f)
        {
            totalToRemove += Mathf.Max(1, _cfg.StacksPerTick);
            _timeToNextTick += _cfg.TickInterval;
        }

        if (!float.IsInfinity(_remainingDuration))
        {
            _remainingDuration -= dt;
            if (_remainingDuration <= 0f)
                return int.MaxValue;
        }

        return totalToRemove;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PlayerStamina : MonoBehaviour
{
    [Header("Base stamina")]
    public float maxBaseStamina = 100f;
    public float currentMaxBaseStamina;
    public float bonusStamina = 0f;

    [Header("Recovery")]
    public float defaultRecoveryRate = 10f;

    // runtime
    private float currentRecoveryRate;
    private float currentStamina;

    private readonly Dictionary<Type, StaminaEffect> activeEffects = new Dictionary<Type, StaminaEffect>();

    private readonly Stack<EffectSource> sourcePool = new Stack<EffectSource>(64);

    void Awake()
    {
        currentMaxBaseStamina = maxBaseStamina;
    }

    void Start()
    {
        currentStamina = GetTotalMax();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        UpdateAllEffects(dt);

        currentRecoveryRate = defaultRecoveryRate + activeEffects.Values.Sum(e => GetEffectRecoveryModifier(e));
        currentRecoveryRate = Mathf.Max(0f, currentRecoveryRate);

        float totalMax = GetTotalMax();
        if (currentStamina < totalMax)
        {
            currentStamina += currentRecoveryRate * dt;
            currentStamina = Mathf.Clamp(currentStamina, 0f, totalMax);
        }
    }

    private float GetEffectRecoveryModifier(StaminaEffect effect)
    {
        return effect.GetRecoveryModifier(this) * effect.Stacks;
    }

    private void UpdateAllEffects(float dt)
    {
        var typesToRemove = new List<Type>();

        foreach (var kv in activeEffects)
        {
            var type = kv.Key;
            var effect = kv.Value;

            for (int i = effect.Sources.Count - 1; i >= 0; i--)
            {
                var src = effect.Sources[i];

                if (!src.Started)
                {
                    if (src.RemainingDelay > 0f)
                    {
                        src.RemainingDelay -= dt;
                        if (src.RemainingDelay <= 0f)
                        {
                            src.Started = true;
                            if (src.StacksPerTick > 0 && src.TickInterval <= 0f)
                            {
                                effect.AddStacksInternal(this, src.StacksPerTick);
                            }
                            if (src.TickInterval > 0f)
                                src.TimeToNextTick = src.TickInterval;
                        }
                    }
                }
                else
                {
                    if (src.TickInterval > 0f)
                    {
                        src.TimeToNextTick -= dt;
                        while (src.TimeToNextTick <= 0f)
                        {
                            if (src.StacksPerTick > 0)
                                effect.AddStacksInternal(this, src.StacksPerTick);
                            src.TimeToNextTick += src.TickInterval;
                        }
                    }
                }

                if (src.Duration > 0f)
                {
                    src.RemainingDuration -= dt;
                    if (src.RemainingDuration <= 0f)
                    {
                        ReturnSourceToPool(src);
                        effect.RemoveSourceAt(i);
                    }
                }
            }

            if (effect.Sources.Count == 0 && effect.Stacks <= 0)
            {
                effect.Expire(this);
                typesToRemove.Add(type);
            }
        }

        // чистка завершённых эффектов
        foreach (var t in typesToRemove)
            activeEffects.Remove(t);
    }

    private EffectSource GetSourceFromPool()
    {
        if (sourcePool.Count > 0)
        {
            var s = sourcePool.Pop();
            return s;
        }
        return new EffectSource();
    }

    private void ReturnSourceToPool(EffectSource s)
    {
        s.Reset();
        sourcePool.Push(s);
    }

    // Public API 

    public T ApplyEffect<T>(EffectSourceConfig cfg, Func<T> createIfMissing = null) where T : StaminaEffect, new()
    {
        Type t = typeof(T);
        if (!activeEffects.TryGetValue(t, out StaminaEffect effectObj))
        {
            T inst = createIfMissing != null ? createIfMissing() : new T();
            effectObj = inst;
            activeEffects[t] = effectObj;
        }

        var effect = effectObj as T;

        if (!effect.AllowMultipleSources)
        {
            if (cfg.RefreshExisting || effect.DefaultRefreshExisting)
            {
                foreach (var src in effect.Sources)
                    ReturnSourceToPool(src);
                effect.Sources.Clear();
            }
            else
            {
                if (effect.Sources.Count > 0)
                {
                    var s0 = effect.Sources[0];
                    s0.Init(cfg);
                    if (cfg.InitialStacks > 0)
                        effect.AddStacksInternal(this, cfg.InitialStacks);
                    return effect;
                }
            }
        }

        var newSource = GetSourceFromPool();
        newSource.Init(cfg);
        // apply immediate initial stacks (если указаны и не зависят от delay)
        if (cfg.InitialStacks > 0)
            effect.AddStacksInternal(this, cfg.InitialStacks);

        effect.AddSource(newSource);
        return effect;
    }

    public int TryRemoveStacks<T>(int amount) where T : StaminaEffect
    {
        Type t = typeof(T);
        if (!activeEffects.TryGetValue(t, out StaminaEffect eff)) return 0;
        int removed = eff.RemoveStacksInternal(this, amount);
        // если после снятия нет стаков и нет источников — удалим эффект и вызовем Expire
        if (eff.Stacks <= 0 && eff.Sources.Count == 0)
        {
            eff.Expire(this);
            activeEffects.Remove(t);
        }
        return removed;
    }
    public void RemoveEffect<T>() where T : StaminaEffect
    {
        Type t = typeof(T);
        if (!activeEffects.TryGetValue(t, out StaminaEffect eff)) return;
        foreach (var src in eff.Sources) ReturnSourceToPool(src);
        eff.Sources.Clear();
        eff.RemoveStacksInternal(this, eff.Stacks);
        eff.Expire(this);
        activeEffects.Remove(t);
    }

    public void AdjustCurrentMaxBaseStamina(float delta)
    {
        currentMaxBaseStamina = Mathf.Clamp(currentMaxBaseStamina + delta, 0f, maxBaseStamina);
        float total = GetTotalMax();
        if (currentStamina > total) currentStamina = total;
    }

    public void RestoreCurrentMaxBaseStamina(float amount)
    {
        AdjustCurrentMaxBaseStamina(Mathf.Abs(amount));
    }

    public void ModifyBonusStamina(float delta)
    {
        bonusStamina = Mathf.Max(0f, bonusStamina + delta);
        float total = GetTotalMax();
        if (currentStamina > total) currentStamina = total;
    }

    public bool Consume(float amount)
    {
        if (currentStamina <= 0f) return false;
        currentStamina -= amount;
        if (currentStamina < 0f) currentStamina = 0f;
        return currentStamina > 0f;
    }

    public float GetTotalMax() => Mathf.Max(0f, currentMaxBaseStamina + bonusStamina);
    public float GetCurrentStamina() => currentStamina;
    public float GetCurrentMaxBaseStamina() => currentMaxBaseStamina;
    public float GetMaxBaseStamina() => maxBaseStamina;
    public float GetStamina01() => currentStamina / Mathf.Max(1e-6f, GetTotalMax());
    public void ApplyClimbingDebuff()
    {
        var key = typeof(ClimbingRecoveryDebuff);
        if (!activeEffects.TryGetValue(key, out var eff))
        {
            var deb = new ClimbingRecoveryDebuff();
            deb.AddStacksInternal(this, 1);
            activeEffects[key] = deb;
        }
        else
        {
            // eff.AddStacksInternal(this, 1);
        }
    }

    public void RemoveClimbingDebuff()
    {
        var key = typeof(ClimbingRecoveryDebuff);
        if (!activeEffects.TryGetValue(key, out var eff)) return;

        // убираем 1 стек
        eff.RemoveStacksInternal(this, 1);

        // если эффект больше пуст (нет стеков и нет источников) — удалим
        if (eff.Stacks == 0 && (eff.Sources == null || eff.Sources.Count == 0))
        {
            eff.Expire(this);
            activeEffects.Remove(key);
        }
    }
    public void AddStamina(float amount)
    {
        if (amount <= 0f) return;
        float totalMax = GetTotalMax();
        currentStamina = Mathf.Clamp(currentStamina + amount, 0f, totalMax);
    }
    public void RemoveStamina(float amount)
    {
        if (amount <= 0f) return;
        currentStamina = Mathf.Max(0f, currentStamina - amount);
    }
}


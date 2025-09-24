using System;
using System.Collections.Generic;
using System.Linq;
using Game.Mechanics.Effects;
using Game.Player.Stamina.Effects;
using UnityEngine;

namespace Game.Player.Stamina
{
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Base stamina")]
        [SerializeField] private float maxBaseStamina = 100f;
        [SerializeField] private float currentMaxBaseStamina;
        [SerializeField] private float bonusStamina;

        [Header("Recovery")]
        [SerializeField] private float defaultRecoveryRate = 10f;

        // runtime
        private float _currentRecoveryRate;
        private float _currentStamina;

        private readonly Dictionary<Type, StaminaEffect> _activeEffects = new();
        private readonly Stack<EffectSource> _sourcePool = new(64);

        public float DefaultRecoveryRate => defaultRecoveryRate;

        private void Start()
        {
            currentMaxBaseStamina = maxBaseStamina;
            _currentStamina = GetTotalMax();
        }

        private void Update()
        {
            var dt = Time.deltaTime;

            UpdateAllEffects(dt);

            _currentRecoveryRate = defaultRecoveryRate + _activeEffects.Values.Sum(e => GetEffectRecoveryModifier(e));
            _currentRecoveryRate = Mathf.Max(0f, _currentRecoveryRate);

            var totalMax = GetTotalMax();
            if (_currentStamina < totalMax)
            {
                _currentStamina += _currentRecoveryRate * dt;
                _currentStamina = Mathf.Clamp(_currentStamina, 0f, totalMax);
            }
        }

        private float GetEffectRecoveryModifier(StaminaEffect effect)
        {
            return effect.GetRecoveryModifier(this) * effect.Stacks;
        }

        private void UpdateAllEffects(float dt)
        {
            var typesToRemove = new List<Type>();

            foreach (var kv in _activeEffects)
            {
                var type = kv.Key;
                var effect = kv.Value;

                for (var i = effect.Sources.Count - 1; i >= 0; i--)
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
                _activeEffects.Remove(t);
        }

        private EffectSource GetSourceFromPool()
        {
            if (_sourcePool.Count > 0)
            {
                var s = _sourcePool.Pop();
                return s;
            }

            return new EffectSource();
        }

        private void ReturnSourceToPool(EffectSource s)
        {
            s.Reset();
            _sourcePool.Push(s);
        }

        // Public API 

        public T ApplyEffect<T>(EffectSourceConfig cfg, Func<T> createIfMissing = null) where T : StaminaEffect, new()
        {
            var t = typeof(T);
            if (!_activeEffects.TryGetValue(t, out var effectObj))
            {
                var inst = createIfMissing != null ? createIfMissing() : new T();
                effectObj = inst;
                _activeEffects[t] = effectObj;
            }

            var effect = effectObj as T;

            if (!effect.AllowMultipleSources)
            {
                if (cfg.refreshExisting || effect.DefaultRefreshExisting)
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
                        if (cfg.initialStacks > 0)
                            effect.AddStacksInternal(this, cfg.initialStacks);
                        return effect;
                    }
                }
            }

            var newSource = GetSourceFromPool();
            newSource.Init(cfg);

            // apply immediate initial stacks (если указаны и не зависят от delay)
            if (cfg.initialStacks > 0)
                effect.AddStacksInternal(this, cfg.initialStacks);

            effect.AddSource(newSource);
            return effect;
        }

        public int TryRemoveStacks<T>(int amount) where T : StaminaEffect
        {
            var t = typeof(T);
            if (!_activeEffects.TryGetValue(t, out var eff)) return 0;
            var removed = eff.RemoveStacksInternal(this, amount);

            // если после снятия нет стаков и нет источников — удалим эффект и вызовем Expire
            if (eff.Stacks <= 0 && eff.Sources.Count == 0)
            {
                eff.Expire(this);
                _activeEffects.Remove(t);
            }

            return removed;
        }

        public void RemoveEffect<T>() where T : StaminaEffect
        {
            var t = typeof(T);
            if (!_activeEffects.TryGetValue(t, out var eff)) return;
            foreach (var src in eff.Sources) ReturnSourceToPool(src);
            eff.Sources.Clear();
            eff.RemoveStacksInternal(this, eff.Stacks);
            eff.Expire(this);
            _activeEffects.Remove(t);
        }

        public void AdjustCurrentMaxBaseStamina(float delta)
        {
            currentMaxBaseStamina = Mathf.Clamp(currentMaxBaseStamina + delta, 0f, maxBaseStamina);
            var total = GetTotalMax();
            if (_currentStamina > total) _currentStamina = total;
        }

        public void RestoreCurrentMaxBaseStamina(float amount)
        {
            AdjustCurrentMaxBaseStamina(Mathf.Abs(amount));
        }

        public void ModifyBonusStamina(float delta)
        {
            bonusStamina = Mathf.Max(0f, bonusStamina + delta);
            var total = GetTotalMax();
            if (_currentStamina > total) _currentStamina = total;
        }

        public bool Consume(float amount)
        {
            if (_currentStamina <= 0f) return false;
            _currentStamina -= amount;
            if (_currentStamina < 0f) _currentStamina = 0f;
            return _currentStamina > 0f;
        }

        public float GetTotalMax() => Mathf.Max(0f, currentMaxBaseStamina + bonusStamina);
        public float GetCurrentStamina() => _currentStamina;
        public float GetCurrentMaxBaseStamina() => currentMaxBaseStamina;
        public float GetMaxBaseStamina() => maxBaseStamina;
        public float GetStamina01() => _currentStamina / Mathf.Max(1e-6f, GetTotalMax());

        public void ApplyClimbingDebuff()
        {
            var key = typeof(ClimbingRecoveryDebuff);
            if (!_activeEffects.TryGetValue(key, out var eff))
            {
                var deb = new ClimbingRecoveryDebuff();
                deb.AddStacksInternal(this, 1);
                _activeEffects[key] = deb;
            }
            else
            {
                // eff.AddStacksInternal(this, 1);
            }
        }

        public void RemoveClimbingDebuff()
        {
            var key = typeof(ClimbingRecoveryDebuff);
            if (!_activeEffects.TryGetValue(key, out var eff)) return;

            // убираем 1 стек
            eff.RemoveStacksInternal(this, 1);

            // если эффект больше пуст (нет стеков и нет источников) — удалим
            if (eff.Stacks == 0 && (eff.Sources == null || eff.Sources.Count == 0))
            {
                eff.Expire(this);
                _activeEffects.Remove(key);
            }
        }

        public void AddStamina(float amount)
        {
            if (amount <= 0f) return;
            var totalMax = GetTotalMax();
            _currentStamina = Mathf.Clamp(_currentStamina + amount, 0f, totalMax);
        }

        public void RemoveStamina(float amount)
        {
            if (amount <= 0f) return;
            _currentStamina = Mathf.Max(0f, _currentStamina - amount);
        }
    }
}
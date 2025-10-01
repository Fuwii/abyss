using System;
using System.Collections.Generic;
using Game.Mechanics.Effects;
using UnityEngine;

namespace Game.Player.Stamina
{
    public abstract class StaminaEffect
    {
        // Публичный интерфейс и поля
        public readonly List<EffectSource> Sources = new();
        public int Stacks { get; protected set; }

        // Настройки поведения
        /// <summary>Можно ли иметь несколько активных источников данного эффекта (например яд: true, горение: false)</summary>
        public virtual bool AllowMultipleSources => true;

        /// <summary>Если AllowMultipleSources == false и RefreshExisting==true, то при Apply существующий источник будет рефрешнут.</summary>
        public virtual bool DefaultRefreshExisting => false;
        /// <summary>
        ///  Просто config для decay если эффект исчезает
        /// </summary>
        private DecaySourceConfig _decayCfg;
        private readonly DecaySource _decay = new DecaySource();
        // Хуки
        protected virtual void OnStacksAdded(PlayerStamina playerStamina, int amount) { }
        protected virtual void OnStacksRemoved(PlayerStamina playerStamina, int amount) { }
        protected virtual void OnExpired(PlayerStamina playerStamina) { }

        /// <summary>
        /// Добавление источника (внешний код не должен напрямую модифицировать Sources, используйте AddSource либо PlayerStamina.ApplyEffect)
        /// После того как источник начинает тикать, он вызывает AddStacksInternal через внешнюю систему.
        /// </summary>
        public void AddSource(EffectSource src)
        {
            Sources.Add(src);
            if (_decayCfg != null)
                _decay.Reset();
        }

        /// <summary>
        /// Удалить источник по индексу.
        /// </summary>
        public void RemoveSourceAt(int index)
        {
            Sources.RemoveAt(index);
            if (_decayCfg != null)
                _decay.Reset();
        }

        /// <summary>
        /// Добавить стаки — вызывает OnStacksAdded и увеличивает счётчик стеков.
        /// </summary>
        public void AddStacksInternal(PlayerStamina s, int amount)
        {
            if (amount <= 0) return;
            Stacks += amount;
            OnStacksAdded(s, amount);
        }

        /// <summary>
        /// Удалить стаки (например антидот/еда вызывает это). Возвращает реальное кол-во удалённых стаков.
        /// </summary>
        public int RemoveStacksInternal(PlayerStamina s, int amount)
        {
            if (amount <= 0) return 0;
            var toRemove = Math.Min(Stacks, amount);
            if (toRemove <= 0) return 0;
            Stacks -= toRemove;
            OnStacksRemoved(s, toRemove);
            return toRemove;
        }

        /// <summary>
        /// Вызывается PlayerStamina при проверке, что эффект полностью истёк (нет источников и стаков).
        /// </summary>
        public void Expire(PlayerStamina s)
        {
            OnExpired(s);
        }

        public virtual float GetRecoveryModifier(PlayerStamina s)
        {
            return 0f;
        }
        /// <summary>
        /// decay. 
        /// Вызывать только если Sources.Count == 0.
        /// Возвращает сколько стаков реально снято.
        /// </summary>
        public int UpdateDecay(PlayerStamina owner, float dt)
        {
            if (_decayCfg == null || Sources.Count > 0)
                return 0;

            int want = _decay.Update(dt);
            if (want == 0) return 0;

            if (want == int.MaxValue)
            {
                // снять всё
                if (Stacks <= 0) return 0;
                return RemoveStacksInternal(owner, Stacks);
            }
            else
            {
                // снять ограниченное число
                var toRemove = Math.Min(Stacks, want);
                if (toRemove <= 0) return 0;
                return RemoveStacksInternal(owner, toRemove);
            }
        }
        public void ConfigureDecay(DecaySourceConfig cfg)
        {
            _decayCfg = cfg;
        }
        public void InitializeDecay()
        {
            if (_decayCfg != null)
                _decay.Init(_decayCfg);
        }
    }
}
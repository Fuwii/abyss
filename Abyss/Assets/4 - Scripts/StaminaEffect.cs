using System;
using System.Collections.Generic;
using UnityEngine;


public abstract class StaminaEffect
{
    // Публичный интерфейс и поля
    public List<EffectSource> Sources = new List<EffectSource>();
    public int Stacks { get; protected set; } = 0;


    // Настройки поведения
    /// <summary>Можно ли иметь несколько активных источников данного эффекта (например яд: true, горение: false)</summary>
    public virtual bool AllowMultipleSources => true;
    /// <summary>Если AllowMultipleSources == false и RefreshExisting==true, то при Apply существующий источник будет рефрешнут.</summary>
    public virtual bool DefaultRefreshExisting => false;


    // Хуки
    protected virtual void OnStacksAdded(PlayerStamina s, int amount) { }
    protected virtual void OnStacksRemoved(PlayerStamina s, int amount) { }
    protected virtual void OnExpired(PlayerStamina s) { }


    /// <summary>
    /// Добавление источника (внешний код не должен напрямую модифицировать Sources, используйте AddSource либо PlayerStamina.ApplyEffect)
    /// После того как источник начинает тикать, он вызывает AddStacksInternal через внешнюю систему.
    /// </summary>
    public void AddSource(EffectSource src)
    {
        Sources.Add(src);
    }


    /// <summary>
    /// Удалить источник по индексу.
    /// </summary>
    public void RemoveSourceAt(int index)
    {
        Sources.RemoveAt(index);
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
        int toRemove = Math.Min(Stacks, amount);
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
}
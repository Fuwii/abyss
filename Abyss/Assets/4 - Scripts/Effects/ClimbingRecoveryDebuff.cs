using UnityEngine;

public class ClimbingRecoveryDebuff : StaminaEffect
{
    // Не позволяем несколько источников (по желанию)
    public override bool AllowMultipleSources => false;
    public override bool DefaultRefreshExisting => true;

    // Этот эффект просто снимает базовый рекап (per stack).
    // Мы возвращаем отрицательное значение, чтобы currentRecoveryRate = defaultRecoveryRate + sum(modifiers)
    public override float GetRecoveryModifier(PlayerStamina s)
    {
        // Уменьшаем на базовый рекавери. Если нужен частичный дебафф, умножь на factor (0..1).
        return -s.defaultRecoveryRate;
    }

    protected override void OnExpired(PlayerStamina s)
    {
        
    }
}

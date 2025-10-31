using UnityEngine;

namespace Game.Player.Stamina.Effects
{
    public class FireEffect : StaminaEffect
    {
        [SerializeField] private DecaySourceConfigSO decayConfigSO;

        public override bool AllowMultipleSources => true;

        protected override void OnStacksAdded(PlayerStamina playerStamina, int amount)
        {
            Debug.Log("stamina adjusted");
            if (amount <= 0) return;
            playerStamina.AdjustCurrentMaxBaseStamina(-amount);
        }

        protected override void OnStacksRemoved(PlayerStamina playerStamina, int amount)
        {
            if (amount <= 0) return;
            playerStamina.AdjustCurrentMaxBaseStamina(amount);
        }

        protected override void OnExpired(PlayerStamina playerStamina)
        {
            if (Stacks > 0)
            {
                playerStamina.AdjustCurrentMaxBaseStamina(Stacks);
                Stacks = 0;
            }
        }
    }
}
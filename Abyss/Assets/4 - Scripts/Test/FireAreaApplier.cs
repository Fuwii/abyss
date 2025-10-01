using UnityEngine;
using Game.Player.Stamina;
using Game.Player.Stamina.Effects;

namespace Game.Mechanics.Effects
{
    [RequireComponent(typeof(Collider))]
    public class FireAreaApplier : MonoBehaviour
    {
        [Tooltip("ScriptableObject с конфигом источника эффекта")]
        public EffectSourceConfigSO configSO;

        [Tooltip("≈сли true Ч будет накладывать эффект только при входе, иначе Ч при удержании (каждый Enter).")]
        public bool applyOncePerEnter = true;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (configSO == null) return;
            var ps = other.GetComponent<PlayerStamina>();
            if (ps == null) return;

            ApplyTo(ps);
        }

        private void ApplyTo(PlayerStamina ps)
        {
            var cfg = configSO.ToConfig();
            var effect = ps.ApplyEffect<FireEffect>(cfg, () => new FireEffect());
        }

    }
}

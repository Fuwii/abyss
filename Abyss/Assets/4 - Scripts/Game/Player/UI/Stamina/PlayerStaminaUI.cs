using System.Collections.Generic;
using System.Linq;
using Game.Mechanics.Effects;
using Game.Player.Stamina;
using Services.Effects;
using UnityEngine;

namespace Game.Player.UI
{
    public class PlayerStaminaUI : MonoBehaviour
    {
        [SerializeField] private PlayerStamina playerStamina;
        [SerializeField] private RectTransform staminaBar;

        [Space]
        [SerializeField] private RectTransform staminaArea;
        [SerializeField] private RectTransform staminaFill;

        [Space]
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private RectTransform effectsArea;
        [SerializeField] private EffectColor[] effectColors;

        private readonly Dictionary<EffectType, PlayerEffectUI> _activeUI = new();

        private void FixedUpdate()
        {
            UpdateStamina();
            UpdateEffects();
        }

        private void UpdateStamina()
        {
            staminaFill.offsetMax = staminaArea.sizeDelta * Vector2.left * (1 - playerStamina.GetStamina01());
        }

        private void UpdateEffects()
        {
            var effects = playerStamina.ActiveEffects
                .Select(e =>
                {
                    EffectRegistry.Instance.TryGetEffectType(e.GetType(), out var type);
                    return (type, e.Stacks);
                })
                .ToArray();

            effectsArea.gameObject.SetActive(effects.Any());

            foreach (var effect in effects)
            {
                if (!_activeUI.TryGetValue(effect.type, out PlayerEffectUI ui))
                {
                    var effectColor = effectColors.FirstOrDefault(ec => ec.Type == effect.type);

                    ui = Instantiate(effectPrefab, effectsArea)
                        .GetComponent<PlayerEffectUI>();

                    ui.SetColor(effectColor.Color);
                    _activeUI[effect.type] = ui;
                }

                var uiTransform = ui.transform as RectTransform;
                var size = uiTransform!.sizeDelta;
                size.x = staminaBar.rect.width * (effect.Stacks / 100.0f);

                uiTransform.sizeDelta = size;
            }

            var toRemove = _activeUI.Keys
                .Except(effects.Select(e => e.type))
                .ToArray();

            foreach (var type in toRemove)
            {
                Destroy(_activeUI[type].gameObject);

                _activeUI.Remove(type);
            }
        }
    }
}
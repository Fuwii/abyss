using Game.Mechanics.Effects;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectRegistry : MonoBehaviour
{
    public static EffectRegistry Instance { get; private set; }

    [SerializeField] private EffectDefinitionSO[] definitions;

    private readonly Dictionary<EffectType, Type> _typeMap = new();
    private Dictionary<Type, DecaySourceConfig> _decayMap;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _decayMap = new Dictionary<Type, DecaySourceConfig>();

        foreach (var def in definitions)
        {
            if (def == null || string.IsNullOrEmpty(def.EffectTypeFullName)) continue;

            var t = Type.GetType(def.EffectTypeFullName);
            if (t == null)
            {
                Debug.LogWarning($"EffectRegistry: type not found {def.EffectTypeFullName} check name {def.name}");
                continue;
            }
            try
            {
                _typeMap[def.effectType] = t;
            }
            catch {  }

            if (def.DecayConfig != null)
                _decayMap[t] = def.DecayConfig.ToConfig();
        }
    }

    public bool TryGetDecayConfig(Type effectType, out DecaySourceConfig cfg)
    {
        cfg = default;
        return _decayMap != null && _decayMap.TryGetValue(effectType, out cfg);
    }

    public bool TryGetEffectType(EffectType effectTypeEnum, out Type outType)
    {
        outType = null;
        return _typeMap != null && _typeMap.TryGetValue(effectTypeEnum, out outType);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public class ConsumableEffectEntry
{
    public EffectType EffectType = EffectType.None;
    public EffectSourceConfig SourceConfig = new EffectSourceConfig();
}

[CreateAssetMenu(menuName = "Items/Consumable", fileName = "New Consumable")]
public class ConsumableData : ScriptableObject
{
    public string Item_id = "Cosumable_1";
    public string ItemName = "Consumable";
    public Sprite Icon;
    public int Weight = 1;
    public int Price = 1;
    public float InstantStamina = 0f;
    public List<ConsumableEffectEntry> Effects = new List<ConsumableEffectEntry>();
}
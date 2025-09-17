
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Tool", fileName = "New Tool")]
public class ToolData : ScriptableObject
{
    public string ItemName = "Tool";
    public Sprite Icon;
    public int Weight = 1;
    public int Price = 1;
}
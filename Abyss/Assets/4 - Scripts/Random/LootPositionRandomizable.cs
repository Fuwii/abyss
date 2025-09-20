using UnityEngine;

public class LootPositionRandomizable : RandomizableComponent
{
    public override RandomCategory Category => RandomCategory.LootPosition;

    [Header("Allowed loot tables for this spawn point")]
    public LootTable[] allowedTables; 
}

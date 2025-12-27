using System;
using Unity.Netcode;
using UnityEngine;

public struct NetworkItem : INetworkSerializable, IEquatable<NetworkItem>
{
    public short ItemID;      // -1 если пусто
    public int RemainingUses;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemID);
        serializer.SerializeValue(ref RemainingUses);
    }

    public bool Equals(NetworkItem other) => ItemID == other.ItemID && RemainingUses == other.RemainingUses;
}

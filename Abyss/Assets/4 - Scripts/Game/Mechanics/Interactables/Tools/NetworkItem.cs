using System;
using Unity.Netcode;
using UnityEngine;

public struct NetworkItem : INetworkSerializable, IEquatable<NetworkItem>
{
    public short ItemID;
    public int RemainingUses;
    public NetworkObjectReference WorldObjRef; 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemID);
        serializer.SerializeValue(ref RemainingUses);
        serializer.SerializeValue(ref WorldObjRef);
    }

    public bool Equals(NetworkItem other)
    {
        return ItemID == other.ItemID && WorldObjRef.Equals(other.WorldObjRef);
    }
    public static NetworkItem Empty => new NetworkItem { ItemID = -1 };
}

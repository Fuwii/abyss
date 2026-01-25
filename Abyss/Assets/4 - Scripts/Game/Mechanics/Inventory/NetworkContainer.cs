using Game.Mechanics.Interactables.Tools;
using Unity.Netcode;
using UnityEngine;

public class NetworkContainer : NetworkBehaviour
{
    public NetworkList<NetworkItem> Contents { get; private set; }

    public int Capacity { get; private set; }

    void Awake()
    {
        Contents = new NetworkList<NetworkItem>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        if (Contents.Count != 0) return;

        var item = GetComponent<ItemBehaviour>();
        if (item == null) return;

        if (item.data is not BackpackItemData bpData) return;
        Capacity = bpData.capacity;
        for (int i = 0; i < Capacity; i++)
            Contents.Add(NetworkItem.Empty);
    }

    public void Init(int capacity)
    {
        if (!IsServer) return;
        Capacity = capacity;
    }
    public bool TryPut(int index, NetworkItem item)
    {
        if (index < 0 || index >= Contents.Count) return false;
        if (Contents[index].ItemID != -1) return false;

        Contents[index] = item;
        return true;
    }

    public bool TryTake(int index, out NetworkItem item)
    {
        item = default;
        if (index < 0 || index >= Contents.Count) return false;
        if (Contents[index].ItemID == -1) return false;

        item = Contents[index];
        Contents[index] = NetworkItem.Empty;
        return true;
    }
}

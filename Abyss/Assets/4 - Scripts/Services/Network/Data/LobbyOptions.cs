using System;
using UnityEngine;

namespace Services.Network.Data
{
    [Serializable]
    public class LobbyOptions
    {
        [SerializeField] private int maxPlayers;

        [SerializeField] private LobbyType lobbyType;

        public int MaxPlayers => maxPlayers;
        public LobbyType LobbyType => lobbyType;
    }
}
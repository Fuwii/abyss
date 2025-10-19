using Game.Mechanics.Random;
using Services.Level;
using UnityEngine;

namespace Game.Locations.Game
{
    public class LevelBootstrap : MonoBehaviour
    {
        private void Start()
        {
            var levelInfo = LevelInfo.Instance;
            var randomManager = RandomizationManager.Instance;

            randomManager.Run(levelInfo.Seed);
        }
    }
}
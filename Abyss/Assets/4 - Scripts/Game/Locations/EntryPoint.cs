using System;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Locations
{
    [DisallowMultipleComponent]
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private SceneReference scene;

        [Space]
        [SerializeField] private int targetFPS = 60;

        private async void Start()
        {
            try
            {
                Application.targetFrameRate = targetFPS;

                await SceneManager.LoadSceneAsync(scene.Path, LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
using Core.Scene;
using Core.Singleton;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.SceneManagement;

namespace Services
{
    public class SceneManager : Service<SceneManager>
    {
        private readonly Subject<Unit> _onSceneLoaded = new();
        private readonly Subject<Unit> _onSceneUnloaded = new();

        public Observable<Unit> OnSceneLoaded => _onSceneLoaded;
        public Observable<Unit> OnSceneUnloaded => _onSceneUnloaded;

        public async UniTask Load(string sceneName, LoadSceneMode mode)
        {
            await GameComponent<LoadingHook>.EnabledComponents
                .Select(h => h.PreLoad());

            await GameComponent<LoadingHook>.EnabledComponents
                .Select(h => h.PostLoad());
        }

        public async UniTask Unload()
        {
            await GameComponent<LoadingHook>.EnabledComponents
                .Select(h => h.PreUnload());

            await GameComponent<LoadingHook>.EnabledComponents
                .Select(h => h.PostUnload());
        }

        private void OnSceneLoadedCallback(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
        }

        private void OnUnloadCompleteCallback(ulong clientId, string sceneName)
        {
        }
    }
}
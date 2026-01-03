using Cysharp.Threading.Tasks;

namespace Core.Scene
{
    public abstract class LoadingHook : GameComponent<LoadingHook>
    {
        public UniTask PreLoad()
        {
            return UniTask.CompletedTask;
        }

        public UniTask PostLoad()
        {
            return UniTask.CompletedTask;
        }

        public UniTask PreUnload()
        {
            return UniTask.CompletedTask;
        }

        public UniTask PostUnload()
        {
            return UniTask.CompletedTask;
        }
    }
}
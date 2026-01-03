using Cysharp.Threading.Tasks;

namespace Core.Scene
{
    public interface ILoader
    {
        public UniTask Load();
        public UniTask Unload();
    }
}
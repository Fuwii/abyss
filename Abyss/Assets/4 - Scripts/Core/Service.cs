namespace Core
{
    public abstract class Service<T> : Singleton<T>
        where T : Service<T>
    {
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
    }
}
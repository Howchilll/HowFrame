using UnityEngine;

public abstract class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    protected abstract bool DestroyOnLoad { get; }
    
    public virtual void InitLevel(){}
    public virtual void InitGame(){}
    protected virtual void Init(){}
    
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this as T;
        if (DestroyOnLoad) DontDestroyOnLoad(gameObject);
        Init();
    }
    public void DisposeSingleton()
    {
        if (Instance == this)
            Instance = null;
        Destroy(gameObject);
    }
}
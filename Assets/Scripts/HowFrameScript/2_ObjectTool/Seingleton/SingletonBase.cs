using UnityEngine;

public abstract class Singleton<T> where T : class, new()
{
    private static T _instance = new T();

    public static T Instance
    {
        get
        {
            if (_instance == null)
                _instance = new T();
            return _instance;
        }
    }

    public static void DestroyInstance()
    {
        _instance = null;
    }
}
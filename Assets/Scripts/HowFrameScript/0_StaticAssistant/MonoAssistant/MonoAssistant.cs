using System;
using UnityEngine;

public static class MonoAssistant
{
    private static MonoHelper _instance;
    private static MonoHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("MonoAssistant_Helper");
                UnityEngine.Object.DontDestroyOnLoad(obj);
                _instance = obj.AddComponent<MonoHelper>();
            }
            return _instance;
        }
    }
    public static event Action OnUpdate;
    public static event Action OnFixedUpdate;
    public static event Action OnLateUpdate;
    
    static MonoAssistant() => Instance.Init();

    private class MonoHelper : MonoBehaviour
    {
        public void Init() { } 
        private void Update() => OnUpdate?.Invoke();
        private void FixedUpdate() => OnFixedUpdate?.Invoke();
        private void LateUpdate() => OnLateUpdate?.Invoke();
    }

    internal static void wake(){}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace HowFrame
{

public static class SceneLoadAssistant
{

    public static float LoadValue = 0f;
    public static bool ChangeSign = false;
    private static GameObject _sceneLoadManager = null;
    private static FakeMono _fakeMono = null;
    private static bool _initialized = false;

    public static void LoadScene(string sceneName, bool changeSign = true)
    {
        if (_fakeMono == null)
        {
            Debug.LogError("SceneLoadAssistant: 未初始化，请先调用 Wake()");
            Wake(); // 自动初始化以保持向后兼容
        }
        ChangeSign = changeSign;
        _fakeMono.StartCoroutine(_LoadScene(sceneName));
    }

    private static IEnumerator _LoadScene(string sceneName)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        while (!load.isDone)
        {
            if (LoadValue >= 0.9f)
            {
                if (ChangeSign)
                {
                    load.allowSceneActivation = true;
                }
            }
            else
            {
                LoadValue = load.progress;
            }

            yield return null;
        }

        LoadValue = 0;
        ChangeSign = false;
    }

    /// <summary>
    /// 初始化 SceneLoadAssistant（延迟初始化，在资源加载完成后调用）
    /// </summary>
    public static void Wake()
    {
        if (_initialized) return; // 防止重复初始化
        _sceneLoadManager = new GameObject("SceneLoadManager");
        _fakeMono = _sceneLoadManager.AddComponent<FakeMono>();
        Object.DontDestroyOnLoad(_sceneLoadManager);
        _initialized = true;
    }

    private class FakeMono : MonoBehaviour
    {
    }
}
}
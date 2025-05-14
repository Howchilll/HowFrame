using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoadAssistant
{
    public static float LoadValue = 0f;
    public static bool ChangeSign = false;
    private static GameObject _sceneLoadManager = null;
    private static FakeMono _fakeMono = null;

    static SceneLoadAssistant()
    {
        _sceneLoadManager = new GameObject("SceneLoadManager");
        _fakeMono = _sceneLoadManager.AddComponent<FakeMono>();
        Object.DontDestroyOnLoad(_sceneLoadManager);
    }

    public static void LoadScene(string sceneName,bool changeSign = true)
    {
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

    public static void wake(){}
    
    private class FakeMono : MonoBehaviour
    {
    }


}
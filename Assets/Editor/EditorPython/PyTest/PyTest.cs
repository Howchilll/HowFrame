using System.IO;
using UnityEditor;
using UnityEngine;

public static class PyTest
{
    [MenuItem("Tools/PyFunctions/PyTest")]
    public static void Run()
    {
        string scriptPath = Path.Combine(Application.dataPath,"Editor","EditorPython","PyTest", "py_test.py");
        scriptPath = Path.GetFullPath(scriptPath);
        
          
        try
        {
            PyCaller  pyCaller = new PyCaller();

            pyCaller.RunPythonScript(scriptPath, OnPyDone);
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"执行 Python 脚本时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void OnPyDone(int exitCode)
    {
        if (exitCode == 0)
        {
            Debug.Log("Python 脚本执行成功！");
        }
        else
        {
            Debug.LogWarning($"Python 脚本执行完成，但退出码不为 0: {exitCode}");
        }
    }
    
}

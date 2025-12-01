using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PyCaller
{
    private Process pythonProcess;
    private string pythonExePath = EditorPath.PythonPath;
    
    public void RunPythonScript(string scriptPath, string args = "", Action<int> onCompleted = null)
    {
        RunPythonScript(scriptPath, args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), onCompleted);
    }
    public void RunPythonScript(string scriptPath, Action<int> onCompleted = null)
    {
        RunPythonScript(scriptPath,  "".Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), onCompleted);
    }
    public void RunPythonScript(string scriptPath, string[] args, Action<int> onCompleted = null)
    {

        
        string argsString = "";
        if (args != null && args.Length > 0)
        {
            var quotedArgs = new System.Collections.Generic.List<string>();
            foreach (string arg in args)
            {
                if (arg.Contains(" "))
                {
                    quotedArgs.Add($"\"{arg}\"");
                }
                else
                {
                    quotedArgs.Add(arg);
                }
            }
            argsString = string.Join(" ", quotedArgs);
            Debug.Log($"Python 脚本参数: {argsString}");
        }
        
        if (string.IsNullOrEmpty(pythonExePath) || !System.IO.File.Exists(pythonExePath))
        {
            Debug.LogError($"Python 可执行文件不存在: {pythonExePath}");
            throw new System.IO.FileNotFoundException($"Python 可执行文件不存在: {pythonExePath}");
        }
        
        if (string.IsNullOrEmpty(scriptPath) || !System.IO.File.Exists(scriptPath))
        {
            Debug.LogError($"Python 脚本不存在: {scriptPath}");
            throw new System.IO.FileNotFoundException($"Python 脚本不存在: {scriptPath}");
        }

        pythonProcess = new Process();
        pythonProcess.StartInfo.FileName = pythonExePath;
        pythonProcess.StartInfo.Arguments = $"-u \"{scriptPath}\" {argsString}";
        pythonProcess.StartInfo.UseShellExecute = false;
        pythonProcess.StartInfo.RedirectStandardOutput = true;
        pythonProcess.StartInfo.RedirectStandardError = true;
        pythonProcess.StartInfo.CreateNoWindow = true;
        
        pythonProcess.StartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

        pythonProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Debug.Log("[Python Output] " + e.Data);
            }
        };

        pythonProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Debug.LogError("[Python Error] " + e.Data);
            }
        };

        if (onCompleted != null)
        {
            pythonProcess.EnableRaisingEvents = true;
            pythonProcess.Exited += (sender, e) =>
            {
                int exitCode = pythonProcess.ExitCode;
                Debug.Log($"Python 进程已退出，退出码: {exitCode}");
                try
                {
                    onCompleted?.Invoke(exitCode);
                }
                catch (Exception callbackEx)
                {
                    Debug.LogError($"执行完成回调时出错: {callbackEx.Message}");
                }
            };
        }

        try
        {
            pythonProcess.Start();
            Debug.Log("Python 进程已启动（已启用实时输出模式）");
            
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();
            
            if (onCompleted == null)
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Run(() => pythonProcess.WaitForExit());
                    Debug.Log($"Python 进程已退出，退出码: {pythonProcess.ExitCode}");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"启动 Python 进程失败: {ex.Message}");
            throw;
        }
    }

    public bool IsFinished()
    {
        if (pythonProcess == null) return true;
        return pythonProcess.HasExited;
    }

    public void WaitForExit()
    {
        pythonProcess?.WaitForExit();
    }
}

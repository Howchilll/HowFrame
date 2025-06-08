using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public static class TimeAssistant
{
    private static readonly Dictionary<string,float> TimeDic = new Dictionary<string, float>();
    private static Tweener _timeScaleTweener;
    
    
    public static (float deltaTime, bool reachedThreshold) TimeCheck(string name, float threshold)
    {
        float currentTime = Time.realtimeSinceStartup;

        if (!TimeDic.TryGetValue(name, out float lastTime))
        {
            TimeDic[name] = currentTime;
            return (0f, true); // 第一次调用，默认视为达到阈值
        }

        float delta = currentTime - lastTime;
        bool reached = delta >= threshold;
        if (reached)
        {
            TimeDic[name] = currentTime; // 达到阈值才更新
        }

        return (delta, reached);
    }
    public static void ClearAllTimers()
    {
        TimeDic.Clear();
    }
    public static void ClearTimer(string name)
    {
        TimeDic.Remove(name);
    }

    public static void SetTimeScale(float target, float duration = 0f, Ease ease = Ease.Linear)
    {
        _timeScaleTweener?.Kill();

        if (duration <= 0f)
        {
            Time.timeScale = target;
        }
        else
        {
            _timeScaleTweener = DOTween.To(() => Time.timeScale, x => Time.timeScale = x, target, duration)
                .SetEase(ease)
                .SetUpdate(true);
        }
    }
    public static void ResetTimeScale(float duration = 0f, Ease ease = Ease.Linear)
    {
        SetTimeScale(1f, duration, ease);
    }
    public static string TimeToString(float time)
    {
        int totalSeconds = Mathf.FloorToInt(time);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        int milliseconds = Mathf.FloorToInt((time - totalSeconds) * 1000);

        if (hours > 0)
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
        else
            return $"{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
    }
    public static long StringToTime(string timeString)
    {
        // 支持格式： "HH:mm:ss.mmm" 或 "mm:ss.mmm"
        var parts = timeString.Split(':');
        int hours = 0, minutes = 0, seconds = 0, milliseconds = 0;

        if (parts.Length == 3)
        {
            hours = int.Parse(parts[0]);
            minutes = int.Parse(parts[1]);

            var secParts = parts[2].Split('.');
            seconds = int.Parse(secParts[0]);
            if (secParts.Length > 1)
                milliseconds = int.Parse(secParts[1].PadRight(3, '0').Substring(0, 3));
        }
        else if (parts.Length == 2)
        {
            minutes = int.Parse(parts[0]);
        
            var secParts = parts[1].Split('.');
            seconds = int.Parse(secParts[0]);
            if (secParts.Length > 1)
                milliseconds = int.Parse(secParts[1].PadRight(3, '0').Substring(0, 3));
        }
        else
        {
            throw new FormatException("时间字符串格式错误，应为 mm:ss.mmm 或 HH:mm:ss.mmm");
        }

        long totalMilliseconds = hours * 3600_000L + minutes * 60_000L + seconds * 1000L + milliseconds;
        return totalMilliseconds;
    }

   

}
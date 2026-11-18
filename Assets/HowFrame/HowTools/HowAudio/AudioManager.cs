using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace HowFrame
{
    /// <summary>
    /// 音频管理：Music（循环、可淡出）/ Sound（一次性、池化 AudioSource）两类通道，经 AudioMixer 控制音量。
    /// 音频资源来自 AssetAssistant 的 Addressables 缓存，生命周期由 AssetAssistant 管理（这里不卸载资源）。
    /// </summary>
    public static class AudioManager
    {
        private static GameObject _audioManagerObject;
        private static GameObject _audioPool;
        private static FakeMono _runner;

        private static readonly Dictionary<string, AudioSource> MusicSources = new Dictionary<string, AudioSource>();
        private static readonly Queue<AudioSource> SoundSources = new Queue<AudioSource>();
        // 防止同一音效（同一挂点）在同一帧 / 延迟期间被重复触发
        private static readonly HashSet<string> SoundCheck = new HashSet<string>();

        private static AudioMixer _mixer;
        private static AudioMixerGroup _musicGroup;
        private static AudioMixerGroup _soundGroup;

        // AudioMixer 音量下限（-80dB，近似静音），避免 Log10(0) 得到负无穷
        private const float MinLinearVolume = 0.0001f;

        private static bool EnsureInit()
        {
            if (_audioManagerObject != null) return true;

            _audioManagerObject = new GameObject("AudioManager");
            _audioPool = new GameObject("AudioPool");
            Object.DontDestroyOnLoad(_audioManagerObject);
            Object.DontDestroyOnLoad(_audioPool);

            _runner = _audioManagerObject.AddComponent<FakeMono>();
            AssetAssistant.TryAddressableGet("AudioMixer", out _mixer);
            if (_mixer != null)
            {
                _musicGroup = _mixer.FindMatchingGroups("Music").FirstOrDefault();
                _soundGroup = _mixer.FindMatchingGroups("Sound").FirstOrDefault();
            }
            else
            {
                Debug.LogWarning("AudioManager: 没有找到 AudioMixer（Instance 标签），音量控制不可用");
            }
            return true;
        }

        #region Music

        public static void AddMusic(string fileName, float delay = 0, float volume = 1,
            GameObject father = null, float minDis = 1, float maxDis = 10)
        {
            try
            {
                EnsureInit();
                if (IsMusicPlaying(fileName)) return;

                AudioClip clip = SafeLoad<AudioClip>(fileName);
                if (!clip) return;

                AudioSource source = GetSoundSource();
                SetupAudioSource(source, clip, father, volume, minDis, maxDis, true, true);
                MusicSources[fileName] = source;
                _runner.StartCoroutine(PlayMusicCoroutine(delay, source));
            }
            catch (Exception ex)
            {
                Debug.LogError($"AddMusic {fileName} failed: {ex}");
            }
        }

        /// <summary>播放已经加载好的音乐（资源缓存按资源名存，按地址单独加载的音乐走这里）</summary>
        public static void AddMusic(string key, AudioClip clip, float volume)
        {
            if (!clip) return;
            EnsureInit();
            if (IsMusicPlaying(key)) return;

            AudioSource source = GetSoundSource();
            SetupAudioSource(source, clip, null, volume, 1, 10, true, true);
            MusicSources[key] = source;
            source.Play();
        }

        /// <summary>淡出并停止音乐</summary>
        public static void EndMusic(string fileName, float fadeTime = 2f)
        {
            if (!MusicSources.TryGetValue(fileName, out var source)) return;
            if (source == null)
            {
                MusicSources.Remove(fileName); // 挂点物体已销毁，AudioSource 跟着没了
                return;
            }
            EnsureInit();
            _runner.StartCoroutine(EndMusicCoroutine(fileName, source, fadeTime));
        }

        public static bool IsMusicPlaying(string key)
        {
            if (!MusicSources.TryGetValue(key, out var source)) return false;
            if (source != null) return true;
            MusicSources.Remove(key);
            return false;
        }

        /// <summary>音乐总音量（0~1，经 AudioMixer 的 MusicVol 参数）</summary>
        public static void ChangeMusicVolume(float vol)
        {
            if (_mixer == null) return;
            _mixer.SetFloat("MusicVol", LinearToDecibel(vol));
        }

        /// <summary>音效总音量（0~1，经 AudioMixer 的 SoundVol 参数）</summary>
        public static void ChangeSoundVolume(float vol)
        {
            if (_mixer == null) return;
            _mixer.SetFloat("SoundVol", LinearToDecibel(vol));
        }

        private static float LinearToDecibel(float vol)
        {
            return Mathf.Log10(Mathf.Clamp(vol, MinLinearVolume, 1f)) * 20f;
        }

        private static IEnumerator PlayMusicCoroutine(float delay, AudioSource source)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (source != null) source.Play();
        }

        private static IEnumerator EndMusicCoroutine(string fileName, AudioSource source, float fadeTime)
        {
            float startVol = source.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                if (source == null) break;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeTime);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 淡出期间同名音乐可能已被重新播放，只移除仍指向这个 AudioSource 的记录
            if (MusicSources.TryGetValue(fileName, out var current) && current == source)
                MusicSources.Remove(fileName);
            if (source != null)
            {
                source.Stop();
                ReturnSourceToPool(source);
            }
        }

        #endregion

        #region Sound

        /// <param name="types">变体数量：大于 1 时随机播放 fileName1 ~ fileNameN</param>
        public static void AddSound(string fileName, float delayTime = 0, float volume = 1, int types = 1,
            GameObject father = null, float minDis = 1, float maxDis = 10)
        {
            try
            {
                EnsureInit();
                string tag = fileName + (father ? father.GetInstanceID().ToString() : "null");
                if (SoundCheck.Contains(tag)) return;

                string soundName = types > 1 ? fileName + Random.Range(1, types + 1) : fileName;
                AudioClip clip = SafeLoad<AudioClip>(soundName);
                if (!clip) return; // 加载失败时不能留下去重标记，否则这个音效以后再也播不出来

                SoundCheck.Add(tag);
                AudioSource source = GetSoundSource();
                SetupAudioSource(source, clip, father, volume, minDis, maxDis, false, false);
                _runner.StartCoroutine(PlaySoundCoroutine(delayTime, source, tag));
            }
            catch (Exception ex)
            {
                Debug.LogError($"AddSound {fileName} failed: {ex}");
            }
        }

        private static IEnumerator PlaySoundCoroutine(float delayTime, AudioSource source, string tag)
        {
            yield return new WaitForSeconds(delayTime);
            SoundCheck.Remove(tag);

            if (source == null) yield break; // 挂点物体在延迟期间被销毁
            source.Play();

            // 按真实时间等待：暂停（timeScale = 0）时音效仍在播放，也要能按时回收
            float length = source.clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
            yield return new WaitForSecondsRealtime(length + 0.1f);

            if (source == null) yield break; // 播放期间挂点物体被销毁，AudioSource 跟着销毁
            source.Stop();
            ReturnSourceToPool(source);
        }

        #endregion

        #region Helpers

        private static void ReturnSourceToPool(AudioSource source)
        {
            source.clip = null;
            source.gameObject.SetActive(false);
            source.transform.SetParent(_audioPool.transform);
            SoundSources.Enqueue(source);
        }

        private static AudioSource GetSoundSource()
        {
            // 跳过已被销毁的（例如曾挂在被销毁物体下）
            while (SoundSources.Count > 0)
            {
                var pooled = SoundSources.Dequeue();
                if (pooled != null) return pooled;
            }

            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(_audioPool.transform);
            return go.AddComponent<AudioSource>();
        }

        private static void SetupAudioSource(AudioSource source, AudioClip clip, GameObject father,
            float volume, float minDis, float maxDis, bool loop, bool isMusic)
        {
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = 1f;
            source.outputAudioMixerGroup = isMusic ? _musicGroup : _soundGroup;

            source.gameObject.SetActive(true);
            source.transform.SetParent(father ? father.transform : _audioManagerObject.transform);
            source.transform.localPosition = Vector3.zero;

            source.rolloffMode = AudioRolloffMode.Logarithmic;
            if (father != null)
            {
                source.spatialBlend = 1f;
                source.minDistance = minDis;
                source.maxDistance = maxDis;
            }
            else
            {
                source.spatialBlend = 0f;
                source.minDistance = 1f;
                source.maxDistance = 500f;
            }
        }

        private static T SafeLoad<T>(string name) where T : Object
        {
            if (AssetAssistant.TryAddressableGet<T>(name, out var asset)) return asset;
            Debug.LogWarning($"AudioManager: 没有找到音频 {name}（需在 Addressables 中并已按 Label 加载）");
            return null;
        }

        private class FakeMono : MonoBehaviour { }

        /// <summary>
        /// 初始化 AudioManager（延迟初始化，在资源加载完成后调用）。未调用时首次播放也会自动初始化。
        /// </summary>
        public static void Wake()
        {
            EnsureInit();
        }
        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Audio;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace HowFrame
{
    public static class AudioManager
    {
        private static GameObject _audioManagerObject;
        private static GameObject _audioPool;

        private static readonly Dictionary<string, AudioSource> MusicSources = new Dictionary<string, AudioSource>();
        private static readonly Dictionary<string, AudioClip> MusicClips = new Dictionary<string, AudioClip>();

        private static readonly Dictionary<string, AudioClip> SoundClips = new Dictionary<string, AudioClip>();
        private static readonly Dictionary<string, int> SoundFreq = new Dictionary<string, int>();
        private static readonly List<string> LruList = new List<string>();
        private static readonly Queue<AudioSource> SoundSources = new Queue<AudioSource>();
        private static readonly HashSet<string> SoundCheck = new HashSet<string>();

        private static float _lastCleanupTime = 0;

        private static AudioMixer _mixer;
        private static AudioMixerGroup _musicGroup;
        private static AudioMixerGroup _soundGroup;

        private const int MaxSoundCache = 50; // 最大缓存音效数量

        private static void Init()
        {
            if (_audioManagerObject != null) return;

            _audioManagerObject = new GameObject("AudioManager");
            _audioPool = new GameObject("AudioPool");
            Object.DontDestroyOnLoad(_audioManagerObject);
            Object.DontDestroyOnLoad(_audioPool);

            _audioManagerObject.AddComponent<FakeMono>();
            _mixer = AssetAssistant.AddressableGet<AudioMixer>("AudioMixer");
            if (_mixer != null)
            {
                _musicGroup = _mixer.FindMatchingGroups("Music").FirstOrDefault();
                _soundGroup = _mixer.FindMatchingGroups("Sound").FirstOrDefault();
            }
        }

        #region Music

        public static void AddMusic(string fileName, float delay = 0, float volume = 1,
            GameObject father = null, float minDis = 1, float maxDis = 10)
        {
            try
            {
                if (MusicSources.ContainsKey(fileName)) return;

                AudioClip clip;
                if (!MusicClips.TryGetValue(fileName, out clip))
                {
                    clip = SafeLoad<AudioClip>(fileName);
                    if (!clip) return;
                    MusicClips[fileName] = clip;
                }

                AudioSource source = GetSoundSource();
                SetupAudioSource(source, clip, father, volume, minDis, maxDis, true, true);

                MusicSources[fileName] = source;

                _audioManagerObject.GetComponent<FakeMono>()
                    .StartCoroutine(PlayMusicCoroutine(delay, source));
            }
            catch (Exception ex)
            {
                Debug.LogError($"AddMusic {fileName} failed: {ex}");
            }
        }

        public static void EndMusic(string fileName)
        {
            if (!MusicSources.TryGetValue(fileName, out var source) || source == null) return;

            _audioManagerObject.GetComponent<FakeMono>()
                .StartCoroutine(EndMusicCoroutine(fileName, source));
        }

        public static void ChangeMusicVolume(float vol)
        {
            if (_mixer == null) return;
            _mixer.SetFloat("MusicVol", Mathf.Log10(Mathf.Clamp01(vol)) * 20);
        }

        public static void ChangeSoundVolume(float vol)
        {
            if (_mixer == null) return;
            _mixer.SetFloat("SoundVol", Mathf.Log10(Mathf.Clamp01(vol)) * 20);
        }

        private static IEnumerator PlayMusicCoroutine(float delay, AudioSource source)
        {
            yield return new WaitForSeconds(delay);
            source.Play();
        }

        private static IEnumerator EndMusicCoroutine(string fileName, AudioSource source, float fadeTime = 2f)
        {
            float startVol = source.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            source.Stop();
            ReturnSourceToPool(source);

            MusicSources.Remove(fileName);

            if (MusicClips.TryGetValue(fileName, out var clip))
            {
              
                MusicClips.Remove(fileName);
            }
        }

        #endregion

        #region Sound

        public static void AddSound(string fileName, float delayTime = 0, float volume = 1, int types = 1,
            GameObject father = null, float minDis = 1, float maxDis = 10)
        {
            try
            {
                string tag = fileName + (father ? father.name : "null");
                if (SoundCheck.Contains(tag)) return;
                SoundCheck.Add(tag);

                string soundName = types > 1 ? fileName + Random.Range(1, types + 1) : fileName;

                AudioClip clip = GetSoundClip(soundName);
                if (!clip) return;

                AudioSource source = GetSoundSource();
                SetupAudioSource(source, clip, father, volume, minDis, maxDis, false, false);

                _audioManagerObject.GetComponent<FakeMono>()
                    .StartCoroutine(PlaySoundCoroutine(delayTime, source, tag));
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
            source.Play();

            yield return new WaitForSeconds(source.clip.length + 0.1f);

            source.Stop();
            ReturnSourceToPool(source);
        }

        private static AudioClip GetSoundClip(string name)
        {
            if (Time.time - _lastCleanupTime > 30)
            {
                _lastCleanupTime = Time.time;
                CleanupSoundCache();
            }

            if (!SoundClips.TryGetValue(name, out var clip))
            {
                clip = SafeLoad<AudioClip>(name);
                if (!clip) return null;

                SoundClips[name] = clip;
                SoundFreq[name] = 1;
                LruList.Add(name);

                if (SoundClips.Count > MaxSoundCache)
                    CleanupSoundCache();
            }
            else
            {
                SoundFreq[name]++;
                LruList.Remove(name);
                LruList.Add(name);
            }

            return clip;
        }

        #endregion

        #region Helpers

        private static void CleanupSoundCache()
        {
            while (SoundClips.Count > MaxSoundCache)
            {
                string oldest = LruList[0];
                LruList.RemoveAt(0);
                SoundFreq.Remove(oldest);
                Resources.UnloadAsset(SoundClips[oldest]);
                SoundClips.Remove(oldest);
            }

            foreach (var key in SoundFreq.Keys.ToList())
            {
                SoundFreq[key] -= 2;
                if (SoundFreq[key] <= 0)
                {
                    LruList.Remove(key);
                    SoundFreq.Remove(key);
                    if (SoundClips.TryGetValue(key, out var clip))
                    {
                        Resources.UnloadAsset(clip);
                        SoundClips.Remove(key);
                    }
                }
            }
        }

        private static void ReturnSourceToPool(AudioSource source)
        {
            source.clip = null;
            source.gameObject.SetActive(false);
            source.transform.SetParent(_audioPool.transform);
            SoundSources.Enqueue(source);
        }

        private static AudioSource GetSoundSource()
        {
            if (SoundSources.Count > 0) return SoundSources.Dequeue();

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
            source.outputAudioMixerGroup = isMusic ? _musicGroup : _soundGroup;

            source.gameObject.SetActive(true);
            source.transform.SetParent(father ? father.transform : _audioManagerObject.transform);
            source.transform.localPosition = Vector3.zero;

            if (father != null)
            {
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = minDis;
                source.maxDistance = maxDis;
            }
            else
            {
                source.spatialBlend = 0f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 1f;
                source.maxDistance = 500f;
            }
        }

        private static T SafeLoad<T>(string path) where T : Object
        {
            try
            {
                return AssetAssistant.AddressableGet<T>(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Audio load failed: {path}, {ex}");
                return null;
            }
        }

        private class FakeMono : MonoBehaviour { }

        /// <summary>
        /// 初始化 AudioManager（延迟初始化，在资源加载完成后调用）
        /// </summary>
        public static void Wake()
        {
            Init();
        }
        #endregion
    }
}

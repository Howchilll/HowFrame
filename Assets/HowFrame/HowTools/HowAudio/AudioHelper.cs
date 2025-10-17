using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using static HowFrame.AssetAssistant;

namespace HowFrame
{
    public class AudioHelper
    {
        private MonoBehaviour _host;
        private AudioSource _audioSource;
        private readonly Dictionary<string, AudioClip> _clipDict = new Dictionary<string, AudioClip>();
        private Coroutine _playingCoroutine;

        public AudioHelper(MonoBehaviour host, params string[] audioNames)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));

            // 初始化 AudioSource
            _audioSource = _host.gameObject.AddComponent<AudioSource>();
            SetAudioSourceParameters();

            // 异步加载音效
            UniTask.Void(async () =>
            {
                foreach (var name in audioNames)
                {
                    var clip = await SafeLoadClip(name);
                    if (clip != null)
                        _clipDict[name] = clip;
                }
            });
        }

        /// <summary>
        /// 设置 AudioSource 参数
        /// </summary>
        public void SetAudioSourceParameters(
            float spatialBlend = 1f,
            float minDistance = 1f,
            float maxDistance = 15f,
            AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic,
            bool playOnAwake = false)
        {
            _audioSource.spatialBlend = spatialBlend;
            _audioSource.minDistance = minDistance;
            _audioSource.maxDistance = maxDistance;
            _audioSource.rolloffMode = rolloff;
            _audioSource.playOnAwake = playOnAwake;
        }

        /// <summary>
        /// 播放音效，name=null 时中断播放
        /// </summary>
        public void PlaySound(string name, float volume = 1f, float delay = 0f)
        {
            // 中断播放
            if (string.IsNullOrEmpty(name))
            {
                if (_playingCoroutine != null)
                {
                    _host.StopCoroutine(_playingCoroutine);
                    _playingCoroutine = null;
                }
                _audioSource.Stop();
                _audioSource.clip = null;
                return;
            }

            if (!_clipDict.TryGetValue(name, out var clip))
            {
                Debug.LogWarning($"AudioHelper: Sound {name} not loaded.");
                return;
            }

            _playingCoroutine = _host.StartCoroutine(PlaySoundCoroutine(clip, volume, delay));
        }

        private IEnumerator PlaySoundCoroutine(AudioClip clip, float volume, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);

            _audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        /// <summary>
        /// 获取所有加载的音效
        /// </summary>
        public List<AudioClip> GetAllClips() => new List<AudioClip>(_clipDict.Values);

        /// <summary>
        /// 安全加载音效
        /// </summary>
        private static async UniTask<AudioClip> SafeLoadClip(string name)
        {
            try
            {
                return await AddressAsset<AudioClip>(name);
            }
            catch (Exception e)
            {
                Debug.LogError($"AudioHelper: Failed to load {name}: {e}");
                return null;
            }
        }
    }
}

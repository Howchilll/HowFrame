using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static AssetAssistant;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;
using Random = System.Random;

public static class AudioManager
{
    private static GameObject _audioPool;
    private static GameObject _audioManagerObject;
    private static readonly Dictionary<string, AudioSource> MusicSources = new Dictionary<string, AudioSource>();
    private static readonly Dictionary<string, AudioClip> MusicClips = new Dictionary<string, AudioClip>();
    private static readonly List<string> SoundCheck = new List<string>();
    private static readonly Dictionary<string, AudioClip> SoundClips = new Dictionary<string, AudioClip>();
    private static readonly Dictionary<string, int> SoundFreq = new Dictionary<string, int>();
    private static readonly List<string> CgNames = new List<string>();
    private static readonly Queue<AudioSource> SoundSources = new Queue<AudioSource>();
    private static float _lastCleanupTime = 0;

    static AudioManager()
    {
        Init();
    }


    private static void Init()
    {
        if (_audioManagerObject == null)
        {
            _audioManagerObject = new GameObject("AudioManager");
            _audioPool = new GameObject("AudioPool");
            _audioManagerObject.AddComponent<FakeMono>();
            Object.DontDestroyOnLoad(_audioManagerObject);
            Object.DontDestroyOnLoad(_audioPool);
        }
    }

    #region Music

    public static async void AddMusic(string fileName, float delay=0, float volume=1,
        GameObject father = null, float minDis = 1, float maxDis = 10)
    {
        if (!MusicSources.ContainsKey(fileName))
        {
            AudioClip clip = null;
            AudioSource audioSource = null;
            if (MusicClips.ContainsKey(fileName))
            {
                clip = MusicClips[fileName];
            }
            else
            {
                clip = await GetMusicClip(fileName);
                MusicClips.Add(fileName, clip);
            }

            audioSource = GetSoundSource();
            audioSource.gameObject.SetActive(true);
            if (father == null) audioSource.transform.SetParent(_audioManagerObject.transform);
            else audioSource.transform.SetParent(father.transform);
            audioSource.transform.localPosition = Vector3.zero;

            MusicSources.Add(fileName, audioSource);
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = Mathf.Clamp01(volume) * GlobalData.MusicVol;

            if (father != null)
            {
                audioSource.spatialBlend = 1.0f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = minDis;
                audioSource.maxDistance = maxDis;
            }

            _audioManagerObject.GetComponent<FakeMono>()
                .StartCoroutine(PlayMusicCoroutine(delay, audioSource));
        }
    }

    public static void EndMusic(string fileName)
    {
        if (MusicSources.ContainsKey(fileName) && (MusicSources[fileName] != null))
            _audioManagerObject.GetComponent<FakeMono>()
                .StartCoroutine(EndMusicCoroutine(fileName, MusicSources[fileName]));
    }

    public static void ChangeVolume(string name,float vol = 1)
    {
        if (MusicSources.ContainsKey(name) && (MusicSources[name] != null))
         MusicSources[name].volume = Mathf.Clamp01(vol * GlobalData.MusicVol);
    }
    

    private static IEnumerator PlayMusicCoroutine(float delayTime, AudioSource audioSource)
    {
        yield return new WaitForSeconds(delayTime);
        audioSource.Play();
    }

    private static IEnumerator EndMusicCoroutine(string fileName, AudioSource audioSource, float time = 2)
    {
        if (MusicSources.ContainsKey(fileName))
        {
            float startVolume = audioSource.volume; // 
            float timeElapsed = 0f;
            while (timeElapsed < time)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0f, timeElapsed / time); // 
                timeElapsed += Time.deltaTime;
                yield return null; // 
            }

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.gameObject.SetActive(false);
            audioSource.transform.SetParent(_audioPool.transform);
            SoundSources.Enqueue(audioSource);
            MusicSources.Remove(fileName);
            
            Object.Destroy(MusicClips[fileName]);
            MusicClips.Remove(fileName);
        }
    }

    public static async void PreAddAudio(string fileName)
    {
        if (!MusicSources.ContainsKey(fileName))
        {
            AudioClip clip = await GetMusicClip(fileName);
            MusicClips.Add(fileName, clip);
        }

        Debug.Log(MusicClips.Count);
    }
    
    static async UniTask<AudioClip> GetMusicClip(string fileName)
    {
        AudioClip result = await ImportAsset<AudioClip>(fileName);
        return result;
    }

    #endregion

    #region Sound

    public static void AddSound(string fileName, float delayTime=0, float volume=1, int types = 1,
        GameObject father = null, float minDis = 1, float maxDis = 10)
    {
        StringBuilder tag = new StringBuilder(fileName);
        tag.Append(father ? father.name : "null");
        if (!SoundCheck.Contains(tag.ToString()))
        {
            SoundCheck.Add(tag.ToString());
            string soundName = types > 1 ? fileName + UnityEngine.Random.Range(1, types) : fileName;

            AudioClip clip = GetSoundClip(soundName);

            AudioSource audioSource = GetSoundSource();
            audioSource.gameObject.SetActive(true);
            if (!father) audioSource.transform.SetParent(_audioManagerObject.transform);
            else audioSource.transform.SetParent(father.transform);
            audioSource.transform.localPosition = Vector3.zero;

            audioSource.clip = clip;
            audioSource.playOnAwake = false;
            audioSource.volume = Mathf.Clamp01(volume) * GlobalData.SoundVol;
            audioSource.loop = false;

            if (father)
            {
                audioSource.spatialBlend = 1.0f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = minDis;
                audioSource.maxDistance = maxDis;
            }

            _audioManagerObject.GetComponent<FakeMono>()
                .StartCoroutine(PlaySoundCoroutine(delayTime, audioSource, tag.ToString()));
        }
    }

    private static IEnumerator PlaySoundCoroutine(float delayTime, AudioSource audioSource, string soundName)
    {
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        SoundCheck.Remove(soundName);
        yield return new WaitForSeconds(delayTime);
        audioSource.Play();
        yield return new WaitForSeconds(audioSource.clip.length + 0.2f);
        audioSource.Stop();
        audioSource.clip = null;
        audioSource.gameObject.SetActive(false);
        audioSource.transform.SetParent(_audioPool.transform);
        SoundSources.Enqueue(audioSource);
    }


    private static AudioClip GetSoundClip(string soundName)
    {
        AudioClip result;
        if (Time.time - _lastCleanupTime > 30)
        {
            _lastCleanupTime = Time.time;
            CgNames.Clear();
            foreach (var key in SoundFreq.Keys.ToList())
            {
                SoundFreq[key] -= 2;
                if (SoundFreq[key] <= 0) CgNames.Add(key);
            }

            foreach (var name in CgNames)
            {
                SoundFreq.Remove(name);
                Resources.UnloadAsset(SoundClips[name]);
                SoundClips.Remove(name);
            }
        }

        if (!SoundClips.ContainsKey(soundName))
        {
            result = LoadAsset<AudioClip>(soundName,E_AssetType.Audio);
            SoundClips.Add(soundName, result);
            SoundFreq.Add(soundName, 1);
        }
        else
        {
            result = SoundClips[soundName];
            SoundFreq[soundName]++;
        }

        return result;
    }


    private static AudioSource GetSoundSource()
    {
        if (SoundSources.Count > 0)
        {
            return SoundSources.Dequeue();
        }
        else
        {
            GameObject newAudioObj = new GameObject("PooledAudioSource");
            newAudioObj.transform.SetParent(_audioPool.transform);
            return newAudioObj.AddComponent<AudioSource>();
        }
    }

    #endregion


    private class FakeMono : MonoBehaviour
    {
    }

    public static void wake()
    {
    }
}
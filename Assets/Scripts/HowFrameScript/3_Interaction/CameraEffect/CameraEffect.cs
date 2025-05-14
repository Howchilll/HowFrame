using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Reflection;
using DG.Tweening;
using UnityEngine.Serialization;


public static class CameraEffect
{
    private static GameObject VolumeObj;
    private static Camera _camera;
    private static Volume _volume;
    private static Transform _cameraTransform;
    private static Vector3 _originalPosition;
    private static Coroutine _shakeCoroutine;
    private static MonoBehaviour _runner;

    // 后处理效果组件
    private static Bloom _bloom; // 光晕效果
    private static Vignette _vignette; // 暗角效果
    private static ColorAdjustments _color; // 色彩调整（曝光、对比度等）
    private static ChromaticAberration _chromatic; // 色差效果
    private static DepthOfField _depthOfField; // 景深效果（模糊）
    private static MotionBlur _motionBlur; // 运动模糊
    private static FilmGrain _filmGrain; // 颗粒感（电影感）
    private static LensDistortion _lensDistortion; // 镜头畸变效果

     static CameraEffect()
    {
        VolumeObj = AssetAssistant.LoadAsset<GameObject>("Volume", E_AssetType.Instance);
        VolumeObj=Object.Instantiate(VolumeObj);
        Object.DontDestroyOnLoad(VolumeObj);
        _volume = VolumeObj.GetComponent<Volume>();
        SetCamera(Camera.main);
        SetupVolume();
    }
    
    public static void Wake()
    {   
    }

    public static void SetCamera(Camera camera)
    {
        _camera = camera;
        var cameraData = _camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true; 
        _cameraTransform = _camera.transform;
        _runner = _camera.GetComponent<MonoBehaviour>();
        _originalPosition = _cameraTransform.localPosition;
    }
    
    
    private static void SetupVolume()
    {
        // 尝试添加后处理模块
        TryAddEffect(out _bloom);
        TryAddEffect(out _vignette);
        TryAddEffect(out _color);
        TryAddEffect(out _chromatic);
        TryAddEffect(out _depthOfField);
        TryAddEffect(out _motionBlur);
        TryAddEffect(out _filmGrain);
        TryAddEffect(out _lensDistortion);
        
        
        // 默认启用一些效果
        _bloom.intensity.value = 0; // 默认不显示光晕
        _vignette.intensity.value = 0f; // 默认不显示暗角
        _color.postExposure.value = 0f; // 默认曝光不变
        _chromatic.intensity.value = 0f; // 默认色差关闭
        _depthOfField.active = false; // 默认关闭景深
        _motionBlur.active = false; // 默认关闭运动模糊
        _filmGrain.intensity.value = 0f; // 默认关闭颗粒感
        _lensDistortion.intensity.value = 0f; // 默认不变形
    }
    
    
    
    private static void TryAddEffect<T>(out T effect) where T : VolumeComponent, new()
    {
        if (!_volume.profile.TryGet(out effect))
        {
            effect = _volume.profile.Add<T>();
            effect.active = true;
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                {
                    var param = field.GetValue(effect) as VolumeParameter;
                    if (param != null)
                        param.overrideState = true;
                }
            }
        }
    }
    
    public static void Shake(float duration = 0.2f, float intensity = 0.1f, float frequency = 25f)
    {
        if (_shakeCoroutine != null) _runner.StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = _runner.StartCoroutine(ShakeRoutine(duration, intensity, frequency));
    }

    private static IEnumerator ShakeRoutine(float duration, float intensity, float frequency)
    {
        float elapsed = 0f;
        float interval = 1f / frequency;

        while (elapsed < duration)
        {
            Vector3 offset = new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity),
                0);

            _cameraTransform.localPosition = _originalPosition + offset;

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        _cameraTransform.localPosition = _originalPosition;
        _shakeCoroutine = null;
    }

    // 设置光晕强度
    public static void SetBloom(float intensity, bool enable = true, bool flash = false, float flashDuration = 0.1f)
    {
        _bloom.active = enable;

        if (!enable)
        {
            _bloom.intensity.value = 0f;
            return;
        }

        if (flash)
        {
            float original = _bloom.intensity.value;

            DOTween.Sequence()
                .Append(DOTween.To(() => _bloom.intensity.value, x => _bloom.intensity.value = x, intensity, flashDuration / 2f))
                .Append(DOTween.To(() => _bloom.intensity.value, x => _bloom.intensity.value = x, original, flashDuration / 2f));
        }
        else
        {
            _bloom.intensity.value = intensity;
        }
    }


// 设置暗角（Vignette）强度
    public static void SetVignette(float intensity, bool enable = true, bool flash = false, float flashDuration = 0.1f)
    {
        _vignette.active = enable;

        if (!enable)
        {
            _vignette.intensity.value = 0f;
            return;
        }

        if (flash)
        {
            float original = _vignette.intensity.value;

            DOTween.Sequence()
                .Append(DOTween.To(() => _vignette.intensity.value, x => _vignette.intensity.value = x, intensity, flashDuration / 2f))
                .Append(DOTween.To(() => _vignette.intensity.value, x => _vignette.intensity.value = x, original, flashDuration / 2f));
        }
        else
        {
            _vignette.intensity.value = intensity;
        }
    }

    
// 设置曝光和对比度（色彩调整）
    public static void SetExposureAndContrast(float exposure, float contrast, float duration = 0.5f)
    {
        if (_color == null) return;

        DOTween.To(() => _color.postExposure.value, x => _color.postExposure.value = x, exposure, duration);
        DOTween.To(() => _color.contrast.value, x => _color.contrast.value = x, contrast, duration);
    }

// 设置色差强度
    public static void SetChromaticAberration(float intensity, float duration = 0.5f, bool resetAfter = false, float resetDelay = 0.1f)
    {
        _chromatic.active = true;

        DOTween.To(
            () => _chromatic.intensity.value,
            x => _chromatic.intensity.value = x,
            intensity,
            duration
        );

        if (resetAfter)
        {
            DOVirtual.DelayedCall(duration + resetDelay, () =>
            {
                DOTween.To(
                    () => _chromatic.intensity.value,
                    x => _chromatic.intensity.value = x,
                    0f,
                    duration * 0.5f
                ).OnComplete(() => {
                    _chromatic.active = false; // 彻底关闭
                });
            });
        }
    }


// 开关景深（模糊背景）
    public static void EnableDepthOfField(bool enable, float focusDistance = 10f, float aperture = 5.6f)
    {
        if (_depthOfField != null)
        {
            _depthOfField.active = enable;
            _depthOfField.focusDistance.value = focusDistance;
            _depthOfField.aperture.value = aperture;
        }
    }

// 开关运动模糊
    public static void EnableMotionBlur(bool enable, float intensity = 0.5f)
    {
        if (_motionBlur != null)
        {
            _motionBlur.active = enable;
            _motionBlur.intensity.value = intensity;
        }
    }

// 快速设置电影感（颗粒感 + 色差）
    public static void EnableCinematicEffect(bool enable, float filmGrainIntensity = 0.5f, float chromaticIntensity = 0.3f, float duration = 0.3f)
    {
        float targetGrain = enable ? filmGrainIntensity : 0f;
        float targetChromatic = enable ? chromaticIntensity : 0f;

        _filmGrain.active = true;  // 必须先激活才能调值
        _chromatic.active = true;

        // 渐变到目标值
        DOTween.To(() => _filmGrain.intensity.value, x => _filmGrain.intensity.value = x, targetGrain, duration)
            .OnComplete(() => { if (!enable) _filmGrain.active = false; });

        DOTween.To(() => _chromatic.intensity.value, x => _chromatic.intensity.value = x, targetChromatic, duration)
            .OnComplete(() => { if (!enable) _chromatic.active = false; });
    }

// 镜头畸变（负值凹陷，正值凸出）
    public static void SetLensDistortion(float intensity)
    {
        _lensDistortion.intensity.value = intensity;
    }
    
    
}


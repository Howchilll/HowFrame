using System.Threading.Tasks;
using UnityEngine;
using HowFrame;

namespace HowFrameExample
{
    public class AudioHelperExample : MonoBehaviour
    {
        private AudioHelper _audioHelper;

        private async void Start()
        {
            await AssetAssistant.LoadLabelsAsync("Audio");
            
            _audioHelper = new AudioHelper(this, "Click", "Jump", "Hit");
            ExamplePlaySound();
        }

        private void ExamplePlaySound()
        {
            _audioHelper.PlaySound("Click");
            _audioHelper.PlaySound("Jump", volume: 0.8f, delay: 0.5f);
            _audioHelper.SetAudioSourceParameters(spatialBlend: 1f, minDistance: 1f, maxDistance: 15f);
            _audioHelper.PlaySound(null);
        }
    }
}


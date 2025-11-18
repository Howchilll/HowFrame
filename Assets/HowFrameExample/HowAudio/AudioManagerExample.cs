using UnityEngine;
using HowFrame;

namespace HowFrameExample
{
    public class AudioManagerExample : MonoBehaviour
    {
        private async void Start()
        {
            await AssetAssistant.LoadLabelsAsync("Audio");
            AudioManager.Wake();
            
            ExampleMusic();
            ExampleSound();
        }

        private void ExampleMusic()
        {
            AudioManager.AddMusic("BackgroundMusic");
            AudioManager.AddMusic("BossMusic", delay: 2f, volume: 0.8f);
            AudioManager.ChangeMusicVolume(0.5f);
            AudioManager.EndMusic("BackgroundMusic");
        }

        private void ExampleSound()
        {
            AudioManager.AddSound("Click");
            AudioManager.AddSound("Jump", delayTime: 0.5f, volume: 0.8f);
            AudioManager.AddSound("Hit", types: 3);
        }
    }
}


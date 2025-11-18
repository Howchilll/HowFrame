using System.Threading.Tasks;
using UnityEngine;
using HowFrame;

namespace HowFrameExample
{
    public class AssetAssistantExample : MonoBehaviour
    {
        private async void Start()
        {
            await ExampleStreamingAssets();
            ExampleResources();
            await ExampleAddressableSingle();
            await ExampleAddressableLabels();
        }

        private async Task ExampleStreamingAssets()
        {
            AudioClip audio = await AssetAssistant.ImportAsset<AudioClip>("Audio/background.mp3");
        }

        private void ExampleResources()
        {
            GameObject prefab = AssetAssistant.LoadAsset<GameObject>("MyPrefab");
            Instantiate(prefab);
            AudioClip clip = AssetAssistant.LoadAsset<AudioClip>("Sounds/click");
        }

        private async Task ExampleAddressableSingle()
        {
            GameObject obj = await AssetAssistant.AddressAsset<GameObject>("MyPrefab");
            Instantiate(obj);
            AudioClip audio = await AssetAssistant.AddressAsset<AudioClip>("MyAudio", 5f);
        }

        private async Task ExampleAddressableLabels()
        {
            await AssetAssistant.LoadLabelsAsync("UI");
            await AssetAssistant.LoadLabelsAsync("Audio", "Textures");
            
            GameObject canvas = AssetAssistant.AddressableGet<GameObject>("Canvas");
            Instantiate(canvas);
            AudioClip bgm = AssetAssistant.AddressableGet<AudioClip>("BackgroundMusic");
            
            AssetAssistant.ReleaseLabels("UI");
            AssetAssistant.ReleaseLabels("Audio", "Textures");
        }
    }
}


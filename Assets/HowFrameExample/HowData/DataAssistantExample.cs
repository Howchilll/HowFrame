using UnityEngine;
using static HowFrame.DataAssistant;

namespace HowFrameExample
{
    public class DataAssistantExample : MonoBehaviour
    {
        private void Start()
        {
            ExampleWriteRead();
            ExampleLoadConfig();
        }

        private void ExampleWriteRead()
        {
            var data = new MyData { Value = 100, Name = "Test" };
            WriteData(data, "MyData");
            WriteData(data, "MyDataJson", isJson: true);
            
            var loaded = ReadData<MyData>("MyData");
            var loadedJson = ReadData<MyData>("MyDataJson", isJson: true);
        }

        private void ExampleLoadConfig()
        {
            var config = LoadConfig<MyConfig>("Configs/MyConfig");
        }
    }

    public class MyData
    {
        public int Value;
        public string Name;
    }

    public class MyConfig
    {
        public int Setting;
    }
}


using UnityEngine;
using HowFrame;

namespace HowFrameExample
{
    public class CoroutineAssistantExample : MonoBehaviour
    {
        private void Start()
        {
            CoroutineAssistant.Wake();
            
            ExampleLoop();
            ExampleDelayInvoke();
        }

        private void ExampleLoop()
        {
            CoroutineAssistant.StartLoop("Timer", 1f, () => Debug.Log("Tick"));
            CoroutineAssistant.StartLoop("Countdown", 5, 0.5f, 
                () => Debug.Log("Loop"), 
                () => Debug.Log("Start"), 
                () => Debug.Log("Complete"));
            CoroutineAssistant.Stop("Timer");
        }

        private void ExampleDelayInvoke()
        {
            CoroutineAssistant.DelayInvoke("Delay1", 2f, () => Debug.Log("Delayed"));
            CoroutineAssistant.DelayInvoke(1f, () => Debug.Log("Anonymous"));
        }
    }
}


using UnityEngine;
using HowFrame;

namespace HowFrameExample
{
    public class CoroutineHelperExample : MonoBehaviour
    {
        private CoroutineHelper _coroutineHelper;

        private void Start()
        {
            _coroutineHelper = new CoroutineHelper(this);
            ExampleLoop();
            ExampleDelayInvoke();
        }

        private void ExampleLoop()
        {
            _coroutineHelper.StartLoop(1f, () => Debug.Log("Tick"));
            _coroutineHelper.StartLoop(5, 0.5f, 
                () => Debug.Log("Loop"), 
                () => Debug.Log("Start"), 
                () => Debug.Log("Complete"));
            _coroutineHelper.Stop();
        }

        private void ExampleDelayInvoke()
        {
            _coroutineHelper.DelayInvoke(2f, () => Debug.Log("Delayed"));
        }
    }
}


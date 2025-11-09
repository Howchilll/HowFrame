using UnityEngine;

public class MCS : MonoBehaviour
{
    private void Start()
    {
        // 注册自己到管理器
        MCSManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        // 注销自己
        MCSManager.Instance.Unregister(this);
    }
}
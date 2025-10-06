
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class AddTest : MonoBehaviour
{ 
    

    void Start()
    {
        //命名空间：
        //UnityEngine.AddressableAssets 和 UnityEngine.ResourceManagement.AsyncOperations
        AsyncOperationHandle<GameObject>  handle = Addressables.LoadAssetAsync<GameObject>("Cube");
        handle.Completed += (Handle) =>
        {
            //判断加载成功
            if (Handle.Status == AsyncOperationStatus.Succeeded)
                Instantiate(Handle.Result);
            Addressables.Release(Handle);
        };
    }

}

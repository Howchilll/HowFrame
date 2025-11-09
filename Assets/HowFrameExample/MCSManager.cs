using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using HowFrame;

// --- 数据和 Job 参数 ---


public class MCSManager : MonoBehaviour
{
    private static MCSManager _instance;
    public static MCSManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MCSManager");
                _instance = go.AddComponent<MCSManager>();
            }
            return _instance;
        }
    }

    private JobHelper<Pos, GameObject, MoveJobData> _helper;

    private void Awake()
    {
        if (_helper != null) return;

        _helper = new JobHelper<Pos, GameObject, MoveJobData>(
            computeCallback: (ref MoveJobData data, int index, ref Pos pos) =>
            {
                Vector3 p = new Vector3(pos.x, pos.y, pos.z);
                Vector3 dir = (data.target - p).normalized;
                p += dir * data.moveSpeed * data.dt;
                pos.x = p.x;
                pos.y = p.y;
                pos.z = p.z;
            },
            applyCallback: (NativeArray<Pos> results, List<GameObject> objs, List<Pos> list) =>
            {
                int count = Mathf.Min(results.Length, objs.Count);
                for (int i = 0; i < count; i++)
                {
                    var v = results[i];
                    list[i] = v;
                    if (objs[i] != null)
                        objs[i].transform.position = new Vector3(v.x, v.y, v.z);
                }
            },
            fps: 60
        );

        _helper.SetJobData(new MoveJobData
        {
            target = Vector3.zero,
            moveSpeed = 1.5f,
            dt = 1f / 60f
        });

        _helper.Enable = false; // 默认开启
    }

    private void Update()
    {
        // 用按键控制计算开关
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _helper.Enable = !_helper.Enable;
            Debug.Log("JobHelper enable: " + _helper.Enable);
        }
    }

    // 注册对象
    public void Register(MCS mcs)
    {
        _helper.AddData(mcs.gameObject, new Pos
        {
            x = mcs.transform.position.x,
            y = mcs.transform.position.y,
            z = mcs.transform.position.z
        });
    }

    // 注销对象
    public void Unregister(MCS mcs)
    {
        _helper.RemoveData(mcs.gameObject);
    }
    
    public struct MoveJobData
    {
        public Vector3 target;
        public float moveSpeed;
        public float dt;
    }

    public struct Pos
    {
        public float x, y, z;
    }
}
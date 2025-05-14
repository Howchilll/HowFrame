using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectPool<P, O> : MonoBehaviour where P: class where O : MonoBehaviour
{
    private static P _instance;
    public static P Instance => _instance;
    protected Queue<O> Pool = new Queue<O>();
    
    protected virtual void Awake() { _instance = this as P; }
    
    private void Start()
    {
        Init();
    }
    public abstract void Init();
    
    public O GetObj()
    {
        if (Pool.Count != 0)
            return IniObj(Pool.Dequeue());
        else
            return IniObj(CreateObj());
    }
    public void PutObj(O obj)
    {
        StartCoroutine(RecyleObj(obj));
    }
    
    //核心方法 GetObj()从池中取物  PutObj()还物体于池   
    protected abstract O IniObj(O obj);
    //取物时的初始化方法
    protected abstract O CreateObj();
    //新建对象的方法
    protected abstract IEnumerator RecyleObj(O obj);
    //归对象时的协程
    protected virtual void DestroyObj(O obj)
    {
        Destroy(obj.gameObject);
    }
    //销毁对象的方法
    protected virtual void IniPool(int size)
    {
        for (int i = 0; i < size; i++)
        {
            Pool.Enqueue(CreateObj());
        }
    }
    //对象池初始化
    public void SizeReset(int size)
    {
        while (Pool.Count > size)
        {
            O obj = Pool.Dequeue();
            DestroyObj(obj);
        }
    }
    //对象池重设大小（当生成了过多多余对象
}


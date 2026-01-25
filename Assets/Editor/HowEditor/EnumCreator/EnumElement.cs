#define EDITOR
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class EnumElement
{
    public bool isList;
    public string value;               
    public string groupName;           
    [UnityEngine.SerializeReference]
    public List<EnumElement> children = new List<EnumElement>();

    [System.NonSerialized] public bool foldout = true; // 是否展开
}
#endif
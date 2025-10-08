#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnumRoot 
{
    public string collectionName;
    public List<EnumElement> elements = new List<EnumElement>();
}
#endif
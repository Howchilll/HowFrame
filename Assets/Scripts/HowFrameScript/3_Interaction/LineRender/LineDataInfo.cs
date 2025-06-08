using UnityEngine;

[CreateAssetMenu(fileName = "LineData", menuName = "ScriptableObj/LineData")]
public class LineDataInfo :ScriptableObject
{
    
    
    
    
    
    
    
    
    public string lineName;
    public Color startColor;
    public Color endColor;
    public float startWidth;
    public float endWidth;
    public float time;
    
    public event System.Action<LineRenderer> OnInit;
    public event System.Action<LineRenderer> OnDraw;


    public void WhenInit(LineRenderer lr) => OnInit?.Invoke(lr);
    public void WhenDraw(LineRenderer lr) => OnDraw?.Invoke(lr);
 
}

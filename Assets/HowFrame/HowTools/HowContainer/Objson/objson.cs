using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using LitJson;
using UnityEngine;

public class OBJson : DynamicObject
{
    private JsonData data;

    // 从 JSON 文件加载
    public OBJson(string jsonPath)
    {
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            data = JsonMapper.ToObject(json);
        }
        else
        {
            Debug.LogWarning($"OBJson: file not found → {jsonPath}");
            data = new JsonData();
        }
    }

    // 空构造
    public OBJson()
    {
        data = new JsonData();
    }

    // 内部构造（用于嵌套）
    private OBJson(JsonData jd)
    {
        data = jd;
    }

    // 动态 getter
    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        string key = binder.Name;
        if (data.IsObject && data.ContainsKey(key))
        {
            JsonData v = data[key];
            result = WrapJsonData(v);
            return true;
        }

        result = null;
        return true;
    }

    // 动态 setter
    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        string key = binder.Name;

        if (!data.IsObject)
        {
            data = new JsonData();  // 转为 object
        }

        if (value is OBJson o)
        {
            data[key] = o.data;
        }
        else if (value is JsonData jd)
        {
            data[key] = jd;
        }
        else
        {
            // 将 object 转换为 JsonData
            // 对于复杂对象，使用 JsonMapper；对于基本类型，使用构造函数
            try
            {
                data[key] = new JsonData(value);
            }
            catch (ArgumentException)
            {
                // 如果是复杂对象，尝试序列化为 JSON 再解析
                string json = JsonMapper.ToJson(value);
                data[key] = JsonMapper.ToObject(json);
            }
        }

        return true;
    }

    // 获取原始 JsonData（如果你需要更复杂操作）
    public JsonData Raw => data;

    // 将 OBJson 保存为 JSON 到文件
    public void Save(string path, bool pretty = true)
    {
        string json;
        if (pretty)
        {
            StringWriter sw = new StringWriter();
            JsonWriter writer = new JsonWriter(sw);
            writer.PrettyPrint = true;
            data.ToJson(writer);
            json = sw.ToString();
        }
        else
        {
            json = data.ToJson();
        }
        File.WriteAllText(path, json);
    }

    // 如果 JsonData 是 Object 或 Array 就封装，否则返回原始值
    private object WrapJsonData(JsonData jd)
    {
        if (jd.IsObject || jd.IsArray)
        {
            return new OBJson(jd);
        }
        else
        {
            // 基本类型：bool / number / string
            if (jd.IsBoolean) return (bool)jd;
            if (jd.IsInt)     return (int)jd;
            if (jd.IsLong)    return (long)jd;
            if (jd.IsDouble)  return (double)jd;
            if (jd.IsString)  return (string)jd;
            // 其他情况就返回 jd 本身
            return jd;
        }
    }

    // 如果你需要通过索引访问 array
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
    {
        if (data.IsArray && indexes.Length == 1 && indexes[0] is int idx)
        {
            JsonData v = data[idx];
            result = WrapJsonData(v);
            return true;
        }

        result = null;
        return false;
    }

    public override string ToString()
    {
        return data.ToJson();
    }
}

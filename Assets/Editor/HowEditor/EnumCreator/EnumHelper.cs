#define EDITOR
#if UNITY_EDITOR
using System.Collections.Generic;
using LitJson;

public static class EnumHelper
{
    public static string Serialize(EnumRoot root)
    {
        var dict = new Dictionary<string, object>();
        dict["collectionName"] = root.collectionName;
        dict["elements"] = SerializeElements(root.elements);
        return JsonMapper.ToJson(dict);
    }

    public static EnumRoot Deserialize(string json)
    {
        var data = JsonMapper.ToObject<JsonData>(json);

        var root = new EnumRoot();
        root.collectionName = data["collectionName"].ToString();

        var elementsData = data["elements"];
        root.elements = DeserializeElements(elementsData);

        return root;
    }

    private static List<object> SerializeElements(List<EnumElement> elements)
    {
        var list = new List<object>();
        foreach (var e in elements)
        {
            if (e.isList)
            {
                var childDict = new Dictionary<string, object>();
                childDict["groupName"] = e.groupName;
                childDict["children"] = SerializeElements(e.children);
                list.Add(childDict);
            }
            else
            {
                list.Add(e.value);
            }
        }
        return list;
    }

    private static List<EnumElement> DeserializeElements(JsonData data)
    {
        var list = new List<EnumElement>();

        for (int i = 0; i < data.Count; i++)
        {
            var item = data[i];
            switch (item.GetJsonType())
            {
                case JsonType.String:
                    list.Add(new EnumElement { isList = false, value = item.ToString() });
                    break;
                case JsonType.Object:
                    var elem = new EnumElement { isList = true };
                    elem.groupName = item["groupName"].ToString();
                    elem.children = DeserializeElements(item["children"]);
                    list.Add(elem);
                    break;
                // 如果数组中出现数组直接当对象处理也行，或者加 JsonType.Array case
            }
        }

        return list;
    }
}
#endif
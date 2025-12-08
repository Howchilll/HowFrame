using UnityEngine;
using System.IO;
using System.Collections.Generic;
using NPOI.XSSF.UserModel;
using Unity.Plastic.Newtonsoft.Json.Linq;

public static class JsonToExcel
{
    public static void Convert(string jsonStr, string excelPath)
    {
        var jsonArray = JArray.Parse(jsonStr);

        // ====== 结构检查（如果不能转，抛异常） ======
        ValidateJsonArray(jsonArray);

        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("Sheet1");

        // 第一行 key
        var headerRow = sheet.CreateRow(0);
        // 第二行 type
        var typeRow = sheet.CreateRow(1);

        // 获取字段
        var firstObj = (JObject)jsonArray[0];
        var keys = new List<string>();
        foreach (var prop in firstObj.Properties())
            keys.Add(prop.Name);

        // 填写 key 和 type
        for (int c = 0; c < keys.Count; c++)
        {
            string key = keys[c];
            headerRow.CreateCell(c).SetCellValue(key);

            // 推断类型
            var val = firstObj[key];
            typeRow.CreateCell(c).SetCellValue(InferTypeFromJson(val));
        }

        // 数据行
        for (int r = 0; r < jsonArray.Count; r++)
        {
            var row = sheet.CreateRow(r + 2);
            var obj = (JObject)jsonArray[r];

            for (int c = 0; c < keys.Count; c++)
            {
                string key = keys[c];
                var val = obj[key];

                if (val == null)
                {
                    row.CreateCell(c).SetCellValue("");
                    continue;
                }

                if (val.Type == JTokenType.Array)
                {
                    var arr = (JArray)val;
                    row.CreateCell(c).SetCellValue(string.Join(",", arr));
                }
                else
                {
                    row.CreateCell(c).SetCellValue(val.ToString());
                }
            }
        }

        // 写入文件
        using var fs = new FileStream(excelPath, FileMode.Create, FileAccess.Write);
        workbook.Write(fs);

        Debug.Log("JSON → XLSX 完成: " + excelPath);
    }

    // ==========================================================
    //                   JSON 结构校验（不能转时抛错）
    // ==========================================================
    static void ValidateJsonArray(JArray arr)
    {
        if (arr == null || arr.Count == 0)
            throw new System.Exception("❌ JSON 为空，无法转换为 Excel。");

        // JSON 必须是数组对象
        if (arr[0].Type != JTokenType.Object)
            throw new System.Exception("❌ JSON 必须是对象数组，例如：[ { ... }, { ... } ]");

        // 拿第一条数据的字段作为标准
        var firstObj = (JObject)arr[0];
        var standardKeys = new HashSet<string>();
        foreach (var p in firstObj.Properties())
            standardKeys.Add(p.Name);

        // 循环检查每条数据
        for (int i = 0; i < arr.Count; i++)
        {
            if (arr[i].Type != JTokenType.Object)
                throw new System.Exception($"❌ 第 {i + 1} 条记录不是对象类型。");

            var obj = (JObject)arr[i];

            // 字段一致性检查
            foreach (var p in obj.Properties())
            {
                if (!standardKeys.Contains(p.Name))
                    throw new System.Exception(
                        $"❌ 第 {i + 1} 行字段不一致：发现额外字段 \"{p.Name}\"。\n" +
                        $"所有记录必须使用相同字段集合。"
                    );
            }

            // 不允许缺字段
            foreach (var key in standardKeys)
            {
                if (obj[key] == null)
                    throw new System.Exception(
                        $"❌ 第 {i + 1} 行缺少字段 \"{key}\"。\n" +
                        $"所有记录必须使用相同字段集合。"
                    );
            }

            // 值类型检查
            foreach (var p in obj.Properties())
            {
                var val = p.Value;

                if (val.Type == JTokenType.Object)
                    throw new System.Exception(
                        $"❌ 字段 \"{p.Name}\" 的值是嵌套对象，Excel 无法表达嵌套结构。\n" +
                        $"请改为基础类型或一维数组。"
                    );

                if (val.Type == JTokenType.Array)
                {
                    foreach (var inner in (JArray)val)
                    {
                        if (inner.Type == JTokenType.Object || inner.Type == JTokenType.Array)
                        {
                            throw new System.Exception(
                                $"❌ 字段 \"{p.Name}\" 的数组中包含对象或子数组。\n" +
                                $"Excel 只支持一维基础类型数组。"
                            );
                        }
                    }
                }
            }
        }
    }

    static string InferTypeFromJson(JToken val)
    {
        if (val == null) return "string";

        switch (val.Type)
        {
            case JTokenType.Integer: return "int";
            case JTokenType.Float: return "float";
            case JTokenType.Boolean: return "bool";
            case JTokenType.Array:
                var arr = (JArray)val;
                if (arr.Count == 0) return "string[]";

                var t = arr[0].Type;
                if (t == JTokenType.Integer) return "int[]";
                if (t == JTokenType.Float) return "float[]";
                return "string[]";
        }
        return "string";
    }
}

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

/// <summary>
/// 配置文件批量转换工具
/// Excel ↔ JSON 双向转换，支持递归处理子文件夹
/// </summary>
public class ConfigConverter : EditorWindow
{
    private string excelFolder = "EditorPath.ConfigExcelPath";
    private string jsonFolder = "EditorPath.ConfigJsonPath";

    [MenuItem("Tools/Config Converter")]
    public static void ShowWindow()
    {
        GetWindow<ConfigConverter>("Config Converter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("配置文件批量转换工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "功能说明：\n" +
            "1. Excel → JSON：将 GameTable/Config 中的 xlsx 文件转换为 StreamingAssets/Config 中的 json 文件\n" +
            "2. JSON → Excel：将 StreamingAssets/Config 中的 json 文件转换为 GameTable/Config 中的 xlsx 文件\n" +
            "注意：会自动保持子文件夹结构",
            MessageType.Info
        );
        EditorGUILayout.Space(10);

        excelFolder = EditorGUILayout.TextField("Excel 文件夹", excelFolder);
        jsonFolder = EditorGUILayout.TextField("JSON 文件夹", jsonFolder);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Excel → JSON（批量转换）", GUILayout.Height(40)))
        {
            ConvertAllExcelToJson();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("JSON → Excel（批量转换）", GUILayout.Height(40)))
        {
            ConvertAllJsonToExcel();
        }
    }

    /// <summary>
    /// 批量将 Excel 文件转换为 JSON
    /// </summary>
    private void ConvertAllExcelToJson()
    {
        string excelPath = ResolvePath(excelFolder);
        string jsonPath = ResolvePath(jsonFolder);

        if (!Directory.Exists(excelPath))
        {
            Debug.LogError($"Excel 文件夹不存在: {excelPath}");
            return;
        }

        int totalConverted = 0;
        int totalFailed = 0;

        // 递归查找所有 xlsx 文件
        string[] excelFiles = Directory.GetFiles(excelPath, "*.xlsx", SearchOption.AllDirectories);

        foreach (string excelFile in excelFiles)
        {
            try
            {
                // 计算相对路径（相对于 excelPath）
                string relativePath = GetRelativePath(excelPath, excelFile);
                string relativeDir = Path.GetDirectoryName(relativePath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(excelFile);

                // 构建 JSON 文件路径
                string jsonDir = string.IsNullOrEmpty(relativeDir) ? jsonPath : Path.Combine(jsonPath, relativeDir);
                string jsonFile = Path.Combine(jsonDir, fileNameWithoutExt + ".json");

                // 确保目录存在
                if (!Directory.Exists(jsonDir))
                {
                    Directory.CreateDirectory(jsonDir);
                }

                // 转换 Excel 到 JSON
                string jsonContent = ExcelToJsonArray(excelFile);
                File.WriteAllText(jsonFile, jsonContent);

                totalConverted++;
                string jsonRelativePath = GetRelativePath(jsonPath, jsonFile);
                Debug.Log($"✅ 转换完成: {relativePath} → {jsonRelativePath}");
            }
            catch (System.Exception ex)
            {
                totalFailed++;
                Debug.LogError($"❌ 转换失败: {excelFile}\n{ex.Message}");
            }
        }

        Debug.Log($"✅ Excel → JSON 批量转换完成！成功: {totalConverted} 个，失败: {totalFailed} 个");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 批量将 JSON 文件转换为 Excel
    /// </summary>
    private void ConvertAllJsonToExcel()
    {
        string jsonPath = ResolvePath(jsonFolder);
        string excelPath = ResolvePath(excelFolder);

        if (!Directory.Exists(jsonPath))
        {
            Debug.LogError($"JSON 文件夹不存在: {jsonPath}");
            return;
        }

        int totalConverted = 0;
        int totalFailed = 0;

        // 递归查找所有 json 文件
        string[] jsonFiles = Directory.GetFiles(jsonPath, "*.json", SearchOption.AllDirectories);

        foreach (string jsonFile in jsonFiles)
        {
            try
            {
                // 计算相对路径（相对于 jsonPath）
                string relativePath = GetRelativePath(jsonPath, jsonFile);
                string relativeDir = Path.GetDirectoryName(relativePath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(jsonFile);

                // 构建 Excel 文件路径
                string excelDir = string.IsNullOrEmpty(relativeDir) ? excelPath : Path.Combine(excelPath, relativeDir);
                string excelFile = Path.Combine(excelDir, fileNameWithoutExt + ".xlsx");

                // 确保目录存在
                if (!Directory.Exists(excelDir))
                {
                    Directory.CreateDirectory(excelDir);
                }

                // 读取 JSON 内容
                string jsonContent = File.ReadAllText(jsonFile);

                // 使用 JsonToExcel 的方法转换
                JsonToExcel.Convert(jsonContent, excelFile);

                totalConverted++;
                string excelRelativePath = GetRelativePath(excelPath, excelFile);
                Debug.Log($"✅ 转换完成: {relativePath} → {excelRelativePath}");
            }
            catch (System.Exception ex)
            {
                totalFailed++;
                Debug.LogError($"❌ 转换失败: {jsonFile}\n{ex.Message}");
            }
        }

        Debug.Log($"✅ JSON → Excel 批量转换完成！成功: {totalConverted} 个，失败: {totalFailed} 个");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 将 Excel 文件转换为 JSON 数组格式
    /// </summary>
    private string ExcelToJsonArray(string excelPath)
    {
        if (!File.Exists(excelPath))
        {
            throw new System.Exception($"Excel 文件不存在: {excelPath}");
        }

        try
        {
            using (var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var workbook = new XSSFWorkbook(fs);
                var sheet = workbook.GetSheetAt(0);

                if (sheet == null)
                {
                    throw new System.Exception("Excel 文件没有工作表");
                }

                // 读取表头（第0行）
                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    throw new System.Exception("Excel 文件没有表头行");
                }

                // 读取类型行（第1行，可选）
                var typeRow = sheet.GetRow(1);

                // 获取所有列名
                var columns = new List<string>();
                int lastCellNum = headerRow.LastCellNum;
                for (int c = 0; c < lastCellNum; c++)
                {
                    var cell = headerRow.GetCell(c);
                    if (cell != null)
                    {
                        string columnName = GetCellValueAsString(cell);
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            columns.Add(columnName);
                        }
                    }
                }

                if (columns.Count == 0)
                {
                    throw new System.Exception("Excel 文件没有有效的列");
                }

                // 读取数据行（从第2行开始，如果有类型行的话）
                int dataStartRow = typeRow != null ? 2 : 1;
                var jsonArray = new JArray();

                for (int r = dataStartRow; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    var jsonObj = new JObject();
                    bool hasData = false;

                    for (int c = 0; c < columns.Count; c++)
                    {
                        var cell = row.GetCell(c);
                        string cellValue = GetCellValueAsString(cell);

                        // 检查是否有数据（至少有一个非空单元格）
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            hasData = true;
                        }

                        // 推断类型并转换
                        if (typeRow != null && c < typeRow.LastCellNum)
                        {
                            var typeCell = typeRow.GetCell(c);
                            string typeStr = GetCellValueAsString(typeCell);
                            jsonObj[columns[c]] = ConvertCellValueByType(cellValue, typeStr);
                        }
                        else
                        {
                            // 没有类型行，尝试自动推断
                            jsonObj[columns[c]] = ConvertCellValueAuto(cell, cellValue);
                        }
                    }

                    // 只有当行有数据时才添加到数组
                    if (hasData)
                    {
                        jsonArray.Add(jsonObj);
                    }
                }

                return jsonArray.ToString(Formatting.Indented);
            }
        }
        catch (System.IO.IOException ioEx)
        {
            throw new System.Exception($"读取 Excel 文件失败（文件可能正在被 Excel 打开）: {ioEx.Message}");
        }
    }

    /// <summary>
    /// 根据类型字符串转换单元格值
    /// </summary>
    private JToken ConvertCellValueByType(string cellValue, string typeStr)
    {
        if (string.IsNullOrEmpty(cellValue))
        {
            return JValue.CreateNull();
        }

        typeStr = typeStr?.ToLower().Trim();

        switch (typeStr)
        {
            case "int":
                if (int.TryParse(cellValue, out int intVal))
                    return new JValue(intVal);
                break;

            case "float":
                if (float.TryParse(cellValue, out float floatVal))
                    return new JValue(floatVal);
                break;

            case "bool":
                if (bool.TryParse(cellValue, out bool boolVal))
                    return new JValue(boolVal);
                break;

            case "int[]":
                return new JArray(cellValue.Split(',').Select(s =>
                {
                    if (int.TryParse(s.Trim(), out int v))
                        return new JValue(v);
                    return new JValue(s.Trim());
                }));

            case "float[]":
                return new JArray(cellValue.Split(',').Select(s =>
                {
                    if (float.TryParse(s.Trim(), out float v))
                        return new JValue(v);
                    return new JValue(s.Trim());
                }));

            case "string[]":
                return new JArray(cellValue.Split(',').Select(s => new JValue(s.Trim())));

            default:
                return new JValue(cellValue);
        }

        return new JValue(cellValue);
    }

    /// <summary>
    /// 自动推断单元格值类型
    /// </summary>
    private JToken ConvertCellValueAuto(ICell cell, string cellValue)
    {
        if (cell == null || string.IsNullOrEmpty(cellValue))
        {
            return JValue.CreateNull();
        }

        // 检查是否包含逗号（可能是数组）
        if (cellValue.Contains(","))
        {
            var parts = cellValue.Split(',');
            // 尝试解析为数字数组
            bool allNumbers = true;
            var array = new JArray();
            foreach (var part in parts)
            {
                string trimmed = part.Trim();
                if (int.TryParse(trimmed, out int intVal))
                {
                    array.Add(new JValue(intVal));
                }
                else if (float.TryParse(trimmed, out float floatVal))
                {
                    array.Add(new JValue(floatVal));
                    allNumbers = false;
                }
                else
                {
                    array.Add(new JValue(trimmed));
                    allNumbers = false;
                }
            }

            _ = allNumbers;
            return array;
        }

        // 尝试解析为数字
        if (int.TryParse(cellValue, out int intResult))
        {
            return new JValue(intResult);
        }

        if (float.TryParse(cellValue, out float floatResult))
        {
            return new JValue(floatResult);
        }

        // 尝试解析为布尔值
        if (bool.TryParse(cellValue, out bool boolResult))
        {
            return new JValue(boolResult);
        }

        // 默认返回字符串
        return new JValue(cellValue);
    }

    /// <summary>
    /// 获取单元格的字符串值
    /// </summary>
    private string GetCellValueAsString(ICell cell)
    {
        if (cell == null) return "";

        switch (cell.CellType)
        {
            case CellType.String:
                return cell.StringCellValue;
            case CellType.Numeric:
                if (DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue.ToString();
                }
                else
                {
                    // 处理数字，避免科学计数法
                    double numValue = cell.NumericCellValue;
                    if (numValue == (long)numValue)
                        return ((long)numValue).ToString();
                    else
                        return numValue.ToString();
                }
            case CellType.Boolean:
                return cell.BooleanCellValue.ToString();
            case CellType.Formula:
                return cell.CellFormula;
            case CellType.Blank:
                return "";
            default:
                return cell.ToString();
        }
    }

    /// <summary>
    /// 计算相对路径（兼容 Unity，替代 Path.GetRelativePath）
    /// </summary>
    private string GetRelativePath(string fromPath, string toPath)
    {
        fromPath = Path.GetFullPath(fromPath).Replace('\\', '/');
        toPath = Path.GetFullPath(toPath).Replace('\\', '/');

        if (!fromPath.EndsWith("/"))
            fromPath += "/";

        if (toPath.StartsWith(fromPath))
        {
            return toPath.Substring(fromPath.Length);
        }

        // 如果不包含关系，返回文件名
        return Path.GetFileName(toPath);
    }

    /// <summary>
    /// 解析路径（支持 EditorPath 索引）
    /// </summary>
    private string ResolvePath(string pathSetting)
    {
        if (!string.IsNullOrEmpty(pathSetting) && pathSetting.Contains("."))
        {
            string resolved = PathEditor.FindPath(pathSetting);
            if (string.IsNullOrEmpty(resolved))
            {
                Debug.LogWarning($"路径索引解析失败: {pathSetting}，使用原始路径");
                return pathSetting;
            }
            return resolved;
        }
        return pathSetting;
    }
}
#endif


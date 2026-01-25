#define EDITOR
#if UNITY_EDITOR
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

/// <summary>
/// 语言配置 Excel 序列化工具
/// Excel 格式：
/// 第1行：id, key, content
/// 第2行：int, string, string (类型行)
/// 第3行开始：数据行
/// </summary>
public static class LanguageExcelSerializer
{
    /// <summary>
    /// 从 Excel 读取语言配置
    /// </summary>
    public static Dictionary<string, string> ReadFromExcel(string excelPath)
    {
        var result = new Dictionary<string, string>();

        if (!File.Exists(excelPath))
        {
            Debug.LogWarning($"语言 Excel 文件不存在: {excelPath}");
            return result;
        }

        try
        {
            // 使用 FileShare.ReadWrite 允许其他程序读取文件（但文件不能被其他程序独占打开）
            using (var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var workbook = new XSSFWorkbook(fs);
                var sheet = workbook.GetSheetAt(0);

                if (sheet == null || sheet.LastRowNum < 1)
                {
                    Debug.LogWarning($"Excel 文件格式不正确: {excelPath}");
                    return result;
                }

                // 跳过表头行（第0行）和类型行（第1行），从第2行开始读取数据
                for (int rowIndex = 2; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null) continue;

                    // 读取 key（第1列，索引1）
                    var keyCell = row.GetCell(1);
                    if (keyCell == null) continue;

                    string key = GetCellValueAsString(keyCell);
                    if (string.IsNullOrEmpty(key)) continue;

                    // 读取 content（第2列，索引2）
                    var contentCell = row.GetCell(2);
                    string content = contentCell != null ? GetCellValueAsString(contentCell) : "";

                    result[key] = content;
                }
            }

            Debug.Log($"从 Excel 读取了 {result.Count} 条语言配置: {excelPath}");
        }
        catch (System.IO.IOException ioEx)
        {
            Debug.LogError($"读取 Excel 文件失败（文件可能正在被 Excel 打开）: {excelPath}\n" +
                          $"请关闭 Excel 文件后重试。\n错误详情: {ioEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"读取 Excel 文件失败: {excelPath}\n{ex}");
        }

        return result;
    }

    /// <summary>
    /// 更新 Excel 文件（只新建新项，删除没有了的项，不改变现有项的赋值）
    /// </summary>
    public static void UpdateExcel(string excelPath, Dictionary<string, string> scannedKeys)
    {
        try
        {
            XSSFWorkbook workbook;
            ISheet sheet;

            if (File.Exists(excelPath))
            {
                // 读取现有文件，使用 FileShare.ReadWrite 允许共享
                using (var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    workbook = new XSSFWorkbook(fs);
                }
                sheet = workbook.GetSheetAt(0);
                if (sheet == null)
                {
                    sheet = workbook.CreateSheet("Sheet1");
                }
            }
            else
            {
                // 创建新文件
                workbook = new XSSFWorkbook();
                sheet = workbook.CreateSheet("Sheet1");
            }

            // 确保表头和类型行存在
            IRow headerRow = sheet.GetRow(0);
            if (headerRow == null)
            {
                headerRow = sheet.CreateRow(0);
                headerRow.CreateCell(0).SetCellValue("id");
                headerRow.CreateCell(1).SetCellValue("key");
                headerRow.CreateCell(2).SetCellValue("content");
            }

            IRow typeRow = sheet.GetRow(1);
            if (typeRow == null)
            {
                typeRow = sheet.CreateRow(1);
                typeRow.CreateCell(0).SetCellValue("int");
                typeRow.CreateCell(1).SetCellValue("string");
                typeRow.CreateCell(2).SetCellValue("string");
            }

            // 读取现有数据（从第2行开始），收集需要保留的数据
            var rowsToKeep = new Dictionary<string, string>();
            for (int rowIndex = 2; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                var keyCell = row.GetCell(1);
                if (keyCell == null) continue;

                string key = GetCellValueAsString(keyCell);
                if (string.IsNullOrEmpty(key)) continue;

                // 如果这个键在扫描结果中，保留现有值（不改变现有项的赋值）
                if (scannedKeys.ContainsKey(key))
                {
                    var contentCell = row.GetCell(2);
                    string existingValue = contentCell != null ? GetCellValueAsString(contentCell) : "";
                    rowsToKeep[key] = existingValue;
                }
            }

            // 添加新项（在扫描结果中但不在 Excel 中）
            foreach (var kvp in scannedKeys)
            {
                if (!rowsToKeep.ContainsKey(kvp.Key))
                {
                    rowsToKeep[kvp.Key] = ""; // 新项默认空值
                }
            }

            // 删除所有数据行（从第2行开始，从后往前删除避免索引问题）
            for (int rowIndex = sheet.LastRowNum; rowIndex >= 2; rowIndex--)
            {
                var row = sheet.GetRow(rowIndex);
                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }

            // 重新写入数据行
            int id = 1;
            var sortedKeys = rowsToKeep.Keys.OrderBy(k => k).ToList();
            foreach (var key in sortedKeys)
            {
                int rowIndex = id + 1; // 第2行开始（索引2）
                var row = sheet.CreateRow(rowIndex);
                row.CreateCell(0).SetCellValue(id);
                row.CreateCell(1).SetCellValue(key);
                row.CreateCell(2).SetCellValue(rowsToKeep[key]);
                id++;
            }

            // 写入文件（使用 FileMode.Create 会覆盖原文件）
            using (var fs = new FileStream(excelPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                workbook.Write(fs);
            }

            Debug.Log($"更新 Excel 完成: {excelPath} (共 {rowsToKeep.Count} 条)");
        }
        catch (System.IO.IOException ioEx)
        {
            Debug.LogError($"更新 Excel 文件失败（文件可能正在被 Excel 打开）: {excelPath}\n" +
                          $"请关闭 Excel 文件后重试。\n错误详情: {ioEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"更新 Excel 文件失败: {excelPath}\n{ex}");
        }
    }

    /// <summary>
    /// 将 Excel 数据转换为 JSON 格式的字典
    /// </summary>
    public static Dictionary<string, string> ExcelToJsonDict(string excelPath)
    {
        return ReadFromExcel(excelPath);
    }

    /// <summary>
    /// 获取单元格的字符串值
    /// </summary>
    private static string GetCellValueAsString(ICell cell)
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
}
#endif


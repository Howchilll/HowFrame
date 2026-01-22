# ExcelJson Python Extension

这是一个用于在Unity Editor中进行Excel和JSON文件双向转换的Python扩展。

## 功能特性

- **Excel → JSON**：将Excel文件转换为JSON格式
- **JSON → Excel**：将JSON文件转换为Excel格式
- **双向转换**：同时执行两个方向的转换
- **递归处理**：自动处理子文件夹结构
- **类型推断**：自动识别和转换数据类型
- **类型指定**：支持通过第二行指定数据类型

## 依赖安装

在使用此工具前，请确保Python环境中已安装必要的包：

```bash
pip install -r requirements.txt
```

或手动安装：

```bash
pip install openpyxl>=3.0.0
```

## 使用方法

1. 在Unity中选择 `Tools/PyFunctions/ExcelJson`
2. 在参数窗口中设置：
   - **Excel文件夹**：Excel文件所在目录路径
   - **JSON文件夹**：JSON文件输出目录路径
   - **转换方向**：选择转换模式
3. 点击"执行"按钮

## Excel文件格式要求

### 数据结构
- **第1行**：字段名（列名）
- **第2行**：类型定义（可选）
- **第3行开始**：数据行

### 支持的数据类型
- `int`：整数
- `float`：浮点数
- `bool`：布尔值
- `string`：字符串
- `int[]`：整数数组（逗号分隔）
- `float[]`：浮点数数组（逗号分隔）
- `string[]`：字符串数组（逗号分隔）

### 示例Excel文件

| Name | Age | Score | Tags | IsActive |
|------|-----|-------|------|----------|
|      | int | float | string[] | bool |
| Alice | 25 | 95.5 | tag1,tag2 | true |
| Bob | 30 | 87.0 | tag3 | false |

## JSON文件格式

转换后的JSON文件为对象数组格式：

```json
[
  {
    "Name": "Alice",
    "Age": 25,
    "Score": 95.5,
    "Tags": ["tag1", "tag2"],
    "IsActive": true
  },
  {
    "Name": "Bob",
    "Age": 30,
    "Score": 87.0,
    "Tags": ["tag3"],
    "IsActive": false
  }
]
```

## 注意事项

- Excel文件必须是`.xlsx`格式
- JSON文件使用UTF-8编码
- 空行会被自动跳过
- 所有记录必须具有相同的字段结构

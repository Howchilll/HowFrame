import sys
import re
import os
if len(sys.argv) > 1:
    path = sys.argv[1] if len(sys.argv) > 1 else ""

# 在这里编写你的 Python 代码
cs_count = 0
line_count = 0
method_count = 0

# C# 方法正则（非常常用的格式）
method_pattern = re.compile(
    r'\b(public|private|protected|internal|static|async|sealed|override|virtual)\s+.*?\b(\w+)\s*\('
)

for root, dirs, files in os.walk(path):
    for file in files:
        if file.endswith(".cs"):
            cs_count += 1
            full_path = os.path.join(root, file)

            # 读取文件统计
            with open(full_path, "r", encoding="utf-8", errors="ignore") as f:
                for line in f:
                    stripped = line.strip()
                    if stripped:
                        line_count += 1

                    # 方法统计
                    if method_pattern.search(line):
                        method_count += 1


print(f"扫描结果：CS脚本数量={cs_count}, 总行数(非空)={line_count}, 方法数量={method_count}")


def write_output(content):
    """
    将内容输出到脚本文件夹的 output.txt 文件
    :param content: 要输出的内容（字符串）
    """
    import os
    # 获取当前脚本所在的文件夹路径
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, 'output.txt')
    
    # 写入文件（追加模式）
    with open(output_path, 'a', encoding='utf-8') as f:
        f.write(str(content))
        f.write('\n')
    
    print(f"内容已写入: {output_path}")

write_output(f"扫描结果：CS脚本数量={cs_count}, 总行数(非空)={line_count}, 方法数量={method_count}")
# 使用示例：
# write_output("这是输出的内容")

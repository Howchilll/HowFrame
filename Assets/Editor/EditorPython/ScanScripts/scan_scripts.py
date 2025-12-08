import sys
import re
import os
if len(sys.argv) > 1:
    path = sys.argv[1] if len(sys.argv) > 1 else ""



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

print("Python 执行完毕！")

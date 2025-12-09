import sys


aim_folder = sys.argv[1] if len(sys.argv) > 1 else ""
kill_list = sys.argv[2] if len(sys.argv) > 2 else ""
kignore = sys.argv[3] if len(sys.argv) > 3 else ""




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


# 使用示例：
# write_output("这是输出的内容")

import sys
import os
import fnmatch

aim_folder = sys.argv[1] if len(sys.argv) > 1 else ""
kill_list = sys.argv[2] if len(sys.argv) > 2 else ""
kignore = sys.argv[3] if len(sys.argv) > 3 else ""


def write_output(content):
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, 'output.txt')

    with open(output_path, 'a', encoding='utf-8') as f:
        f.write(str(content) + "\n")

    print(f"[OUTPUT] {content}")


def load_list(filepath):
    """读取 kill_list 或 kignore，每行去除空白和换行"""
    if not filepath or not os.path.exists(filepath):
        return []
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = [line.strip() for line in f.readlines() if line.strip()]
    return lines


def should_ignore(path, ignore_rules):
    r"""
    判断某个 kill entry 是否应该被 kignore 排除。
    支持：
      - 完整路径
      - 目录匹配（以 / 或 \ 结尾）
      - 通配符 (*.mat)
      - 部分字符串包含
    """
    for rule in ignore_rules:
        # 1. 目录规则
        if rule.endswith("/") or rule.endswith("\\"):
            if path.startswith(rule.rstrip("/\\")):
                return True

        # 2. 通配符规则 (*.mat)
        if "*" in rule or "?" in rule:
            if fnmatch.fnmatch(path, rule):
                return True

        # 3. 完全匹配
        if path == rule:
            return True

        # 4. 子字符串匹配
        if rule in path:
            return True

    return False


def clean_files():
    # 清空输出文件（每次运行都重新开始）
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, 'output.txt')
    if os.path.exists(output_path):
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write('')  # 清空文件
        print(f"已清空输出文件: {output_path}")
    
    if not aim_folder:
        write_output("错误：未提供 aim_folder")
        return

    aim_folder_abs = os.path.abspath(aim_folder)

    kill_entries = load_list(kill_list)
    ignore_rules = load_list(kignore)

    write_output(f"目标目录: {aim_folder_abs}")
    write_output(f"待删除数量: {len(kill_entries)}")
    write_output(f"忽略规则数量: {len(ignore_rules)}")

    for entry in kill_entries:
        relative_path = entry.replace("\\", "/")
        full_path = os.path.join(aim_folder_abs, relative_path)

        # 判断忽略
        if should_ignore(relative_path, ignore_rules):
            write_output(f"[忽略] {relative_path}")
            continue

        # 删除
        if os.path.exists(full_path):
            try:
                os.remove(full_path)
                write_output(f"[删除] {relative_path}")
            except Exception as e:
                write_output(f"[失败] {relative_path} -> {str(e)}")
        else:
            write_output(f"[不存在] {relative_path}")


# 执行清理
if __name__ == "__main__":
    try:
        clean_files()
        print("Python 执行完毕！")
    except Exception as e:
        error_msg = f"执行出错: {str(e)}"
        print(error_msg)
        write_output(error_msg)
        import traceback
        traceback.print_exc()
        sys.exit(1)

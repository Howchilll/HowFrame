import sys
import os
import re
from collections import deque, defaultdict

# ----------------------------------------------------------------------
# 获取输入参数
# ----------------------------------------------------------------------
scene_path = sys.argv[1] if len(sys.argv) > 1 else ""
resources_path = sys.argv[2] if len(sys.argv) > 2 else ""
streaming_assets_path = sys.argv[3] if len(sys.argv) > 3 else ""
addressable_path = sys.argv[4] if len(sys.argv) > 4 else ""
art_asset_path = sys.argv[5] if len(sys.argv) > 5 else ""


# ----------------------------------------------------------------------
# 输出到脚本目录
# ----------------------------------------------------------------------
def write_output(content):
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, 'output.txt')
    with open(output_path, 'a', encoding='utf-8') as f:
        f.write(str(content) + "\n")
    print(f"内容已写入: {output_path}")


# ----------------------------------------------------------------------
# 工具函数：读取所有 Unity 资源文件（prefab, scene, mat, asset）
# ----------------------------------------------------------------------
UNITY_EXTS = (".prefab", ".unity", ".mat", ".asset", ".controller", ".anim")


def iter_unity_files(folder):
    for root, dirs, files in os.walk(folder):
        for name in files:
            if name.endswith(UNITY_EXTS):
                yield os.path.join(root, name)


def iter_all_files(folder):
    """Art 目录需要收集所有文件，不过滤扩展名"""
    for root, dirs, files in os.walk(folder):
        for name in files:
            yield os.path.join(root, name)


# ----------------------------------------------------------------------
# 提取文件中的 GUID（逐行扫描，内存友好）
# ----------------------------------------------------------------------
guid_pattern = re.compile(r"guid: ([0-9a-f]{32})")


def extract_guids_from_file(path):
    """返回该资源文件里引用到的所有 guid（set）"""
    guid_set = set()
    try:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                m = guid_pattern.search(line)
                if m:
                    guid_set.add(m.group(1))
    except:
        pass
    return guid_set


# ----------------------------------------------------------------------
# 从 meta 文件读出当前资源的 GUID
# ----------------------------------------------------------------------
def get_guid_from_meta(meta_path):
    try:
        with open(meta_path, "r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                if line.startswith("guid: "):
                    return line.split("guid: ")[1].strip()
    except:
        pass
    return None


def get_file_guid(file_path):
    meta = file_path + ".meta"
    return get_guid_from_meta(meta)


# ----------------------------------------------------------------------
# 构建引用图 graph[guid] = [dep_guid...]
# ----------------------------------------------------------------------
def build_graph_from_folder(folder, graph):
    for file in iter_unity_files(folder):
        file_guid = get_file_guid(file)
        if not file_guid:
            continue

        deps = extract_guids_from_file(file)
        graph[file_guid].update(deps)


# ----------------------------------------------------------------------
# BFS 从入口节点查找所有被使用的 GUID
# ----------------------------------------------------------------------
def bfs_all_reachable(entry_guids, graph):
    visited = set()
    queue = deque(entry_guids)

    while queue:
        guid = queue.popleft()
        if guid in visited:
            continue
        visited.add(guid)

        for dep in graph.get(guid, []):
            if dep not in visited:
                queue.append(dep)

    return visited


# ----------------------------------------------------------------------
# 获取入口目录中所有文件 GUID
# ----------------------------------------------------------------------
def collect_entry_guids(folder):
    result = set()
    for file in iter_unity_files(folder):
        guid = get_file_guid(file)
        if guid:
            result.add(guid)
    return result


# ----------------------------------------------------------------------
# 获取 Art Asset 下的所有文件 GUID
# ----------------------------------------------------------------------
def collect_art_guids(folder):
    art_guid_to_path = {}
    for file in iter_all_files(folder):
        guid = get_file_guid(file)
        if guid:
            art_guid_to_path[guid] = file
    return art_guid_to_path


# ----------------------------------------------------------------------
# 主流程
# ----------------------------------------------------------------------

def main():
    # 清空输出文件（每次运行都重新开始）
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, 'output.txt')
    if os.path.exists(output_path):
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write('')  # 清空文件
        print(f"已清空输出文件: {output_path}")

    graph = defaultdict(set)

    # ---------------------------
    # 1. 构建引用图（从四个入口目录）
    # ---------------------------
    print("构建引用图中...")
    build_graph_from_folder(scene_path, graph)
    build_graph_from_folder(resources_path, graph)
    build_graph_from_folder(addressable_path, graph)

    # StreamingAssets 不含 GUID 引用，不需要加入图，但它属于入口资源

    # ---------------------------
    # 2. 收集入口 GUID
    # ---------------------------
    entry_guids = set()
    entry_guids.update(collect_entry_guids(scene_path))
    entry_guids.update(collect_entry_guids(resources_path))
    entry_guids.update(collect_entry_guids(addressable_path))

    # StreamingAssets 里的文件不是 meta GUID 引用体系，这里略过。

    # ---------------------------
    # 3. BFS 查找所有可达资源
    # ---------------------------
    print("BFS 引用扩展中...")
    reachable_guids = bfs_all_reachable(entry_guids, graph)

    # ---------------------------
    # 4. 收集 Art 下资源 GUID
    # ---------------------------
    print("检查 Art 资源...")
    art_guid_to_path = collect_art_guids(art_asset_path)

    unused = []
    for guid, path in art_guid_to_path.items():
        if guid not in reachable_guids:
            unused.append(path)

    # ---------------------------
    # 5. 输出未被引用的 Art 资源路径（相对路径）
    # ---------------------------
    print("输出未引用资源...")
    for full_path in unused:
        rel = os.path.relpath(full_path, art_asset_path)
        write_output(rel)

    print("扫描完成，共发现未引用资源：", len(unused))


if __name__ == "__main__":
    main()
    print("Python 执行完毕！")

import sys

if len(sys.argv) > 1:
    print("参数列表:")
    for i, arg in enumerate(sys.argv[1:], 1):
        print(f"  参数 {i}: {arg}")

    name = sys.argv[1] if len(sys.argv) > 1 else ""

print(name)
print("Hello Unity")
# 在这里编写你的 Python 代码

print("Python 执行完毕！")

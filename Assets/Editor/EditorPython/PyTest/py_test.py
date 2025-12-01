import sys


if len(sys.argv) > 1:
    print("参数列表:")
    for i, arg in enumerate(sys.argv[1:], 1):
        print(f"  参数 {i}: {arg}")




print("Python 执行完毕！")

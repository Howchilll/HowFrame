# HowFrame 框架总览

> 一个面向 Unity 项目的轻量级游戏框架集合，以"助手类 + 静态方法 + 类型安全枚举"为核心风格，强调模块解耦、调用便捷与编辑器自动化。

> 本文档位于 `Assets/Docs/`，属于项目内部文档，仅关注 `Assets/` 范围内的内容，对 `Assets/` 之外的部分默认视为不可知。

---

## 1. 框架定位与目标

- **定位**：在 Unity 中提供一套即拿即用的运行时工具集与编辑器扩展。
- **风格**：以 `*Assistant` / `*Manager` 命名的静态/单例助手类，统一通过 `Wake()` 进行延迟初始化。
- **目标**：
  - 减少重复样板代码（资源、事件、UI、本地化、输入、数据存储……）
  - 提供类型安全的枚举系统，替代传统 C# `enum` 的硬编码弊端
  - 提供可视化编辑器工具，自动生成代码/JSON，降低维护成本
  - 让"业务逻辑"与"工程基础设施"解耦

---

## 2. Assets 顶层目录结构

```
Assets/
├── HowFrame/                # 框架运行时核心（asmdef: HowFrame）
│   ├── HowEnum/             # 类型安全的枚举系统
│   ├── HowPath/             # 全局路径常量
│   └── HowTools/            # 各功能模块助手（HowAsset/HowUI/HowEvent ...）
├── HowFrameExample/         # 用法示例集合（推荐作为接入参考）
├── Editor/                  # 编辑器扩展（asmdef: HowEditor）
│   ├── AutoUI/              # UI 自动绑定窗口
│   ├── EditorPython/        # 通过 C# 调用 Python 脚本的工具链
│   └── HowEditor/           # 自定义窗口（EnumCreator / FileMover / KillAsset / PathCreator ...）
├── HowEditorConfig/         # 编辑器配置（枚举 JSON、路径 JSON、UI 名表）
├── GameRes/                 # Addressables 资源（Audio/Instance/UI）
├── Resources/               # Resources 加载资源（UI 等）
├── Scenes/                  # 示例场景
├── StreamingAssets/         # 流式资源（语言包、配置表）
├── Packages/                # 已下载的第三方包（本地包形式）
└── Docs/                    # 项目内部文档（本目录）
```

---

## 3. 启动与初始化流程

入口挂载脚本：[GameInit.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/GameInit.cs)

```text
GameInit.Awake()
   └── AwakeAsync()
         ├── BoolDic / FloatDic 注入
         ├── HowInit.Init()             // 框架级初始化
         │     └── AssetAssistant.LoadLabelsAsync("Instance", onComplete:
         │           ├── AudioManager.Wake()
         │           ├── CoroutineAssistant.Wake()
         │           ├── DataAssistant.Wake()
         │           ├── DebugAssistant.Wake()
         │           ├── InputAssistant.Wake()
         │           ├── LangManager.Wake()
         │           ├── MonoAssistant.Wake()
         │           ├── PropertyAssistant.Wake()
         │           ├── SceneLoadAssistant.Wake()
         │           ├── UIManager.Wake()
         │           ├── TypeAssistant.Wake()
         │           └── UpdateAssistant.Wake()
         ├── LangManager.SetLanguage(...) // 语言包加载
         └── AssetAssistant.LoadLabelsAsync(resourcesTags) // 业务资源预加载
```

### 3.1 关键设计：延迟初始化（`Wake()`）

- 大多数助手类提供 `Wake()` 方法，在 `HowInit.Init()` 回调中统一调用。
- 内部通过 `_initialized` 标记防止重复初始化。
- 依赖 Addressables 在资源加载完成后才能拿到 `Canvas` / `AudioMixer` / `InputActionAsset` 等资源。

### 3.2 阻塞 vs 异步

`GameInit` 提供 `blockOnAwake` 开关：
- `true`：同步阻塞主线程直到初始化完成（适合场景启动）。
- `false`：异步后台初始化（适合热更新/补丁场景）。

---

## 4. 核心模块速览

### 4.1 HowEnum —— 类型安全枚举系统

> 详细文档：[HowEnumExplain.txt](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowEnum/HowEnumExplain.txt)

**核心类型**：

```csharp
public record EnumKeyBase { public string name; }
public record EnumKey<TTag> : EnumKeyBase { ... }
```

- 每个枚举类自带 `Tag` 嵌套类型做"类型标签"，编译期阻止不同枚举混用。
- 支持嵌套分组（如 `PlayerEnum.Body.Hight`）。
- 可选自动生成 `Convert(string)` 与 `GetAll()` 方法。

**当前枚举类**：

| 类名 | 用途 |
| --- | --- |
| `PlayerEnum` | 玩家属性（HP/SP/EXE/Body…） |
| `GlobalEventEnum` | 全局事件 |
| `LangModuleEnum` | 语言模块（UI/ItemInfo/Default） |
| `LangTypeEnum` | 语言类型（Chinese/English/Malayu） |
| `InputEnum` | 输入设备类型 |
| `NetEnum` | 网络协议 |
| `UINameEnum` | UI 面板名 |

**编辑器工具**：[EnumCreator](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/EnumCreator/EnumCreator.cs)
- `Tools > Enum Creator` 打开可视化窗口。
- 一键导出 JSON 配置 + C# 枚举类。

---

### 4.2 HowPath —— 全局路径常量

位置：[HowFrame/HowPath](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowPath)

- `GlobalPath`：StreamingAssets、Lang、Config 等路径。
- `EditorPath`：编辑器专用路径。
- `ResourcePath`：Resources 资源路径。
- `UINames`：UI 面板名常量（与 `UINameEnum` 配合）。

由 [PathCreator](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/PathCreator/PathCreator.cs) 编辑器工具生成。

---

### 4.3 HowAsset —— 资源加载三件套

位置：[AssetAssistant.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowAsset/AssetAssistant.cs)

| 方法 | 来源 | 用途 |
| --- | --- | --- |
| `ImportAsset<T>(relativePath)` | StreamingAssets | 跨平台文件加载（音频/纹理/文本） |
| `LoadAsset<T>(fileName)` | Resources | 同步加载 |
| `AddressAsset<T>(address)` | Addressables | 异步单资源加载 |
| `LoadLabelsAsync(...labels)` | Addressables | 按 Label 批量预加载并缓存 |
| `AddressableGet<T>(name)` | 缓存 | 从缓存按名取（O(1)） |
| `ReleaseLabels(...)` | Addressables | 按 Label 卸载 |

**设计要点**：
- 通过 `asset.name` 作为缓存 key，避免重复加载。
- Label 批量加载时记录所有 `AsyncOperationHandle`，便于整体释放。
- 缓存与 Handle 分离，避免多 Label 共享资源被误清。

---

### 4.4 HowUI —— UI 管理系统

位置：[UIManager.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowUI/UIManager.cs) 与 [PanelBase.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowUI/PanelBase.cs)

**核心抽象 `PanelBase`**：

```csharp
public abstract class PanelBase : MonoBehaviour
{
    protected internal abstract void Init();
    protected internal virtual void WhenShow();
    protected internal virtual void WhenHide();
    protected internal virtual void WhenShowWithParameter(object parameter);
}
```

**`UIManager` 能力**：
- `Show(...)` / `Hide(...)` / `Check(...)` / `HideAll(...)`，同时支持 `string` 与 `EnumKeyBase`。
- 自动从 Addressables 加载 Prefab，通过 `TypeAssistant` 反射挂载 `PanelBase`。
- 支持父子结构（`father` 参数）、参数透传（`WhenShowWithParameter`）。

**UI 自动绑定窗口**：[AutoUI](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/AutoUI)
- 一键扫描 Prefab 节点，自动生成 `View/Model/Panel` 模板脚本。

---

### 4.5 HowEvent —— 事件系统

位置：[EventAssistant.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowEvent/EventAssistant/EventAssistant.cs)

提供两类事件总线：
- `EventAssistant`：无参 `Action`，支持 `Subscribe / Unsubscribe / Invoke / ClearOne / ClearAll`。
- `EventAssistant<T, TResult>`：泛型带参 + 带返回值 `Func<T, TResult>`。

特性：
- 重载支持 `EnumKeyBase`，可直接使用枚举键作为事件 ID。
- `Editor` 下未注册事件会 `LogWarning` 提示。

子目录还提供 `Once`（一次性）、`Order`（顺序）/ `OnceOrder`（一次性顺序）变体。

---

### 4.6 HowLanguage —— 多语言系统

位置：[LangManager.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowLanguage/LangManager.cs)

- 以 `LangTypeEnum`（语种）× `LangModuleEnum`（模块）二维键加载 JSON。
- 文件命名约定：`{LangName}_{ModuleName}_Lang.json`，存放于 `StreamingAssets/Config/Languages/`。
- 提供 `GetLangContent(Module, key)` / `GetLangContent(key)`。
- 由 [LanguageConfiger](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/LanguageConfiger) 通过 Excel 序列化生成 JSON。

---

### 4.7 HowInput —— 输入系统封装

位置：[InputAssistant.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowInput/InputAssistant.cs)

- 基于 Unity 新输入系统 `InputActionAsset` (`HowInputActions`)。
- `EnableMap / DisableMap / IsMapEnabled`：按 ActionMap 管理。
- `BindAction / UnbindAction`：支持 `performed` + `canceled` 双回调，支持 `EnumKeyBase`。
- `ReadValue<T>`：直接读 Action 值。
- 自动识别设备切换 `ControlScheme`（Keyboard&Mouse / Gamepad / Touch），并通过 `InputType` + `PropertyAssistant` 广播 `GlobalEventEnum.InputTypeChange`。

---

### 4.8 HowAudio —— 音频系统

位置：[AudioManager.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowAudio/AudioManager.cs)

- 区分 `Music` / `Sound` 两类通道，使用 `AudioMixer` 控制音量。
- 音乐支持淡出结束、音效池化复用。
- 音效内置 LRU 缓存（`MaxSoundCache = 50`），按频率衰减清理。
- 同名短时间重复播放会被 `SoundCheck` 去重。

---

### 4.9 HowData —— 数据持久化

位置：[DataAssistant.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowData/DataAssistant.cs)

| 方法 | 用途 |
| --- | --- |
| `WriteData(obj, name, isJson, upperPath)` | 写入 `persistentDataPath`（MessagePack+XOR 或 JSON） |
| `ReadData<T>(...)` | 反序列化读取 |
| `LoadConfig<T>(fileName)` | 从 `StreamingAssets` 加载只读配置 |
| `HasData(...)` | 检查文件是否存在 |

二进制使用 `MessagePack` 序列化 + `Encryption.XOR` 异或加密；JSON 使用 `LitJson`。

---

### 4.10 HowSceneLoad —— 场景加载

位置：[SceneLoadAssistant.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowSceneLoad/SceneLoadAssistant.cs)

统一异步加载/卸载入口，支持进度回调与事件广播。

### 4.11 HowCoroutine / HowMono —— 协程与 Mono 辅助

- `CoroutineAssistant` / `CoroutineHelper`：无 `MonoBehaviour` 也能启动协程。
- `MonoAssistant` / `MonoHelper`：全局 Fake Mono、`Invoke` 等常用能力。

### 4.12 HowUpdate —— 每帧/定时更新

位置：[HowUpdate](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowUpdate)

- `UpdateAssistant` / `UpdateHelper`：注册/注销每帧回调，无需自己写 Mono。
- `Updater`：核心驱动器。

### 4.13 HowType —— 运行时反射

位置：[HowType](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowType)

- `TypeAssistant.GetType(name)`：按类名字符串取 `Type`（用于 UI 自动挂载）。
- `RuntimeGetAttribute`：自定义特性，配合 `TypeAssistant` 标注。

### 4.14 HowProperty / Ref —— 响应式属性

位置：[HowProperty](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowProperty)

- `PropertyAssistant.SetObj / GetObj`：全局响应式对象注册表。
- `Ref<T>`：引用包装，可被 `PropertyAssistant` 观察。

### 4.15 HowThread —— 多线程

位置：[HowThread](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowThread)

- `ThreadAssistant` / `ThreadHelper`：把异步回调投递回主线程。

### 4.16 HowJObj —— Json 包装

位置：[JObjAssistant.cs](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowJObj/JObjAssistant.cs)

基于 `LitJson` 的轻量 Json 助手（与 `DataAssistant` 互补）。

### 4.17 HowContainer —— 容器合集

| 子模块 | 作用 |
| --- | --- |
| `Buffer/` | `BufferDictionary` / `BufferList`：带读写指针的环形缓冲 |
| `ObjectPool/` | `ObjectPool`：通用对象池 |
| `Objson/` | 对象 ↔ JSON 双向转换 |
| `Ref/` | `RefList`：引用类型列表 |
| `Weighted/` | `WeightedList` / `WightedDictionary`：按权重随机抽取 |

### 4.18 HowDetection —— 检测
- `AreaDetection`：区域触发/进入退出。
- `RayManager`：统一射线检测管理。

### 4.19 HowRender —— 渲染
- `CameraEffect`：相机后处理。
- `LineRender` / `LineManager`：统一线渲染管理。

### 4.20 HowTime / HowRandom / HowDebug / HowDefer / HowJobs / HowKeys / HowSingleton / HowSimpleTool

零碎的小工具，按命名直译即可使用：

- `HowTime`：时间/计时相关。
- `HowRandom`：随机数/概率工具。
- `HowDebug`：调试日志增强（含 `DebugColor`）。
- `HowDefer`：延迟执行（`Defer`）。
- `HowJobs`：Job 系统辅助。
- `HowKeys`：键盘码 / 输入键常量。
- `HowSingleton`：`Singleton<T>` / `SingletonMono<T>`。
- `HowSimpleTool`：杂项常用小工具（`STAssistant`）。

---

## 5. 编辑器扩展（HowEditor）

位置：[Editor/HowEditor](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor)

| 模块 | 能力 |
| --- | --- |
| [EnumCreator](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/EnumCreator) | 可视化编辑枚举，生成 JSON + C# |
| [PathCreator](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/PathCreator) | 路径常量生成 |
| [ExcelJson](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/ExcelJson) | Excel ↔ JSON 互转、Config 转换 |
| [LanguageConfiger](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/LanguageConfiger) | 语言 Excel 序列化 |
| [FileMover](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/FileMover) | 批量移动/重命名资源 |
| [KillAsset](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/KillAsset) | 按规则清理无用资源 |
| [AddressableBuilder](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/AddressableBuilder) | Addressables 增量打包 |

### 5.1 AutoUI 自动绑定窗口

位置：[Editor/AutoUI](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/AutoUI)

针对 Prefab：
- 扫描节点 → 按命名生成 `View`（视图层）/ `Model`（数据层）/ `Panel`（逻辑层）模板。
- 模板存放在同目录的 `Template_*.cs.txt`。

### 5.2 EditorPython（C# ↔ Python 桥接）

位置：[Editor/EditorPython](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/EditorPython)

- `PyCaller`：通过 `Process` 调用 Python 脚本。
- 子工具：
  - `DependencyManager`：依赖关系管理。
  - `ExcelJson`：Python 版 Excel ↔ JSON。
  - `LanguageConfiger`：Python 版语言配置生成。
  - `ScanScripts`：脚本扫描。

每个工具都附带 `parameters.json` 和 `requirements.txt`，可独立运行。

---

## 6. 第三方依赖（Packages）

`Assets/Packages/` 下的本地包：

- **DOTween** —— 动画缓动。
- **LitJson** —— JSON 序列化。
- **MessagePack** + **MessagePack.Annotations** + **MessagePackAnalyzer** —— 二进制序列化。
- **Newtonsoft.Json** —— 通用 JSON。
- **Enums.NET** —— 枚举扩展。
- **NPOI** —— Excel 读写。
- **NSax** / **SharpZipLib** —— 压缩。
- **SixLabors.ImageSharp** / **SixLabors.Fonts** —— 图像与字体处理。
- **BouncyCastle.Cryptography** / **ExtendedNumerics.BigDecimal** / **MathNet.Numerics** —— 数学与加密。
- **Microsoft.IO.RecyclableMemoryStream** / **Microsoft.NET.StringTools** / **System.Collections.Immutable** 等基础库。
- **TextMesh Pro** —— 文本渲染。

---

## 7. 示例（HowFrameExample）

位置：[HowFrameExample](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrameExample)

按"一个助手类一个示例"组织：

- `AssetAssistantExample.cs`
- `AudioManagerExample.cs` / `AudioHelperExample.cs`
- `CoroutineAssistantExample.cs` / `CoroutineHelperExample.cs`
- `DataAssistantExample.cs`
- `DeferExample.cs`
- `HowMonoExample.cs`
- `InputSubcribe.cs`
- `PropertyTest.cs` / `PropertyTest1.cs`
- `UpdateHelperExample.cs`
- `TestPanel/*` / `ExamplePanel.cs` —— UI 面板示例（Model/View/Panel 三段式）。

`ConfigTest.cs` / `Doneshow.cs` 演示了 `GameInit` 的字段注入与"加载完毕"切换 UI 的写法。

---

## 8. 推荐的接入顺序

1. **挂 `GameInit`**：场景里挂上 `GameInit` 组件，配置 `langName` 和 `resourcesTags`。
2. **配置枚举**：用 `EnumCreator` 把业务枚举补齐，生成代码。
3. **准备路径**：用 `PathCreator` 生成路径常量。
4. **接入 UI**：继承 `PanelBase`，通过 `UIManager.Show(UINameEnum.X)` 打开。
5. **接入数据**：用 `DataAssistant.ReadData/WriteData` 或 `LoadConfig`。
6. **接入事件**：用 `EventAssistant.Subscribe(GlobalEventEnum.X, ...)`。
7. **接入输入**：配置 `HowInputActions.inputactions`，用 `InputAssistant.BindAction`。

---

## 9. 维护与扩展建议

- **新模块**：在 `HowTools/HowXxx/` 下新增助手类，统一提供 `Wake()`、`namespace HowFrame`。
- **新枚举**：走 `EnumCreator`，禁止手改生成的 `*.cs`。
- **新语言**：在 `LangTypeEnum` 加项 + `StreamingAssets/Config/Languages/` 下放对应 JSON。
- **新 UI**：在 `UINameEnum` 注册 → Prefab 放 `GameRes/UI/` 并打 Addressables Label。
- **新依赖**：放入 `Assets/Packages/`（已使用本地包形式），并在 `Packages/manifest.json` 中登记。

---

## 10. 相关文档索引

- [HowEnum 系统详解](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowEnum/HowEnumExplain.txt)
- [EditorPython / ExcelJson README](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/EditorPython/ExcelJson/README.md)
- [GameInit 入口](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/GameInit.cs)
- [HowInit 总初始化](file:///Users/Apple/UnityProject/HowFrame/Assets/HowFrame/HowTools/HowInit/HowInit.cs)
- [EnumCreator 窗口](file:///Users/Apple/UnityProject/HowFrame/Assets/Editor/HowEditor/EnumCreator/EnumCreator.cs)

---

> 最后更新：2026-09-07
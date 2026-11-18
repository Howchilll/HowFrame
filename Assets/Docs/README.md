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
├── HowFrameTests/           # 框架 EditMode 测试（asmdef: HowFrame.Tests.Editor）
├── Editor/                  # 编辑器扩展
│   ├── AutoUI/              # UI 自动绑定窗口 + 代码模板
│   ├── EditorPython/        # 通过 C# 调用 Python 脚本的工具链
│   ├── HowEditor/           # 自定义窗口（asmdef: HowEditor，仅编辑器）
│   │   └── Settings/        # HowFrame 全局配置（Project Settings > HowFrame）
│   └── HowEditorConfig/     # 编辑器配置数据（枚举 JSON、路径 JSON）
├── GameRes/                 # Addressables 资源（Audio/Instance/UI）
├── Resources/               # Resources 加载资源（UI 等）
├── Scenes/                  # 示例场景
├── StreamingAssets/         # 流式资源（语言包、配置表）
├── Packages/                # 已下载的第三方包（本地包形式）
└── Docs/                    # 项目内部文档（本目录）
```

---

## 3. 启动与初始化流程

入口挂载脚本：[GameInit.cs](../HowFrame/GameInit.cs)

```text
GameInit.Awake()
   └── AwakeAsync()
         ├── BoolDic / FloatDic 注入
         ├── HowInit.Init()             // 框架级初始化（多次调用共享同一个任务）
         │     ├── AssetAssistant.LoadLabelsAsync(HowInit.InstanceLabel = "Instance")
         │     └── WakeSafe(...)        // 逐个 Wake，单个模块异常只记录、不中断
         │           Debug → Type → Mono → Coroutine → Update → Property
         │           → Data → Lang → Audio → Input → SceneLoad → UI
         ├── LangManager.SetLanguage(...)                     // 语言包加载
         └── AssetAssistant.LoadLabelsAsync(null, onProgress, resourcesTags) // 业务资源预加载（GameInit.Progress）
```

### 3.1 关键设计：延迟初始化（`Wake()`）

- 大多数助手类提供 `Wake()` 方法，在 `HowInit.Init()` 中统一调用。
- 内部通过 `_initialized` 标记防止重复初始化。
- 依赖 Addressables 在资源加载完成后才能拿到 `Canvas` / `AudioMixer` / `InputActionAsset` 等资源。
- 某个模块 `Wake` 抛异常时，`HowInit` 记录错误并继续初始化其余模块（`HowInit.FailedModules` 记录失败数）。

### 3.2 等待初始化完成

初始化是异步的。**不要阻塞主线程等待**：Addressables / UnityWebRequest 都要在主线程推进，阻塞会死锁（旧版的 `blockOnAwake` 已移除）。

| 需求 | 用法 |
| --- | --- |
| 框架就绪后做事 | `HowInit.WhenReady(() => ...)` / `await HowInit.Ready` / `HowInit.IsReady` |
| 游戏（框架 + 语言 + 业务资源）就绪后做事 | `GameInit.WhenReady(() => ...)` / `await GameInit.Ready` / `GameInit.IsReady` |
| 启动页进度条 | 读 `GameInit.Progress`（0~1） |

`doneHide` / `doneShow` 可不填。

---

## 4. 核心模块速览

### 4.1 HowEnum —— 类型安全枚举系统

> 详细文档：[HowEnumExplain.txt](../HowFrame/HowEnum/HowEnumExplain.txt)

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

**编辑器工具**：[EnumCreator](../Editor/HowEditor/EnumCreator/EnumCreator.cs)
- `Tools > Enum Creator` 打开可视化窗口。
- 一键导出 JSON 配置 + C# 枚举类。

---

### 4.2 HowPath —— 全局路径常量

位置：[HowFrame/HowPath](../HowFrame/HowPath)

- `GlobalPath`：StreamingAssets、Lang、Config 等运行时路径。
- `ResourcePath`：Resources 资源路径。
- 编辑器工具用的路径不在这里（原 `EditorPath` 已移除），统一放在 HowFrame 全局配置，见 5.0。
- `UINames`：UI 面板名常量（与 `UINameEnum` 配合）。

由 [PathCreator](../Editor/HowEditor/PathCreator/PathCreator.cs) 编辑器工具生成。

---

### 4.3 HowAsset —— 资源加载三件套

位置：[AssetAssistant.cs](../HowFrame/HowTools/HowAsset/AssetAssistant.cs)

| 方法 | 来源 | 用途 |
| --- | --- | --- |
| `ImportAsset<T>(relativePath)` | StreamingAssets | 跨平台文件加载（音频/纹理/文本） |
| `LoadAsset<T>(fileName)` | Resources | 同步加载 |
| `AddressAsset<T>(address)` | Addressables | 异步单资源加载 |
| `LoadLabelsAsync(...labels)` | Addressables | 按 Label 批量预加载并缓存 |
| `LoadLabelsAsync(onComplete, onProgress, ...labels)` | Addressables | 同上，带 0~1 总体进度回调 |
| `IsLabelLoaded(label)` | 缓存 | Label 是否已加载 |
| `AddressableGet<T>(name)` / `TryAddressableGet<T>(name, out asset)` | 缓存 | 从缓存按名取（O(1)） |
| `ReleaseLabels(...)` | Addressables | 按 Label 卸载 |

**设计要点**：
- 通过 `asset.name` 作为缓存 key，避免重复加载；重名资源会警告并只保留先加载的。
- 已加载 / 正在加载的 Label 再次请求时直接复用，不会重复加载。
- 每个缓存项记录归属 Label：同一资源属于多个 Label 时，只有全部卸载后才移出缓存，既不会误清也不会留下已释放的引用。

---

### 4.4 HowUI —— UI 管理系统

位置：[UIManager.cs](../HowFrame/HowTools/HowUI/UIManager.cs) 与 [PanelBase.cs](../HowFrame/HowTools/HowUI/PanelBase.cs)

**核心抽象 `PanelBase`**：

```csharp
public abstract class PanelBase : MonoBehaviour
{
    protected internal abstract void Init();
    protected internal virtual void WhenShow();
    protected internal virtual void WhenHide();
    protected internal virtual void WhenShowWithParameter(object parameter);
    protected internal virtual void WhenLanguageChanged(); // 首次创建及每次切换语言时调用
}
```

**`UIManager` 能力**：
- `Show(...)` / `Hide(...)` / `Check(...)` / `HideAll(...)`，同时支持 `string` 与 `EnumKeyBase`。
- 多层 Canvas：`Show(name, uiLevel)`，层级越大越晚渲染；`HideLevel(level)` 关闭一层。
- 自动从 Addressables 加载 Prefab，通过 `TypeAssistant` 反射挂载 `PanelBase`。
- 支持父子结构（`father` 参数）、参数透传（`WhenShowWithParameter`）。
- **导航栈**：`Push(name, parameter, uiLevel, hidePrevious)` 打开并入栈；`Back()` 关闭栈顶并重新显示上一个（栈空返回 false）；`BackTo(name)`、`ClearStack()`、`StackTop`、`StackCount`。直接 `Show` 的面板不入栈。
- **多语言刷新**：`LangManager.SetLanguage` 完成后自动调用所有面板的 `WhenLanguageChanged`。

**UI 自动绑定窗口**：[AutoUI](../Editor/AutoUI)
- 一键扫描 Prefab 节点，自动生成 `View/Model/Panel` 模板脚本。
- Model 中为文本组件生成 `partial void ChangeLanguage()`，View 模板在 `WhenLanguageChanged` 中调用它，切换语言即自动刷新。

---

### 4.5 HowEvent —— 事件系统

位置：[EventAssistant.cs](../HowFrame/HowTools/HowEvent/EventAssistant/EventAssistant.cs)

提供两类事件总线：
- `EventAssistant`：无参 `Action`，支持 `Subscribe / Unsubscribe / Invoke / ClearOne / ClearAll`。
- `EventAssistant<T, TResult>`：泛型带参 + 带返回值 `Func<T, TResult>`。

特性：
- 重载支持 `EnumKeyBase`，可直接使用枚举键作为事件 ID。
- `Editor` 下未注册事件会 `LogWarning` 提示。

子目录还提供 `Once`（一次性）、`Order`（顺序）/ `OnceOrder`（一次性顺序）变体。

---

### 4.6 HowLanguage —— 多语言系统

位置：[LangManager.cs](../HowFrame/HowTools/HowLanguage/LangManager.cs)

- 以 `LangTypeEnum`（语种）× `LangModuleEnum`（模块）二维键加载 JSON。
- 文件命名约定：`{LangName}_{ModuleName}_Lang.json`，存放于 `StreamingAssets/{GlobalPath.LangPath}/`（默认 `StreamingAssets/Languages/`）。
- 提供 `GetLangContent(Module, key)` / `GetLangContent(key)` / `TryGetLangContent(...)`；缺失的 key 返回 key 本身并只警告一次。
- 运行中缺失的 key 记录在 `MissingKeys`（按模块，切换语言不清空），可在运行时调试窗口里一键追加到语言 Excel。
- `CurrentLanguage`（`Ref`，可订阅）与 `OnLanguageChanged` 事件；切换时先整体加载再替换，不会读到一半新一半旧。
- 由 [LanguageConfiger](../Editor/HowEditor/LanguageConfiger)（菜单 `Tools/HowFrame/Language Configer`）通过 Excel 序列化生成 JSON。

---

### 4.7 HowInput —— 输入系统封装

位置：[InputAssistant.cs](../HowFrame/HowTools/HowInput/InputAssistant.cs)

- 基于 Unity 新输入系统 `InputActionAsset` (`HowInputActions`)。
- `EnableMap / DisableMap / IsMapEnabled`：按 ActionMap 管理。
- `BindAction / UnbindAction`：支持 `performed` + `canceled` 双回调，支持 `EnumKeyBase`。
- `ReadValue<T>`：直接读 Action 值。
- 自动识别设备切换 `ControlScheme`（Keyboard&Mouse / Gamepad / Touch），并通过 `InputType` + `PropertyAssistant` 广播 `GlobalEventEnum.InputTypeChange`。

---

### 4.8 HowAudio —— 音频系统

位置：[AudioManager.cs](../HowFrame/HowTools/HowAudio/AudioManager.cs)

- 区分 `Music` / `Sound` 两类通道，使用 `AudioMixer` 控制音量。
- 音乐支持淡出结束（按真实时间，暂停时也能淡出）、`IsMusicPlaying(key)`；音效池化复用，挂点物体被销毁时自动跳过已销毁的 AudioSource。
- 音频资源直接取 `AssetAssistant` 的 Label 缓存，生命周期由 AssetAssistant 管理（AudioManager 不再自行卸载，避免把共享资源卸掉）。
- 同一音效 + 同一挂点在延迟期间重复触发会被去重；加载失败不会留下去重标记。
- 音量 0 时按 -80dB 处理（不会出现 Log10(0) = -∞）。

---

### 4.9 HowData —— 数据持久化

位置：[DataAssistant.cs](../HowFrame/HowTools/HowData/DataAssistant.cs)

| 方法 | 用途 |
| --- | --- |
| `WriteData(obj, name, isJson, upperPath)` | 写入 `persistentDataPath`（MessagePack+XOR 或 JSON），先写临时文件再替换 |
| `ReadData<T>(...)` | 反序列化读取；不存在 / 损坏返回 default |
| `HasData(...)` / `DeleteData(...)` | 检查 / 删除存档 |
| `LoadConfig<T>(fileName)` | 同步加载 `StreamingAssets` 下的 JSON（Android / WebGL 不可用） |
| `LoadConfigAsync<T>(fileName)` | 异步加载 `StreamingAssets` 下的 JSON（全平台） |

二进制使用 `MessagePack`（Contractless，普通类无需标注即可序列化，读写同一套选项）+ `Encryption.XOR` 异或；JSON 使用 `LitJson`。

---

### 4.10 HowSceneLoad —— 场景加载

位置：[SceneLoadAssistant.cs](../HowFrame/HowTools/HowSceneLoad/SceneLoadAssistant.cs)

统一异步加载/卸载入口，支持进度回调与事件广播。

### 4.11 HowCoroutine / HowMono —— 协程与 Mono 辅助

- `CoroutineAssistant` / `CoroutineHelper`：无 `MonoBehaviour` 也能启动协程。
- `MonoAssistant` / `MonoHelper`：全局 Fake Mono、`Invoke` 等常用能力。

### 4.12 HowUpdate —— 每帧/定时更新

位置：[HowUpdate](../HowFrame/HowTools/HowUpdate)

- `UpdateAssistant` / `UpdateHelper`：注册/注销每帧回调，无需自己写 Mono。
- `Updater`：核心驱动器（基础 60 帧节拍）。遍历快照 + 每个回调单独 try/catch：回调里 Dispose / 新建、或某个回调抛异常，都不会让整个循环停掉。
- `new UpdateHelper(fps)`：fps 取 1~60 任意值，按 `60 / fps` 取整为触发间隔（20 → 每 3 帧），超出范围会警告并限制。

### 4.13 HowType —— 运行时反射

位置：[HowType](../HowFrame/HowTools/HowType)

- `TypeAssistant.GetType(name)`：按类名字符串取 `Type`（用于 UI 自动挂载）。
- `RuntimeGetAttribute`：自定义特性，配合 `TypeAssistant` 标注。

### 4.14 HowProperty / Ref —— 响应式属性

位置：[HowProperty](../HowFrame/HowTools/HowProperty)

- `PropertyAssistant.SetObj / GetObj`：全局响应式对象注册表。
- `Ref<T>`：引用包装，可被 `PropertyAssistant` 观察。

### 4.15 HowThread —— 多线程

位置：[HowThread](../HowFrame/HowTools/HowThread)

- `ThreadAssistant`：按名字管理后台线程的循环任务（`StartLoop`）与延迟任务（`DelayInvoke`），`Stop` 后不再执行回调。
- ⚠ 回调运行在**后台线程**，不能直接调用 Unity API；单个回调异常只记录日志，不会让线程静默死掉。

### 4.16 HowJObj —— Json 包装

位置：[JObjAssistant.cs](../HowFrame/HowTools/HowJObj/JObjAssistant.cs)

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

位置：[Editor/HowEditor](../Editor/HowEditor)

### 5.0 HowFrame 全局配置（路径依赖统一入口）

位置：[Editor/HowEditor/Settings](../Editor/HowEditor/Settings)　入口：**Edit > Project Settings > HowFrame** 或菜单 `Tools/HowFrame/Settings`

框架所有编辑器工具用到的路径都只在这里配置，保存在 `ProjectSettings/HowFrameSettings.asset`（纳入版本管理，团队共享；未保存过时使用代码中的默认值）。工具窗口里只读显示当前路径，并提供「在 HowFrame 设置中修改路径」按钮。

| 分组 | 配置项 | 使用者 |
| --- | --- | --- |
| Python | Python 可执行文件（留空自动在 PATH 查找）、Py 工具生成目录 | PyCaller、PyTemplateGenerator |
| 枚举 | JSON 目录、C# 输出目录、命名空间、是否生成 Convert / GetAll | EnumCreator |
| 路径常量 | JSON 目录、C# 输出目录 | PathCreator |
| 配置表 | Excel 目录、JSON 目录 | ConfigConverter、ExcelJson(Py) |
| 多语言 | Excel 目录、JSON 目录 | LanguageConfiger、LanguageConfiger(Py) |
| UI | Panel 脚本根目录、AutoUI 模板目录（留空用内置） | AutoUI、UIAutoBind |
| Addressables | 自动收录根目录 | AddressableAutoIncremental |
| KillAsset | 清理目标目录、依赖扫描起点、忽略规则 | KillAsset |

约定：
- 相对路径都相对**工程根目录**（Assets 的上一级），也可填绝对路径；`HowFrameSettings.ToFullPath()` 统一解析。
- 框架**自带**的文件（AutoUI 模板、Python 脚本）不写死路径，按脚本名定位（`HowFrameSettings.GetScriptDir`），框架放在 `Assets/` 或以 UPM 包放在 `Packages/` 都能找到。
- 新增编辑器工具需要路径时，请在 `HowFrameSettings` 加字段，不要在工具里写死 `Assets/...`。

| 模块 | 菜单 | 能力 |
| --- | --- | --- |
| [EnumCreator](../Editor/HowEditor/EnumCreator) | `Tools/Enum Creator` | 可视化编辑枚举，生成 JSON + C# |
| [PathCreator](../Editor/HowEditor/PathCreator) | `Tools/Path Creator` | 路径常量生成 |
| [ConfigConverter](../Editor/HowEditor/ExcelJson) | `Tools/HowFrame/Config Converter` | Excel ↔ JSON 批量互转 |
| [LanguageConfiger](../Editor/HowEditor/LanguageConfiger) | `Tools/HowFrame/Language Configer` | 扫描 `GetLangContent` 调用更新语言 Excel，并导出 JSON |
| [FileMover](../Editor/HowEditor/FileMover) | （已隐藏） | 批量移动/删除文件，支持 `类名.字段名` 引用路径常量 |
| [KillAsset](../Editor/HowEditor/KillAsset) | `Tools/KillAsset` | 清理目标目录中未被引用的资源（删除前确认，移到系统回收站） |
| [AddressableBuilder](../Editor/HowEditor/AddressableBuilder) | 自动 | 收录根目录下的资源自动加入 Addressables，按一级子目录分组 |
| [Doctor](../Editor/HowEditor/Doctor) | `Tools/HowFrame/Doctor (健康检查)` | 一键检查框架约定，见 5.4 |
| [Runtime Debugger](../Editor/HowEditor/DebugWindow) | `Tools/HowFrame/Runtime Debugger (运行时调试)` | Play 模式下查看框架运行状态，见 5.5 |

### 5.1 AutoUI 自动绑定窗口

位置：[Editor/AutoUI](../Editor/AutoUI)

针对 Prefab：
- 扫描节点 → 按命名生成 `View`（视图层）/ `Model`（数据层）/ `Panel`（逻辑层）模板。
- 模板默认使用同目录的 `Template_*.cs.txt`，可在 HowFrame 设置里换成项目自己的模板目录。

### 5.2 EditorPython（C# ↔ Python 桥接）

位置：[Editor/EditorPython](../Editor/EditorPython)

- `PyCaller`：通过 `Process` 调用 Python 脚本，解释器来自 HowFrame 设置。
- 子工具（菜单 `Tools/PyFunctions/...`）：
  - `ExcelJson`：Python 版 Excel ↔ JSON。
  - `LanguageConfiger`：Python 版语言配置生成。
  - `ScanScripts`：统计目录下脚本数 / 行数 / 方法数。
- `PyTemplateGenerator`：生成新的 Python 工具骨架（C# 窗口 + .py + parameters.json）到「Py 工具生成目录」。

内置工具的目录参数来自 HowFrame 设置，窗口只记住操作模式（EditorPrefs）。依赖见各工具的 `requirements.txt`。

### 5.3 测试

位置：[HowFrameTests/Editor](../HowFrameTests/Editor)。Window > General > Test Runner > EditMode 运行，或命令行：

```
Unity.exe -batchmode -projectPath <工程> -runTests -testPlatform EditMode -assemblyNames HowFrame.Tests.Editor -testResults results.xml
```

覆盖：配置路径解析与内置文件定位、PathEditor、DataAssistant 读写 / 覆盖 / 删除、LangManager 缺失 key 回退与记录、EnumKey 转换、UIManager 空栈 Back、
健康检查各项（枚举同步含分组、语言文件、资源重名、Instance 资源）、语言 Excel 只追加写入。

### 5.4 Doctor（健康检查）

位置：[Editor/HowEditor/Doctor](../Editor/HowEditor/Doctor)。检查项：

| 类别 | 检查内容 |
| --- | --- |
| 路径设置 | HowFrame 设置里的各目录是否存在 |
| Python | 能否找到 Python 解释器 |
| Instance 资源 | `Canvas` / `HowInputActions`（必需）、`AudioMixer` / `EventSystem`（可选）是否在 Addressables 中并带 `Instance` 标签 |
| UI 预设体 | `UINameEnum` 每一项是否有同名可寻址 `.prefab`、是否有 Label、是否带 PanelBase（或有同名 PanelBase 子类） |
| 资源重名 | 共享 Label 的同名资源（运行时缓存按名字存，只能取到其中一个） |
| 语言文件 | 每个 语种 × 模块 的 JSON 是否存在，同一模块各语种 key 是否一致 |
| 枚举同步 | 枚举 JSON 与生成的 `.cs` 是否一致（改了 JSON 忘了重新生成） |

命令行 / CI：`Unity.exe -batchmode -quit -projectPath <工程> -executeMethod HowFrameDoctor.RunFromCommandLine`，有错误时退出码为 1。

### 5.5 Runtime Debugger（运行时调试）

位置：[Editor/HowEditor/DebugWindow](../Editor/HowEditor/DebugWindow)。Play 模式下每 0.5 秒刷新：

- **概览**：HowInit / GameInit 状态与进度、Wake 失败的模块、各项统计。
- **语言**：下拉切换语言；运行中缺失的 key 列表，可「追加到 Excel」（追加到所有语种对应模块表的末尾，content 留空，不改动已有行）或复制。
- **资源**：已加载 / 加载中的 Label，缓存资源（名字、类型、归属 Label），可搜索、定位。
- **UI**：导航栈（Back / 清空）、已创建面板（显示 / 隐藏 / 定位），一键 Show `UINameEnum` 中的面板。
- **事件**：`EventAssistant` 全局事件、订阅者列表，可手动触发。

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

位置：[HowFrameExample](../HowFrameExample)

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

- **新模块**：在 `HowTools/HowXxx/` 下新增助手类，统一提供 `Wake()`、`namespace HowFrame`，并在 `HowInit.InitCore` 里用 `WakeSafe` 注册。
- **新编辑器工具**：需要的路径加到 `HowFrameSettings`，窗口里用 `HowFrameEditorGUI.PathInfo` 展示，不要写死 `Assets/...`。
- **新枚举**：走 `EnumCreator`，禁止手改生成的 `*.cs`。
- **新语言**：在 `LangTypeEnum` 加项 + 语言 JSON 目录（默认 `StreamingAssets/Languages/`）下放对应 JSON。
- **新 UI**：在 `UINameEnum` 注册 → Prefab 放 `GameRes/UI/` 并打 Addressables Label。
- **新依赖**：放入 `Assets/Packages/`（已使用本地包形式），并在 `Packages/manifest.json` 中登记。

---

## 10. 相关文档索引

- [HowEnum 系统详解](../HowFrame/HowEnum/HowEnumExplain.txt)
- [EditorPython / ExcelJson README](../Editor/EditorPython/ExcelJson/README.md)
- [GameInit 入口](../HowFrame/GameInit.cs)
- [HowInit 总初始化](../HowFrame/HowTools/HowInit/HowInit.cs)
- [EnumCreator 窗口](../Editor/HowEditor/EnumCreator/EnumCreator.cs)
- [HowFrame 全局配置](../Editor/HowEditor/Settings/HowFrameSettings.cs)

---

> 最后更新：2026-11-18
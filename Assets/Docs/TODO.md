# HowFrame 待办事项（TODO）

> 本文档汇总框架运行时与编辑器拓展的可改进方向，按"价值/工作量"分成三个优先级。每个条目都给出涉及模块、建议改动与验收要点，便于后续直接挑拣动手。

---

## 0. 阅读指引

- 标记 ✅ = 已完成　🚧 = 进行中　📌 = 已纳入计划
- 标记 **[P0/P1/P2]** = 优先级
  - **P0**：影响面大、改动可控、立刻受益。
  - **P1**：明显提升体验或稳定性，但需要较多设计/重构。
  - **P2**：锦上添花，长期演进。
- 每条任务独立成项，可单独领取、单独验收。

---

## 1. 框架运行时 TODO

### 1.1 HowEnum 类型安全枚举系统

- [ ] **[P0]** IDE/LSP 友好化：将 `EnumKey<TTag>` 生成成 `partial class` 形式 + `nameof()` 常量，保留类型安全同时让 IDE 可跳转、重命名、查找引用。
  - 涉及：`Assets/Editor/HowEditor/EnumCreator/EnumGenerater.cs`
- [ ] **[P0]** EnumCreator 树形编辑：嵌套分组改为 TreeView，可折叠/拖拽。
- [ ] **[P0]** 引用扫描 + 重命名修复：导出前扫描所有 `EnumKey.xxx` 使用点，重命名时自动修复引用，避免全工程编译错。
- [ ] **[P1]** `HowInit` 中加入 `EnumKey.VerifyAll()`：校验 JSON 配置 ↔ 生成代码一致性，防止漏导出。
- [ ] **[P1]** 跨枚举桥接 `EnumKey.Convert<TFrom, TTo>(string)`：用字符串做中间格式做受控转换。
- [ ] **[P2]** 枚举项 ↔ 语言 key 映射：枚举创建时可关联 `LangModuleEnum` + key，自动写进 `Languages/*.json`。

### 1.2 HowAsset 资源系统

- [ ] **[P0]** `LoadLabelsAsync` 增加进度回调：`onProgress(label, percent)` + `EventAssistant` 事件，便于启动页显示加载条。
  - 涉及：`Assets/HowFrame/HowTools/HowAsset/AssetAssistant.cs`
- [ ] **[P1]** 缓存自动回收：当前缓存只有手动 `ReleaseLabels`。引入 LRU + 引用计数，长时间未用自动卸载。
- [ ] **[P1]** 统一按路径加载：把 `Resources/` 与 `StreamingAssets/` 差异藏掉，提供 `AssetPath.Get<T>(relativePath)`，调用方无需关心来源。
- [ ] **[P1]** 远端校验钩子：接入 Addressables `CheckForCatalogUpdates`，启动时检测并触发远端热更。
- [ ] **[P2]** 加载失败重试 + 失败上报：网络抽风时自动重试 N 次，最终失败走 `EventAssistant` 错误总线。

### 1.3 HowUI UI 管理系统

- [ ] **[P0]** UI 栈管理：在 `UIManager` 字典之上加 `Stack<EnumKeyBase>`，支持 `Back / Push / Pop`。
  - 涉及：`Assets/HowFrame/HowTools/HowUI/UIManager.cs`
- [ ] **[P1]** 层级与模态：Canvas 下子层级排序、模态遮罩统一处理（`Modal Mask`）。
- [ ] **[P1]** 动效生命周期抽象：把开/关动画从 `WhenShow/WhenHide` 解耦，提供 `ITransition`。
- [ ] **[P1]** 数据驱动：面板绑定 `Model` 后自动订阅 `PropertyAssistant`，减少样板。
- [ ] **[P2]** UI 锚点/适配策略：抽象 `IUIAnchorStrategy`，避免每个面板重复写 `RectTransform`。

### 1.4 HowEvent 事件系统

- [ ] **[P1]** Pipeline 中间件：在 `Order` 之上做 `EventPipeline`，按阶段过滤/拦截/短路。
- [ ] **[P1]** 线程安全：当前 `Dictionary` 非线程安全，后台线程 `Subscribe` 易炸。分段锁或主线程派发。
- [ ] **[P1]** 异步事件：支持 `Func<T, Task<TResult>>`，方便 IO 流程。
- [ ] **[P2]** 事件追踪（Editor only）：订阅 / 派发 / 取消全部打点，便于排查循环派发。

### 1.5 HowInput 输入系统

- [ ] **[P1]** 复合手势库：在 `InputAssistant` 之上加 `Swipe / Pinch / LongPress / DoubleTap`。
- [ ] **[P1]** 输入录制/回放：测试场景录制输入并重放，便于复现 bug。
- [ ] **[P2]** 重绑键 UI：把 InputAction Asset 暴露给运行时，允许玩家重映射。
- [ ] **[P2]** 触屏虚拟摇杆：在 `HowInputActions.inputactions` 基础上生成虚拟摇杆/按键组件。

### 1.6 HowData 数据持久化

- [ ] **[P0]** 数据版本迁移：定义 `IDataMigrator<T>`，读取旧存档时按版本链升级。
  - 涉及：`Assets/HowFrame/HowTools/HowData/DataAssistant.cs`
- [ ] **[P1]** 加密升级：当前仅 `XOR`，加 AES + 密钥派生。
- [ ] **[P1]** 云同步钩子：暴露 Save/Load 事件，便于接 Steam Cloud / iCloud / Google Play。
- [ ] **[P2]** 存档快照 / 回滚：损坏时自动回滚到上一份快照。

### 1.7 HowAudio 音频系统

- [ ] **[P1]** 3D 音源池化：`AudioPool` 已有雏形，补 spatial blend / occlusion。
- [ ] **[P1]** 音频事件总线：把 `AddSound` 包装成 `EventAssistant.Send(AudioEventEnum.X)`，便于策划配表驱动。
- [ ] **[P1]** 混音快照：`AudioMixerSnapshot` 切换（战斗 / UI / 暂停 三态）。
- [ ] **[P2]** 音频预算：限制同时播放的音效数量，超出排队而非硬塞。

### 1.8 HowSceneLoad 场景加载

- [ ] **[P1]** Loading 界面管理：与 `UIManager` 协作，自动显示/隐藏 loading UI。
- [ ] **[P1]** 场景预热 + GC 调度：切场景前后主动 GC / 异步加载下一关资源。
- [ ] **[P2]** 多场景叠加：支持 additive load 多场景并存（适用于开放世界）。

### 1.9 HowUpdate / HowCoroutine / HowTime

- [ ] **[P1]** 统一调度器：把三者整合为 `TickScheduler`（Update / FixedUpdate / LateUpdate / Timer），统一注册入口。
- [ ] **[P2]** 帧率自适应：高频更新按帧间隔动态降频，移动端省电。

### 1.10 HowContainer / HowRender / HowDetection

- [ ] **[P2]** Weighted 容器持久化：目前权重是运行时，改为配表驱动。
- [ ] **[P2]** LineRender 编辑器：拖拽节点生成 `LineDataInfo`，免手填。
- [ ] **[P2]** AreaDetection Gizmo：编辑器内可视化区域，免盲调。

### 1.11 HowProperty / Ref 响应式属性

- [ ] **[P1]** 绑定管线：类似 WPF Binding，`Ref<float>` 可直接绑到 TMP_Text 文本、Slider 值等。
- [ ] **[P2]** 脏标记批处理：高频写时合并到下一帧刷新，避免多次回调。

### 1.12 HowThread / HowJobs / HowDefer

- [ ] **[P0]** 统一异步上下文：把 `Task` / `UniTask` / `Coroutine` / `Thread` 包成 `IAwaiter` 接口，让业务侧只写一种 await 风格。
- [ ] **[P1]** 取消令牌（CancelToken）：补 CancellationToken 支持，避免大流程无法中断。

### 1.13 横切关注点

- [ ] **[P0]** 统一日志 / 上报：抽 `HowLog`，支持 Editor / Console / File / Remote 多渠道。
- [ ] **[P0]** 运行时配置中心：把 `GameInit.BoolDic / FloatDic` 升级为强类型 `RuntimeConfig`，支持热更。
- [ ] **[P1]** 框架单元测试基座：新建 `HowTest` 助手 + 一组 PlayMode/EditMode 测试。
- [ ] **[P1]** 文档自动化：从 `///` 注释 + 反射生成 `Docs/` 子文档，跟代码同步。
- [ ] **[P1]** 统一异常处理：抽 `HowException`，在 `Wake()` 链路中兜底，避免单个模块异常导致初始化中断。
- [ ] **[P2]** 性能基准：常用 API 加 Benchmark，Unity Test Runner 跑回归曲线。

---

## 2. 编辑器拓展 TODO

### 2.1 EnumCreator 升级

- [ ] **[P0]** 树形编辑（重复见 1.1，跨条目联动）
- [ ] **[P0]** 引用扫描 + 重命名修复（重复见 1.1）
- [ ] **[P1]** 导出前 Diff 预览：覆盖前显示代码 Diff，避免手改注释被吃掉。
- [ ] **[P2]** 多语言键映射（重复见 1.1）

### 2.2 AutoUI 自动绑定窗口

- [ ] **[P1]** 节点命名规范检测：扫描 Prefab，提示未按 `Button_xxx` / `Text_xxx` 命名的节点。
- [ ] **[P1]** 字段类型推断：按节点名（`Button` / `Text` / `Image`）自动生成对应 `View` 字段类型，目前模板偏死板。
- [ ] **[P2]** 与 `UIManager` 双向跳转：点击 `View.BindButton` 跳到 `UIManager.Show` 调用点。
- [ ] **[P2]** 模板可配置：不同项目风格不同模板，支持项目级 JSON 配置。

### 2.3 PathCreator / FileMover / KillAsset

- [ ] **[P1]** 批量预览：移动 / 删除前弹 Diff 预览。
- [ ] **[P1]** 依赖图可视化：`KillAsset` 输出"被谁引用"图，不只是文本列表。
- [ ] **[P2]** 批量重命名 + 引用修复：改名后批量修复场景/脚本里的 GUID / 路径引用。

### 2.4 ExcelJson / LanguageConfiger

- [ ] **[P1]** 字段类型推导：Excel 列头加类型注释（如 `id:int`, `name:string#desc`）。
- [ ] **[P1]** 多 Sheet 合并：Excel 多 Sheet → 多 JSON，按文件名分发。
- [ ] **[P1]** 校验 / 预览面板：导出前在 Editor 内看 JSON Diff。
- [ ] **[P2]** 改动热重载：CSV/Excel 改动保存后自动重新导入并触发脚本刷新。

### 2.5 EditorPython 桥接

- [ ] **[P1]** 统一脚本调度器：每个工具独立 `Process`，改成统一后台管理、可中断、可看日志。
- [ ] **[P1]** Python 环境检测：提示用 venv，避免依赖系统 Python。
- [ ] **[P2]** Editor Web 面板：Editor 内显示 Python 进度条 / 日志窗格。
- [ ] **[P2]** 工具注册中心：把 `PyCaller` 抽成注册式，方便新增 Python 脚本。

### 2.6 AddressableBuilder

- [ ] **[P1]** 差异分析：增量构建时给出"哪些资源改了 → 影响了哪些 Group → 估算包体"。
- [ ] **[P1]** 构建模板：Debug / Release / Hotfix 三套配置 + 一键切换。
- [ ] **[P2]** 资源使用统计：Editor 内查看某资源在多少 Prefab/Scene/AssetBundle 中被引用。
- [ ] **[P2]** 远端构建钩子：把构建产物自动推到 CDN / OSS。

### 2.7 新增编辑器工具候选

- [ ] **[P1]** ConfigViewer：JSON / Excel / MessagePack 数据可视化检视器（类似 inspector）。
- [ ] **[P1]** ReferenceGraph：资源 / 资产 / 脚本之间的引用图谱，排查循环依赖。
- [ ] **[P2]** HotfixConsole：打 AB 包 → 拷贝到 StreamingAssets 工作流自动化。
- [ ] **[P2]** LocalizationLivePreview：Editor 内拖语言下拉，实时预览 UI 文本。
- [ ] **[P2]** ProfilerShortcut：把 `Profiler.GetCounterValue` 关键指标绑到独立窗口，开发期持续观察。

---

## 3. 推荐先做的「高 ROI 小步快跑」

按"影响大、改动小、立刻受益"筛选：

1. **[P0] EnumCreator 树形编辑 + 引用扫描**：彻底解决"改枚举名 = 全工程编译错"的痛点。
2. **[P0] UIManager 栈 + 动效抽象**：所有业务面板立刻受益。
3. **[P0] `LoadLabelsAsync` 进度事件 + 进度 UI**：启动页体验大幅提升。
4. **[P0] 统一异步上下文（Cancel/UniTask/Coroutine）**：为热更/网络铺路。
5. **[P0] 文档自动化**：让 `Docs/` 自己"长"出来，不再靠手维护。

---

## 4. 状态追踪

> 当一个条目完成时，把前面的 `[ ]` 改成 `[x]`，并在文末"变更记录"补一行。

<!--
示例：
- [x] [P0] EnumCreator 树形编辑（2026-09-07 完工）
-->

### 4.1 变更记录

- 2026-09-07：初稿，从框架解读梳理出 12 类运行时 TODO + 7 类编辑器 TODO。

---

> 最后更新：2026-09-07
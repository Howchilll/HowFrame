# HowFrame Overview

> A lightweight collection of game-framework modules for Unity, built around "assistant classes + static methods + type-safe enums", with an emphasis on decoupled modules, low-friction APIs and editor automation.

> This document lives in `Assets/Docs/` and is internal project documentation. It only covers what is inside `Assets/`.

---

## 1. Purpose and Goals

- **What it is**: a ready-to-use runtime toolkit plus editor extensions for Unity.
- **Style**: static / singleton helpers named `*Assistant` / `*Manager`, all lazily initialized through `Wake()`.
- **Goals**:
  - Remove repetitive boilerplate (assets, events, UI, localization, input, persistence…)
  - Provide a type-safe enum system instead of hard-coded C# `enum`s
  - Provide visual editor tools that generate code / JSON to keep maintenance cheap
  - Keep gameplay logic decoupled from engineering infrastructure

---

## 2. Top-level `Assets/` Layout

```
Assets/
├── HowFrame/                # Framework runtime core
│   ├── HowEnum/             # Type-safe enum system
│   ├── HowPath/             # Global path constants
│   └── HowTools/            # Feature modules (HowAsset / HowUI / HowEvent ...)
├── HowFrameExample/         # Usage examples (recommended starting point)
├── HowFrameTests/           # EditMode tests (asmdef: HowFrame.Tests.Editor)
├── Editor/                  # Editor extensions
│   ├── AutoUI/              # UI auto-binding window + code templates
│   ├── EditorPython/        # Toolchain for calling Python scripts from C#
│   ├── HowEditor/           # Custom windows (asmdef: HowEditor, editor only)
│   │   └── Settings/        # HowFrame global settings (Project Settings > HowFrame)
│   └── HowEditorConfig/     # Editor config data (enum JSON, path JSON)
├── GameRes/                 # Addressables assets (Audio / Instance / UI)
├── Resources/               # Assets loaded through Resources (UI, etc.)
├── Scenes/                  # Example scenes
├── StreamingAssets/         # Streaming assets (language packs, config tables)
├── Packages/                # Third-party packages (vendored as local packages)
└── Docs/                    # Internal docs (this folder)
```

---

## 3. Startup and Initialization

Entry component: [GameInit.cs](../HowFrame/GameInit.cs)

```text
GameInit.Awake()
   └── AwakeAsync()
         ├── Inject BoolDic / FloatDic
         ├── HowInit.Init()             // Framework init (repeated calls share one task)
         │     ├── AssetAssistant.LoadLabelsAsync(HowInit.InstanceLabel = "Instance")
         │     └── WakeSafe(...)        // Wake each module; one failure is logged, not fatal
         │           Debug → Type → Mono → Coroutine → Update → Property
         │           → Data → Lang → Shader → Audio → Input → SceneLoad → UI → Console
         ├── LangManager.SetLanguage(...)                     // Load language pack
         └── AssetAssistant.LoadLabelsAsync(null, onProgress, resourcesTags) // Preload game assets (GameInit.Progress)
```

### 3.1 Key design: lazy initialization (`Wake()`)

- Most assistants expose `Wake()`, called centrally from `HowInit.Init()`.
- An internal `_initialized` flag prevents double initialization.
- Assets such as `Canvas` / `AudioMixer` / `InputActionAsset` only become available once Addressables has finished loading.
- If a module's `Wake` throws, `HowInit` logs it and keeps initializing the rest (`HowInit.FailedModules` / `FailedModuleNames`).

### 3.2 Waiting for initialization

Initialization is asynchronous. **Never block the main thread waiting for it**: Addressables / UnityWebRequest progress on the main thread, so blocking deadlocks (the old `blockOnAwake` option was removed).

| Need | API |
| --- | --- |
| Run something once the framework is ready | `HowInit.WhenReady(() => ...)` / `await HowInit.Ready` / `HowInit.IsReady` |
| Run something once the game (framework + language + game assets) is ready | `GameInit.WhenReady(() => ...)` / `await GameInit.Ready` / `GameInit.IsReady` |
| Loading-screen progress bar | Read `GameInit.Progress` (0–1) |

`doneHide` / `doneShow` are optional.

---

## 4. Core Modules

### 4.1 HowEnum — type-safe enums

> Details: [HowEnumExplain.txt](../HowFrame/HowEnum/HowEnumExplain.txt)

**Core types**:

```csharp
public record EnumKeyBase { public string name; }
public record EnumKey<TTag> : EnumKeyBase { ... }
```

- Every enum class has a nested `Tag` type used as a type label, so different enums can't be mixed up at compile time.
- Nested groups are supported (e.g. `PlayerEnum.Body.Hight`, value `"Body.Hight"`).
- `Convert(string)` and `GetAll()` can be generated optionally.

**Current enums**:

| Class | Purpose |
| --- | --- |
| `PlayerEnum` | Player attributes (HP/SP/EXE/Body…) |
| `GlobalEventEnum` | Global events |
| `LangModuleEnum` | Language modules (UI/ItemInfo/Default) |
| `LangTypeEnum` | Languages (Chinese/English/Malayu) |
| `InputEnum` | Input device types |
| `NetEnum` | Network protocol |
| `UINameEnum` | UI panel names (this repo: `TestPanel`, `ExamplePanel`) |

**Editor tool**: [EnumCreator](../Editor/HowEditor/EnumCreator/EnumCreator.cs)
- Open the visual editor via `Tools > Enum Creator`.
- Exports the JSON config and the C# enum class in one click.

---

### 4.2 HowPath — global path constants

Location: [HowFrame/HowPath](../HowFrame/HowPath)

- `GlobalPath`: runtime paths such as StreamingAssets, Lang, Config.
- `ResourcePath`: Resources asset paths.
- Paths used by editor tools are not here (the old `EditorPath` was removed); they live in the HowFrame global settings, see 5.0.
- `UINames`: UI panel name constants (used together with `UINameEnum`).

Generated by the [PathCreator](../Editor/HowEditor/PathCreator/PathCreator.cs) editor tool.

---

### 4.3 HowAsset — asset loading

Location: [AssetAssistant.cs](../HowFrame/HowTools/HowAsset/AssetAssistant.cs)

| Method | Source | Purpose |
| --- | --- | --- |
| `ImportAsset<T>(relativePath)` | StreamingAssets | Cross-platform file loading (audio / texture / text) |
| `LoadAsset<T>(fileName)` | Resources | Synchronous load |
| `AddressAsset<T>(address)` | Addressables | Async single-asset load |
| `LoadLabelsAsync(...labels)` | Addressables | Preload and cache by label |
| `LoadLabelsAsync(onComplete, onProgress, ...labels)` | Addressables | Same, with an overall 0–1 progress callback |
| `IsLabelLoaded(label)` | Cache | Whether a label is loaded |
| `AddressableGet<T>(name)` / `TryAddressableGet<T>(name, out asset)` | Cache | Get from cache by name (O(1)) |
| `ReleaseLabels(...)` | Addressables | Unload by label |
| `LoadedLabels` / `LoadingLabels` / `CachedCount` / `CacheEntries()` | Cache | Read-only state (debug window, Doctor) |

**Design notes**:
- `asset.name` is the cache key, so assets are never loaded twice; duplicate names log a warning and only the first one is kept.
- Requesting a label that is loaded or currently loading reuses it instead of loading again.
- Each cache entry records the labels that own it: an asset shared by several labels leaves the cache only after all of them are released, so nothing is evicted too early and no released references are left behind.

---

### 4.4 HowUI — UI management

Location: [UIManager.cs](../HowFrame/HowTools/HowUI/UIManager.cs) and [PanelBase.cs](../HowFrame/HowTools/HowUI/PanelBase.cs)

**Core abstraction `PanelBase`**:

```csharp
public abstract class PanelBase : MonoBehaviour
{
    protected internal abstract void Init();
    protected internal virtual void WhenShow();
    protected internal virtual void WhenHide();
    protected internal virtual void WhenShowWithParameter(object parameter);
    protected internal virtual void WhenLanguageChanged(); // On first creation and on every language switch
}
```

**`UIManager` features**:
- `Show(...)` / `Hide(...)` / `Check(...)` / `HideAll(...)`, accepting both `string` and `EnumKeyBase`.
- Layered canvases: `Show(name, uiLevel)`; higher levels render later. `HideLevel(level)` closes a whole layer.
- Prefabs are loaded from Addressables automatically, and `PanelBase` is attached via `TypeAssistant` reflection when missing.
- Parent/child panels (`father` parameter) and parameter passing (`WhenShowWithParameter`).
- **Navigation stack**: `Push(name, parameter, uiLevel, hidePrevious)` opens and pushes; `Back()` closes the top and re-shows the previous one (returns false on an empty stack); `BackTo(name)`, `ClearStack()`, `StackTop`, `StackCount`, `StackNames`. Panels opened with plain `Show` are not pushed.
- **Language refresh**: after `LangManager.SetLanguage` completes, `WhenLanguageChanged` is called on every panel.
- If no EventSystem prefab is provided, a default one is created (uses `InputSystemUIInputModule` when only the new Input System is enabled).

**UI auto-binding window**: [AutoUI](../Editor/AutoUI)
- Scans prefab nodes and generates `View/Model/Panel` template scripts in one click.
- The Model gets a `partial void ChangeLanguage()` for text components, and the View template calls it from `WhenLanguageChanged`, so text refreshes automatically on language switch.

---

### 4.5 HowEvent — events

Location: [EventAssistant.cs](../HowFrame/HowTools/HowEvent/EventAssistant/EventAssistant.cs)

Two event buses:
- `EventAssistant`: parameterless `Action`, with `Subscribe / Unsubscribe / Invoke / ClearOne / ClearAll`.
- `EventAssistant<T, TResult>`: generic `Func<T, TResult>` with an argument and a return value.

Features:
- Overloads accept `EnumKeyBase`, so enum keys can be used directly as event IDs.
- In the editor, invoking an unregistered event logs a warning.

Subfolders also provide `Once` (one-shot), `Order` (ordered) and `OnceOrder` (one-shot, ordered) variants.

---

### 4.6 HowLanguage — localization

Location: [LangManager.cs](../HowFrame/HowTools/HowLanguage/LangManager.cs)

- Loads JSON keyed by `LangTypeEnum` (language) × `LangModuleEnum` (module).
- File naming: `{LangName}_{ModuleName}_Lang.json`, stored in `StreamingAssets/{GlobalPath.LangPath}/` (default `StreamingAssets/Languages/`).
- `GetLangContent(Module, key)` / `GetLangContent(key)` / `TryGetLangContent(...)`; a missing key returns the key itself and warns only once.
- Keys found missing at runtime are recorded in `MissingKeys` (per module, kept across language switches) and can be appended to the language Excel files from the Runtime Debugger in one click.
- `CurrentLanguage` (a subscribable `Ref`) and the `OnLanguageChanged` event. A switch loads everything first and then swaps, so you never read half old / half new text.
- JSON is generated from Excel by [LanguageConfiger](../Editor/HowEditor/LanguageConfiger) (menu `Tools/HowFrame/Language Configer`).

---

### 4.7 HowInput — input

Location: [InputAssistant.cs](../HowFrame/HowTools/HowInput/InputAssistant.cs)

- Built on the new Input System's `InputActionAsset` (`HowInputActions`).
- `EnableMap / DisableMap / IsMapEnabled`: manage by ActionMap.
- `BindAction / UnbindAction`: `performed` + `canceled` callbacks, with `EnumKeyBase` support.
- `ReadValue<T>`: read an action's value directly.
- Detects device changes and switches `ControlScheme` (Keyboard&Mouse / Gamepad / Touch), broadcasting `GlobalEventEnum.InputTypeChange` through `InputType` + `PropertyAssistant`.

---

### 4.8 HowAudio — audio

Location: [AudioManager.cs](../HowFrame/HowTools/HowAudio/AudioManager.cs)

- Separate `Music` / `Sound` channels with volume controlled through an `AudioMixer`.
- Music supports fade-out (in real time, so it also fades while paused) and `IsMusicPlaying(key)`. Sound sources are pooled; sources destroyed together with their parent object are skipped.
- Clips come straight from the `AssetAssistant` label cache, whose lifetime AssetAssistant owns (AudioManager no longer unloads assets itself, so shared assets aren't unloaded by accident).
- The same sound on the same parent is de-duplicated while its delay is pending; a failed load doesn't leave a stale de-dup marker.
- Volume 0 maps to -80 dB (no `Log10(0) = -∞`).

---

### 4.9 HowData — persistence

Location: [DataAssistant.cs](../HowFrame/HowTools/HowData/DataAssistant.cs)

| Method | Purpose |
| --- | --- |
| `WriteData(obj, name, isJson, upperPath)` | Write to `persistentDataPath` (MessagePack + XOR, or JSON); writes a temp file first, then replaces |
| `ReadData<T>(...)` | Deserialize; returns default when missing or corrupted |
| `HasData(...)` / `DeleteData(...)` | Check / delete a save |
| `LoadConfig<T>(fileName)` | Synchronously load JSON from `StreamingAssets` (not on Android / WebGL) |
| `LoadConfigAsync<T>(fileName)` | Asynchronously load JSON from `StreamingAssets` (all platforms) |

Binary uses `MessagePack` (contractless, so plain classes serialize without attributes; read and write share one options object) + `Encryption.XOR`; JSON uses `LitJson`.

---

### 4.10 HowSceneLoad — scene loading

Location: [SceneLoadAssistant.cs](../HowFrame/HowTools/HowSceneLoad/SceneLoadAssistant.cs)

A single entry point for async load / unload, with progress callbacks and event broadcasting.

### 4.11 HowCoroutine / HowMono — coroutine and Mono helpers

- `CoroutineAssistant` / `CoroutineHelper`: start coroutines without a `MonoBehaviour`.
- `MonoAssistant` / `MonoHelper`: a global fake Mono, `Invoke` and other common helpers.

### 4.12 HowUpdate — per-frame / timed updates

Location: [HowUpdate](../HowFrame/HowTools/HowUpdate)

- `UpdateAssistant` / `UpdateHelper`: register / unregister per-frame callbacks without writing a Mono.
- `Updater`: the core driver (60-tick base rate). Iterates over a snapshot and wraps each callback in try/catch, so disposing or creating helpers inside a callback, or a callback throwing, never stops the loop.
- `new UpdateHelper(fps)`: any fps from 1 to 60, rounded to an interval of `60 / fps` ticks (20 → every 3 ticks); out-of-range values are clamped with a warning.

### 4.13 HowType — runtime reflection

Location: [HowType](../HowFrame/HowTools/HowType)

- `TypeAssistant.GetType(name)`: get a `Type` by class name (used for UI auto-attach).
- `RuntimeGetAttribute`: custom attribute used together with `TypeAssistant`.

### 4.14 HowProperty / Ref — reactive properties

Location: [HowProperty](../HowFrame/HowTools/HowProperty)

- `PropertyAssistant.SetObj / GetObj`: global registry of reactive objects.
- `Ref<T>`: a reference wrapper that `PropertyAssistant` can observe.

### 4.15 HowThread — threading

Location: [HowThread](../HowFrame/HowTools/HowThread)

- `ThreadAssistant`: named background loop tasks (`StartLoop`) and delayed tasks (`DelayInvoke`); after `Stop` no further callbacks run.
- ⚠ Callbacks run on a **background thread** and must not call Unity APIs directly. An exception in a callback is logged instead of silently killing the thread.

### 4.15.1 HowConsole — in-game command console

Location: [HowConsole](../HowFrame/HowTools/HowConsole)

- Press `` ` `` or `F1` in game to open it (IMGUI, no prefab needed). Enter runs, ↑↓ browse history, Tab completes, Esc closes. Enabled by default only in the editor and Development Builds (`HowConsole.Enabled`).
- Any static method with the attribute becomes a command, with automatic argument conversion (string / int / long / float / double / bool / enum, default values supported; the last string parameter takes the rest of the line):

```csharp
[HowCommand("gold.add", "Add gold")]
static void AddGold(int amount, bool notify = true) { ... }
```

- Register manually with `HowConsole.Register(name, desc, args => ...)`, or run a command from code with `HowConsole.Execute("gold.add 100")`.
- Built-in commands: `help` `clear` `history` `timescale` `lang` `lang.missing` `ui.show` `ui.hide` `ui.back` `ui.list` `event` `assets` `fps`.
- Also captures Unity logs (`CaptureUnityLog`), so errors are visible on device without an editor attached.

### 4.16 HowJObj — JSON wrapper

Location: [JObjAssistant.cs](../HowFrame/HowTools/HowJObj/JObjAssistant.cs)

A lightweight JSON helper built on Newtonsoft.Json's `JObject` (complements `DataAssistant`).

### 4.17 HowContainer — containers

| Submodule | Purpose |
| --- | --- |
| `Buffer/` | `BufferDictionary` / `BufferList`: ring buffers with read/write cursors |
| `ObjectPool/` | `ObjectPool`: generic object pool |
| `Objson/` | Object ↔ JSON conversion |
| `Ref/` | `RefList`: list of reference types |
| `Weighted/` | `WeightedList` / `WightedDictionary`: weighted random picks |

### 4.18 HowDetection — detection
- `AreaDetection`: area triggers / enter and exit.
- `RayManager`: centralized raycast management.

### 4.19 HowRender — rendering
- `CameraEffect`: camera post-processing.
- `LineRender` / `LineManager`: centralized line rendering.

### 4.19.1 HowShader — shader & material manager

Location: [ShaderManager.cs](../HowFrame/HowTools/HowShader/ShaderManager.cs), key type [ShaderProperty.cs](../HowFrame/HowEnum/ShaderProperty.cs)

- **Shader dictionary**: `GetShader(name)` / `TryGetShader` look up the dictionary → the `AssetAssistant` cache → `Shader.Find`, then cache the result. `RegisterShader`, `RefreshFromCache()` (registers every cached shader; called in `Wake`).
- **Material dictionary**: `CreateMaterial(key, shaderName)`, `CloneMaterial(newKey, source)` (source = material key, cached material asset name, or shader name), `RegisterMaterial(key, material)` for external materials, `Apply(renderer, key, slot)` to assign a shared material, `ReleaseMaterial` / `ReleaseAllMaterials` (destroys only materials the manager created).
- **Set parameters by string**: `SetFloat / SetInt / SetColor / SetVector / SetTexture / SetKeyword(key, property, value)`. If `key` is a material key, that material changes; otherwise it's treated as a shader name and **every registered material using that shader** changes (material keys win). Returns the number of materials changed; missing properties warn once.
- `TrySetFromString(key, property, "value", out error)` parses by the property's declared type (`1.5`, `#FF8800`, `1,0.5,0,1`, texture name) — handy for consoles and config tables.
- **Globals**: `SetGlobalFloat / Int / Color / Vector / Texture`, `SetGlobalKeyword`, matching getters.
- **Asset safety**: in the editor, material *assets* are snapshotted before the first change and restored when Play mode exits, so runtime tweaking never rewrites `.mat` files.
- Property names can be strings or generated `ShaderProperty` constants:

```csharp
ShaderManager.CreateMaterial("Hero", ShaderKeys.Lit.ShaderName);
ShaderManager.Apply(heroRenderer, "Hero");
ShaderManager.SetColor("Hero", ShaderKeys.Lit.BaseColor, Color.red);
ShaderManager.SetKeyword("Hero", ShaderKeys.Lit.Keywords.EMISSION, true);
ShaderManager.SetFloat(ShaderKeys.Lit.ShaderName, "_Smoothness", 0.2f); // every registered Lit material
```

- Console commands: `shader.list`, `shader.set <key> <property> <value>`, `shader.keyword <key> <keyword> <on|off>`, `shader.global <property|keyword> <value|on|off>`, `shader.clone <newKey> <source>`. The Runtime Debugger has a **Shader** tab listing registered shaders and materials.

### 4.20 HowTime / HowRandom / HowDebug / HowDefer / HowJobs / HowKeys / HowSingleton / HowSimpleTool

Small utilities whose names say what they do:

- `HowTime`: time / timers.
- `HowRandom`: random numbers / probability.
- `HowDebug`: enhanced logging (with `DebugColor`).
- `HowDefer`: deferred execution (`Defer`).
- `HowJobs`: Job System helpers.
- `HowKeys`: key bindings (`KeyAssistant`).
- `HowSingleton`: `Singleton<T>` / `SingletonMono<T>`.
- `HowSimpleTool`: miscellaneous helpers (`STAssistant`).

---

## 5. Editor Extensions (HowEditor)

Location: [Editor/HowEditor](../Editor/HowEditor)

### 5.0 HowFrame global settings (single place for all path dependencies)

Location: [Editor/HowEditor/Settings](../Editor/HowEditor/Settings). Open via **Edit > Project Settings > HowFrame** or the menu `Tools/HowFrame/Settings`.

Every path used by the framework's editor tools is configured here only, and saved in `ProjectSettings/HowFrameSettings.asset` (under version control and shared by the team; defaults from code apply until it is first saved). Tool windows show the current paths read-only, with a button that opens these settings.

| Group | Settings | Used by |
| --- | --- | --- |
| Python | Python executable (empty = search PATH), Py tool output folder | PyCaller, PyTemplateGenerator |
| Enums | JSON folder, C# output folder, namespace, generate Convert / GetAll | EnumCreator |
| Path constants | JSON folder, C# output folder | PathCreator |
| Config tables | Excel folder, JSON folder | ConfigConverter, ExcelJson (Py) |
| Localization | Excel folder, JSON folder | LanguageConfiger, LanguageConfiger (Py) |
| UI | Panel script root, AutoUI template folder (empty = built-in) | AutoUI, UIAutoBind |
| Addressables | Auto-include root folder | AddressableAutoIncremental |
| Shader | Scan folders, excluded folders (default `Assets/Packages`, `Assets/Plugins`), extra shader names, `ShaderKeys.cs` output folder | Shader Keys Generator |
| Boot scene | Scene used by "play from boot scene" (empty = first enabled scene in Build Settings) | PlayFromBoot |
| KillAsset | Target folder, dependency scan roots, ignore rules | KillAsset |

Conventions:
- Relative paths are relative to the **project root** (the parent of `Assets`); absolute paths also work. `HowFrameSettings.ToFullPath()` resolves them.
- Files that **ship with** the framework (AutoUI templates, Python scripts) are never hard-coded; they're located by script name (`HowFrameSettings.GetScriptDir`), so the framework works both under `Assets/` and as a UPM package under `Packages/`.
- If a new editor tool needs a path, add a field to `HowFrameSettings` instead of hard-coding `Assets/...`.

| Module | Menu | What it does |
| --- | --- | --- |
| [EnumCreator](../Editor/HowEditor/EnumCreator) | `Tools/Enum Creator` | Visual enum editor; generates JSON + C# |
| [PathCreator](../Editor/HowEditor/PathCreator) | `Tools/Path Creator` | Generates path constants |
| [ConfigConverter](../Editor/HowEditor/ExcelJson) | `Tools/HowFrame/Config Converter` | Batch Excel ↔ JSON conversion |
| [LanguageConfiger](../Editor/HowEditor/LanguageConfiger) | `Tools/HowFrame/Language Configer` | Scans `GetLangContent` calls to update language Excel files, and exports JSON |
| [FileMover](../Editor/HowEditor/FileMover) | (hidden) | Batch move / delete files; supports `ClassName.FieldName` path-constant references |
| [KillAsset](../Editor/HowEditor/KillAsset) | `Tools/KillAsset` | Removes unreferenced assets from the target folder (confirms first, moves to the OS trash) |
| [AddressableBuilder](../Editor/HowEditor/AddressableBuilder) | automatic | Adds assets under the root folder to Addressables, grouped by first-level subfolder |
| [Doctor](../Editor/HowEditor/Doctor) | `Tools/HowFrame/Doctor (健康检查)` | One-click check of framework conventions, see 5.4 |
| [Runtime Debugger](../Editor/HowEditor/DebugWindow) | `Tools/HowFrame/Runtime Debugger (运行时调试)` | Inspect framework state in Play mode, see 5.5 |
| [Missing References](../Editor/HowEditor/MissingRefs) | `Tools/HowFrame/Missing References (丢失引用)` | Finds missing scripts and references to deleted assets in prefabs / ScriptableObjects / scenes; can remove missing scripts in one click |
| [Shader Keys Generator](../Editor/HowEditor/ShaderKeys) | `Tools/HowFrame/Shader Keys Generator` | Scans project shaders / Shader Graphs (minus excluded folders) plus extra shaders listed in settings (URP Lit / Simple Lit / Unlit / Particles Unlit by default) and generates `ShaderKeys.cs`: `ShaderKeys.Lit.ShaderName`, `ShaderKeys.Lit.BaseColor`, `ShaderKeys.Lit.Keywords.EMISSION` … Also runs headless via `-executeMethod ShaderKeysGenerator.Generate` |
| [PlayFromBoot](../Editor/HowEditor/PlayFromBoot) | `Tools/HowFrame/从启动场景运行` (toggle), `Tools/HowFrame/运行启动场景` (Ctrl+Alt+P) | Pressing Play in any scene enters the boot scene (the one with `GameInit`) first, and returns to the original scene afterwards; the boot scene is set in HowFrame settings |

### 5.1 AutoUI auto-binding window

Location: [Editor/AutoUI](../Editor/AutoUI)

For a prefab:
- Scans nodes → generates `View` (view layer) / `Model` (data layer) / `Panel` (logic layer) templates based on naming.
- Uses the `Template_*.cs.txt` files next to it by default; HowFrame settings can point to a project-specific template folder.

### 5.2 EditorPython (C# ↔ Python bridge)

Location: [Editor/EditorPython](../Editor/EditorPython)

- `PyCaller`: runs Python scripts through `Process`, using the interpreter from HowFrame settings.
- Sub-tools (menu `Tools/PyFunctions/...`):
  - `ExcelJson`: Python Excel ↔ JSON.
  - `LanguageConfiger`: Python language config generation.
  - `ScanScripts`: counts scripts / lines / methods under a folder.
- `PyTemplateGenerator`: generates a skeleton for a new Python tool (C# window + .py + parameters.json) into the Py tool output folder.

Built-in tools take their folders from HowFrame settings; windows only remember the operation mode (EditorPrefs). See each tool's `requirements.txt` for dependencies.

### 5.3 Tests

Location: [HowFrameTests/Editor](../HowFrameTests/Editor). Run from Window > General > Test Runner > EditMode, or from the command line:

```
Unity.exe -batchmode -projectPath <project> -runTests -testPlatform EditMode -assemblyNames HowFrame.Tests.Editor -testResults results.xml
```

Coverage: settings path resolution and built-in file lookup, PathEditor, DataAssistant read / overwrite / delete, LangManager missing-key fallback and recording, EnumKey conversion, UIManager Back on an empty stack, every Doctor check (enum sync incl. groups, language files, duplicate names, Instance assets), append-only language Excel writes, HowConsole parsing / conversion / errors, the missing-reference scanner, and ShaderManager + the shader key generator.

### 5.4 Doctor (health check)

Location: [Editor/HowEditor/Doctor](../Editor/HowEditor/Doctor). Checks:

| Category | What is checked |
| --- | --- |
| Settings paths | Whether each folder in HowFrame settings exists (folders that tools create on first use are not flagged) |
| Python | Whether a Python interpreter can be found |
| Instance assets | `Canvas` / `HowInputActions` (required) and `AudioMixer` (optional) are in Addressables with the `Instance` label; `EventSystem` is optional because the framework has a fallback |
| UI prefabs | Every `UINameEnum` entry has an addressable `.prefab` with the same name, has a label, and has a `PanelBase` (or a `PanelBase` subclass with the same name exists) |
| Duplicate names | Same-named assets sharing a label (the runtime cache is keyed by name, so only one of them can be fetched) |
| Language files | The JSON for every language × module exists, and keys match across languages within a module |
| Enum sync | Enum JSON matches the generated `.cs` (JSON edited but not regenerated) |

Command line / CI: `Unity.exe -batchmode -quit -projectPath <project> -executeMethod HowFrameDoctor.RunFromCommandLine`, which exits with code 1 if there are errors.

### 5.5 Runtime Debugger

Location: [Editor/HowEditor/DebugWindow](../Editor/HowEditor/DebugWindow). Refreshes every 0.5 s in Play mode:

- **Overview**: HowInit / GameInit state and progress, modules whose `Wake` failed, summary stats.
- **Language**: switch language from a dropdown; list of keys missing at runtime, which can be appended to Excel (to the end of every language's table for that module, empty content, existing rows untouched) or copied.
- **Assets**: loaded / loading labels and cached assets (name, type, owning labels), searchable, with ping.
- **UI**: navigation stack (Back / clear), created panels (show / hide / ping), and one-click Show for `UINameEnum` panels.
- **Events**: `EventAssistant` global events and their subscribers, with manual invoke.

---

## 6. Third-party Dependencies (Packages)

Local packages under `Assets/Packages/`:

- **DOTween** — tweening.
- **LitJson** — JSON serialization.
- **MessagePack** + **MessagePack.Annotations** + **MessagePackAnalyzer** — binary serialization.
- **Newtonsoft.Json** — general-purpose JSON.
- **Enums.NET** — enum extensions.
- **NPOI** — Excel read/write.
- **NSax** / **SharpZipLib** — compression.
- **SixLabors.ImageSharp** / **SixLabors.Fonts** — image and font processing.
- **BouncyCastle.Cryptography** / **ExtendedNumerics.BigDecimal** / **MathNet.Numerics** — math and cryptography.
- **Microsoft.IO.RecyclableMemoryStream** / **Microsoft.NET.StringTools** / **System.Collections.Immutable** and other base libraries.
- **TextMesh Pro** — text rendering.

---

## 7. Examples (HowFrameExample)

Location: [HowFrameExample](../HowFrameExample)

Organized as one example per assistant:

- `AssetAssistantExample.cs`
- `AudioManagerExample.cs` / `AudioHelperExample.cs`
- `CoroutineAssistantExample.cs` / `CoroutineHelperExample.cs`
- `DataAssistantExample.cs`
- `DeferExample.cs`
- `HowMonoExample.cs`
- `InputSubcribe.cs`
- `PropertyTest.cs` / `PropertyTest1.cs`
- `UpdateHelperExample.cs`
- `TestPanel/*` / `ExamplePanel.cs` — UI panel examples (Model / View / Panel split).

`ConfigTest.cs` / `Doneshow.cs` show `GameInit` field injection and switching UI once loading is done.

---

## 8. Recommended Adoption Order

1. **Add `GameInit`**: put the `GameInit` component in your boot scene and set `langName` and `resourcesTags`. Turn on `Tools/HowFrame/从启动场景运行` so Play always starts from it.
2. **Define enums**: fill in your game's enums with `EnumCreator` and generate the code.
3. **Prepare paths**: generate path constants with `PathCreator`.
4. **UI**: derive from `PanelBase` and open panels with `UIManager.Show(UINameEnum.X)`.
5. **Data**: use `DataAssistant.ReadData/WriteData` or `LoadConfig`.
6. **Events**: use `EventAssistant.Subscribe(GlobalEventEnum.X, ...)`.
7. **Input**: configure `HowInputActions.inputactions` and use `InputAssistant.BindAction`.
8. **Verify**: run `Tools/HowFrame/Doctor` to check everything is wired up.

---

## 9. Maintenance and Extension Guidelines

- **New module**: add an assistant under `HowTools/HowXxx/` with a `Wake()` in `namespace HowFrame`, and register it in `HowInit.InitCore` via `WakeSafe`.
- **New editor tool**: add any paths it needs to `HowFrameSettings`, show them with `HowFrameEditorGUI.PathInfo`, and never hard-code `Assets/...`.
- **New enum**: go through `EnumCreator`; never edit generated `*.cs` files by hand.
- **New language**: add an entry to `LangTypeEnum` and put the matching JSON in the language JSON folder (default `StreamingAssets/Languages/`).
- **New UI**: register it in `UINameEnum` → put the prefab in `GameRes/UI/` with an Addressables label.
- **New dependency**: put it in `Assets/Packages/` (vendored as local packages) and register it in `Packages/manifest.json`.
- **Syncing with other projects**: `UINameEnum` (and other generated enums) are project data; don't overwrite another project's copy when syncing framework code.

---

## 10. Related Docs

- [HowEnum in detail](../HowFrame/HowEnum/HowEnumExplain.txt)
- [EditorPython / ExcelJson README](../Editor/EditorPython/ExcelJson/README.md)
- [GameInit entry point](../HowFrame/GameInit.cs)
- [HowInit framework init](../HowFrame/HowTools/HowInit/HowInit.cs)
- [EnumCreator window](../Editor/HowEditor/EnumCreator/EnumCreator.cs)
- [HowFrame global settings](../Editor/HowEditor/Settings/HowFrameSettings.cs)
- [TODO / roadmap](TODO.md)

---

> Last updated: 2026-11-18

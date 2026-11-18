# 🎮 HowFrame — a lightweight Unity framework for indie developers

A simple, efficient Unity framework for small, offline games, so you can focus on gameplay and content instead of plumbing.

Built around **static assistant classes**, **type-safe enums** and **editor automation**: most features are one static call away, and the editor tools generate the boring code and data for you.

> Full documentation: [Assets/Docs/README.md](Assets/Docs/README.md) · Roadmap: [Assets/Docs/TODO.md](Assets/Docs/TODO.md)

## 🧩 What's inside

**Runtime (`Assets/HowFrame`)**

| Area | Modules |
| --- | --- |
| Startup | `GameInit` / `HowInit`: async init with ready signals (`WhenReady`, `await Ready`, `Progress`); one failing module never blocks the rest |
| Assets | `AssetAssistant`: StreamingAssets / Resources / Addressables, label-based preloading and caching with progress |
| UI | `UIManager` + `PanelBase`: layered canvases, navigation stack (`Push` / `Back`), automatic language refresh |
| Localization | `LangManager`: language × module JSON, live switching, missing-key fallback and tracking |
| Events & data | `EventAssistant`, `PropertyAssistant` / `Ref<T>`, `DataAssistant` (MessagePack / JSON saves with atomic writes) |
| Audio & input | `AudioManager` (music / pooled sounds via AudioMixer), `InputAssistant` (new Input System, device-change detection) |
| Utilities | Update / coroutine / thread helpers, object pools, weighted random, scene loading, and more |
| Debugging | `HowConsole`: in-game command console; turn any static method into a command with `[HowCommand]` |
| Enums | `HowEnum`: type-safe `EnumKey<TTag>` enums generated from JSON |

**Editor (`Assets/Editor`)**

- **Project Settings > HowFrame**: one place for every path the tools depend on
- **Doctor**: one-click health check of framework conventions (also runs from the command line / CI)
- **Runtime Debugger**: inspect init state, asset cache, UI stack, events and missing translations in Play mode
- **Missing References**: find missing scripts and references to deleted assets
- **Play from boot scene**: press Play anywhere, start from the scene that holds `GameInit`
- Generators: Enum Creator, Path Creator, AutoUI (View / Model / Panel scripts from a prefab), Excel ↔ JSON config and language tools, Addressables auto-grouping, unused-asset cleanup

## 🚀 Quick start

1. Open the project in **Unity 6** (6000.4).
2. Add the `GameInit` component to your boot scene and enable `Tools/HowFrame/从启动场景运行` ("play from boot scene").
3. Put framework assets (`Canvas`, `HowInputActions`, optional `AudioMixer`) in Addressables with the `Instance` label.
4. Run `Tools/HowFrame/Doctor` and fix anything it reports.
5. See `Assets/HowFrameExample` and `Assets/Scenes/HowFrameExampleScene` for usage of each module.

```csharp
GameInit.WhenReady(() =>
{
    UIManager.Push(UINameEnum.TestPanel);
    AudioManager.AddMusic("BGM");
});
```

## 🚫 Out of scope

- No hot-update framework
- No networking / server dependencies

→ Aimed at **small, single-player, fast-iterating** projects.

## 👤 Who it's for

- Indie developers
- Unity learners
- Small teams building prototypes or internal tool frameworks

---

✨ **Like it? Give it a Star ⭐️ so it's easy to find when new features land!**

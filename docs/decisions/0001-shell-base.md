# 0001. What the shell is built on

Status: **proposed**, 2026-11-24. The maintainer decides.

## Context

The rule of the platform is to look for what exists before writing it ([AGENTS.md §4](https://github.com/fsharp-wasm/fsharp-wasm/blob/develop/AGENTS.md)). A shell that gives a web application a window on six operating systems,
packages it, and runs the device APIs through a bridge exists several times over. It is also the part of the platform that is least about F#.

## Options

1. **Build on an existing shell that uses the system web view.** The leading candidate is [Tauri](https://tauri.app/) (Rust core, desktop and, in version 2, iOS and Android, with a plugin system); also
   [Capacitor](https://capacitorjs.com/) (mobile web views, plugins) and [Wails](https://wails.io/) (Go, desktop). We would write the app contract (`app.json`), the plugins for the device APIs and the glue, and
   take the windows, the bundling and the platform plumbing as they are. *For*: months less work; the web view of each OS is what the user already has. *Against*: a Rust (or Go) toolchain next to ours; their release
   rhythm; a dependency the platform does not control.
2. **Bundle an engine** (the Electron way). *For*: the same engine everywhere, so the engine check is moot. *Against*: tens of MB per app, desktop only.
3. **Write thin native hosts** (a window and a web view per OS: C# or Win32 for WebView2, Swift for WKWebView, Kotlin for Android, C and GTK for Linux). *For*: nothing foreign, small. *Against*: five hosts to
   maintain, signing and packaging per OS written by us.
4. **A host in F# on .NET** (MAUI, Avalonia). *Against*: the platform is moving away from the .NET runtime; the application is already F#.

## Proposal

Spike option 1 with Tauri, on desktop first, with the accounting grid as the application, and **decide on measurements, not on the table above**:

| Measure | Why |
|---|---|
| size of the packed app per OS, and start-up to first frame | the promise of small modules must survive the shell |
| what the probe says in the web view of each OS | decides which devices are in at all |
| the bridge round trip (page → host → page), and the frame rate of the canvas demo inside the shell | decides what can cross the bridge |
| effort to run the same app on Android and iOS | the point of "universal" |
| licences of what is pulled in (MIT and Apache-2.0 are compatible with ours; the notices go to `NOTICE`) | [AGENTS.md §10](https://github.com/fsharp-wasm/fsharp-wasm/blob/develop/AGENTS.md) |

If the spike shows that the dependency costs more than it saves, option 3 stays open: the app contract and the bridge schema do not depend on which host runs them.

# The shell: architecture (proposal, nothing of it is built)

Written 2026-11-24. Everything below is a proposal to be argued with; each part says what would make it wrong.

## What the shell is for

The applications of the platform are web pages that load a WebAssembly GC module. A page has no icon on a home screen, no store, no window of its own, no way to
start offline, and reaches the device only as far as the browser lets it. The shell adds exactly that and nothing about how the application is written: the
module and its assets do not change.

## The parts

| Part | What it is | Proposal |
|---|---|---|
| **App package** | what is shipped: the module, the assets and `app.json` | `app.json` names the application, its version, its entry (the module and its start export), its window defaults, and the **capabilities** it asks for (`haptics`, `camera`, …). The shell grants only those. |
| **Host** | the native process per target | creates the window or activity, loads the app from its own origin (not `file:`), forwards lifecycle (pause, resume, back, deep link, memory warning), owns the bridge |
| **Bridge** | how the page and the host talk | a message channel with a request/answer pair and events, JSON on the wire; the schema is the contract of [fsharp-wasm-mobile](https://github.com/fsharp-wasm/fsharp-wasm-mobile). An error is a value (`unsupported`, `denied`, `failed`), as in `Browser.Net`. |
| **Permissions** | what the user and the OS allow | the manifest says what the application may ask; the OS asks the user; the host answers `denied` without asking twice in a session |
| **Packaging** | one build per target | an installer or bundle per desktop OS, an `.apk`/`.aab` and an `.ipa`; signing keys are the maintainer's and never in a repository |
| **Updates** | new versions of the application | the module and assets can be replaced without a store release where the store allows it; the host itself goes through the store |

## Targets

| Target | Web view | Known | To measure |
|---|---|---|---|
| Windows | WebView2 (Chromium) | follows the Evergreen runtime | probe on a Windows 10 and 11 |
| macOS | WKWebView (WebKit) | depends on the OS version | probe on the macOS releases we support |
| Linux | WebKitGTK | depends on the distribution's version | probe on Debian, Ubuntu, Fedora |
| Android | System WebView (Chromium, updated by the store) | follows Chrome | probe on three Android versions |
| iOS | WKWebView (WebKit) | depends on the OS version | probe on the iOS versions we support |

"Depends on the OS version" is the point of the probe: a shell can only promise what its web view runs. The first thing the shell does at start-up is the probe's engine check, and it says plainly
(not with a blank window) when the device is too old.

## What would make this wrong

- A web view that runs our modules but not fast enough (measure with the benchmark of the runtime, in the web view and not in a browser).
- A bridge that is too slow for what applications do with it (frames, pointer events): measure the round trip before building 30 APIs on it.
- A permission model that the stores reject: read the store rules for each OS before the packaging step, not after.

# WasmOS

One F# program, four systems: a small desktop that is **Windows, macOS, Android or a Linux desktop**. Windows you drag, resize, minimise, maximise and close; a launcher; a menu (the start menu,
Launchpad, the app drawer, the activities view); five apps (Notes, Todo, Primes, Task Manager, Settings). It is what the universal shell of this repository is about, shown small: **the same
application in the chrome of the device it runs on**.

| | |
|---|---|
| `Core/` | the model: skins, windows, gestures, apps, `update : Msg -> Model -> Model * Effect list`. Pure, no host; effects (the timed sieve) are data. Compiled to WebAssembly GC with the browser project, and run under .NET |
| `Browser/` | the view and the Elmish loop on `Browser.Html`: the DOM is the same in every system, the class of the screen says which one; what differs in content (a menu bar, a navigation bar) is decided here |
| `Tests/` | 39 checks of the model under .NET: windows and focus, the gestures (from where the press started, kept on the screen), the Android navigation, the apps, the sieve (168 primes below a thousand, 78 498 below a million) |
| the page | `demos/wasmos/index.html` in [fsharp-wasm-runtime](https://github.com/fsharp-wasm/fsharp-wasm-runtime): the CSS that makes each system look as it does, and `tests/e2e/wasmos.mjs`, which drives the compiled module in Chromium (46 checks) |

## Run it

```bash
dotnet run --project demos/wasmos/Tests -c Release          # the model under .NET

# the module, in a checkout of fsharp-wasm-runtime next to this one (a compiler: see its README)
FSHARP_WASM_EMBEDDED_DIR=$PWD node build/build.js --demo wasmos
node tests/e2e/wasmos.mjs
```

## How a skin works

A skin is not a second program. The model knows four things about a system: the size of its screen (`screen`), its name and its class, and a few rules (on Android a launcher press does not minimise, every window is
full screen). The CSS of the page does the rest: where the controls are (right and flat on Windows, three lights on the left on macOS, none on Android), where the launcher is (a taskbar, a dock, a navigation
bar, a column), what the menu covers. So a window's state, the apps' state and the whole of the behaviour are identical in the four, which the tests check once.

## What it is not

It does not talk to a device: there is no bridge yet ([docs/architecture.md](../../docs/architecture.md)), and the "system" is a look, not an emulation. The clock is the browser's. Measured 2026-11-24 in a
headless Chromium without a GPU: the sieve below ten million takes 48 to 61 ms in the module.

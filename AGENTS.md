# AGENTS.md

How we work is written down once, in the compiler repository: **https://github.com/fsharp-wasm/fsharp-wasm/blob/develop/AGENTS.md**. Read it first; it applies here. What matters most:

- **The compiler comes first**: a fault found while building on the stack is fixed in the compiler (fsharp-wasm), never worked around here; this repository is written the natural way.
- Work on `develop`; `main` only by fast-forward after green CI; never rewrite pushed history.
- Author and committer **Sascha Dewald** (the git identity of the machine), with the `Co-Authored-By` trailer.
- Do not write it twice: look for what exists in a lower layer first.
- Claims are measured and dated; answers to the maintainer are in German; code, comments and commits in English.
- Nothing outward-facing (deploy, release, pull request, issue) without his go.

This repository: the universal shell: the native desktop (Windows, macOS, Linux) and mobile (iOS, Android) hosts that run a compiled WebAssembly GC application with its web assets as an app, with the packaging, the bridge to the device and the capability checks. The device APIs themselves are in fsharp-wasm-mobile; this repository hosts them. First artifact: `probe/`, the page that says what a device's web view can run.

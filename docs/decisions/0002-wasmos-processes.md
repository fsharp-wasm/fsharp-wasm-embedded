# 0002. The apps of WasmOS as processes, and the stream between an app and the renderer

Status: **roadmap**, 2026-11-26, from the maintainer ("the apps in the OS are separate wasm modules under our Erlang-style orchestrator; the rendering pipeline needs a streaming protocol, so that the
renderer integrates our TEA streams"). Nothing of it is built; this record says how it fits what exists and in what order it could be built. The maintainer decides.

## Where WasmOS is today

One module, one model: the desktop (windows, launcher, menu) and the five apps (Notes, Todo, Primes, Task Manager, Settings) are one F# program whose model is a pure function (40 checks under .NET) and whose view
states the DOM; the page's CSS makes it look like Windows, macOS, Android or a Linux desktop. An app cannot fail alone, cannot be loaded alone and cannot be written by someone else.

## What exists that the roadmap uses

| Piece | State (read in the code, 2026-11-26) |
|---|---|
| Processes and supervision | `FSharp.Wasm.Actors` (fsharp-wasm-core): a supervisor (one-for-one, one-for-all, restart intensity, give up) and a pool (a mailbox per worker, at-least-once delivery with an idempotent end), as pure state machines whose effects are data; the demos `actor-orchestra` and `worker-actor` of fsharp-wasm-runtime run it with Web Workers as the processes |
| The TEA loop | `FSharp.Graphics.Tea` (graphics) and the Elmish loop of the runtime: `update : Msg -> Model -> Model * Cmd`, a view, subscriptions |
| A stream to a renderer | the FSG command stream, version 2 (`FSharp.Graphics.Render.Protocol`): frames (`BeginFrame` with a sequence number, fills, strokes, text, clips, transforms, layers) for the canvas hosts, with `Diff` and `CachedNode` so that a frame redraws only what changed; for the DOM, the runtime's reconciler |
| Patches as data | `FSharp.Wasm.Graph` (fsharp-wasm-core, 2026-11-26): stamped, invertible `Patch`es of a document, the same graph in any order, a tape; the wire form (text, then binary) is not written yet |
| Capabilities | `apis.json` and the bridge contract of fsharp-wasm-mobile: what an app may ask of the device, checked |

## The proposal

**An app is a process: a module of its own with a TEA contract at its boundary and nothing else.** Two modules cannot share GC objects, so only data crosses, in two streams:

- **in** (shell to app): `Msg` as data. The pointer and the keys in the coordinates of the app's window, focus, resize, and the life cycle (start, suspend, stop). The shell routes; the app never sees another app's events.
- **out** (app to shell): the **view as a stream of patches**. The first message of a process is a full snapshot of its view; afterwards each `update` that changes the view sends the patch (what the diff of the old view
  and the new says), numbered, so that the renderer knows what it has. A restarted process starts with a snapshot again: that is what makes a restart invisible to the window beyond a flicker.

The shell runs a **supervisor** over the processes (the library that exists): an app that traps is restarted with the one-for-one strategy, the window says so, and a process that crashes too often is given up and
shown as such; the OS itself is another process, and one-for-all is what a desktop that cannot live without its menu would ask for. An app's mailbox is the pool's (at-least-once, an idempotent end by message number).

**The renderer integrates the streams.** A window is a surface; the compositor takes each app's patch stream into its surface (a DOM subtree for a DOM app, a canvas region with its clip and transform for a canvas
app; FSG already has clips, transforms and layers) and composes the surfaces in the order of the windows. It acknowledges what it has applied, which gives the process backpressure: an app whose renderer is behind sends
the latest state and not every step.

**It is the patch protocol of the data graph.** The view of an app is a tree of nodes; a patch of it is `Set`, `Insert`, `Remove`, `Move` of nodes, versioned and with an inverse; "events as data" (decision 2 of
`fsharp-wasm-core/docs/data-graph.md`) is the `Msg` stream. So the work is the steps of the data graph that are not built (the scene graph as nodes with the style apart, the wire form of a patch, the transport), with
this as the first customer, and not a protocol of its own next to FSG and the reconciler: FSG stays the way a canvas host draws a frame; the patch stream is what reaches the compositor.

## Order

1. **Two processes in two workers**, the shell with the supervisor (messages as JSON text): Primes alone, as a module of its own, with a view the shell takes as DOM patches; kill it and see it restart. Measures: the
   size of an app module with and without the base library it carries, the start of a process, the round trip of an event to the first patch.
2. **The wire form of a patch** (the graph's `Patch` as text, then binary), numbered, with acknowledgement and a snapshot on (re)start; the same stream under .NET for the tests (a simulated world that crashes, as the actors' tests have).
3. **Every app a module**, the compositor with surfaces, focus and the order of windows; the apps of the shell are the first, a third party's app is the point.
4. **Canvas apps** in a window (the accounting grid as an app of WasmOS): a surface with a canvas, FSG inside the clip of the window.
5. **Capabilities**: an app declares what it may call (`apis.json`), the shell grants and checks.

## Open questions

- Whether an app module carries its own copy of the base library (simple; a few tens of kB each, to be measured) or the shell shares one by linking at the Wasm level (the component model: the compiler can embed a WIT
  world, but linking GC modules is not proven here).
- Whether the view of an app is the DOM (as in WasmOS) or a scene (as in the editors) in the stream: both are trees, and the patch is the same; the compositor has two kinds of surface.
- What a trapped process may keep: nothing (it restarts from its `init`), or the last model, which has to travel out of it and back (a snapshot of the model, not only of the view).

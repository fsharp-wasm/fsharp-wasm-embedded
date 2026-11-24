/// WasmOS in the browser: the view of the model of Core/, in four skins.  The DOM is the same in all of them (a top bar, a desktop with its windows, a launcher, a menu); what makes it
/// Windows, macOS, Android or a Linux desktop is the class of the screen and the CSS of the page, and the little that differs in content (a menu bar, a navigation bar) is decided here.
module Demos.WasmOs

open WasmOs.Model
open Browser.Html
open Browser.Elmish.Core
open Browser.Timing

let private pad2 (n: int) : string = if n < 10 then "0" + string n else string n
let private clockText (m: Model) : string = pad2 (m.Minutes / 60) + ":" + pad2 (m.Minutes % 60)

let private glyph (app: App) : string =
    match app with
    | Notes -> "N"
    | Todo -> "T"
    | Primes -> "π"
    | Tasks -> "M"
    | Settings -> "S"

let private appIcon (app: App) = span { class' ("icon app-" + appClass app); attr "aria-hidden" "true"; glyph app }

let private ctl (dispatch: Dispatch<Msg>) (cls: string) (label: string) (msg: Msg) =
    button {
        class' ("ctl " + cls)
        ariaLabel label
        onClick (fun () -> dispatch msg)
    }

// ── the apps ──

let private notesBody (m: Model) (dispatch: Dispatch<Msg>) =
    div {
        class' "body notes-body"
        textarea {
            class' "notes-text"
            ariaLabel "Notes"
            value' m.Notes
            onInput (fun s -> dispatch (NotesChanged s))
        }
        el "p" { class' "foot"; string (wordCount m.Notes) + " words" }
    }

let private todoBody (m: Model) (dispatch: Dispatch<Msg>) =
    div {
        class' "body todo-body"
        div {
            class' "row"
            input {
                type' "text"
                ariaLabel "New todo"
                placeholder "Add a todo…"
                value' m.TodoInput
                onInput (fun s -> dispatch (TodoInput s))
            }
            button {
                class' "btn"
                onClick (fun () -> dispatch AddTodo)
                "Add"
            }
        }
        ul {
            class' "todos"
            for (id, text, isDone) in m.Todos do
                li {
                    key' (string id)
                    class' (if isDone then "done" else "")
                    button {
                        class' "check"
                        ariaLabel ((if isDone then "Mark as open: " else "Mark as done: ") + text)
                        attr "aria-pressed" (if isDone then "true" else "false")
                        onClick (fun () -> dispatch (ToggleTodo id))
                        if isDone then "✓" else ""
                    }
                    span { class' "text"; text }
                    button {
                        class' "remove"
                        ariaLabel ("Remove " + text)
                        onClick (fun () -> dispatch (RemoveTodo id))
                        "×"
                    }
                }
        }
    }

let private primesBody (m: Model) (dispatch: Dispatch<Msg>) =
    div {
        class' "body primes-body"
        p "The sieve of Eratosthenes, run by the WebAssembly module on this page: count the primes below"
        div {
            class' "row"
            for limit in [ 100000; 1000000; 10000000 ] do
                button {
                    key' (string limit)
                    class' "btn"
                    onClick (fun () -> dispatch (RunPrimes limit))
                    string limit
                }
        }
        match m.Prime with
        | Some r ->
            div {
                class' "result"
                strong (string r.Count)
                span { " primes below " + string r.Limit + ", counted in " + formatMs r.Ms + " ms" }
            }
        | None -> el "p" { class' "foot"; "Nothing counted yet." }
    }

let private tasksBody (m: Model) (dispatch: Dispatch<Msg>) =
    div {
        class' "body tasks-body"
        table {
            thead { tr { th { "Name" }; th { "State" }; th { "" } } }
            tbody {
                for w in m.Wins do
                    tr {
                        key' (string w.Id)
                        td { appIcon w.App; span { " " + appTitle w.App } }
                        td { if w.Minimized then "Minimised" else "Running" }
                        td {
                            button {
                                class' "btn small"
                                ariaLabel ("End task " + appTitle w.App)
                                onClick (fun () -> dispatch (Close w.Id))
                                "End task"
                            }
                        }
                    }
            }
        }
        el "p" { class' "foot"; string (List.length m.Wins) + " windows open, " + skinName m.Skin + " skin" }
    }

let private settingsBody (m: Model) (dispatch: Dispatch<Msg>) =
    div {
        class' "body settings-body"
        h3 "System"
        div {
            class' "row wrap"
            for skin in skins do
                button {
                    key' (skinClass skin)
                    class' "btn"
                    attr "aria-pressed" (if skin = m.Skin then "true" else "false")
                    onClick (fun () -> dispatch (SetSkin skin))
                    skinName skin
                }
        }
        h3 "Accent"
        div {
            class' "row"
            for a in accents do
                button {
                    key' a
                    class' "swatch"
                    ariaLabel ("Accent " + a)
                    attr "aria-pressed" (if a = m.Accent then "true" else "false")
                    attr "style" ("background:" + a)
                    onClick (fun () -> dispatch (SetAccent a))
                }
        }
        h3 "Appearance"
        div {
            class' "row"
            button {
                class' "btn"
                attr "aria-pressed" (if m.Dark then "true" else "false")
                onClick (fun () -> dispatch ToggleDark)
                if m.Dark then "Dark" else "Light"
            }
        }
        el "p" { class' "foot"; "One F# program compiled to WebAssembly GC. The system is a skin: the windows, the apps and the state are the same in all of them." }
    }

let private body (m: Model) (dispatch: Dispatch<Msg>) (app: App) =
    match app with
    | Notes -> notesBody m dispatch
    | Todo -> todoBody m dispatch
    | Primes -> primesBody m dispatch
    | Tasks -> tasksBody m dispatch
    | Settings -> settingsBody m dispatch

// ── windows ──

let private winView (m: Model) (dispatch: Dispatch<Msg>) (front: int option) (w: Win) =
    let cls =
        "win app-" + appClass w.App
        + (if front = Some w.Id then " focused" else "")
        + (if w.Minimized then " min" else "")
        + (if w.Maximized then " max" else "")
    div {
        key' (string w.Id)
        class' cls
        role "dialog"
        ariaLabel (appTitle w.App)
        attr "style" ("left:" + string w.X + "px;top:" + string w.Y + "px;width:" + string w.W + "px;height:" + string w.H + "px;z-index:" + string w.Z)
        onClick (fun () -> dispatch (Focus w.Id))
        div {
            class' "titlebar"
            // the title is the handle: the controls are beside it, because a pressed pointer is captured by the element that took the press and would take the click from a button inside it
            div {
                class' "drag"
                onPointerDown (fun x y -> dispatch (PressTitle(w.Id, int x, int y)))
                onPointerMove (fun x y -> dispatch (PointerMove(int x, int y)))
                onPointerUp (fun _ _ -> dispatch PointerUp)
                onPointerCancel (fun () -> dispatch PointerUp)
                appIcon w.App
                span { class' "title"; appTitle w.App }
            }
            div {
                class' "ctls"
                ctl dispatch "min" "Minimise" (Minimize w.Id)
                ctl dispatch "max" (if w.Maximized then "Restore" else "Maximise") (ToggleMaximize w.Id)
                ctl dispatch "close" "Close" (Close w.Id)
            }
        }
        div { class' "content"; body m dispatch w.App }
        div {
            class' "grip"
            onPointerDown (fun x y -> dispatch (PressGrip(w.Id, int x, int y)))
            onPointerMove (fun x y -> dispatch (PointerMove(int x, int y)))
            onPointerUp (fun _ _ -> dispatch PointerUp)
            onPointerCancel (fun () -> dispatch PointerUp)
        }
    }

// ── the bars of each system ──

let private topBar (m: Model) (dispatch: Dispatch<Msg>) (front: Win option) =
    match m.Skin with
    | Windows -> div { class' "topbar" }
    | MacOs ->
        div {
            class' "topbar"
            span { class' "logo"; "W" }
            strong (match front with Some w -> appTitle w.App | None -> "Finder")
            span { class' "menus"; span { "File" }; span { "Edit" }; span { "View" }; span { "Window" }; span { "Help" } }
            span { class' "spacer" }
            span { class' "clock"; clockText m }
        }
    | Android ->
        div {
            class' "topbar"
            span { class' "clock"; clockText m }
            span { class' "spacer" }
            span { class' "signal"; attr "aria-hidden" "true" }
            span { class' "battery"; attr "aria-hidden" "true" }
        }
    | Linux ->
        div {
            class' "topbar"
            button {
                class' "activities"
                onClick (fun () -> dispatch ToggleMenu)
                "Activities"
            }
            span { class' "spacer" }
            span { class' "clock"; clockText m }
            span { class' "spacer" }
            span { class' "battery"; attr "aria-hidden" "true" }
        }

let private launcherItem (m: Model) (dispatch: Dispatch<Msg>) (front: Win option) (app: App) =
    let running = m.Wins |> List.exists (fun w -> w.App = app)
    let active = (match front with Some w -> w.App = app | None -> false)
    button {
        key' (appClass app)
        class' ("item" + (if running then " running" else "") + (if active then " active" else ""))
        ariaLabel (appTitle app)
        onClick (fun () -> dispatch (Launch app))
        appIcon app
        span { class' "label"; appTitle app }
    }

let private launcher (m: Model) (dispatch: Dispatch<Msg>) (front: Win option) =
    match m.Skin with
    | Android ->
        div {
            class' "launcher"
            button { class' "nav apps"; ariaLabel "All apps"; onClick (fun () -> dispatch ToggleMenu) }
            button { class' "nav back"; ariaLabel "Back"; onClick (fun () -> dispatch Back) }
            button { class' "nav home"; ariaLabel "Home"; onClick (fun () -> dispatch Home) }
            button { class' "nav recents"; ariaLabel "Recent apps"; onClick (fun () -> dispatch Recents) }
        }
    | _ ->
        div {
            class' "launcher"
            button { class' "start"; ariaLabel (if m.Skin = Windows then "Start" else "All apps"); onClick (fun () -> dispatch ToggleMenu) }
            div { class' "items"; for app in apps do launcherItem m dispatch front app }
            span { class' "tray"; span { class' "clock"; clockText m } }
        }

let private menu (m: Model) (dispatch: Dispatch<Msg>) =
    div {
        class' "menu"
        role "menu"
        ariaLabel "Applications"
        h3 (match m.Skin with Windows -> "Pinned" | MacOs -> "Launchpad" | Android -> "Apps" | Linux -> "Activities")
        div {
            class' "grid"
            for app in apps do
                button {
                    key' (appClass app)
                    class' "tile"
                    role "menuitem"
                    onClick (fun () -> dispatch (Open app))
                    appIcon app
                    span { class' "label"; appTitle app }
                }
        }
    }

// ── the program ──

let init () =
    init,
    [ fun dispatch ->
        dispatch (Tick(localMinutes ()))
        setInterval 15000 (fun () -> dispatch (Tick(localMinutes ()))) |> ignore ]

/// The model decides; the effects it names are carried out here, and their answers come back as messages.
let update (msg: Msg) (m: Model) : Model * Cmd<Msg> =
    let m', effects = WasmOs.Model.update msg m
    let run (CountPrimes limit) =
        fun (dispatch: Dispatch<Msg>) ->
            let t0 = highNow ()
            let count = countPrimes limit
            dispatch (PrimesDone(limit, count, highNow () - t0))
    m', List.map run effects

let view (m: Model) (dispatch: Dispatch<Msg>) =
    let sw, sh = screen m.Skin
    let front = focused m
    let frontId = front |> Option.map (fun w -> w.Id)
    div {
        class' "wasmos"
        div {
            class' "skins"
            role "group"
            ariaLabel "System"
            for skin in skins do
                button {
                    key' (skinClass skin)
                    class' "skin"
                    attr "aria-pressed" (if skin = m.Skin then "true" else "false")
                    onClick (fun () -> dispatch (SetSkin skin))
                    skinName skin
                }
        }
        div {
            class' ("stage skin-" + skinClass m.Skin + (if m.Dark then " dark" else ""))
            role "application"
            ariaLabel ("WasmOS, " + skinName m.Skin + " skin")
            attr "style" ("width:" + string sw + "px;height:" + string sh + "px;--accent:" + m.Accent)
            topBar m dispatch front
            div {
                class' "desktop"
                onClick (fun () -> dispatch CloseMenu)
                div {
                    class' "icons"
                    for app in apps do
                        button {
                            key' (appClass app)
                            class' "dicon"
                            onClick (fun () -> dispatch (Open app))
                            appIcon app
                            span { class' "label"; appTitle app }
                        }
                }
                for w in m.Wins do winView m dispatch frontId w
                when' m.MenuOpen (menu m dispatch)
            }
            launcher m dispatch front
        }
    }

let start () =
    run "wasmos-root" { Init = init; Update = update; View = view }

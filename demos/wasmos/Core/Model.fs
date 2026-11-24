/// The model of WasmOS: a small desktop that looks like Windows, macOS, Android or a Linux desktop, from one program.
///
/// Everything here is a pure function of a message and a state, and what has to happen in the world (a timed computation) is data (`Effect`): the same module runs under .NET (the tests)
/// and as WebAssembly GC (the demo).  What the four systems share is the model; what makes them look different is the skin, a few numbers and the CSS of the page.
module WasmOs.Model

type Skin =
    | Windows
    | MacOs
    | Android
    | Linux

type App =
    | Notes
    | Todo
    | Primes
    | Tasks
    | Settings

/// One window.  Position and size are in the pixels of the desktop; a maximised window ignores them (the skin's CSS fills the desktop).
type Win =
    { Id: int
      App: App
      X: int
      Y: int
      W: int
      H: int
      /// the stacking order: the highest of the windows that are not minimised has the focus
      Z: int
      Minimized: bool
      Maximized: bool }

/// What a pressed pointer is doing: the position it started at and the geometry of the window then, so that every move is computed from the start and not accumulated.
type Gesture =
    | Idle
    | Moving of id: int * px: int * py: int * wx: int * wy: int
    | Resizing of id: int * px: int * py: int * w: int * h: int

type Prime = { Limit: int; Count: int; Ms: float }

type Model =
    { Skin: Skin
      Dark: bool
      Accent: string
      Wins: Win list
      NextId: int
      NextZ: int
      Gesture: Gesture
      MenuOpen: bool
      /// minutes since midnight, local time (the browser tells)
      Minutes: int
      Notes: string
      TodoInput: string
      Todos: (int * string * bool) list
      NextTodo: int
      Prime: Prime option }

type Msg =
    | SetSkin of Skin
    | ToggleDark
    | SetAccent of string
    | Open of App
    | Launch of App
    | Close of int
    | Minimize of int
    | ToggleMaximize of int
    | Focus of int
    | PressTitle of id: int * x: int * y: int
    | PressGrip of id: int * x: int * y: int
    | PointerMove of x: int * y: int
    | PointerUp
    | ToggleMenu
    | CloseMenu
    | Home
    | Back
    | Recents
    | Tick of minutes: int
    | NotesChanged of string
    | TodoInput of string
    | AddTodo
    | ToggleTodo of int
    | RemoveTodo of int
    | RunPrimes of int
    | PrimesDone of limit: int * count: int * ms: float

/// What the world has to do for the model: the timed computation.
type Effect = CountPrimes of limit: int

// ── the skins: what differs in numbers (the look is the CSS of the page) ──

/// The size of the screen of a skin, in pixels.
let screen (skin: Skin) : int * int =
    match skin with
    | Android -> 390, 780
    | _ -> 920, 580

let skinName (skin: Skin) : string =
    match skin with
    | Windows -> "Windows"
    | MacOs -> "macOS"
    | Android -> "Android"
    | Linux -> "Linux"

let skinClass (skin: Skin) : string =
    match skin with
    | Windows -> "windows"
    | MacOs -> "macos"
    | Android -> "android"
    | Linux -> "linux"

let skins : Skin list = [ Windows; MacOs; Android; Linux ]
let apps : App list = [ Notes; Todo; Primes; Tasks; Settings ]

let appTitle (app: App) : string =
    match app with
    | Notes -> "Notes"
    | Todo -> "Todo"
    | Primes -> "Primes"
    | Tasks -> "Task Manager"
    | Settings -> "Settings"

let appClass (app: App) : string =
    match app with
    | Notes -> "notes"
    | Todo -> "todo"
    | Primes -> "primes"
    | Tasks -> "tasks"
    | Settings -> "settings"

let private defaultSize (app: App) : int * int =
    match app with
    | Notes -> 420, 300
    | Todo -> 340, 400
    | Primes -> 400, 310
    | Tasks -> 430, 290
    | Settings -> 470, 360

let accents : string list = [ "#0a84ff"; "#8e44ad"; "#e67e22"; "#16a085" ]

/// Primes below `limit`, by the sieve of Eratosthenes: the work of the Primes app (a few milliseconds for a million, in the module).
let countPrimes (limit: int) : int =
    if limit < 2 then 0
    else
        let composite = Array.zeroCreate<bool> (limit + 1)
        let mutable count = 0
        for i in 2 .. limit do
            if not composite.[i] then
                count <- count + 1
                if i <= limit / i then
                    let mutable j = i * i
                    while j <= limit do
                        composite.[j] <- true
                        j <- j + i
        count

/// Milliseconds with one decimal, as text (F#'s `string` of a float can be `5E-05`).
let formatMs (ms: float) : string =
    let tenths = int64 (ms * 10.0 + 0.5)
    string (tenths / 10L) + "." + string (tenths % 10L)

/// The words of a text: runs of characters that are not blank.
let wordCount (text: string) : int =
    let mutable words = 0
    let mutable inside = false
    for i in 0 .. text.Length - 1 do
        let c = text.[i]
        let blank = c = ' ' || c = '\n' || c = '\t' || c = '\r'
        if blank then inside <- false
        elif not inside then
            inside <- true
            words <- words + 1
    words

let init : Model =
    { Skin = Windows
      Dark = false
      Accent = "#0a84ff"
      Wins = []
      NextId = 1
      NextZ = 1
      Gesture = Idle
      MenuOpen = false
      Minutes = 9 * 60 + 41
      Notes = "Welcome to WasmOS.\n\nOne program, four looks: change the system in Settings, or with the buttons above the screen.\nThe windows, the menu and the apps are the same in all of them."
      TodoInput = ""
      Todos = [ 1, "Compile F# to WebAssembly GC", true; 2, "Run it as a desktop", false ]
      NextTodo = 3
      Prime = None }

// ── windows ──

let private mapWin (id: int) (f: Win -> Win) (m: Model) : Model =
    { m with Wins = m.Wins |> List.map (fun w -> if w.Id = id then f w else w) }

let private find (id: int) (m: Model) : Win option = m.Wins |> List.tryFind (fun w -> w.Id = id)

/// The window that has the focus: the highest of those that are not minimised.
let focused (m: Model) : Win option =
    m.Wins |> List.filter (fun w -> not w.Minimized) |> List.sortByDescending (fun w -> w.Z) |> List.tryHead

let private bump (id: int) (m: Model) : Model =
    let m' = mapWin id (fun w -> { w with Z = m.NextZ; Minimized = false }) m
    { m' with NextZ = m.NextZ + 1 }

let private openApp (app: App) (m: Model) : Model =
    match m.Wins |> List.tryFind (fun w -> w.App = app) with
    | Some w -> bump w.Id m
    | None ->
        let n = List.length m.Wins
        let width, height = defaultSize app
        let win = { Id = m.NextId; App = app; X = 130 + 34 * (n % 6); Y = 40 + 30 * (n % 6); W = width; H = height; Z = m.NextZ; Minimized = false; Maximized = false }
        { m with Wins = m.Wins @ [ win ]; NextId = m.NextId + 1; NextZ = m.NextZ + 1 }

let private clamp (lo: int) (hi: int) (v: int) : int = max lo (min hi v)

/// A press on a launcher item: a window that is in front goes away, one that is behind or minimised comes to the front, none is opened.
let private launch (app: App) (m: Model) : Model =
    match m.Wins |> List.tryFind (fun w -> w.App = app), focused m with
    | Some w, Some f when f.Id = w.Id && m.Skin <> Android -> mapWin w.Id (fun w -> { w with Minimized = true }) m
    | _ -> openApp app m

let update (msg: Msg) (m: Model) : Model * Effect list =
    let m1 =
        match msg with
        | SetSkin skin -> { m with Skin = skin; MenuOpen = false; Gesture = Idle }
        | ToggleDark -> { m with Dark = not m.Dark }
        | SetAccent a -> { m with Accent = a }
        | Open app -> { openApp app m with MenuOpen = false }
        | Launch app -> { launch app m with MenuOpen = false }
        | Close id -> { m with Wins = m.Wins |> List.filter (fun w -> w.Id <> id); Gesture = Idle }
        | Minimize id -> mapWin id (fun w -> { w with Minimized = true }) m
        | ToggleMaximize id -> bump id (mapWin id (fun w -> { w with Maximized = not w.Maximized }) m)
        // a press inside a window brings it to the front; a minimised window is not pressed on (its own button's click reaches here after it was minimised): it comes back by the launcher
        | Focus id -> (match find id m with Some w when not w.Minimized && (focused m |> Option.map (fun f -> f.Id)) <> Some id -> bump id m | _ -> m)
        | PressTitle(id, x, y) ->
            (match find id m with
             | Some w -> { bump id m with Gesture = Moving(id, x, y, w.X, w.Y); MenuOpen = false }
             | None -> m)
        | PressGrip(id, x, y) ->
            (match find id m with
             | Some w -> { bump id m with Gesture = Resizing(id, x, y, w.W, w.H) }
             | None -> m)
        | PointerMove(x, y) ->
            let sw, sh = screen m.Skin
            (match m.Gesture with
             | Moving(id, px, py, wx, wy) ->
                 mapWin id (fun w -> { w with X = clamp (120 - w.W) (sw - 120) (wx + x - px); Y = clamp 0 (sh - 90) (wy + y - py) }) m
             | Resizing(id, px, py, w0, h0) ->
                 mapWin id (fun w -> { w with W = clamp 240 (sw - w.X) (w0 + x - px); H = clamp 160 (sh - w.Y) (h0 + y - py) }) m
             | Idle -> m)
        | PointerUp -> { m with Gesture = Idle }
        | ToggleMenu -> { m with MenuOpen = not m.MenuOpen }
        | CloseMenu -> { m with MenuOpen = false }
        | Home -> { m with Wins = m.Wins |> List.map (fun w -> { w with Minimized = true }); MenuOpen = false }
        | Back ->
            (match focused m with
             | Some w -> { m with Wins = m.Wins |> List.filter (fun x -> x.Id <> w.Id); MenuOpen = false }
             | None -> { m with MenuOpen = false })
        | Recents ->
            // the window that has been in the back the longest comes to the front
            (match m.Wins |> List.sortBy (fun w -> w.Z) |> List.tryHead with
             | Some w -> bump w.Id { m with MenuOpen = false }
             | None -> m)
        | Tick minutes -> { m with Minutes = minutes }
        | NotesChanged s -> { m with Notes = s }
        | TodoInput s -> { m with TodoInput = s }
        | AddTodo ->
            let text = m.TodoInput.Trim()
            if text = "" then m
            else { m with Todos = m.Todos @ [ m.NextTodo, text, false ]; NextTodo = m.NextTodo + 1; TodoInput = "" }
        | ToggleTodo id -> { m with Todos = m.Todos |> List.map (fun (i, t, d) -> if i = id then i, t, not d else i, t, d) }
        | RemoveTodo id -> { m with Todos = m.Todos |> List.filter (fun (i, _, _) -> i <> id) }
        | RunPrimes _ -> m
        | PrimesDone(limit, count, ms) -> { m with Prime = Some { Limit = limit; Count = count; Ms = ms } }
    let effects = match msg with RunPrimes limit -> [ CountPrimes limit ] | _ -> []
    m1, effects

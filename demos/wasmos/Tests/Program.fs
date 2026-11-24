// The model of WasmOS under .NET: windows, focus, the gestures, the Android navigation, the apps.   dotnet run --project demos/wasmos/Tests
open WasmOs.Model

let mutable failed = 0
let check (name: string) (ok: bool) =
    printfn "%s %s" (if ok then "✓" else "✗") name
    if not ok then failed <- failed + 1

let run (msgs: Msg list) (m: Model) : Model = msgs |> List.fold (fun acc msg -> fst (update msg acc)) m
let ids (m: Model) = m.Wins |> List.map (fun w -> w.App)
let top (m: Model) = focused m |> Option.map (fun w -> w.App)
let win (app: App) (m: Model) = m.Wins |> List.find (fun w -> w.App = app)

// ── opening, focus, closing ──
let m1 = run [ Open Notes; Open Todo ] init
check "opening two apps makes two windows" (ids m1 = [ Notes; Todo ])
check "the last one opened has the focus" (top m1 = Some Todo)
check "opening an app that is open does not make another window, it brings it to the front" (let m = run [ Open Notes ] m1 in List.length m.Wins = 2 && top m = Some Notes)
check "closing the focused window gives the focus to the next one" (let m = run [ Close (win Todo m1).Id ] m1 in ids m = [ Notes ] && top m = Some Notes)
check "a press inside a window that was just minimised does not bring it back (the click of the minimise button reaches the window after it)" (
    let id = (win Todo m1).Id
    let m = run [ Minimize id; Focus id ] m1
    (win Todo m).Minimized && top m = Some Notes)
check "a press inside a window behind brings it to the front" (let m = run [ Focus (win Notes m1).Id ] m1 in top m = Some Notes)
check "a minimised window does not have the focus" (let m = run [ Minimize (win Todo m1).Id ] m1 in top m = Some Notes)
check "the menu closes when an app is opened from it" (let m = run [ ToggleMenu; Open Tasks ] init in not m.MenuOpen && ids m = [ Tasks ])

// ── the launcher: a press on the item of the window in front sends it away, as on Windows and macOS; on Android nothing is minimised ──
check "a launcher press on the window in front minimises it" (let m = run [ Open Notes; Launch Notes ] init in (win Notes m).Minimized && top m = None)
check "a launcher press on a minimised window brings it back" (let m = run [ Open Notes; Launch Notes; Launch Notes ] init in not (win Notes m).Minimized && top m = Some Notes)
check "a launcher press on a window behind brings it to the front" (let m = run [ Open Notes; Open Todo; Launch Notes ] init in top m = Some Notes)
check "a launcher press opens an app that is not open" (let m = run [ Launch Tasks ] init in ids m = [ Tasks ])
check "on Android a press on the window in front does not minimise it" (let m = run [ SetSkin Android; Open Notes; Launch Notes ] init in not (win Notes m).Minimized)

// ── gestures: moves are computed from where the press started, and stay on the screen ──
let moved = run [ PressTitle((win Notes m1).Id, 100, 100); PointerMove(130, 140) ] m1
check "dragging the title bar moves the window by the distance of the pointer" (let a, b = win Notes m1, win Notes moved in b.X = a.X + 30 && b.Y = a.Y + 40)
check "the move does not accumulate: the same position twice is the same window" (let again = run [ PointerMove(130, 140) ] moved in win Notes again = win Notes moved)
check "a press on the title bar brings the window to the front" (let m = run [ PressTitle((win Notes m1).Id, 0, 0) ] m1 in top m = Some Notes)
check "the window stays on the screen" (let m = run [ PressTitle((win Notes m1).Id, 500, 300); PointerMove(-5000, -5000) ] m1 in let w = win Notes m in w.Y = 0 && w.X + w.W >= 120)
check "releasing the pointer ends the gesture" ((run [ PointerUp ] moved).Gesture = Idle)
check "a pointer move without a press does nothing" (run [ PointerMove(10, 10) ] m1 = m1)
let resized = run [ PressGrip((win Notes m1).Id, 400, 300); PointerMove(450, 340) ] m1
check "dragging the grip resizes the window" (let a, b = win Notes m1, win Notes resized in b.W = a.W + 50 && b.H = a.H + 40)
check "a window cannot be made smaller than 240 by 160" (let m = run [ PressGrip((win Notes m1).Id, 400, 300); PointerMove(-9000, -9000) ] m1 in let w = win Notes m in w.W = 240 && w.H = 160)
check "changing the system ends a gesture" ((run [ SetSkin MacOs ] moved).Gesture = Idle)
check "maximising and back" (let id = (win Notes m1).Id in let a = run [ ToggleMaximize id ] m1 in (win Notes a).Maximized && not (win Notes (run [ ToggleMaximize id ] a)).Maximized)

// ── the Android navigation ──
let three = run [ SetSkin Android; Open Notes; Open Todo; Open Tasks ] init
check "Home minimises every window" (let m = run [ Home ] three in List.forall (fun w -> w.Minimized) m.Wins && top m = None)
check "Back closes the window in front" (let m = run [ Back ] three in ids m = [ Notes; Todo ])
check "Back with nothing open does nothing" (run [ Back ] init = init)
check "Recents brings the window that has been in the back the longest to the front" (top (run [ Recents ] three) = Some Notes)
check "Recents again goes on to the next" (top (run [ Recents; Recents ] three) = Some Todo)

// ── the skins ──
check "every skin has a size, the phone is narrow" (let w, h = screen Android in w < h && fst (screen Windows) > fst (screen Android))
check "the skins have distinct class names" (List.length (skins |> List.map skinClass |> List.distinct) = 4)
check "settings: dark and accent" (let m = run [ ToggleDark; SetAccent "#8e44ad" ] init in m.Dark && m.Accent = "#8e44ad")

// ── the apps ──
check "todo: add, toggle, remove" (
    let m = run [ TodoInput "buy milk"; AddTodo ] init
    let id = m.NextTodo - 1
    List.length m.Todos = 3 && m.TodoInput = "" && (let t = run [ ToggleTodo id ] m in t.Todos |> List.exists (fun (i, _, d) -> i = id && d)) && List.length (run [ RemoveTodo id ] m).Todos = 2)
check "todo: an empty text adds nothing" (run [ TodoInput "   "; AddTodo ] init |> fun m -> List.length m.Todos = 2)
check "notes keep what is typed" ((run [ NotesChanged "x" ] init).Notes = "x")
check "the primes below 1 000, 100 000 and 1 000 000" (countPrimes 1000 = 168 && countPrimes 100000 = 9592 && countPrimes 1000000 = 78498)
check "the primes below 0, 1, 2 and 3" (countPrimes 0 = 0 && countPrimes 1 = 0 && countPrimes 2 = 1 && countPrimes 3 = 2)
check "RunPrimes asks the world to count, and the answer is kept" (
    let m, fx = update (RunPrimes 1000) init
    fx = [ CountPrimes 1000 ] && m = init && (let r = run [ PrimesDone(1000, 168, 0.5) ] init in r.Prime = Some { Limit = 1000; Count = 168; Ms = 0.5 }))
check "formatMs: one decimal" (formatMs 0.04 = "0.0" && formatMs 2.34 = "2.3" && formatMs 12.0 = "12.0" && formatMs 0.96 = "1.0")
check "wordCount" (wordCount "" = 0 && wordCount "  one  two\nthree\t" = 3 && wordCount "x" = 1)
check "the clock is set by Tick" ((run [ Tick 615 ] init).Minutes = 615)

if failed > 0 then
    printfn "\n%d failed" failed
    exit 1
else
    printfn "\nall checks passed"

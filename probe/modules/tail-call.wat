;; Tail calls: `return_call` is how the compiler turns F#'s tail recursion into a loop (every shipped module has dozens).
(module
  (func $count (param $n i32) (param $acc i32) (result i32)
    (if (i32.eqz (local.get $n)) (then (return (local.get $acc))))
    (return_call $count (i32.sub (local.get $n) (i32.const 1)) (i32.add (local.get $acc) (i32.const 1))))
  (func (export "run") (result i32) (call $count (i32.const 100000) (i32.const 0))))

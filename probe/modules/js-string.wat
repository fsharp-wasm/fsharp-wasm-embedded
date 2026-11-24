;; JS String Builtins: the engine provides `wasm:js-string` when the module is compiled with { builtins: ["js-string"] }.  No import object is given
;; for it below: an engine without the proposal cannot link this module at all.
(module
  (import "wasm:js-string" "length" (func $length (param externref) (result i32)))
  (func (export "run") (param externref) (result i32) (call $length (local.get 0))))

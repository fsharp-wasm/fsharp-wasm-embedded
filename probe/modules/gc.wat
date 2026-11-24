;; WebAssembly GC: a struct is allocated by the engine and read back.  Every module of the platform is made of these.
(module
  (type $box (struct (field i32)))
  (func (export "run") (result i32)
    (struct.get $box 0 (struct.new $box (i32.const 7)))))

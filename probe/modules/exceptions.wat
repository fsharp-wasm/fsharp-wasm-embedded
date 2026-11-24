;; Exception handling: a module declares a tag and throws it; the host sees a WebAssembly.Exception (the compiler's `raise`).
(module
  (tag $failure)
  (func (export "run") (throw $failure)))

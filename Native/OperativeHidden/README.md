# Hidden Operative native runtime

This directory builds `libwhiskey_operativo_oculto.so`, a Linux x86-64 ELF
library written in NASM. It owns every Hidden Operative state transition,
cooldown, procedure/conversion decision, patient counter, and positional audio
cadence. The managed bridge supplies immutable ECS facts and
executes the generic command records returned by `operative_hidden_dispatch`.

The public ABI is version 1 and exports only six symbols. Handles are
generation-checked per-instance slots. No CLR pointer, component pointer, wall
clock, or unsynchronized random state is retained by native code.

Build a release library with `make`; use `make CONFIG=debug` for DWARF symbols.
Run `make test` for black-box ABI/gameplay coverage and `make benchmark` for
the reproducible 1k/10k/100k dispatch benchmark.
Generated `.o` and `.so` files remain build artifacts and are not committed.

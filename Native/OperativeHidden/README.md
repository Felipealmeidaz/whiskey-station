# Hidden Operative native runtime

This directory builds `libwhiskey_operativo_oculto.so`, a Linux x86-64 ELF
library written in NASM. It owns every Hidden Operative state transition,
cooldown, procedure/conversion decision, patient counter, and positional audio
cadence. The managed bridge supplies immutable ECS facts and
executes the generic command records returned by `operative_hidden_dispatch`.

The supported runtime target is intentionally restricted to Linux x86-64. On
any other OS/architecture the loader reports the incompatibility and the game
rule fails closed; there is no behaviorally different managed fallback. Local
review from Windows or macOS therefore requires a Linux x86-64 VM, container,
or WSL2 environment. The Linux x64 server packager treats the `.so` as a
required root-level artifact and fails packaging if it is absent, matching the
loader's `AppContext.BaseDirectory` lookup.

The public ABI is version 1 and exports only six symbols. Handles are
generation-checked per-instance slots. No CLR pointer, component pointer, wall
clock, or unsynchronized random state is retained by native code.

Command production and execution use two commit boundaries. Native dispatch
snapshots the complete 160-byte instance state and restores it if its bounded
command buffer saturates, returning the ABI error high bit. The managed bridge
then executes atomic command groups with reverse-order compensation and sends
explicit committed events back to native code; cooldowns, conversion counters,
and terminal procedure state are mutated only by those acknowledgements.
Procedure deadlines are absolute monotonic milliseconds derived from
`IGameTiming.CurTime`, never accumulated `frameTime`.

The native scenario components and objective mirror are server-only. Only the
action events and the two relationship-icon marker components live in shared
code; the marker components are explicitly networked. Actions remain
server-authoritative because prediction cannot execute or rewind this native
state machine or its transactional ECS side effects deterministically.

Build a release library with `make`; use `make CONFIG=debug` for DWARF symbols.
Run `make test` for black-box ABI/gameplay coverage and `make benchmark` for
the reproducible 1k/10k/100k dispatch benchmark.
Generated `.o` and `.so` files remain build artifacts and are not committed.

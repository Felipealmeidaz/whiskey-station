#!/usr/bin/env python3
# SPDX-License-Identifier: MIT

"""Small reproducible dispatch benchmark for the native ABI."""

from __future__ import annotations

import ctypes
import pathlib
import time

from test_native import CAPACITY, EVENT_OBJECTIVE_QUERY, EVENT_SPAWN, NativeCommand, NativeEvent


ROOT = pathlib.Path(__file__).resolve().parents[1]
LIBRARY = ROOT / "build" / "libwhiskey_operativo_oculto.so"


def main() -> None:
    library = ctypes.CDLL(str(LIBRARY))
    library.operative_hidden_initialize.restype = ctypes.c_uint32
    library.operative_hidden_create.argtypes = [ctypes.c_uint64]
    library.operative_hidden_create.restype = ctypes.c_uint64
    library.operative_hidden_dispatch.argtypes = [
        ctypes.POINTER(NativeEvent),
        ctypes.POINTER(NativeCommand),
        ctypes.c_uint32,
    ]
    library.operative_hidden_dispatch.restype = ctypes.c_uint32

    if library.operative_hidden_initialize() != 1:
        raise RuntimeError("native initialization failed")
    handle = library.operative_hidden_create(1001)
    if handle == 0:
        raise RuntimeError("native instance allocation failed")

    commands = (NativeCommand * CAPACITY)()
    spawn = NativeEvent(type=EVENT_SPAWN, handle=handle, self_entity=1001, random=3)
    if library.operative_hidden_dispatch(spawn, commands, CAPACITY) != 5:
        raise RuntimeError("native spawn sanity check failed")

    query = NativeEvent(
        type=EVENT_OBJECTIVE_QUERY,
        handle=handle,
        self_entity=1001,
        input=1,
    )
    for iterations in (1_000, 10_000, 100_000):
        started = time.perf_counter_ns()
        for _ in range(iterations):
            if library.operative_hidden_dispatch(query, commands, CAPACITY) != 1:
                raise RuntimeError("unexpected objective-query command count")
        elapsed = time.perf_counter_ns() - started
        per_dispatch_ns = elapsed / iterations
        rate = iterations * 1_000_000_000 / elapsed
        print(
            f"{iterations:>6} dispatches: {elapsed / 1_000_000:9.3f} ms total, "
            f"{per_dispatch_ns:8.1f} ns/dispatch, {rate:,.0f} dispatches/s"
        )


if __name__ == "__main__":
    main()

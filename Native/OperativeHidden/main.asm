; SPDX-License-Identifier: AGPL-3.0-or-later
default rel

%include "abi.inc"

section .note.GNU-stack noalloc noexec nowrite progbits

section .bss
align 64
operative_states: resb OPERATIVE_MAX_INSTANCES * ST_SIZE

section .rodata
align 4
float_zero:              dd 0.0
procedure_step_seconds:  dd 3.5
procedure_move_tolerance_squared: dd 0.0625

section .text

global operative_hidden_get_abi_version
global operative_hidden_initialize
global operative_hidden_shutdown
global operative_hidden_create
global operative_hidden_destroy
global operative_hidden_dispatch

operative_hidden_get_abi_version:
    mov eax, WHISKEY_NATIVE_ANTAG_ABI_VERSION
    ret

; Initialization and shutdown deliberately clear all native ownership. No
; callback or managed handle survives a round/server lifecycle boundary.
operative_hidden_initialize:
operative_hidden_shutdown:
    lea rdi, [operative_states]
    xor eax, eax
    mov ecx, (OPERATIVE_MAX_INSTANCES * ST_SIZE) / 8
    rep stosq
    mov eax, 1
    ret

%include "memory.asm"
%include "commands.asm"
%include "audio.asm"
%include "spawn.asm"
%include "cooldown.asm"
%include "procedure.asm"
%include "conversion.asm"
%include "combat.asm"
%include "patient.asm"
%include "lifecycle.asm"
%include "dispatch.asm"

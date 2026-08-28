; SPDX-License-Identifier: AGPL-3.0-or-later

; Common command constructors. These macros deliberately describe generic ECS
; operations; semantic combinations remain in the native handlers.
%macro EMIT_POPUP 1
    COMMAND_OPEN CMD_POPUP
    test rdi, rdi
    jz %%done
    mov dword [rdi + CMD_TOKEN], %1
%%done:
%endmacro

%macro EMIT_ACTION_COOLDOWN 2
    COMMAND_OPEN CMD_SET_ACTION_COOLDOWN
    test rdi, rdi
    jz %%done
    mov dword [rdi + CMD_TOKEN], %1
    mov dword [rdi + CMD_VALUE0], %2
%%done:
%endmacro

%macro EMIT_ADD_ACTION 1
    COMMAND_OPEN CMD_ADD_ACTION
    test rdi, rdi
    jz %%done
    mov dword [rdi + CMD_TOKEN], %1
%%done:
%endmacro

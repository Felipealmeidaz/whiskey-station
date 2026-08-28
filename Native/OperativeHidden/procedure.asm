; SPDX-License-Identifier: MIT

event_procedure_action:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    jne .invalid
    mov rax, [r12 + EV_TARGET]
    cmp rax, [r15 + ST_SELF]
    je .invalid
    mov eax, [r12 + EV_FLAGS]
    mov edx, FLAG_TARGET_VALID | FLAG_TARGET_HUMANOID | FLAG_TARGET_IN_MELEE_RANGE
    and eax, edx
    cmp eax, edx
    jne .invalid
    test dword [r12 + EV_FLAGS], FLAG_TARGET_ALIVE | FLAG_TARGET_DEAD
    jz .invalid
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    test rax, rax
    jz .target_ready
    cmp rax, [r12 + EV_TARGET]
    jne .invalid
.target_ready:
    mov ecx, [r15 + ST_REQUIRED_TOOL]
    cmp ecx, 1
    jb .invalid
    cmp ecx, PROCEDURE_TOOL_COUNT
    ja .invalid
    dec ecx
    bt dword [r12 + EV_INPUT], ecx
    jnc .wrong_tool
    mov eax, [r12 + EV_ACTIVE_ITEM]
    test eax, eax
    jz .wrong_tool
    test dword [r12 + EV_FLAGS], FLAG_TARGET_CONVERTED
    jnz .invalid
    test dword [r12 + EV_FLAGS], FLAG_TARGET_PROTECTED
    jnz .invalid
    mov rax, [r12 + EV_SERVER_TICK]
    cmp rax, [r15 + ST_PROCEDURE_READY]
    jb .cooldown
    mov rax, [r12 + EV_TARGET]
    mov [r15 + ST_PROCEDURE_TARGET], rax
    mov [r15 + ST_TARGET], rax
    mov eax, [procedure_step_seconds]
    mov [r15 + ST_PROCEDURE_REMAINING], eax
    mov eax, [r12 + EV_SELF_X]
    mov [r15 + ST_PROCEDURE_SELF_X], eax
    mov eax, [r12 + EV_SELF_Y]
    mov [r15 + ST_PROCEDURE_SELF_Y], eax
    mov eax, [r12 + EV_TARGET_X]
    mov [r15 + ST_PROCEDURE_TARGET_X], eax
    mov eax, [r12 + EV_TARGET_Y]
    mov [r15 + ST_PROCEDURE_TARGET_Y], eax
    mov eax, [r12 + EV_ACTIVE_ITEM]
    mov [r15 + ST_PROCEDURE_TOOL], eax
    mov dword [r15 + ST_STATE], STATE_OPERATING
    EMIT_POPUP TOKEN_POPUP_OPERATING
    ret
.wrong_tool:
    COMMAND_OPEN CMD_POPUP
    test rdi, rdi
    jz .done
    mov eax, [r15 + ST_REQUIRED_TOOL]
    add eax, TOKEN_POPUP_TOOL_CAUTERY - 1
    mov [rdi + CMD_TOKEN], eax
.done:
    ret
.cooldown:
    EMIT_POPUP TOKEN_POPUP_COOLDOWN
    ret
.invalid:
    EMIT_POPUP TOKEN_POPUP_INVALID
    ret

event_update:
    INTERNAL_CALL operative_audio_update
    cmp dword [r15 + ST_STATE], STATE_OPERATING
    jne .done
    mov eax, [r12 + EV_FLAGS]
    mov edx, FLAG_TARGET_VALID | FLAG_TARGET_HUMANOID | FLAG_TARGET_IN_MELEE_RANGE
    and eax, edx
    cmp eax, edx
    jne event_procedure_interrupted
    test dword [r12 + EV_FLAGS], FLAG_TARGET_ALIVE | FLAG_TARGET_DEAD
    jz event_procedure_interrupted
    mov ecx, [r15 + ST_REQUIRED_TOOL]
    cmp ecx, 1
    jb event_procedure_interrupted
    cmp ecx, PROCEDURE_TOOL_COUNT
    ja event_procedure_interrupted
    dec ecx
    bt dword [r12 + EV_INPUT], ecx
    jnc event_procedure_interrupted
    mov eax, [r12 + EV_ACTIVE_ITEM]
    cmp eax, [r15 + ST_PROCEDURE_TOOL]
    jne event_procedure_interrupted
    test dword [r12 + EV_FLAGS], FLAG_TARGET_CONVERTED
    jnz event_procedure_interrupted
    test dword [r12 + EV_FLAGS], FLAG_TARGET_PROTECTED
    jnz event_procedure_interrupted
    ; Physics settling and float integration can move an idle body by a tiny
    ; fraction of a tile. Treat that as stationary, but still interrupt a real
    ; step or any movement that takes either participant out of melee range.
    movss xmm0, [r12 + EV_SELF_X]
    subss xmm0, [r15 + ST_PROCEDURE_SELF_X]
    mulss xmm0, xmm0
    movss xmm1, [r12 + EV_SELF_Y]
    subss xmm1, [r15 + ST_PROCEDURE_SELF_Y]
    mulss xmm1, xmm1
    addss xmm0, xmm1
    comiss xmm0, [procedure_move_tolerance_squared]
    ja event_procedure_interrupted
    movss xmm0, [r12 + EV_TARGET_X]
    subss xmm0, [r15 + ST_PROCEDURE_TARGET_X]
    mulss xmm0, xmm0
    movss xmm1, [r12 + EV_TARGET_Y]
    subss xmm1, [r15 + ST_PROCEDURE_TARGET_Y]
    mulss xmm1, xmm1
    addss xmm0, xmm1
    comiss xmm0, [procedure_move_tolerance_squared]
    ja event_procedure_interrupted
    movss xmm0, [r15 + ST_PROCEDURE_REMAINING]
    subss xmm0, [r12 + EV_VALUE0]
    movss [r15 + ST_PROCEDURE_REMAINING], xmm0
    comiss xmm0, [float_zero]
    ja .done
    cmp dword [r15 + ST_REQUIRED_TOOL], PROCEDURE_TOOL_COUNT
    jae event_conversion_complete
    inc dword [r15 + ST_REQUIRED_TOOL]
    mov dword [r15 + ST_STATE], STATE_ACTIVE
    mov dword [r15 + ST_PROCEDURE_REMAINING], 0
    mov dword [r15 + ST_PROCEDURE_TOOL], 0
    COMMAND_OPEN CMD_POPUP
    test rdi, rdi
    jz .done
    mov eax, [r15 + ST_REQUIRED_TOOL]
    add eax, TOKEN_POPUP_TOOL_CAUTERY - 1
    mov [rdi + CMD_TOKEN], eax
.done:
    ret

event_procedure_interrupted:
    cmp dword [r15 + ST_STATE], STATE_OPERATING
    je .interrupt
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    jne .done
    cmp qword [r15 + ST_PROCEDURE_TARGET], 0
    je .done
.interrupt:
    mov dword [r15 + ST_STATE], STATE_INTERRUPTED
    mov qword [r15 + ST_PROCEDURE_TARGET], 0
    mov dword [r15 + ST_PROCEDURE_REMAINING], 0
    mov dword [r15 + ST_PROCEDURE_TOOL], 0
    mov dword [r15 + ST_REQUIRED_TOOL], 1
    COMMAND_OPEN CMD_CLEAR_ROUTED_TARGET
    EMIT_POPUP TOKEN_POPUP_INTERRUPTED
    mov dword [r15 + ST_STATE], STATE_ACTIVE
.done:
    ret

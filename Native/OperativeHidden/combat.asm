; SPDX-License-Identifier: AGPL-3.0-or-later

event_touch_action:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    jne .invalid
    mov eax, [r12 + EV_FLAGS]
    mov edx, FLAG_TARGET_VALID | FLAG_TARGET_ALIVE | FLAG_TARGET_HUMANOID | FLAG_TARGET_IN_MELEE_RANGE | FLAG_TARGET_CAN_DIE
    and eax, edx
    cmp eax, edx
    jne .invalid
    mov rax, [r12 + EV_TARGET]
    cmp rax, [r15 + ST_SELF]
    je .invalid
    test dword [r12 + EV_FLAGS], FLAG_TARGET_PROTECTED
    jnz .invalid
    mov rax, [r12 + EV_SERVER_TICK]
    cmp rax, [r15 + ST_TOUCH_READY]
    jb .cooldown
    COMMAND_OPEN CMD_SET_MOB_STATE
    test rdi, rdi
    jz .done
    mov dword [rdi + CMD_VALUE0], MOB_STATE_DEAD
    COMMAND_OPEN CMD_NOTIFY_EVENT
    test rdi, rdi
    jz .done
    mov dword [rdi + CMD_TOKEN], EVENT_TOUCH_COMMITTED
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.done:
    ret
.cooldown:
    EMIT_POPUP TOKEN_POPUP_COOLDOWN
    ret
.invalid:
    EMIT_POPUP TOKEN_POPUP_INVALID
    ret

event_touch_committed:
    mov rax, [r12 + EV_SERVER_TICK]
    add rax, TOUCH_COOLDOWN_MS
    mov [r15 + ST_TOUCH_READY], rax
    EMIT_ACTION_COOLDOWN TOKEN_ACTION_TOUCH, TOUCH_COOLDOWN_MS
    ret

event_self_heal:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    jne .invalid
    mov rax, [r12 + EV_SERVER_TICK]
    cmp rax, [r15 + ST_SELF_HEAL_READY]
    jb .cooldown
    COMMAND_OPEN CMD_REJUVENATE_ENTITY
    test rdi, rdi
    jz .done
    mov rax, [r15 + ST_SELF]
    mov [rdi + CMD_TARGET], rax
    COMMAND_OPEN CMD_NOTIFY_EVENT
    test rdi, rdi
    jz .done
    mov rax, [r15 + ST_SELF]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], EVENT_SELF_HEAL_COMMITTED
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.done:
    ret
.cooldown:
    EMIT_POPUP TOKEN_POPUP_COOLDOWN
    ret
.invalid:
    EMIT_POPUP TOKEN_POPUP_INVALID
    ret

event_self_heal_committed:
    mov rax, [r12 + EV_SERVER_TICK]
    add rax, HEAL_COOLDOWN_MS
    mov [r15 + ST_SELF_HEAL_READY], rax
    EMIT_ACTION_COOLDOWN TOKEN_ACTION_SELF_HEAL, HEAL_COOLDOWN_MS
    ret

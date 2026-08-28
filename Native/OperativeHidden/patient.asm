; SPDX-License-Identifier: AGPL-3.0-or-later

event_patient_heal:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    jne .invalid
    mov eax, [r12 + EV_FLAGS]
    mov edx, FLAG_TARGET_VALID | FLAG_TARGET_CONVERTED | FLAG_TARGET_OWN_PATIENT
    and eax, edx
    cmp eax, edx
    jne .invalid
    mov rax, [r12 + EV_SERVER_TICK]
    cmp rax, [r15 + ST_PATIENT_HEAL_READY]
    jb .cooldown
    COMMAND_OPEN CMD_REJUVENATE_ENTITY
    test rdi, rdi
    jz .done
    COMMAND_OPEN CMD_NOTIFY_EVENT
    test rdi, rdi
    jz .done
    mov dword [rdi + CMD_TOKEN], EVENT_PATIENT_HEAL_COMMITTED
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.done:
    ret
.cooldown:
    EMIT_POPUP TOKEN_POPUP_COOLDOWN
    ret
.invalid:
    EMIT_POPUP TOKEN_POPUP_INVALID
    ret

event_patient_heal_committed:
    mov rax, [r12 + EV_SERVER_TICK]
    add rax, HEAL_COOLDOWN_MS
    mov [r15 + ST_PATIENT_HEAL_READY], rax
    EMIT_ACTION_COOLDOWN TOKEN_ACTION_PATIENT_HEAL, HEAL_COOLDOWN_MS
    ret

event_patient_kill:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    jne .invalid
    mov eax, [r12 + EV_FLAGS]
    mov edx, FLAG_TARGET_VALID | FLAG_TARGET_CONVERTED | FLAG_TARGET_OWN_PATIENT
    and eax, edx
    cmp eax, edx
    jne .invalid
    mov rax, [r12 + EV_SERVER_TICK]
    cmp rax, [r15 + ST_PATIENT_KILL_READY]
    jb .cooldown
    COMMAND_OPEN CMD_UNZOMBIFY_ENTITY
    test rdi, rdi
    jz .done
    mov dword [rdi + CMD_TOKEN], TOKEN_ZOMBIE_PATIENT
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC
    COMMAND_OPEN CMD_REMOVE_COMPONENT_BUNDLE
    test rdi, rdi
    jz .unrevivable
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_PATIENT
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.unrevivable:
    COMMAND_OPEN CMD_ADD_COMPONENT_BUNDLE
    test rdi, rdi
    jz .dead
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_UNREVIVABLE
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.dead:
    COMMAND_OPEN CMD_SET_MOB_STATE
    test rdi, rdi
    jz .removed
    mov dword [rdi + CMD_VALUE0], MOB_STATE_DEAD
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.removed:
    COMMAND_OPEN CMD_NOTIFY_EVENT
    test rdi, rdi
    jz .done
    mov dword [rdi + CMD_TOKEN], EVENT_PATIENT_KILL_COMMITTED
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.done:
    ret
.cooldown:
    EMIT_POPUP TOKEN_POPUP_COOLDOWN
    ret
.invalid:
    EMIT_POPUP TOKEN_POPUP_INVALID
    ret

event_patient_kill_committed:
    mov rax, [r12 + EV_SERVER_TICK]
    add rax, PATIENT_KILL_COOLDOWN_MS
    mov [r15 + ST_PATIENT_KILL_READY], rax
    EMIT_ACTION_COOLDOWN TOKEN_ACTION_PATIENT_KILL, PATIENT_KILL_COOLDOWN_MS
    ret

event_patient_created:
    test dword [r12 + EV_FLAGS], FLAG_COUNTER_ACCEPTED
    jz .patient_count
    inc dword [r15 + ST_CONVERSIONS]
.patient_count:
    inc dword [r15 + ST_PATIENTS]
    jmp event_conversion_committed

event_patient_removed:
    cmp dword [r15 + ST_PATIENTS], 0
    je .done
    dec dword [r15 + ST_PATIENTS]
.done:
    ret

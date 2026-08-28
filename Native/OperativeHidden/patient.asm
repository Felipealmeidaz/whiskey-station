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
    mov rax, [r12 + EV_SERVER_TICK]
    add rax, HEAL_COOLDOWN_MS
    mov [r15 + ST_PATIENT_HEAL_READY], rax
    EMIT_ACTION_COOLDOWN TOKEN_ACTION_PATIENT_HEAL, HEAL_COOLDOWN_MS
    ret
.cooldown:
    EMIT_POPUP TOKEN_POPUP_COOLDOWN
    ret
.invalid:
    EMIT_POPUP TOKEN_POPUP_INVALID
    ret

event_patient_kill:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    jne .invalid
    mov eax, [r12 + EV_FLAGS]
    mov edx, FLAG_TARGET_VALID | FLAG_TARGET_CONVERTED | FLAG_TARGET_OWN_PATIENT
    and eax, edx
    cmp eax, edx
    jne .invalid
    COMMAND_OPEN CMD_UNZOMBIFY_ENTITY
    COMMAND_OPEN CMD_REMOVE_COMPONENT_BUNDLE
    test rdi, rdi
    jz .unrevivable
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_PATIENT
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.unrevivable:
    COMMAND_OPEN CMD_ADD_COMPONENT_BUNDLE
    test rdi, rdi
    jz .dead
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_UNREVIVABLE
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.dead:
    COMMAND_OPEN CMD_SET_MOB_STATE
    test rdi, rdi
    jz .removed
    mov dword [rdi + CMD_VALUE0], MOB_STATE_DEAD
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.removed:
    ret
.invalid:
    EMIT_POPUP TOKEN_POPUP_INVALID
    ret

event_patient_created:
    inc dword [r15 + ST_CONVERSIONS]
    inc dword [r15 + ST_PATIENTS]
    ret

event_patient_removed:
    cmp dword [r15 + ST_PATIENTS], 0
    je .done
    dec dword [r15 + ST_PATIENTS]
.done:
    ret

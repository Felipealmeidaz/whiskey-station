; SPDX-License-Identifier: AGPL-3.0-or-later

event_conversion_complete:
    COMMAND_OPEN CMD_ADD_COMPONENT_BUNDLE
    test rdi, rdi
    jz .zombify
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_PATIENT
.zombify:
    COMMAND_OPEN CMD_ZOMBIFY_ENTITY
    test rdi, rdi
    jz .rejuvenate
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_ZOMBIE_PATIENT
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS | COMMAND_FLAG_PRESERVE_VISUAL_SKIN
.ghost_role:
    test dword [r12 + EV_FLAGS], FLAG_TARGET_HAS_SESSION
    jnz .remove_ai
    COMMAND_OPEN CMD_ADD_COMPONENT_BUNDLE
    test rdi, rdi
    jz .remove_ai
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_PATIENT_GHOST
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.remove_ai:
    COMMAND_OPEN CMD_REMOVE_COMPONENT_BUNDLE
    test rdi, rdi
    jz .rejuvenate
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_REMOVE_AI
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.rejuvenate:
    COMMAND_OPEN CMD_REJUVENATE_ENTITY
    test rdi, rdi
    jz .faction
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.faction:
    COMMAND_OPEN CMD_SET_FACTION
    test rdi, rdi
    jz .owner
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_FACTION_SYNDICATE
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.owner:
    COMMAND_OPEN CMD_SET_NATIVE_OWNER
    test rdi, rdi
    jz .state_only
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.state_only:
    ; Counters are committed only after the managed executor confirms every
    ; preceding ECS primitive. The generic notification is queued until the
    ; command buffer has finished, so it cannot re-enter or overwrite it.
    COMMAND_OPEN CMD_NOTIFY_EVENT
    test rdi, rdi
    jz .reset_state
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], EVENT_PATIENT_CREATED
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.reset_state:
    mov rax, [r12 + EV_SERVER_TICK]
    add rax, PROCEDURE_COOLDOWN_MS
    mov [r15 + ST_PROCEDURE_READY], rax
    EMIT_ACTION_COOLDOWN TOKEN_ACTION_PROCEDURE, PROCEDURE_COOLDOWN_MS
    mov qword [r15 + ST_PROCEDURE_TARGET], 0
    mov dword [r15 + ST_PROCEDURE_REMAINING], 0
    mov dword [r15 + ST_PROCEDURE_TOOL], 0
    mov dword [r15 + ST_REQUIRED_TOOL], 1
    mov dword [r15 + ST_STATE], STATE_ACTIVE
    COMMAND_OPEN CMD_CLEAR_ROUTED_TARGET
    test rdi, rdi
    jz .popup
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.popup:
    EMIT_POPUP TOKEN_POPUP_CONVERTED
    test rdi, rdi
    jz .done
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.done:
    ret

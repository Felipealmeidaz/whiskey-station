; SPDX-License-Identifier: AGPL-3.0-or-later

event_conversion_complete:
    ; Stage every compensable mutation before the irreversible upstream zombie
    ; transform. If zombification rejects its profile, the managed executor
    ; removes these bundles in LIFO order and no patient marker survives.
    COMMAND_OPEN CMD_ADD_COMPONENT_BUNDLE
    test rdi, rdi
    jz .ghost_role
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_PATIENT
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.ghost_role:
    test dword [r12 + EV_FLAGS], FLAG_TARGET_HAS_SESSION
    jnz .zombify
    COMMAND_OPEN CMD_ADD_COMPONENT_BUNDLE
    test rdi, rdi
    jz .zombify
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_PATIENT_GHOST
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.zombify:
    ; This is the transaction's irreversible commit point. The patient bundle
    ; already exists so ZombieSystem observes its accent override. Every later
    ; primitive has a validated token/target and cannot reject under this state.
    COMMAND_OPEN CMD_ZOMBIFY_ENTITY
    test rdi, rdi
    jz .remove_ai
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_ZOMBIE_PATIENT
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS | COMMAND_FLAG_PRESERVE_VISUAL_SKIN
.remove_ai:
    COMMAND_OPEN CMD_REMOVE_COMPONENT_BUNDLE
    test rdi, rdi
    jz .rejuvenate
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_BUNDLE_REMOVE_AI
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.faction:
    COMMAND_OPEN CMD_SET_FACTION
    test rdi, rdi
    jz .owner
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_TOKEN], TOKEN_FACTION_SYNDICATE
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.owner:
    COMMAND_OPEN CMD_SET_NATIVE_OWNER
    test rdi, rdi
    jz .state_only
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.rejuvenate:
    ; Rejuvenation is intentionally last among the entity mutations: every
    ; potentially fallible/profile-driven primitive has already succeeded.
    COMMAND_OPEN CMD_REJUVENATE_ENTITY
    test rdi, rdi
    jz .state_only
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.state_only:
    ; Counter, cooldown, target clearing, and state reset are committed only by
    ; EVENT_PATIENT_CREATED after the managed transaction succeeds in full.
    COMMAND_OPEN CMD_NOTIFY_EVENT
    test rdi, rdi
    jz .done
    mov rax, [r15 + ST_PROCEDURE_TARGET]
    mov [rdi + CMD_TARGET], rax
    mov dword [rdi + CMD_VALUE0], TOKEN_COUNTER_CONVERSIONS
    mov dword [rdi + CMD_TOKEN], EVENT_PATIENT_CREATED
    mov dword [rdi + CMD_FLAGS], COMMAND_FLAG_ATOMIC | COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS
.done:
    ret

event_conversion_committed:
    mov rax, [r12 + EV_SERVER_TICK]
    add rax, PROCEDURE_COOLDOWN_MS
    mov [r15 + ST_PROCEDURE_READY], rax
    EMIT_ACTION_COOLDOWN TOKEN_ACTION_PROCEDURE, PROCEDURE_COOLDOWN_MS
    mov qword [r15 + ST_PROCEDURE_TARGET], 0
    mov qword [r15 + ST_PROCEDURE_DEADLINE], 0
    mov dword [r15 + ST_PROCEDURE_TOOL], 0
    mov dword [r15 + ST_REQUIRED_TOOL], 1
    mov dword [r15 + ST_STATE], STATE_ACTIVE
    COMMAND_OPEN CMD_CLEAR_ROUTED_TARGET
    test rdi, rdi
    jz .popup
.popup:
    EMIT_POPUP TOKEN_POPUP_CONVERTED
.done:
    ret

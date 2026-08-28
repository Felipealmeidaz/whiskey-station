; SPDX-License-Identifier: AGPL-3.0-or-later

; uint32 operative_hidden_dispatch(const NativeEvent *event,
;                                   NativeCommand *commands,
;                                   uint32 capacity)
; RBX/R12-R15 are callee-saved as required by System V. Five pushes leave RSP
; 16-byte aligned before every internal call. XMM registers used here are
; caller-saved; no preservation is required.
operative_hidden_dispatch:
    test rdi, rdi
    jz .invalid_early
    test rsi, rsi
    jz .invalid_early
    test edx, edx
    jz .invalid_early
    push rbx
    push r12
    push r13
    push r14
    push r15
    mov r12, rdi
    mov r13, rsi
    mov r14d, edx
    xor ebx, ebx
    mov rdi, [r12 + EV_HANDLE]
    call state_resolve
    test rax, rax
    jz .finish
    mov r15, rax
    mov eax, [r12 + EV_TYPE]
    cmp eax, EVENT_SPAWN
    je .spawn
    cmp eax, EVENT_UPDATE
    je .update
    cmp eax, EVENT_TOUCH_ACTION
    je .touch
    cmp eax, EVENT_PROCEDURE_ACTION
    je .procedure
    cmp eax, EVENT_SELF_HEAL_ACTION
    je .self_heal
    cmp eax, EVENT_PATIENT_HEAL_ACTION
    je .patient_heal
    cmp eax, EVENT_PATIENT_KILL_ACTION
    je .patient_kill
    cmp eax, EVENT_PROCEDURE_INTERRUPTED
    je .interrupt
    cmp eax, EVENT_ENTITY_DELETED
    je .deleted
    cmp eax, EVENT_DISCONNECTED
    je .disconnected
    cmp eax, EVENT_DIED
    je .died
    cmp eax, EVENT_PATIENT_CREATED
    je .patient_created
    cmp eax, EVENT_PATIENT_REMOVED
    je .patient_removed
    cmp eax, EVENT_ROUND_ENDED
    je .round_end
    cmp eax, EVENT_OBJECTIVE_QUERY
    je .objective_query
    cmp eax, EVENT_PLAYER_ATTACHED
    je .player_attached
    cmp eax, EVENT_SPOKE
    je .spoke
    jmp .finish
.spawn:          call event_spawn
                 jmp .finish
.update:         call event_update
                 jmp .finish
.touch:          call event_touch_action
                 jmp .finish
.procedure:      call event_procedure_action
                 jmp .finish
.self_heal:      call event_self_heal
                 jmp .finish
.patient_heal:   call event_patient_heal
                 jmp .finish
.patient_kill:   call event_patient_kill
                 jmp .finish
.interrupt:      call event_procedure_interrupted
                 jmp .finish
.deleted:        call event_entity_deleted
                 jmp .finish
.disconnected:   call event_disconnected
                 jmp .finish
.died:           call event_died
                 jmp .finish
.patient_created: call event_patient_created
                  jmp .finish
.patient_removed: call event_patient_removed
                  jmp .finish
.round_end:      call event_round_ended
                 jmp .finish
.objective_query: call event_objective_query
                  jmp .finish
.player_attached: call event_player_attached
                  jmp .finish
.spoke:          call event_spoke
.finish:
    mov eax, ebx
    pop r15
    pop r14
    pop r13
    pop r12
    pop rbx
    ret
.invalid_early:
    xor eax, eax
    ret

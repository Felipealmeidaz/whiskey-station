; SPDX-License-Identifier: MIT

event_entity_deleted:
    mov rax, [r12 + EV_TARGET]
    cmp rax, [r15 + ST_PROCEDURE_TARGET]
    jne .done
    INTERNAL_CALL event_procedure_interrupted
.done:
    ret

event_disconnected:
    ; The disclosure radio belongs to the living body, not to its controller.
    ; Keep it active while disconnected so nearby crew can still detect an
    ; abandoned operative. Death and round cleanup remain terminal.
    mov dword [r15 + ST_STATE], STATE_INTERRUPTED
    mov qword [r15 + ST_PROCEDURE_TARGET], 0
    mov dword [r15 + ST_PROCEDURE_REMAINING], 0
    mov dword [r15 + ST_PROCEDURE_TOOL], 0
    mov dword [r15 + ST_REQUIRED_TOOL], 1
    ret

event_died:
    INTERNAL_CALL operative_audio_stop_all
    mov dword [r15 + ST_STATE], STATE_DEAD
    mov qword [r15 + ST_PROCEDURE_TARGET], 0
    mov dword [r15 + ST_PROCEDURE_REMAINING], 0
    mov dword [r15 + ST_PROCEDURE_TOOL], 0
    mov dword [r15 + ST_REQUIRED_TOOL], 1
    ret

event_round_ended:
    INTERNAL_CALL operative_audio_stop_all
    mov dword [r15 + ST_STATE], STATE_INTERRUPTED
    mov qword [r15 + ST_PROCEDURE_TARGET], 0
    mov dword [r15 + ST_PROCEDURE_REMAINING], 0
    mov dword [r15 + ST_PROCEDURE_TOOL], 0
    mov dword [r15 + ST_REQUIRED_TOOL], 1
    ret

; Reconnection restores an interrupted living instance. Death is terminal for
; the current native handle and cannot be bypassed through session churn.
event_player_attached:
    cmp dword [r15 + ST_STATE], STATE_DEAD
    je .done
    mov dword [r15 + ST_STATE], STATE_ACTIVE
    INTERNAL_CALL operative_audio_start
.done:
    ret

event_spoke:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    je .valid_state
    cmp dword [r15 + ST_STATE], STATE_OPERATING
    jne .done
.valid_state:
    test dword [r12 + EV_FLAGS], FLAG_SELF_HAS_SESSION
    jz .done
    COMMAND_OPEN CMD_PLAY_SOUND
    test rdi, rdi
    jz .done
    mov dword [rdi + CMD_TOKEN], TOKEN_SOUND_SPEECH
    mov dword [rdi + CMD_VALUE0], 1500
    test dword [r12 + EV_RANDOM], 1
    jz .speech_duration_ready
    mov dword [rdi + CMD_VALUE0], 2000
.speech_duration_ready:
    mov dword [rdi + CMD_VALUE1], 0
    mov dword [rdi + CMD_VALUE2], __float32__(5.0)
.done:
    ret

; Objective UI asks for a named counter. Reporting is a read-only native
; command; the managed condition only normalizes the returned value.
event_objective_query:
    COMMAND_OPEN CMD_REPORT_COUNTER
    test rdi, rdi
    jz .done
    mov eax, [r12 + EV_INPUT]
    mov [rdi + CMD_TOKEN], eax
    cmp eax, TOKEN_COUNTER_CONVERSIONS
    je .conversions
    cmp eax, TOKEN_COUNTER_PATIENTS
    je .patients
    mov dword [rdi + CMD_VALUE0], 0
    ret
.conversions:
    mov eax, [r15 + ST_CONVERSIONS]
    mov [rdi + CMD_VALUE0], eax
    ret
.patients:
    mov eax, [r15 + ST_PATIENTS]
    mov [rdi + CMD_VALUE0], eax
.done:
    ret

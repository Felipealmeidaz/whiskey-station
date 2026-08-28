; SPDX-License-Identifier: MIT

; Positional disclosure audio remains continuous while the operative is alive
; and controlled. RobustToolbox supplies monotonic time and random bits; this
; module never reads a clock or implements an unsynchronised RNG.

operative_audio_start:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    je .valid_state
    cmp dword [r15 + ST_STATE], STATE_OPERATING
    je .valid_state
    cmp dword [r15 + ST_STATE], STATE_INTERRUPTED
    jne .done
.valid_state:
    cmp dword [r15 + ST_AUDIO_ACTIVE], 0
    jne .done

    ; The low byte is reserved for the procedure-tool token.
    mov eax, [r12 + EV_RANDOM]
    shr eax, 8
    mov ecx, eax
    and ecx, 3
    cmp ecx, 0
    je .duration_5
    cmp ecx, 1
    je .duration_10
    cmp ecx, 2
    je .duration_15
    mov edx, AUDIO_DURATION_30_MS
    jmp .duration_ready
.duration_5:
    mov edx, AUDIO_DURATION_5_MS
    jmp .duration_ready
.duration_10:
    mov edx, AUDIO_DURATION_10_MS
    jmp .duration_ready
.duration_15:
    mov edx, AUDIO_DURATION_15_MS
.duration_ready:
    mov rax, [r12 + EV_SERVER_TICK]
    mov ecx, edx
    add rax, rcx
    mov [r15 + ST_AUDIO_END], rax
    mov dword [r15 + ST_AUDIO_ACTIVE], 1

    ; Pick an offset that leaves room in the configured long-form source.
    mov eax, [r12 + EV_RANDOM]
    shr eax, 10
    xor edx, edx
    mov ecx, AUDIO_MAX_OFFSET_SECONDS
    div ecx

    COMMAND_OPEN CMD_PLAY_SOUND
    test rdi, rdi
    jz .done
    mov dword [rdi + CMD_TOKEN], TOKEN_SOUND_POSITION
    mov rax, [r15 + ST_AUDIO_END]
    sub rax, [r12 + EV_SERVER_TICK]
    mov [rdi + CMD_VALUE0], eax
    mov [rdi + CMD_VALUE1], edx
    mov dword [rdi + CMD_VALUE2], __float32__(5.0)
.done:
    ret

operative_audio_update:
    cmp dword [r15 + ST_STATE], STATE_ACTIVE
    je .valid_state
    cmp dword [r15 + ST_STATE], STATE_OPERATING
    je .valid_state
    cmp dword [r15 + ST_STATE], STATE_INTERRUPTED
    je .valid_state
    cmp dword [r15 + ST_AUDIO_ACTIVE], 0
    je .done
    jmp operative_audio_stop_all
.valid_state:
    cmp dword [r15 + ST_AUDIO_ACTIVE], 0
    je .done
    mov rax, [r12 + EV_SERVER_TICK]
    cmp rax, [r15 + ST_AUDIO_END]
    jb .done
    INTERNAL_CALL operative_audio_stop_all
    jmp operative_audio_start
.done:
    ret

operative_audio_stop_all:
    ; Token zero stops every stream, including a short speech cue that may
    ; overlap the continuous positional channel.
    COMMAND_OPEN CMD_STOP_SOUND
    test rdi, rdi
    jz .clear
    mov dword [rdi + CMD_TOKEN], 0
.clear:
    mov dword [r15 + ST_AUDIO_ACTIVE], 0
    mov qword [r15 + ST_AUDIO_END], 0
    ret

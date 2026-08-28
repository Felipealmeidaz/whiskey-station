; SPDX-License-Identifier: AGPL-3.0-or-later

; uint64 operative_hidden_create(uint64 self)
; Allocates a deterministic slot. Calls are serialized by the ECS main thread;
; no CLR pointer or GC handle is retained.
operative_hidden_create:
    lea r8, [operative_states]
    xor ecx, ecx
.find_slot:
    cmp dword [r8 + ST_USED], 0
    je .found
    add r8, ST_SIZE
    inc ecx
    cmp ecx, OPERATIVE_MAX_INSTANCES
    jb .find_slot
    xor eax, eax
    ret
.found:
    mov edx, [r8 + ST_GENERATION]
    inc edx
    jnz .generation_ready
    inc edx                         ; generation zero is never issued
.generation_ready:
    ; Preserve generation while clearing prior instance data.
    mov r9d, edx
    mov r10, rdi
    mov rdi, r8
    xor eax, eax
    mov edx, ST_SIZE / 8
.clear_slot:
    mov [rdi], rax
    add rdi, 8
    dec edx
    jnz .clear_slot
    mov dword [r8 + ST_USED], 1
    mov [r8 + ST_GENERATION], r9d
    mov [r8 + ST_SELF], r10
    mov dword [r8 + ST_STATE], STATE_INITIALIZING
    mov eax, ecx
    inc eax
    mov rdx, r9
    shl rdx, 32
    or rax, rdx
    ret

; Internal: RDI=handle, RAX=state pointer or zero.
state_resolve:
    mov eax, edi
    test eax, eax
    jz .invalid
    dec eax
    cmp eax, OPERATIVE_MAX_INSTANCES
    jae .invalid
    imul rax, ST_SIZE
    lea rdx, [operative_states]
    add rax, rdx
    cmp dword [rax + ST_USED], 1
    jne .invalid
    mov rdx, rdi
    shr rdx, 32
    cmp [rax + ST_GENERATION], edx
    jne .invalid
    ret
.invalid:
    xor eax, eax
    ret

; uint32 operative_hidden_destroy(uint64 handle)
operative_hidden_destroy:
    sub rsp, 8                      ; align before internal call
    call state_resolve
    add rsp, 8
    test rax, rax
    jz .not_found
    mov dword [rax + ST_USED], 0
    inc dword [rax + ST_GENERATION]
    mov eax, 1
    ret
.not_found:
    xor eax, eax
    ret

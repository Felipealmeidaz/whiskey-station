; SPDX-License-Identifier: MIT

; CF=1 when server tick RAX precedes ready tick RDX.
cooldown_pending:
    cmp rax, rdx
    jb .pending
    clc
    ret
.pending:
    stc
    ret

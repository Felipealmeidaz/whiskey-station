; SPDX-License-Identifier: AGPL-3.0-or-later

; CF=1 when server tick RAX precedes ready tick RDX.
cooldown_pending:
    cmp rax, rdx
    jb .pending
    clc
    ret
.pending:
    stc
    ret

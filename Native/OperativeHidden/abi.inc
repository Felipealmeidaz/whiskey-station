; SPDX-License-Identifier: AGPL-3.0-or-later
;
; Stable ABI shared by libwhiskey_operativo_oculto.so and the managed bridge.
; All public entry points use the System V AMD64 calling convention. The module
; never retains an event pointer, command pointer, or any other managed memory.

%ifndef OPERATIVE_HIDDEN_ABI_INC
%define OPERATIVE_HIDDEN_ABI_INC

%define WHISKEY_NATIVE_ANTAG_ABI_VERSION 1
%define OPERATIVE_MAX_INSTANCES 64

; NativeEvent, explicitly packed to 72 bytes. Coordinates are local-to-parent
; snapshots supplied by ECS so native procedure state can detect movement and
; active-hand changes without retaining transforms or managed references.
%define EV_TYPE             0
%define EV_FLAGS            4
%define EV_HANDLE           8
%define EV_SERVER_TICK     16
%define EV_SELF            24
%define EV_TARGET          32
%define EV_INPUT           40
%define EV_VALUE0          44
%define EV_SELF_X          48
%define EV_SELF_Y          52
%define EV_TARGET_X        56
%define EV_TARGET_Y        60
%define EV_RANDOM          64
%define EV_ACTIVE_ITEM     68
%define EV_SIZE            72

; NativeCommand, explicitly packed to 48 bytes.
%define CMD_TYPE            0
%define CMD_FLAGS           4
%define CMD_SOURCE          8
%define CMD_TARGET         16
%define CMD_VALUE0         24
%define CMD_VALUE1         28
%define CMD_VALUE2         32
%define CMD_VALUE3         36
%define CMD_TOKEN          40
%define CMD_RESERVED       44
%define CMD_SIZE           48

; Event identifiers. Server tick values are monotonic milliseconds supplied by
; RobustToolbox; native gameplay never reads a clock or generates random data.
%define EVENT_SPAWN                    1
%define EVENT_UPDATE                   2
%define EVENT_TOUCH_ACTION             4
%define EVENT_PROCEDURE_ACTION         5
%define EVENT_SELF_HEAL_ACTION         6
%define EVENT_PATIENT_HEAL_ACTION      7
%define EVENT_PATIENT_KILL_ACTION      8
%define EVENT_PROCEDURE_INTERRUPTED    9
%define EVENT_ENTITY_DELETED          10
%define EVENT_DISCONNECTED            11
%define EVENT_DIED                    12
%define EVENT_PATIENT_CREATED         14
%define EVENT_PATIENT_REMOVED         15
%define EVENT_ROUND_ENDED             16
%define EVENT_OBJECTIVE_QUERY         17
%define EVENT_PLAYER_ATTACHED         18
%define EVENT_SPOKE                   19
%define EVENT_TOUCH_COMMITTED         20
%define EVENT_SELF_HEAL_COMMITTED     21
%define EVENT_PATIENT_HEAL_COMMITTED  22
%define EVENT_PATIENT_KILL_COMMITTED  23

; Snapshot flags. Managed code reports ECS facts; native code combines those
; facts into antagonist-specific decisions.
%define FLAG_TARGET_VALID              (1 << 0)
%define FLAG_TARGET_ALIVE              (1 << 1)
%define FLAG_TARGET_DEAD               (1 << 2)
%define FLAG_TARGET_HUMANOID           (1 << 3)
%define FLAG_TARGET_CONVERTED          (1 << 4)
%define FLAG_TARGET_IN_MELEE_RANGE     (1 << 5)
%define FLAG_REQUIRED_TOOL_HELD        (1 << 14)
%define FLAG_TARGET_OWN_PATIENT        (1 << 16)
%define FLAG_TARGET_PROTECTED          (1 << 17)
%define FLAG_TARGET_HAS_SESSION        (1 << 19)
%define FLAG_SELF_HAS_SESSION          (1 << 20)
%define FLAG_TARGET_CAN_DIE            (1 << 21)
%define FLAG_TARGET_PREVIOUSLY_CONVERTED (1 << 22)
%define FLAG_COUNTER_ACCEPTED          (1 << 23)

%define COMMAND_FLAG_REQUIRE_PREVIOUS_SUCCESS (1 << 0)
%define COMMAND_FLAG_PRESERVE_VISUAL_SKIN      (1 << 1)
%define COMMAND_FLAG_ATOMIC                    (1 << 2)

; The high bit of operative_hidden_dispatch's return value is an error status,
; never a command count. A saturated command buffer restores the complete
; pre-dispatch native state snapshot and returns this value with count zero.
%define DISPATCH_ERROR_COMMAND_OVERFLOW (1 << 31)
%define STATE_FLAG_COMMAND_OVERFLOW      (1 << 0)

%define STATE_INITIALIZING      0
%define STATE_ACTIVE            1
%define STATE_OPERATING         2
%define STATE_INTERRUPTED       3
%define STATE_DEAD              4
%define STATE_TERMINATED        5

; Commands are generic ECS primitives. Tokens are resolved through prototype
; configuration in the managed bridge; command execution contains no 049 rules.
%define CMD_NONE                 0
%define CMD_ADD_ACTION           1
%define CMD_SET_ACTION_COOLDOWN  2
%define CMD_SET_MOB_STATE        6
%define CMD_ADD_COMPONENT_BUNDLE     9
%define CMD_REMOVE_COMPONENT_BUNDLE 10
%define CMD_ZOMBIFY_ENTITY          11
%define CMD_UNZOMBIFY_ENTITY        12
%define CMD_REJUVENATE_ENTITY       13
%define CMD_POPUP                   14
%define CMD_SET_FACTION             16
%define CMD_SET_NATIVE_OWNER        17
%define CMD_REPORT_COUNTER          18
%define CMD_CLEAR_ROUTED_TARGET     20
%define CMD_PLAY_SOUND             21
%define CMD_STOP_SOUND             22
%define CMD_NOTIFY_EVENT           23

%define TOKEN_BUNDLE_PATIENT       1
%define TOKEN_BUNDLE_UNREVIVABLE   2
%define TOKEN_BUNDLE_PATIENT_GHOST 3
%define TOKEN_BUNDLE_REMOVE_AI     4
%define TOKEN_FACTION_SYNDICATE    1
%define TOKEN_ZOMBIE_PATIENT       1

%define TOKEN_COUNTER_CONVERSIONS  1
%define TOKEN_COUNTER_PATIENTS     2

%define TOKEN_SOUND_POSITION       1
%define TOKEN_SOUND_SPEECH         2

; Content.Shared.Mobs.MobState includes Trauma's SoftCrit and Critical states:
; Invalid=0, Alive=1, SoftCrit=2, Critical=3, Dead=4.
%define MOB_STATE_DEAD 4

%define TOKEN_ACTION_TOUCH        1
%define TOKEN_ACTION_PROCEDURE    2
%define TOKEN_ACTION_SELF_HEAL    3
%define TOKEN_ACTION_PATIENT_HEAL 4
%define TOKEN_ACTION_PATIENT_KILL 5

%define TOKEN_POPUP_INVALID       1
%define TOKEN_POPUP_COOLDOWN      2
%define TOKEN_POPUP_OPERATING     3
%define TOKEN_POPUP_INTERRUPTED   4
%define TOKEN_POPUP_CONVERTED     5
%define TOKEN_POPUP_TOOL_CAUTERY 10
%define TOKEN_POPUP_TOOL_DRILL   11
%define TOKEN_POPUP_TOOL_SCALPEL 12
%define TOKEN_POPUP_TOOL_RETRACTOR 13
%define TOKEN_POPUP_TOOL_HEMOSTAT 14
%define TOKEN_POPUP_TOOL_SAW     15
%define PROCEDURE_TOOL_COUNT      6

; Per-instance state. Handles encode (generation << 32) | (slot + 1), so a
; destroyed instance cannot be confused with a later occupant of the same slot.
%define ST_USED                 0
%define ST_GENERATION           4
%define ST_SELF                 8
%define ST_STATE               16
%define ST_FLAGS               20
%define ST_TARGET              24
%define ST_PROCEDURE_TARGET    32
%define ST_TOUCH_READY         40
%define ST_PROCEDURE_READY     48
%define ST_SELF_HEAL_READY     56
%define ST_PATIENT_HEAL_READY  64
%define ST_PROCEDURE_DEADLINE  96
%define ST_CONVERSIONS         76
%define ST_PATIENTS            80
%define ST_PATIENT_KILL_READY  88
%define ST_REQUIRED_TOOL      112
%define ST_PROCEDURE_SELF_X   116
%define ST_PROCEDURE_SELF_Y   120
%define ST_PROCEDURE_TARGET_X 124
%define ST_PROCEDURE_TARGET_Y 128
%define ST_PROCEDURE_TOOL     136
%define ST_AUDIO_END          144
%define ST_AUDIO_ACTIVE       152
%define ST_SIZE               160

%define TOUCH_COOLDOWN_MS      180000
%define PROCEDURE_COOLDOWN_MS  120000
%define HEAL_COOLDOWN_MS       300000
%define PATIENT_KILL_COOLDOWN_MS 60000
%define PROCEDURE_STEP_MS        3500

%define AUDIO_DURATION_5_MS       5000
%define AUDIO_DURATION_10_MS     10000
%define AUDIO_DURATION_15_MS     15000
%define AUDIO_DURATION_30_MS     30000
%define AUDIO_MAX_OFFSET_SECONDS   240
; Opens a zeroed command record and returns its address in RDI. The dispatcher
; owns RBX (command count), R13 (buffer), and R14D (capacity). Capacity exhaustion
; returns RDI=0 and leaves the count unchanged.
%macro COMMAND_OPEN 1
    cmp ebx, r14d
    jae %%full
    mov eax, ebx
    imul rax, CMD_SIZE
    lea rdi, [r13 + rax]
    pxor xmm0, xmm0
    movdqu [rdi], xmm0
    movdqu [rdi + 16], xmm0
    movdqu [rdi + 32], xmm0
    mov dword [rdi + CMD_TYPE], %1
    mov rax, [r15 + ST_SELF]
    mov [rdi + CMD_SOURCE], rax
    mov rax, [r12 + EV_TARGET]
    mov [rdi + CMD_TARGET], rax
    inc ebx
    jmp %%done
%%full:
    or dword [r15 + ST_FLAGS], STATE_FLAG_COMMAND_OVERFLOW
    xor edi, edi
%%done:
%endmacro

; Internal handlers enter with RSP%16=8. Use this macro for nested calls so
; every CALL instruction still observes the System V pre-call alignment rule.
%macro INTERNAL_CALL 1
    sub rsp, 8
    call %1
    add rsp, 8
%endmacro

%endif

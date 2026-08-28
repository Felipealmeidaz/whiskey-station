#!/usr/bin/env python3
# SPDX-License-Identifier: MIT

"""Black-box ABI and gameplay tests for the NASM native runtime."""

from __future__ import annotations

import ctypes
import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[1]
LIBRARY = ROOT / "build" / "libwhiskey_operativo_oculto.so"
CAPACITY = 16


class NativeEvent(ctypes.Structure):
    _layout_ = "gcc-sysv"
    _fields_ = [
        ("type", ctypes.c_uint32),
        ("flags", ctypes.c_uint32),
        ("handle", ctypes.c_uint64),
        ("server_tick", ctypes.c_uint64),
        ("self_entity", ctypes.c_uint64),
        ("target", ctypes.c_uint64),
        ("input", ctypes.c_uint32),
        ("value0", ctypes.c_float),
        ("self_x", ctypes.c_float),
        ("self_y", ctypes.c_float),
        ("target_x", ctypes.c_float),
        ("target_y", ctypes.c_float),
        ("random", ctypes.c_uint32),
        ("active_item", ctypes.c_uint32),
    ]


class NativeCommand(ctypes.Structure):
    _layout_ = "gcc-sysv"
    _fields_ = [
        ("type", ctypes.c_uint32),
        ("flags", ctypes.c_uint32),
        ("source", ctypes.c_uint64),
        ("target", ctypes.c_uint64),
        ("value0", ctypes.c_int32),
        ("value1", ctypes.c_int32),
        ("value2", ctypes.c_float),
        ("value3", ctypes.c_float),
        ("token", ctypes.c_uint32),
        ("reserved", ctypes.c_uint32),
    ]


EVENT_SPAWN = 1
EVENT_UPDATE = 2
EVENT_TOUCH = 4
EVENT_PROCEDURE = 5
EVENT_SELF_HEAL = 6
EVENT_PATIENT_HEAL = 7
EVENT_PATIENT_KILL = 8
EVENT_INTERRUPTED = 9
EVENT_ENTITY_DELETED = 10
EVENT_DISCONNECTED = 11
EVENT_DIED = 12
EVENT_PATIENT_CREATED = 14
EVENT_PATIENT_REMOVED = 15
EVENT_ROUND_ENDED = 16
EVENT_OBJECTIVE_QUERY = 17
EVENT_PLAYER_ATTACHED = 18
EVENT_SPOKE = 19

TARGET_VALID = 1 << 0
TARGET_ALIVE = 1 << 1
TARGET_DEAD = 1 << 2
TARGET_HUMANOID = 1 << 3
TARGET_CONVERTED = 1 << 4
TARGET_IN_RANGE = 1 << 5
REQUIRED_TOOL_HELD = 1 << 14
TARGET_OWN_PATIENT = 1 << 16
TARGET_HAS_SESSION = 1 << 19
SELF_HAS_SESSION = 1 << 20
TARGET_CAN_DIE = 1 << 21

CMD_ADD_ACTION = 1
CMD_SET_ACTION_COOLDOWN = 2
CMD_SET_MOB_STATE = 6
CMD_ADD_COMPONENT_BUNDLE = 9
CMD_REMOVE_COMPONENT_BUNDLE = 10
CMD_ZOMBIFY = 11
CMD_UNZOMBIFY = 12
CMD_REJUVENATE = 13
CMD_POPUP = 14
CMD_SET_FACTION = 16
CMD_SET_NATIVE_OWNER = 17
CMD_REPORT_COUNTER = 18
CMD_CLEAR_ROUTED_TARGET = 20
CMD_PLAY_SOUND = 21
CMD_STOP_SOUND = 22
CMD_NOTIFY_EVENT = 23


class NativeRuntimeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.lib = ctypes.CDLL(str(LIBRARY))
        cls.lib.operative_hidden_get_abi_version.restype = ctypes.c_uint32
        cls.lib.operative_hidden_initialize.restype = ctypes.c_uint32
        cls.lib.operative_hidden_shutdown.restype = ctypes.c_uint32
        cls.lib.operative_hidden_create.argtypes = [ctypes.c_uint64]
        cls.lib.operative_hidden_create.restype = ctypes.c_uint64
        cls.lib.operative_hidden_destroy.argtypes = [ctypes.c_uint64]
        cls.lib.operative_hidden_destroy.restype = ctypes.c_uint32
        cls.lib.operative_hidden_dispatch.argtypes = [
            ctypes.POINTER(NativeEvent),
            ctypes.POINTER(NativeCommand),
            ctypes.c_uint32,
        ]
        cls.lib.operative_hidden_dispatch.restype = ctypes.c_uint32

    def setUp(self) -> None:
        self.assertEqual(self.lib.operative_hidden_initialize(), 1)
        self.self_entity = 1001
        self.handle = self.lib.operative_hidden_create(self.self_entity)
        self.assertNotEqual(self.handle, 0)

    def tearDown(self) -> None:
        self.assertEqual(self.lib.operative_hidden_shutdown(), 1)

    def dispatch(
        self,
        event_type: int,
        *,
        target: int = 0,
        flags: int = 0,
        tick: int = 0,
        input_value: int = 0,
        value0: float = 0.0,
        random: int = 3,
        active_item: int = 3333,
        handle: int | None = None,
        coordinates: tuple[float, float, float, float] = (1.0, 2.0, 3.0, 4.0),
    ) -> list[NativeCommand]:
        sx, sy, tx, ty = coordinates
        event = NativeEvent(
            type=event_type,
            flags=flags,
            handle=self.handle if handle is None else handle,
            server_tick=tick,
            self_entity=self.self_entity,
            target=target,
            input=input_value,
            value0=value0,
            self_x=sx,
            self_y=sy,
            target_x=tx,
            target_y=ty,
            random=random,
            active_item=active_item,
        )
        commands = (NativeCommand * CAPACITY)()
        count = self.lib.operative_hidden_dispatch(event, commands, CAPACITY)
        self.assertLessEqual(count, CAPACITY)
        return list(commands[:count])

    def spawn(self, tool: int = 3, random_bits: int = 0, flags: int = 0) -> list[NativeCommand]:
        return self.dispatch(EVENT_SPAWN, random=(random_bits << 8) | tool, flags=flags)

    @staticmethod
    def tool_mask(token: int) -> int:
        return 1 << (token - 1)

    def test_abi_layout_and_version(self) -> None:
        self.assertEqual(self.lib.operative_hidden_get_abi_version(), 1)
        self.assertEqual(ctypes.sizeof(NativeEvent), 72)
        self.assertEqual(ctypes.sizeof(NativeCommand), 48)
        expected_event_offsets = {
            "type": 0,
            "flags": 4,
            "handle": 8,
            "server_tick": 16,
            "self_entity": 24,
            "target": 32,
            "input": 40,
            "value0": 44,
            "self_x": 48,
            "self_y": 52,
            "target_x": 56,
            "target_y": 60,
            "random": 64,
            "active_item": 68,
        }
        for field, offset in expected_event_offsets.items():
            self.assertEqual(getattr(NativeEvent, field).offset, offset)

    def test_spawn_and_stale_handle_safety(self) -> None:
        commands = self.spawn()
        self.assertEqual([command.type for command in commands], [CMD_ADD_ACTION] * 5)
        self.assertEqual([command.token for command in commands], [1, 2, 3, 4, 5])

        stale = self.handle
        self.assertEqual(self.lib.operative_hidden_destroy(stale), 1)
        replacement = self.lib.operative_hidden_create(self.self_entity)
        self.assertNotEqual(replacement, stale)
        self.assertEqual(self.dispatch(EVENT_SPAWN, handle=stale), [])

    def test_positional_audio_cadence_speech_and_death_cleanup(self) -> None:
        commands = self.spawn(random_bits=0, flags=SELF_HAS_SESSION)
        self.assertEqual([command.type for command in commands[:5]], [CMD_ADD_ACTION] * 5)
        self.assertEqual(commands[5].type, CMD_PLAY_SOUND)
        self.assertEqual((commands[5].token, commands[5].value0), (1, 5000))
        self.assertAlmostEqual(commands[5].value2, 5.0)

        commands = self.dispatch(
            EVENT_UPDATE,
            tick=5000,
            random=(3 << 8) | 3,
            flags=SELF_HAS_SESSION,
        )
        self.assertEqual([command.type for command in commands], [CMD_STOP_SOUND, CMD_PLAY_SOUND])
        self.assertEqual((commands[1].token, commands[1].value0), (1, 30000))

        short_spoken = self.dispatch(EVENT_SPOKE, tick=5001, flags=SELF_HAS_SESSION, random=2)
        self.assertEqual([(command.type, command.token) for command in short_spoken], [(CMD_PLAY_SOUND, 2)])
        self.assertEqual(short_spoken[0].value0, 1500)
        self.assertAlmostEqual(short_spoken[0].value2, 5.0)

        long_spoken = self.dispatch(EVENT_SPOKE, tick=5001, flags=SELF_HAS_SESSION, random=3)
        self.assertEqual([(command.type, command.token) for command in long_spoken], [(CMD_PLAY_SOUND, 2)])
        self.assertEqual(long_spoken[0].value0, 2000)
        self.assertAlmostEqual(long_spoken[0].value2, 5.0)

        disconnected = self.dispatch(EVENT_DISCONNECTED, tick=5002)
        self.assertEqual(disconnected, [])
        persisted = self.dispatch(EVENT_UPDATE, tick=35000, random=0)
        self.assertEqual([(command.type, command.token) for command in persisted],
                         [(CMD_STOP_SOUND, 0), (CMD_PLAY_SOUND, 1)])

        died = self.dispatch(EVENT_DIED, tick=35001, flags=SELF_HAS_SESSION)
        self.assertEqual([(command.type, command.token) for command in died], [(CMD_STOP_SOUND, 0)])
        self.assertEqual(self.dispatch(EVENT_SPOKE, tick=5003, flags=SELF_HAS_SESSION), [])
        self.assertEqual(self.dispatch(EVENT_UPDATE, tick=40000, flags=SELF_HAS_SESSION), [])

    def test_round_end_does_not_restart_positional_audio(self) -> None:
        self.spawn(random_bits=0, flags=SELF_HAS_SESSION)
        ended = self.dispatch(EVENT_ROUND_ENDED, tick=1000, flags=SELF_HAS_SESSION)
        self.assertEqual([(command.type, command.token) for command in ended], [(CMD_STOP_SOUND, 0)])
        self.assertEqual(self.dispatch(EVENT_UPDATE, tick=10000, flags=SELF_HAS_SESSION), [])
        self.assertEqual(self.dispatch(EVENT_SPOKE, tick=10001, flags=SELF_HAS_SESSION), [])

    def test_touch_validation_and_cooldown(self) -> None:
        self.spawn()
        valid = TARGET_VALID | TARGET_ALIVE | TARGET_HUMANOID | TARGET_IN_RANGE | TARGET_CAN_DIE
        commands = self.dispatch(EVENT_TOUCH, target=2002, flags=valid, tick=1000)
        self.assertEqual([command.type for command in commands], [CMD_SET_MOB_STATE, CMD_SET_ACTION_COOLDOWN])
        self.assertEqual(commands[0].value0, 4)
        self.assertEqual(commands[1].value0, 180000)

        cooldown = self.dispatch(EVENT_TOUCH, target=2002, flags=valid, tick=1001)
        self.assertEqual([(command.type, command.token) for command in cooldown], [(CMD_POPUP, 2)])
        invalid = self.dispatch(EVENT_TOUCH, target=self.self_entity, flags=valid, tick=181001)
        self.assertEqual([(command.type, command.token) for command in invalid], [(CMD_POPUP, 1)])
        cannot_die = valid & ~TARGET_CAN_DIE
        rejected = self.dispatch(EVENT_TOUCH, target=2002, flags=cannot_die, tick=181001)
        self.assertEqual([(command.type, command.token) for command in rejected], [(CMD_POPUP, 1)])

    def test_procedure_conversion_cooldown_and_counter(self) -> None:
        self.spawn()
        flags = TARGET_VALID | TARGET_ALIVE | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        for tool in range(1, 6):
            started = self.dispatch(
                EVENT_PROCEDURE,
                target=2002,
                flags=flags,
                tick=tool * 1000,
                input_value=self.tool_mask(tool),
                active_item=3000 + tool,
            )
            self.assertEqual([(command.type, command.token) for command in started], [(CMD_POPUP, 3)])
            advanced = self.dispatch(
                EVENT_UPDATE,
                target=2002,
                flags=flags,
                tick=tool * 1000 + 3500,
                input_value=self.tool_mask(tool),
                value0=3.5,
                active_item=3000 + tool,
            )
            self.assertEqual([(command.type, command.token) for command in advanced], [(CMD_POPUP, 10 + tool)])

        started = self.dispatch(
            EVENT_PROCEDURE,
            target=2002,
            flags=flags,
            tick=6000,
            input_value=self.tool_mask(6),
            active_item=3006,
        )
        self.assertEqual([(command.type, command.token) for command in started], [(CMD_POPUP, 3)])
        completed = self.dispatch(
            EVENT_UPDATE,
            target=2002,
            flags=flags,
            tick=9500,
            input_value=self.tool_mask(6),
            value0=3.5,
            active_item=3006,
        )
        self.assertEqual(
            [command.type for command in completed],
            [
                CMD_ADD_COMPONENT_BUNDLE,
                CMD_ZOMBIFY,
                CMD_ADD_COMPONENT_BUNDLE,
                CMD_REMOVE_COMPONENT_BUNDLE,
                CMD_REJUVENATE,
                CMD_SET_FACTION,
                CMD_SET_NATIVE_OWNER,
                CMD_NOTIFY_EVENT,
                CMD_SET_ACTION_COOLDOWN,
                CMD_CLEAR_ROUTED_TARGET,
                CMD_POPUP,
            ],
        )
        self.assertEqual(completed[0].token, 1)
        self.assertEqual(completed[2].token, 3)
        self.assertEqual(completed[3].token, 4)

        self.assertEqual((completed[7].token, completed[7].target), (EVENT_PATIENT_CREATED, 2002))
        uncommitted = self.dispatch(EVENT_OBJECTIVE_QUERY, input_value=1)
        self.assertEqual([(command.type, command.token, command.value0) for command in uncommitted], [(CMD_REPORT_COUNTER, 1, 0)])
        self.dispatch(EVENT_PATIENT_CREATED, target=2002)
        counter = self.dispatch(EVENT_OBJECTIVE_QUERY, input_value=1)
        self.assertEqual([(command.type, command.token, command.value0) for command in counter], [(CMD_REPORT_COUNTER, 1, 1)])

        blocked = self.dispatch(EVENT_PROCEDURE, target=2003, flags=flags, tick=30000, input_value=self.tool_mask(1))
        self.assertEqual([(command.type, command.token) for command in blocked], [(CMD_POPUP, 2)])

    def test_procedure_with_session_avoids_ghost_role(self) -> None:
        self.spawn()
        flags = (
            TARGET_VALID
            | TARGET_DEAD
            | TARGET_HUMANOID
            | TARGET_IN_RANGE
            | REQUIRED_TOOL_HELD
            | TARGET_HAS_SESSION
        )
        commands = []
        for tool in range(1, 7):
            self.dispatch(
                EVENT_PROCEDURE,
                target=2002,
                flags=flags,
                tick=tool * 1000,
                input_value=self.tool_mask(tool),
                active_item=3000 + tool,
            )
            commands = self.dispatch(
                EVENT_UPDATE,
                target=2002,
                flags=flags,
                tick=tool * 1000 + 3500,
                input_value=self.tool_mask(tool),
                value0=3.5,
                active_item=3000 + tool,
            )
        self.assertNotIn(3, [command.token for command in commands if command.type == CMD_ADD_COMPONENT_BUNDLE])

    def test_procedure_interruption_and_deleted_target(self) -> None:
        self.spawn()
        flags = TARGET_VALID | TARGET_DEAD | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        self.dispatch(EVENT_PROCEDURE, target=2002, flags=flags, tick=1000, input_value=self.tool_mask(1))
        interrupted = self.dispatch(EVENT_ENTITY_DELETED, target=2002, tick=2000)
        self.assertEqual(
            [(command.type, command.token) for command in interrupted],
            [(CMD_CLEAR_ROUTED_TARGET, 0), (CMD_POPUP, 4)],
        )
        self.assertEqual(
            self.dispatch(EVENT_UPDATE, target=2002, flags=flags, tick=25000, input_value=self.tool_mask(1), value0=30.0),
            [],
        )

    def test_switching_to_identical_tool_entity_interrupts_procedure(self) -> None:
        self.spawn()
        flags = TARGET_VALID | TARGET_DEAD | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        self.dispatch(
            EVENT_PROCEDURE,
            target=2002,
            flags=flags,
            tick=1000,
            input_value=self.tool_mask(1),
            active_item=3001,
        )
        interrupted = self.dispatch(
            EVENT_UPDATE,
            target=2002,
            flags=flags,
            tick=2000,
            input_value=self.tool_mask(1),
            value0=1.0,
            active_item=3002,
        )
        self.assertEqual(
            [(command.type, command.token) for command in interrupted],
            [(CMD_CLEAR_ROUTED_TARGET, 0), (CMD_POPUP, 4)],
        )

    def test_tiny_physics_jitter_does_not_interrupt_procedure(self) -> None:
        self.spawn()
        flags = TARGET_VALID | TARGET_DEAD | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        self.dispatch(
            EVENT_PROCEDURE,
            target=2002,
            flags=flags,
            tick=1000,
            input_value=self.tool_mask(1),
            active_item=3001,
        )
        advanced = self.dispatch(
            EVENT_UPDATE,
            target=2002,
            flags=flags,
            tick=4500,
            input_value=self.tool_mask(1),
            value0=3.5,
            coordinates=(1.1, 1.9, 3.1, 3.9),
            active_item=3001,
        )
        self.assertEqual([(command.type, command.token) for command in advanced], [(CMD_POPUP, 11)])

    def test_real_movement_still_interrupts_procedure(self) -> None:
        self.spawn()
        flags = TARGET_VALID | TARGET_DEAD | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        self.dispatch(
            EVENT_PROCEDURE,
            target=2002,
            flags=flags,
            tick=1000,
            input_value=self.tool_mask(1),
            active_item=3001,
        )
        interrupted = self.dispatch(
            EVENT_UPDATE,
            target=2002,
            flags=flags,
            tick=2000,
            input_value=self.tool_mask(1),
            value0=1.0,
            coordinates=(1.5, 2.0, 3.0, 4.0),
            active_item=3001,
        )
        self.assertEqual(
            [(command.type, command.token) for command in interrupted],
            [(CMD_CLEAR_ROUTED_TARGET, 0), (CMD_POPUP, 4)],
        )

    def test_already_converted_and_wrong_tool_are_rejected(self) -> None:
        self.spawn()
        base = TARGET_VALID | TARGET_DEAD | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        wrong = self.dispatch(EVENT_PROCEDURE, target=2002, flags=base, tick=1000, input_value=self.tool_mask(4))
        self.assertEqual([(command.type, command.token) for command in wrong], [(CMD_POPUP, 10)])
        converted = self.dispatch(EVENT_PROCEDURE, target=2002, flags=base | TARGET_CONVERTED, tick=1000, input_value=self.tool_mask(1))
        self.assertEqual([(command.type, command.token) for command in converted], [(CMD_POPUP, 1)])

    def test_missing_required_tool_fails_closed(self) -> None:
        self.spawn()
        flags = TARGET_VALID | TARGET_DEAD | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        rejected = self.dispatch(EVENT_PROCEDURE, target=2002, flags=flags, tick=1000, input_value=0)
        self.assertEqual([(command.type, command.token) for command in rejected], [(CMD_POPUP, 10)])

    def test_required_tool_can_be_held_with_another_instrument(self) -> None:
        self.spawn()
        flags = TARGET_VALID | TARGET_DEAD | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        started = self.dispatch(
            EVENT_PROCEDURE,
            target=2002,
            flags=flags,
            tick=1000,
            input_value=self.tool_mask(1) | self.tool_mask(5),
        )
        self.assertEqual(
            [(command.type, command.token) for command in started],
            [(CMD_POPUP, 3)],
        )

    def test_patient_ownership_and_lifecycle(self) -> None:
        self.spawn()
        patient = TARGET_VALID | TARGET_CONVERTED | TARGET_OWN_PATIENT
        healed = self.dispatch(EVENT_PATIENT_HEAL, target=2002, flags=patient, tick=1000)
        self.assertEqual([command.type for command in healed], [CMD_REJUVENATE, CMD_SET_ACTION_COOLDOWN])
        self_healed = self.dispatch(EVENT_SELF_HEAL, tick=1001)
        self.assertEqual([command.type for command in self_healed], [CMD_REJUVENATE, CMD_SET_ACTION_COOLDOWN])
        patient_cooldown = self.dispatch(EVENT_PATIENT_HEAL, target=2002, flags=patient, tick=1002)
        self.assertEqual([(command.type, command.token) for command in patient_cooldown], [(CMD_POPUP, 2)])

        killed = self.dispatch(EVENT_PATIENT_KILL, target=2002, flags=patient, tick=2000)
        self.assertEqual(
            [command.type for command in killed],
            [CMD_UNZOMBIFY, CMD_REMOVE_COMPONENT_BUNDLE, CMD_ADD_COMPONENT_BUNDLE, CMD_SET_MOB_STATE],
        )
        invalid = self.dispatch(EVENT_PATIENT_HEAL, target=2003, flags=TARGET_VALID | TARGET_CONVERTED, tick=400000)
        self.assertEqual([(command.type, command.token) for command in invalid], [(CMD_POPUP, 1)])

        self.assertEqual(self.dispatch(EVENT_PATIENT_REMOVED, target=2002), [])

    def test_disconnect_death_and_round_end_cancel_state(self) -> None:
        self.spawn()
        flags = TARGET_VALID | TARGET_DEAD | TARGET_HUMANOID | TARGET_IN_RANGE | REQUIRED_TOOL_HELD
        self.dispatch(EVENT_PROCEDURE, target=2002, flags=flags, tick=1000, input_value=self.tool_mask(1))
        disconnected = self.dispatch(EVENT_DISCONNECTED)
        self.assertEqual(disconnected, [])
        self.assertEqual(self.dispatch(EVENT_UPDATE, target=2002, flags=flags, value0=30.0, input_value=self.tool_mask(1)), [])
        attached = self.dispatch(EVENT_PLAYER_ATTACHED, flags=SELF_HAS_SESSION)
        self.assertEqual([(command.type, command.token) for command in attached], [(CMD_PLAY_SOUND, 1)])
        restored = self.dispatch(EVENT_SELF_HEAL, tick=400000)
        self.assertEqual([command.type for command in restored], [CMD_REJUVENATE, CMD_SET_ACTION_COOLDOWN])
        died = self.dispatch(EVENT_DIED)
        self.assertEqual([(command.type, command.token) for command in died], [(CMD_STOP_SOUND, 0)])
        rejected = self.dispatch(EVENT_SELF_HEAL, tick=800000)
        self.assertEqual([(command.type, command.token) for command in rejected], [(CMD_POPUP, 1)])
        ended = self.dispatch(EVENT_ROUND_ENDED)
        self.assertEqual([(command.type, command.token) for command in ended], [(CMD_STOP_SOUND, 0)])


if __name__ == "__main__":
    unittest.main(verbosity=2)

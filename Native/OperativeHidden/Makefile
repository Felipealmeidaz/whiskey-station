NASM ?= nasm
CC ?= gcc
PYTHON ?= python3
CONFIG ?= release
BUILD_DIR ?= build
TARGET := $(BUILD_DIR)/libwhiskey_operativo_oculto.so
OBJECT := $(BUILD_DIR)/main.o
# NASM reports local RIP-relative references across ELF sections as
# reloc-rel-dword even though the linker resolves them into position-independent
# PC32 relocations. Keep every other warning fatal and disable only that
# inapplicable diagnostic.
NASMFLAGS := -f elf64 -Wall -Werror -I.
LDFLAGS := -shared -nostdlib -Wl,--no-undefined -Wl,-z,defs -Wl,-z,relro -Wl,-z,now

# This warning category was introduced in NASM 3. Keep compatibility with the
# NASM 2 package currently shipped by GitHub's Ubuntu runners.
ifeq ($(shell $(NASM) -v 2>/dev/null | awk '{ split($$3, version, "."); if (version[1] >= 3) print "yes" }'),yes)
NASMFLAGS += -w-reloc-rel-dword
endif

ifeq ($(CONFIG),debug)
NASMFLAGS += -g -F dwarf
else
LDFLAGS += -Wl,--strip-debug
endif

.PHONY: all clean validate test benchmark
all: $(TARGET)

$(BUILD_DIR):
	mkdir -p $@

$(OBJECT): main.asm abi.inc *.asm | $(BUILD_DIR)
	$(NASM) $(NASMFLAGS) -o $@ main.asm

$(TARGET): $(OBJECT)
	$(CC) $(LDFLAGS) -o $@ $<

validate: $(TARGET)
	@test "$$($(shell command -v nm) -D --defined-only $(TARGET) | awk '{print $$3}' | sort | tr '\n' ' ')" = "operative_hidden_create operative_hidden_destroy operative_hidden_dispatch operative_hidden_get_abi_version operative_hidden_initialize operative_hidden_shutdown "

test: validate
	$(PYTHON) tests/test_native.py

benchmark: validate
	$(PYTHON) tests/benchmark_native.py

clean:
	rm -f $(OBJECT) $(TARGET)

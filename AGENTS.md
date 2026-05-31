# ETS2 Cheat Agent Notes

These notes document how the ETS2 damage reset/stopper was found and how to re-find it after a game update.

## Current Build Findings

Target process:

- Process: `eurotrucks2.exe`
- Module: `eurotrucks2.exe`

Working money add path:

- Stable pointer chain:
  - `eurotrucks2.exe+2D4F118`
  - read qword: money container
  - money container `+10`
  - read qword: money object
  - money object `+10`
  - read/write qword money value
- Tested live:
  - `eurotrucks2.exe+2D4F118 -> 28FD0131450`
  - `28FD0131450+10 -> 28FD7E01ED0`
  - `28FD7E01ED0+10 -> 1002333467`

Money writer found while spending:

```asm
eurotrucks2.exe+76D11E: mov rax,[rcx+10]
eurotrucks2.exe+76D122: sub rax,rbx
eurotrucks2.exe+76D129: mov [rcx+10],rax
```

At the writer:

- `RCX` was the money object.
- `RCX+10` was the qword money value.
- `RBX/R8` held the spend amount.
- Directly adding or subtracting `100000` from `[moneyObject+10]` updates the UI.

Working stop-damage patches:

- `eurotrucks2.exe+10C81F9`
  - Original: `44 89 44 88 04`
  - Patch: `83 64 88 04 00`
  - Meaning: clamp the committed display/cache damage write safely.
- `eurotrucks2.exe+10C7A10`
  - Original: `41 8B 41 04`
  - Patch: `31 C0 66 90`
- `eurotrucks2.exe+10C7B4A`
  - Original: `F3 48 0F 2C C2`
  - Patch: `31 C0 0F 1F 00`
- `eurotrucks2.exe+10C7600`
  - Original: `8B 41 04`
  - Patch: `31 C0 90`
- `eurotrucks2.exe+8FC9B5`
  - Original: `44 89 38`
  - Patch: `89 10 90`

Unsafe patch that crashed and should not be reused:

- `eurotrucks2.exe+10C81F9`
  - Bad patch: `xor r8d,r8d; nop; nop`

Working proper reset path:

- Stable pointer chain:
  - `eurotrucks2.exe+33C0548`
  - read qword, then `+2F98`
  - read qword: current truck object
  - current truck `+18`
  - read qword: primary damage root
  - damage root `+1A8`
  - read qword: damage node
- Source damage floats to zero:
  - damage node `+80`
  - damage node `+128`
  - damage node `+160`
  - damage node `+164`
  - damage node `+168`
- Source wear floats currently included in reset:
  - damage node `+84`
  - damage node `+12C`
  - damage node `+130`
  - damage node `+140`
  - damage node `+16C`
  - damage node `+170`
  - damage node `+174`
  - damage node `+1C8`
  - damage node `+254`
  - damage node `+33C`

The nearby float at damage node `+184` was observed as `0.75` and appears to be wear/condition, not crash damage. Do not zero it for damage reset.

Tyre/wheel damage note:

- Tyre damage was missed in the first reset because it was `0%` during the first source-float test.
- Later, the repair screen showed tyre damage `4%`.
- The matching source float was damage node `+128 = 0.043843`.
- Zeroing `+128` cleared tyre damage, so `+128` is now included in reset.

Wear note:

- After damage reset, the repair screen still showed `1%` wear on the axle/chassis and cabin rows.
- The nearby small wear-ish source values were:
  - `+84 = 0.019919`
  - `+12C = 0.004004`
  - `+130 = 0.005500`
  - `+140 = 0.024677`
  - `+16C = 0.006624`
  - `+170 = 0.003312`
  - `+174 = 0.015935`
  - `+1C8 = 0.022559`
  - `+254 = 0.009937`
  - `+33C = 0.077263`
- These were zeroed live and the repair screen confirmed the last axle wear row cleared to `0%`, so they were added to the C# reset path.

## Why Reset Works This Way

The game has cached/display integer damage values such as `7`, `8`, and `19`. Directly zeroing those can crash or only hide damage because the real component state still exists underneath.

The real source layer is stored as floats, for example:

- `0.052753` becomes about `5%`
- `0.017288` becomes about `1-2%`
- `0.010374` becomes about `1%`
- `0.042202` becomes about `4%`

Zeroing the source floats made the garage report and HUD both show `0%`.

## Trace History

Display/cache setter:

- `eurotrucks2.exe+10C81F9`
  - Instruction: `mov [rax+rcx*4+04],r8d`
  - `R8D` held the displayed damage percent.

Damage calculation caller:

```asm
eurotrucks2.exe+10C78B6: call eurotrucks2.exe+542300
eurotrucks2.exe+10C78BB: mulss xmm0,[...]
eurotrucks2.exe+10C78D0: cvttss2si rax,xmm0
eurotrucks2.exe+10C78D5: cmp eax,r15d
eurotrucks2.exe+10C78D8: cmova eax,r15d
eurotrucks2.exe+10C78DE: mov r8d,eax
eurotrucks2.exe+10C78E1: call eurotrucks2.exe+10C81B0
```

Second similar path:

```asm
eurotrucks2.exe+10C7907: call eurotrucks2.exe+542360
eurotrucks2.exe+10C790C: mulss xmm0,[...]
eurotrucks2.exe+10C791F: cvttss2si rax,xmm0
eurotrucks2.exe+10C792B: mov r8d,eax
eurotrucks2.exe+10C792E: call eurotrucks2.exe+10C81B0
```

Float reader:

```asm
eurotrucks2.exe+542300:
  mov r11,[rcx+18]
  mov r11,[r11+000001A8]
  movss xmm4,[r11+00000080]
  call eurotrucks2.exe+5BC280
  maxss xmm4,xmm0
  maxss xmm4,[r11+00000168]
  maxss xmm4,[r11+00000160]
  maxss xmm4,[r11+00000164]
  movaps xmm0,xmm4
  ret
```

This is how the source float offsets were identified.

## Re-Finding After A Game Update

If the app stops working after an ETS2 update, do this:

1. Attach Cheat Engine/MCP to `eurotrucks2.exe`.
2. Restore all code bytes to original first.
3. Repair the truck to `0%`.
4. Damage the truck to a small non-zero amount.
5. Open the garage damage report and record:
   - HUD damage percent
   - garage aggregate percent
   - component damage values
6. Find the display setter:
   - Scan dword for the HUD percent.
   - Damage a little more and narrow by increased/new exact percent.
   - Break on writes to a small candidate set.
   - Look for a setter where a register holds the displayed percent.
7. From that setter, inspect the caller that calculates the percent.
   - Look for `mulss` by `100.0`
   - Look for `cvttss2si`
   - Look for call(s) immediately before the multiply; those are source float readers.
8. Disassemble the source float reader.
   - Look for `movss`/`maxss` reads from a damage node.
   - Current build damage offsets: `+80`, `+128`, `+160`, `+164`, `+168`.
   - Current build wear offsets: `+84`, `+12C`, `+130`, `+140`, `+16C`, `+170`, `+174`, `+1C8`, `+254`, `+33C`.
9. Resolve the object pointer chain.
   - Search around global pointers referenced near the old caller.
   - Current build used `eurotrucks2.exe+33C0548 -> +2F98 -> current truck`.
   - Confirm by checking `truck+18 -> root`, `root+1A8 -> node`, then read source floats.
10. Test reset only by zeroing the source floats, not display/cache integers.
11. Close/reopen the garage report and confirm all component damage is `0%`.
12. Update `Program.cs` offsets if needed:
   - `TruckManagerGlobalOffset`
   - `CurrentTruckOffset`
   - `PrimaryDamageRootOffset`
   - `DamageNodeOffset`
   - `SourceDamageFloatOffsets`
   - code patch offsets/original bytes if stop damage changed.

## Re-Finding Money After A Game Update

1. Note the exact displayed money value.
2. Scan exact qword and dword for that value.
3. Spend or earn a small amount.
4. Next scan exact for the new value.
5. The current build narrowed to one address.
6. Break on writes to that money address.
7. Look for the spend writer:

```asm
mov rax,[rcx+10]
sub rax,rbx
mov [rcx+10],rax
```

8. The money object is `RCX`; the value is `RCX+10`.
9. Scan qword for the money object pointer.
   - Current build found a single owner slot at `container+10`.
10. Scan qword for the container pointer.
    - Current build found a module static reference at `eurotrucks2.exe+2D4F118`.
11. Verify:

```lua
local base = getAddress('eurotrucks2.exe')
local container = readQword(base + 0x2D4F118)
local moneyObject = readQword(container + 0x10)
local money = readQword(moneyObject + 0x10)
print(container, moneyObject, money)
```

12. Update `MoneyContainerStaticOffset`, `MoneyObjectOffset`, or `MoneyValueOffset` in `Program.cs` if they changed.

## Cheat Engine Lua Snippets

Read current source floats:

```lua
local base = getAddress('eurotrucks2.exe')
local manager = readQword(base + 0x33C0548)
local truck = readQword(manager + 0x2F98)
local root = readQword(truck + 0x18)
local node = readQword(root + 0x1A8)

for _,off in ipairs({0x80,0x128,0x160,0x164,0x168,0x184}) do
  print(string.format('+%X %X %.6f', off, node + off, readFloat(node + off)))
end
```

Reset source damage:

```lua
local base = getAddress('eurotrucks2.exe')
local manager = readQword(base + 0x33C0548)
local truck = readQword(manager + 0x2F98)
local root = readQword(truck + 0x18)
local node = readQword(root + 0x1A8)

for _,off in ipairs({0x80,0x128,0x160,0x164,0x168,0x84,0x12C,0x130,0x140,0x16C,0x170,0x174,0x1C8,0x254,0x33C}) do
  writeFloat(node + off, 0)
end
```

Change money:

```lua
local base = getAddress('eurotrucks2.exe')
local container = readQword(base + 0x2D4F118)
local moneyObject = readQword(container + 0x10)
local moneyAddress = moneyObject + 0x10
writeQword(moneyAddress, readQword(moneyAddress) + 100000)
-- or subtract:
-- writeQword(moneyAddress, readQword(moneyAddress) - 100000)
```

## C# App Location

Project:

- `C:\Users\Craig\Documents\Codex\2026-05-30\can-you-set-this-github-up\outputs\Ets2DamagePatcherGui`

Published EXE:

- `C:\Users\Craig\Documents\Codex\2026-05-30\can-you-set-this-github-up\outputs\Ets2DamagePatcherGui\run\ETS2 Cheat.exe`

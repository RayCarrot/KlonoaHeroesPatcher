using System;
using System.Collections.Generic;
using BinarySerializer;

namespace KlonoaHeroesPatcher;

public class ModifiedSFXTextPatch : Patch
{
    public ModifiedSFXTextPatch()
    {
        Entries = new Entry[]
        {
            new Entry(new int[] { 0, 4, 12 }, 1),
            new Entry(new int[] { 1, 9, 12 }, 0),
            new Entry(new int[] { 3, 7, 8 }, 1),
            new Entry(new int[] { 10, 4, 12 }, 0),
            new Entry(new int[] { 4, 5, 2, 6 }, 0),
            new Entry(new int[] { 1, 12, 11 }, 1),
        };
    }

    private const long _switchTableAddress = 0x08037ec0;
    private readonly byte[] _switchTableOriginalBytes =
    {
        0xd8, 0x7e, 0x03, 0x08, 
        0xea, 0x7e, 0x03, 0x08, 
        0xf8, 0x7e, 0x03, 0x08, 
        0x04, 0x7f, 0x03, 0x08, 
        0x16, 0x7f, 0x03, 0x08, 
        0x2c, 0x7f, 0x03, 0x08,
    };
    private readonly long[] _setAnimIndexAddresses =
    {
        0x08037f26,
        0x08037f38
    };

    public override string ID => "SFXT";
    public override string DisplayName => "Modified SFX Text";
    public Entry[] Entries { get; }

    public override object GetPatchUI() => new ModifiedSFXTextPatchUI()
    {
        DataContext = new ModifiedSFXTextPatchViewModel(this, AppViewModel.Current.ROM.GameplayPack.File_14)
    };

    public override void Load(BinaryDeserializer s, BinaryFile romFile)
    {
        // Read the modified switch table
        Pointer[] switchTable = s.DoAt(new Pointer(_switchTableAddress, romFile), 
            () => s.SerializePointerArray(default, Entries.Length, name: "SwitchTable"));

        // Load each entry
        for (int entryIndex = 0; entryIndex < Entries.Length; entryIndex++)
        {
            // Go to the modified code
            s.DoAt(switchTable[entryIndex], () =>
            {
                List<int> animGroupIndices = new();
                int animIndex;

                while (true)
                {
                    byte[] code = s.SerializeArray<byte>(default, 4);

                    // mov
                    if (code[1] == 0x20)
                    {
                        animGroupIndices.Add(code[0]);
                    }
                    // ldr
                    else
                    {
                        Pointer returnPointer = s.SerializePointer(default);
                        animIndex = Array.IndexOf(_setAnimIndexAddresses, returnPointer.AbsoluteOffset);

                        break;
                    }
                }

                Entries[entryIndex] = new Entry(animGroupIndices.ToArray(), animIndex);
            });
        }
    }

    public override void Apply(BinarySerializer.BinarySerializer s, BinaryFile romFile)
    {
        for (int entryIndex = 0; entryIndex < Entries.Length; entryIndex++)
        {
            Entry entry = Entries[entryIndex];

            Pointer dataAddress = s.CurrentPointer;

            // Replace the switch table pointer to where we're adding the new code
            s.DoAt(new Pointer(_switchTableAddress + entryIndex * 4, romFile), 
                () => s.SerializePointer(dataAddress, name: $"SwitchTable[{entryIndex}]"));

            // Write the code for setting up the anim group indices array
            for (int i = 0; i < entry.AnimGroupIndices.Length; i++)
            {
                int animGroupIndex = entry.AnimGroupIndices[i];

                s.SerializeArray<byte>(new byte[]
                {
                    (byte)animGroupIndex, 0x20, // mov   r0, animGroupIndex
                }, 2);

                ushort str = (ushort)BitHelpers.SetBits(0b0110000000100000, i, 3, 6);

                s.Serialize<ushort>(str); // str   r0,[r4,i]
            }

            // Write the code for returning back to the original code
            s.SerializeArray<byte>(new byte[]
            {
                0x00, 0x48, // ldr   (load address which we write after this into r0)
                0x87, 0x46, // mov   pc, r0 (set pc to the value stored in r0)
            }, 4);

            // Write address for returning
            s.SerializePointer(new Pointer(_setAnimIndexAddresses[entry.AnimIndex], romFile));
        }
    }

    public override void Revert(BinarySerializer.BinarySerializer s, BinaryFile romFile)
    {
        // Revert the switch table
        s.DoAt(new Pointer(_switchTableAddress, romFile), 
            () => s.SerializeArray<byte>(_switchTableOriginalBytes, _switchTableOriginalBytes.Length, name: "SwitchTableOriginalBytes"));
    }

    public record Entry(int[] AnimGroupIndices, int AnimIndex);
}
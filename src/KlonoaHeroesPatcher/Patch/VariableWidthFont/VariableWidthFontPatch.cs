using BinarySerializer;

namespace KlonoaHeroesPatcher
{
    public class VariableWidthFontPatch : Patch
    {
        private const long _functionPatchAddress = 0x0802e84a;
        private const long _newFunctionPointerAddress = 0x0802e85c;
        public const uint _maxTileIndex = 0x3FF;
        private readonly byte[] _functionPatchOriginalBytes = { 0x08, 0x35, 0x02, 0x21, 0x8a, 0x44, 0x02, 0x48 };
        private readonly byte[] _functionPatchBytes =
        {
            0x04, 0x48,             // ldr    r0,[DAT_0802e85c] ; Load the address at 0x0802e85c into r0
            0x38, 0xf0, 0xc4, 0xfc, // bl     FUN_080671d8      ; Call FUN_080671d8 which in turns calls the function at r0
            0x00, 0x00,             // nop                      ; Since we overwrite what's at 0x0802e85c we can't process this here
        };
        private readonly byte[] _newFunctionBytes =
        {
            0x00, 0xB5, // push   { lr }       ; Save return address.
            0x40, 0xB4, // push   { r6 }       ; Push r6 onto the stack.
                
            0x04, 0x49, // ldr    r1,[pc+4]    ; Set r1 to the pointer defined at pc+4 (right after the function). This leads to the width table.
            0x26, 0x88, // ldrh   r6,[r4,#0x0] ; r6 = *r4 (r4 is the text buffer, so we get the current value from it as a 16-bit value).
                
            0x89, 0x5d, // ldrb   r1,[r1,r6]   ; r1 = r1 + r6 (r1 is the width table, r6 is the index).
            0x6d, 0x18, // add    r5,r5,r1     ; r5 = r5 + r1 (r5 is the x position, so we add the specified width to it).
                
            0x02, 0x21, // mov    r1,#0x2      ; r1 = 2
            0x8a, 0x44, // add    r10, r1      ; r10 = r10 + r1 (add 2 to the tile index)
            0x02, 0x48, // ldr    r0, [pc+2]   ; Set r0 to the value at pc+2, this is the max tile index

            0x40, 0xbc, // pop    { r6 }       ; Pop r6 from stack
                
            0x00, 0xbd, // pop    { pc }       ; Return
        };

        public override string ID => "VWF";
        public override string DisplayName => "Variable Width Font";
        public byte[] Widths { get; set; }

        public override object GetPatchUI() => new VariableWidthFontPatchUI()
        {
            DataContext = new VariableWidthFontPatchViewModel(this, AppViewModel.Current.ROM.UIPack.Font_0)
        };

        public override void Load(BinaryDeserializer s, BinaryFile romFile)
        {
            // Skip the function
            s.Goto(s.CurrentPointer + _newFunctionBytes.Length);
            s.Align();
            s.Goto(s.CurrentPointer + 8);

            // Read the width table
            int length = s.Serialize<int>(default, name: "WidthTableLength");
            Widths = s.SerializeArray<byte>(default, length, name: "WidthTable");
        }

        public override void Apply(BinarySerializer.BinarySerializer s, BinaryFile romFile)
        {
            Pointer dataAddress = s.CurrentPointer;

            // Patch the text render function
            s.DoAt(new Pointer(_functionPatchAddress, romFile), () => s.SerializeArray<byte>(_functionPatchBytes, _functionPatchBytes.Length, name: "FunctionPatchBytes"));

            // Set the pointer to the new function. We need to do +1 since we want it in Thumb rather than Arm. Bit 0 determines this.
            s.DoAt(new Pointer(_newFunctionPointerAddress, romFile), () => s.Serialize<uint>((uint)dataAddress.AbsoluteOffset + 1, name: "NewFunctionPointer"));

            // Add the new function at the patch's data address
            s.SerializeArray<byte>(_newFunctionBytes, _newFunctionBytes.Length, name: "Function");

            // Align
            s.Align();

            // Add the width table pointer
            s.SerializePointer(new Pointer(s.CurrentAbsoluteOffset + 12, romFile), name: "WidthTablePointer");

            // Add the max tile index
            s.Serialize<uint>(_maxTileIndex, name: "MaxTileIndex");

            // Write the table length (not referenced by any code, but good to have when parsing it)
            s.Serialize<int>(Widths.Length, name: "WidthTableLength");

            // Write the width table
            s.SerializeArray<byte>(Widths, Widths.Length, name: "WidthTable");
        }

        public override void Revert(BinarySerializer.BinarySerializer s, BinaryFile romFile)
        {
            // Revert the text render function
            s.DoAt(new Pointer(_functionPatchAddress, romFile), () => s.SerializeArray<byte>(_functionPatchOriginalBytes, _functionPatchOriginalBytes.Length, name: "FunctionPatchOriginalBytes"));

            // Revert the max tile index
            s.DoAt(new Pointer(_newFunctionPointerAddress, romFile), () => s.Serialize<uint>(_maxTileIndex, name: "MaxTileIndex"));
        }
    }
}
using System;
using BinarySerializer;
using BinarySerializer.Klonoa;

namespace KlonoaHeroesPatcher
{
    public class RelocatedData
    {
        public RelocatedData(BinarySerializable obj, ArchiveFile parentArchiveFile, bool isNewData)
        {
            Obj = obj;
            IsNewData = isNewData;
            UpdateRefsAction = (s, p) =>
            {
                // Enumerate every offset which points to this file object
                for (int fileIndex = 0; fileIndex < parentArchiveFile.OffsetTable.FilesCount; fileIndex++)
                {
                    if (parentArchiveFile.ParsedFiles[fileIndex].Item1 != obj)
                        continue;

                    s.DoAt(parentArchiveFile.OffsetTable.Offset + 4 + (fileIndex * 4), () =>
                    {
                        // Update the offset to point to the new location
                        s.Serialize<uint>((uint)(p.AbsoluteOffset - parentArchiveFile.Offset.AbsoluteOffset));
                    });
                }
            };
        }
        public RelocatedData(BinarySerializable obj, Action<SerializerObject, Pointer> updateRefsAction, bool isNewData)
        {
            Obj = obj;
            UpdateRefsAction = updateRefsAction;
            IsNewData = isNewData;
        }

        public BinarySerializable Obj { get; }
        public Action<SerializerObject, Pointer> UpdateRefsAction { get; }
        public bool IsNewData { get; }

        public PatchedFooter.RelocatedStruct Relocate(SerializerObject s)
        {
            // Get the current pointer. This is where the data is being relocated to.
            var newPointer = s.CurrentPointer;

            // Get the original pointer
            var origPointer = Obj.Offset;

            // Init the object
            Obj.Init(newPointer);

            // TODO: Some data might be compressed! We need an encoder.

            // Serialize the object
            Obj.SerializeImpl(s);

            // Update all the references in the ROM to the data with the new relocated pointer
            UpdateRefsAction(s, newPointer);

            return new PatchedFooter.RelocatedStruct
            {
                OriginalPointer = origPointer,
                NewPointer = newPointer,
                DataSize = (uint)(s.CurrentPointer.FileOffset - newPointer.FileOffset)
            };
        }
    }
}
using System;
using BinarySerializer;
using BinarySerializer.Klonoa;
using NLog;

namespace KlonoaHeroesPatcher
{
    public class RelocatedData
    {
        public RelocatedData(BinarySerializable obj, ArchiveFile parentArchiveFile, bool isNewData)
        {
            Obj = obj;
            IsNewData = isNewData;
            UpdateRefsAction = (s, originalPointer, newPointer) =>
            {
                int count = 0;

                // Enumerate every offset which points to this file object
                for (int fileIndex = 0; fileIndex < parentArchiveFile.OffsetTable.FilesCount; fileIndex++)
                {
                    if (parentArchiveFile.OffsetTable.FilePointers[fileIndex] != originalPointer)
                        continue;

                    s.DoAt(parentArchiveFile.OffsetTable.Offset + 4 + (fileIndex * 4), () =>
                    {
                        // Update the offset to point to the new location
                        s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - parentArchiveFile.Offset.AbsoluteOffset));
                    });

                    count++;
                }

                Logger.Info("Updated {0} file references for relocated data from 0x{1}", count, originalPointer.StringAbsoluteOffset);
            };
        }
        public RelocatedData(BinarySerializable obj, UpdateRefs updateRefsAction, bool isNewData)
        {
            Obj = obj;
            UpdateRefsAction = updateRefsAction;
            IsNewData = isNewData;
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public BinarySerializable Obj { get; }
        public UpdateRefs UpdateRefsAction { get; }
        public bool IsNewData { get; }

        public PatchedFooter.RelocatedStruct Relocate(SerializerObject s)
        {
            // Get the current pointer. This is where the data is being relocated to.
            Pointer newPointer = s.CurrentPointer;

            // Get the original pointer
            Pointer origPointer = Obj.Offset;

            // Init the object
            Obj.Init(newPointer);

            // TODO: Some data might be compressed! We need an encoder.

            // Serialize the object
            Obj.SerializeImpl(s);

            uint dataSize = (uint)(s.CurrentPointer.FileOffset - newPointer.FileOffset);

            Logger.Info("Relocated data from 0x{0} to 0x{1} with the size of {2}", origPointer.StringAbsoluteOffset, newPointer.StringAbsoluteOffset, dataSize);

            // Update all the references in the ROM to the data with the new relocated pointer
            UpdateRefsAction(s, origPointer, newPointer);

            return new PatchedFooter.RelocatedStruct
            {
                OriginalPointer = origPointer,
                NewPointer = newPointer,
                DataSize = dataSize
            };
        }

        public delegate void UpdateRefs(SerializerObject s, Pointer originalPointer, Pointer newPointer);
    }
}
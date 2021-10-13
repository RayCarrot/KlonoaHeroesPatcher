using System;
using BinarySerializer;
using BinarySerializer.Klonoa;
using NLog;

namespace KlonoaHeroesPatcher
{
    public class RelocatedData
    {
        public RelocatedData(BinarySerializable obj, ArchiveFile parentArchiveFile, Pointer originPointer = null)
        {
            Obj = obj;
            ParentArchiveFile = parentArchiveFile;
            OriginPointer = originPointer;
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

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public BinarySerializable Obj { get; }
        public ArchiveFile ParentArchiveFile { get; }
        public UpdateRefs UpdateRefsAction { get; }
        public Pointer OriginPointer { get; } // Only specified if it's not new data
        public bool IsNewData => OriginPointer == null; // Indicates if this data is being relocated first time. Otherwise it has been relocated before.

        public PatchedFooter.RelocatedStruct Relocate(SerializerObject s)
        {
            // Get the current pointer. This is where the data is being relocated to.
            Pointer newPointer = s.CurrentPointer;

            // Get the original pointer
            Pointer originalPointer = Obj.Offset;

            // Init the object
            Obj.Init(newPointer);

            // TODO: Some data might be compressed! We need an encoder.

            // Serialize the object
            Obj.SerializeImpl(s);

            uint dataSize = (uint)(s.CurrentPointer.FileOffset - newPointer.FileOffset);

            Logger.Info("Relocated data from 0x{0} to 0x{1} with the size of {2}", originalPointer.StringAbsoluteOffset, newPointer.StringAbsoluteOffset, dataSize);

            // Update all the references in the ROM to the data with the new relocated pointer
            UpdateRefsAction(s, originalPointer, newPointer);

            return new PatchedFooter.RelocatedStruct
            {
                OriginalPointer = OriginPointer ?? originalPointer,
                NewPointer = newPointer,
                ParentArchivePointer = ParentArchiveFile.Offset,
                DataSize = dataSize
            };
        }

        public delegate void UpdateRefs(SerializerObject s, Pointer originalPointer, Pointer newPointer);
    }
}
using System;
using BinarySerializer;
using BinarySerializer.Klonoa;
using NLog;

namespace KlonoaHeroesPatcher
{
    public record RelocatedData
    {
        public RelocatedData(BinarySerializable obj, ArchiveFile parentArchiveFile)
        {
            Obj = obj;
            Offset = BinaryHelpers.GetROMPointer(Obj.Offset);
            ParentArchiveFile = parentArchiveFile;
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected BinarySerializable Obj { get; }
        public Pointer Offset { get; }
        protected ArchiveFile ParentArchiveFile { get; }

        public Pointer OriginPointer { get; init; } // Only specified if it's not new data
        public bool IsNewData => OriginPointer == null; // Indicates if this data is being relocated first time. Otherwise it has been relocated before.
        public IStreamEncoder Encoder { get; init; }
        public bool IsCompressed => Encoder != null;

        protected void UpdateDataReferences(SerializerObject s, Pointer originalPointer, Pointer newPointer, long fileSize)
        {
            int count = 0;

            // Enumerate every offset which points to this file object
            for (int fileIndex = 0; fileIndex < ParentArchiveFile.OffsetTable.FilesCount; fileIndex++)
            {
                if (ParentArchiveFile.OffsetTable.FilePointers[fileIndex] != originalPointer)
                    continue;

                Pointer anchor = ParentArchiveFile.Offset;

                // Perhaps this could be replaced by serializing the offset table object?
                switch (ParentArchiveFile.Pre_Type)
                {
                    case ArchiveFileType.Default:
                        s.DoAt(ParentArchiveFile.OffsetTable.Offset + 4 + (fileIndex * 4), () =>
                        {
                            // Update the offset to point to the new location
                            s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - anchor.AbsoluteOffset));
                        });
                        break;

                    case ArchiveFileType.KH_PF:
                        anchor = ParentArchiveFile.OffsetTable.Offset + 4 + (ParentArchiveFile.OffsetTable.FilesCount * 4) + (ParentArchiveFile.OffsetTable.FilesCount * 4);
                        s.DoAt(ParentArchiveFile.OffsetTable.Offset + 4 + (ParentArchiveFile.OffsetTable.FilesCount * 4) + (fileIndex * 4), () =>
                        {
                            // Update the offset to point to the new location
                            s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - anchor.AbsoluteOffset));
                        });
                        s.DoAt(ParentArchiveFile.OffsetTable.Offset + 4 + (fileIndex * 4), () =>
                        {
                            // Update the file size
                            s.Serialize<int>((int)fileSize);
                        });
                        break;

                    case ArchiveFileType.KH_TP:
                        s.DoAt(ParentArchiveFile.OffsetTable.KH_TP_FileOffsetsPointer + (fileIndex * 4), () =>
                        {
                            // Update the offset to point to the new location
                            s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - anchor.AbsoluteOffset));
                        });
                        break;

                    case ArchiveFileType.KH_KW:
                        s.DoAt(ParentArchiveFile.OffsetTable.KH_KW_FileOffsetsPointer + (fileIndex * 16) + 4, () =>
                        {
                            // Update the offset to point to the new location
                            s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - anchor.AbsoluteOffset));
                        });
                        break;

                    default:
                        throw new Exception($"Unsupported archive type {ParentArchiveFile.Pre_Type}");
                }

                count++;
            }

            Logger.Info("Updated {0} file references for relocated data from 0x{1}", count, originalPointer.StringAbsoluteOffset);
        }

        public PatchedFooter.RelocatedStruct Relocate(SerializerObject s)
        {
            // Get the current pointer. This is where the data is being relocated to.
            Pointer newPointer = s.CurrentPointer;

            // Get the original pointer
            Pointer originalPointer = BinaryHelpers.GetROMPointer(Obj.Offset);

            // If it's a compressed archive we want to update the offsets to make sure the data is written correctly
            if (Obj is ArchiveFile { Pre_IsCompressed: true } a)
                a.RecalculateFileOffsets();

            // Write the data
            s.DoEncodedIf(Encoder, IsCompressed, () =>
            {
                // Init the object
                Obj.Init(s.CurrentPointer);

                // Serialize the object
                Obj.SerializeImpl(s);
            });

            uint dataSize = (uint)(s.CurrentPointer.FileOffset - newPointer.FileOffset);

            Logger.Info("Relocated data from 0x{0} to 0x{1} with the size of {2}", originalPointer.StringAbsoluteOffset, newPointer.StringAbsoluteOffset, dataSize);

            // Update all the references in the ROM to the data with the new relocated pointer
            UpdateDataReferences(s, originalPointer, newPointer, dataSize);

            return new PatchedFooter.RelocatedStruct
            {
                OriginalPointer = OriginPointer ?? originalPointer,
                NewPointer = newPointer,
                ParentArchivePointer = ParentArchiveFile.Offset,
                DataSize = dataSize
            };
        }
    }
}
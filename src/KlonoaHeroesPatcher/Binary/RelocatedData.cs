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
            ParentArchiveFile = parentArchiveFile;
            UpdateRefsAction = (s, originalPointer, newPointer, fileSize) =>
            {
                int count = 0;

                // Enumerate every offset which points to this file object
                for (int fileIndex = 0; fileIndex < parentArchiveFile.OffsetTable.FilesCount; fileIndex++)
                {
                    if (parentArchiveFile.OffsetTable.FilePointers[fileIndex] != originalPointer)
                        continue;

                    var anchor = parentArchiveFile.Offset;

                    // Perhaps this could be replaced by serializing the offset table object?
                    switch (parentArchiveFile.Pre_Type)
                    {
                        case ArchiveFileType.Default:
                            s.DoAt(parentArchiveFile.OffsetTable.Offset + 4 + (fileIndex * 4), () =>
                            {
                                // Update the offset to point to the new location
                                s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - anchor.AbsoluteOffset));
                            });
                            break;
                        
                        case ArchiveFileType.KH_PF:
                            anchor = parentArchiveFile.OffsetTable.Offset + 4 + (parentArchiveFile.OffsetTable.FilesCount * 4) + (parentArchiveFile.OffsetTable.FilesCount * 4);
                            s.DoAt(parentArchiveFile.OffsetTable.Offset + 4 + (parentArchiveFile.OffsetTable.FilesCount * 4) + (fileIndex * 4), () =>
                            {
                                // Update the offset to point to the new location
                                s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - anchor.AbsoluteOffset));
                            });
                            s.DoAt(parentArchiveFile.OffsetTable.Offset + 4 + (fileIndex * 4), () =>
                            {
                                // Update the file size
                                s.Serialize<int>((int)fileSize);
                            });
                            break;
                        
                        case ArchiveFileType.KH_TP:
                            s.DoAt(parentArchiveFile.OffsetTable.KH_TP_FileOffsetsPointer + (fileIndex * 4), () =>
                            {
                                // Update the offset to point to the new location
                                s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - anchor.AbsoluteOffset));
                            });
                            break;

                        case ArchiveFileType.KH_KW:
                            s.DoAt(parentArchiveFile.OffsetTable.KH_KW_FileOffsetsPointer + (fileIndex * 16) + 4, () =>
                            {
                                // Update the offset to point to the new location
                                s.Serialize<uint>((uint)(newPointer.AbsoluteOffset - anchor.AbsoluteOffset));
                            });
                            break;

                        default:
                            throw new Exception($"Unsupported archive type {parentArchiveFile.Pre_Type}");
                    }

                    count++;
                }

                Logger.Info("Updated {0} file references for relocated data from 0x{1}", count, originalPointer.StringAbsoluteOffset);
            };
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected BinarySerializable Obj { get; }
        protected ArchiveFile ParentArchiveFile { get; }
        protected UpdateRefs UpdateRefsAction { get; }

        public Pointer Offset => Obj.Offset;
        public Pointer OriginPointer { get; init; } // Only specified if it's not new data
        public bool IsNewData => OriginPointer == null; // Indicates if this data is being relocated first time. Otherwise it has been relocated before.
        public IStreamEncoder Encoder { get; init; }
        public bool IsCompressed => Encoder != null;

        public PatchedFooter.RelocatedStruct Relocate(SerializerObject s)
        {
            // Get the current pointer. This is where the data is being relocated to.
            Pointer newPointer = s.CurrentPointer;

            // Get the original pointer
            Pointer originalPointer = BinaryHelpers.GetROMPointer(Obj.Offset);

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
            UpdateRefsAction(s, originalPointer, newPointer, dataSize);

            return new PatchedFooter.RelocatedStruct
            {
                OriginalPointer = OriginPointer ?? originalPointer,
                NewPointer = newPointer,
                ParentArchivePointer = ParentArchiveFile.Offset,
                DataSize = dataSize
            };
        }

        public delegate void UpdateRefs(SerializerObject s, Pointer originalPointer, Pointer newPointer, long fileSize);
    }
}
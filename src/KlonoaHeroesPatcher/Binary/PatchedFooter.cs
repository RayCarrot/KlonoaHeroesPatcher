﻿using BinarySerializer;

namespace KlonoaHeroesPatcher
{
    public class PatchedFooter : BinarySerializable
    {
        public const string Magic = "EDIT";
        public const int CurrentEditorVersion = 2;

        public int EditorVersion { get; set; } = CurrentEditorVersion;

        public int PatchDatasCount { get; set; }
        public PatchData[] PatchDatas { get; set; } = new PatchData[0];

        public int RelocatedStructsCount { get; set; }
        public RelocatedStruct[] RelocatedStructs { get; set; } = new RelocatedStruct[0];

        public override void SerializeImpl(SerializerObject s)
        {
            // Get the start of the footer data
            var footerOffset = s.CurrentPointer.FileOffset;

            // Check the magic footer header to make sure we're reading an editor footer
            var magic = s.SerializeString(Magic, 4, name: nameof(Magic));

            // If the magic is not correct we return
            if (Magic != magic)
            {
                s.LogWarning($"Incorrect magic identifier {magic}");
                return;
            }

            // Get the editor version
            EditorVersion = s.Serialize<int>(EditorVersion, name: nameof(EditorVersion));

            // If the version is higher than the current version we don't read the data
            if (EditorVersion > CurrentEditorVersion)
            {
                s.LogWarning($"Unknown editor version {EditorVersion}");

                // Set to the current version again so it's correct when we write the data
                EditorVersion = CurrentEditorVersion;

                return;
            }

            // Version 2 adds patch data
            if (EditorVersion >= 2)
            {
                PatchDatasCount = s.Serialize<int>(PatchDatasCount, name: nameof(PatchDatasCount));
                PatchDatas = s.SerializeObjectArray<PatchData>(PatchDatas, PatchDatasCount, name: nameof(PatchDatas));
            }

            // Keep a table of relocated data structs
            RelocatedStructsCount = s.Serialize<int>(RelocatedStructsCount, name: nameof(RelocatedStructsCount));

            // Make sure the count is reasonable
            if (RelocatedStructsCount is < 0 or > 9999)
            {
                s.LogWarning($"Invalid relocated structs count {RelocatedStructsCount}");
                return;
            }

            RelocatedStructs = s.SerializeObjectArray<RelocatedStruct>(RelocatedStructs, RelocatedStructsCount, name: nameof(RelocatedStructs));

            // End with the footer offset and magic. This way the footer can be read without knowing where it begins.
            if (s.CurrentLength - s.CurrentPointer.FileOffset >= 12)
                s.Goto(Offset.File.StartPointer + s.CurrentLength - 12);

            s.Serialize<long>(footerOffset, name: nameof(footerOffset));
            magic = s.SerializeString(Magic, 4, name: nameof(Magic));

            if (Magic != magic)
                s.LogWarning($"Unknown magic identifier {magic}");
        }

        public void TryReadFromEnd(BinaryDeserializer s, Pointer fileStart)
        {
            s.Goto(fileStart + s.CurrentLength - 4);
            var magic = s.SerializeString(default, 4, name: "Magic");

            if (magic != Magic)
                return;

            s.Goto(fileStart + s.CurrentLength - 12);
            var footerOffset = s.Serialize<long>(default, name: "FooterOffset");

            if (footerOffset >= s.CurrentLength - 12 || footerOffset <= 0)
                return;

            s.Goto(fileStart + footerOffset);

            Init(s.CurrentPointer);
            SerializeImpl(s);
        }
        
        public class PatchData : BinarySerializable
        {
            public string ID { get; set; }
            public Pointer DataPointer { get; set; }
            public uint DataSize { get; set; }

            public override void SerializeImpl(SerializerObject s)
            {
                ID = s.SerializeString(ID, 4, name: nameof(ID));
                DataPointer = s.SerializePointer(DataPointer, name: nameof(DataPointer));
                DataSize = s.Serialize<uint>(DataSize, name: nameof(DataSize));
            }
        }

        public class RelocatedStruct : BinarySerializable
        {
            public Pointer OriginalPointer { get; set; }
            public Pointer NewPointer { get; set; }
            public Pointer ParentArchivePointer { get; set; }
            public uint DataSize { get; set; }

            public override void SerializeImpl(SerializerObject s)
            {
                OriginalPointer = s.SerializePointer(OriginalPointer, name: nameof(OriginalPointer));
                NewPointer = s.SerializePointer(NewPointer, name: nameof(NewPointer));
                ParentArchivePointer = s.SerializePointer(ParentArchivePointer, name: nameof(ParentArchivePointer));
                DataSize = s.Serialize<uint>(DataSize, name: nameof(DataSize));
            }
        }
    }
}
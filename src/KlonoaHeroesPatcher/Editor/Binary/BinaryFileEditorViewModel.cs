﻿using BinarySerializer;
using BinarySerializer.Klonoa;

namespace KlonoaHeroesPatcher
{
    public class BinaryFileEditorViewModel : FileEditorViewModel
    {
        public RawData_File RawFile => (RawData_File)SerializableObject;

        public string HexString { get; set; }

        protected override void Load(bool firstLoad)
        {
            HexString = RawFile.Data.ToHexString(align: 16);
        }
    }
}
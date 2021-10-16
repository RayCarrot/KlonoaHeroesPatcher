using System;
using BinarySerializer;

namespace KlonoaHeroesPatcher
{
    public static class BinaryHelpers
    {
        public static Pointer GetROMPointer(Pointer pointer, bool throwOnError = true)
        {
            if (pointer == null) 
                throw new ArgumentNullException(nameof(pointer));
            
            if (pointer.File is VirtualFile virtualFile)
            {
                if (pointer.FileOffset != 0)
                    return !throwOnError ? null : throw new Exception($"Can't get ROM pointer for virtual pointer at offset 0x{pointer.FileOffset:X8}");

                pointer = virtualFile.ParentPointer;

                if (pointer == null)
                    return !throwOnError ? null : throw new Exception($"Can't get ROM pointer for virtual pointer where no parent pointer has been specified");

                if (pointer.File is VirtualFile)
                    return !throwOnError ? null : throw new Exception($"Can't get ROM pointer for virtual pointer within a second virtual file");

                return pointer;
            }

            return pointer;
        }
    }
}
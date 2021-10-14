using BinarySerializer;

namespace KlonoaHeroesPatcher
{
    public static class BinaryHelpers
    {
        public static Pointer GetROMPointer(Pointer pointer)
        {
            if (pointer.File is VirtualFile virtualFile)
                return virtualFile.ParentPointer;
            else
                return pointer;
        }
    }
}
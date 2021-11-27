using BinarySerializer;

namespace KlonoaHeroesPatcher;

public abstract class Patch
{
    public abstract string ID { get; } // 4 characters
    public abstract string DisplayName { get; }

    public abstract object GetPatchUI();

    public abstract void Load(BinaryDeserializer s, BinaryFile romFile);

    public abstract void Apply(BinarySerializer.BinarySerializer s, BinaryFile romFile);

    public abstract void Revert(BinarySerializer.BinarySerializer s, BinaryFile romFile);
}
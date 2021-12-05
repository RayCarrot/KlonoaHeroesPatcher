using BinarySerializer;

namespace KlonoaHeroesPatcher;

public class ModifiedSFXTextPatch : Patch
{
    public ModifiedSFXTextPatch()
    {
        Entries = new Entry[]
        {
            new Entry(new int[] { 0, 4, 12 }, 1),
            new Entry(new int[] { 1, 9, 12 }, 0),
            new Entry(new int[] { 3, 7, 8 }, 1),
            new Entry(new int[] { 10, 4, 12 }, 0),
            new Entry(new int[] { 4, 5, 2, 6 }, 0),
            new Entry(new int[] { 1, 12, 11 }, 1),
        };
    }

    public override string ID => "SFXT";
    public override string DisplayName => "Modified SFX Text";
    public Entry[] Entries { get; }

    public override object GetPatchUI() => new ModifiedSFXTextPatchUI()
    {
        DataContext = new ModifiedSFXTextPatchViewModel(this, AppViewModel.Current.ROM.GameplayPack.File_14)
    };

    public override void Load(BinaryDeserializer s, BinaryFile romFile)
    {
        // TODO: Implement
    }

    public override void Apply(BinarySerializer.BinarySerializer s, BinaryFile romFile)
    {
        // TODO: Implement
    }

    public override void Revert(BinarySerializer.BinarySerializer s, BinaryFile romFile)
    {
        // TODO: Implement
    }

    public record Entry(int[] AnimGroupIndices, int AnimIndex);
}
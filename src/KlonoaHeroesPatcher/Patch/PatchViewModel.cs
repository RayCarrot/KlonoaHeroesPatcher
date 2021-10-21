namespace KlonoaHeroesPatcher
{
    public class PatchViewModel : BaseViewModel
    {
        public PatchViewModel(Patch patch, bool wasEnabled)
        {
            Patch = patch;
            WasEnabled = wasEnabled;
            IsEnabled = wasEnabled;
        }

        public Patch Patch { get; }

        public object _patchUI;
        public object PatchUI => _patchUI ??= Patch.GetPatchUI();

        public bool WasEnabled { get; }
        public bool IsEnabled { get; set; }
    }
}
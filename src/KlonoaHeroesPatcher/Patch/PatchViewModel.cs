namespace KlonoaHeroesPatcher
{
    public class PatchViewModel : BaseViewModel
    {
        public PatchViewModel(Patch patch, bool wasEnabled)
        {
            Patch = patch;
            WasEnabled = wasEnabled;
            IsEnabled = wasEnabled;
            PatchUI = patch.GetPatchUI();
        }

        public Patch Patch { get; }
        public object PatchUI { get; }

        public bool WasEnabled { get; }
        public bool IsEnabled { get; set; }
    }
}
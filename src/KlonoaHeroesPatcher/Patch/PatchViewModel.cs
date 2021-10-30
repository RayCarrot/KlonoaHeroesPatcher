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
        public object PatchUI => _patchUI ??= IsEnabled ? Patch.GetPatchUI() : null;

        public bool WasEnabled { get; }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;

                if (_isEnabled)
                    OnPropertyChanged(nameof(PatchUI));
            }
        }
    }
}
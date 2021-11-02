using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using BinarySerializer;
using BinarySerializer.Klonoa;
using BinarySerializer.Klonoa.KH;
using MahApps.Metro.IconPacks;

namespace KlonoaHeroesPatcher
{
    public class FilesPageViewModel : BaseViewModel
    {
        #region Constructor

        public FilesPageViewModel()
        {
            SearchProvider = new BaseSuggestionProvider(SearchForEntries);

            NavigationItems = new ObservableCollection<NavigationItemViewModel>();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }
        public NavigationItemViewModel SelectedNavigationItem { get; set; }

        private NavigationItemViewModel _selectedSearchEntry;
        public NavigationItemViewModel SelectedSearchEntry
        {
            get => _selectedSearchEntry;
            set
            {
                _selectedSearchEntry = value;

                // If an entry has been selected we navigate to it and then clear the value
                if (SelectedSearchEntry != null)
                {
                    SelectItem(SelectedSearchEntry);
                    SelectedSearchEntry = null;
                }
            }
        }
        public BaseSuggestionProvider SearchProvider { get; }

        #endregion

        #region Public Methods

        private IEnumerable<NavigationItemViewModel> EnumerateNavigationItems()
        {
            return NavigationItems.SelectMany(x => x.GetAllChildren(true));
        }

        private IEnumerable<NavigationItemViewModel> SearchForEntries(string search)
        {
            return EnumerateNavigationItems().Where(y => y.DisplayName.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        public void AddNavigationItem(NavigationItemViewModel parent, PatchedFooter footer, ICollection<NavigationItemViewModel> collection, string title, BinarySerializable obj, ArchiveFile parentArchiveFile, ArchiveFile compressedParentArchiveFile = null, string overrideFileName = null)
        {
            FileEditorViewModel editor;
            PackIconMaterialKind icon;
            Color iconColor;
            ObservableCollection<DuoGridItemViewModel> info = new ObservableCollection<DuoGridItemViewModel>();

            info.Add(new DuoGridItemViewModel("File Type", obj?.GetType().Name));

            if (obj is BaseFile f)
            {
                info.Add(new DuoGridItemViewModel("File Size", $"{f.Pre_FileSize} bytes"));
                info.Add(new DuoGridItemViewModel("Compressed", $"{f.Pre_IsCompressed}"));
            }

            if (obj is Graphics_File)
            {
                editor = new GraphicsFileEditorViewModel();
                icon = PackIconMaterialKind.FileImageOutline;
                iconColor = Color.FromRgb(0xFF, 0xC1, 0x07);
            }
            else if (obj is Cutscene_File)
            {
                editor = new CutsceneFileEditorViewModel();
                icon = PackIconMaterialKind.FileVideoOutline;
                iconColor = Color.FromRgb(0x8B, 0xC3, 0x4A);
            }
            else if (obj is TextCommands_File)
            {
                editor = new TextFileEditorViewModel();
                icon = PackIconMaterialKind.FileDocumentOutline;
                iconColor = Color.FromRgb(0x4C, 0xAF, 0x50);
            }
            else if (obj is TextCollection_File)
            {
                editor = new TextCollectionFileEditorViewModel();
                icon = PackIconMaterialKind.FileDocumentMultipleOutline;
                iconColor = Color.FromRgb(0x4C, 0xAF, 0x50);
            }
            else if (obj is ItemsCollection_File)
            {
                editor = new ItemsCollectionFileEditorViewModel();
                icon = PackIconMaterialKind.ShapeOutline;
                iconColor = Color.FromRgb(0xE9, 0x1E, 0x63);
            }
            else if (obj is Animation_File)
            {
                editor = new AnimationFileEditorViewModel();
                icon = PackIconMaterialKind.FileVideoOutline;
                iconColor = Color.FromRgb(0x21, 0x96, 0xF3);
            }
            else if (obj is ArchiveFile a)
            {
                editor = null;
                icon = a.Pre_IsCompressed ? PackIconMaterialKind.FolderZipOutline : PackIconMaterialKind.FolderOutline;
                iconColor = Color.FromRgb(0xEF, 0x6C, 0x00);

                info.Add(new DuoGridItemViewModel("Archive Type", $"{a.Pre_Type}"));
            }
            else
            {
                editor = obj is RawData_File { Data: { } } ? new BinaryFileEditorViewModel() : null;

                icon = PackIconMaterialKind.FileOutline;
                iconColor = Color.FromRgb(0x8B, 0x00, 0x8B);
            }

            bool relocated = obj != null && footer.RelocatedStructs.Any(x => x.NewPointer == BinaryHelpers.GetROMPointer(obj.Offset, throwOnError: false));

            var navItem = new NavigationItemViewModel(parent, title, icon, iconColor, info, obj, editor, relocated, parentArchiveFile, compressedParentArchiveFile)
            {
                OverrideFileName = overrideFileName,
            };
            collection.Add(navItem);

            if (obj is not ArchiveFile archive)
                return;

            for (var fileIndex = 0; fileIndex < archive.ParsedFiles.Length; fileIndex++)
            {
                ArchiveFile.ParsedFile file = archive.ParsedFiles[fileIndex];

                string fileOverrideFileName = null;

                if (archive.Pre_Type == ArchiveFileType.KH_KW)
                {
                    var entry = archive.OffsetTable.KH_KW_Entries[fileIndex];
                    fileOverrideFileName = $"Map {entry.MapID1}-{entry.MapID2}-{entry.MapID3}";
                }
                else if (archive.Pre_Type == ArchiveFileType.KH_WMAP)
                {
                    var entry = archive.OffsetTable.KH_WMAP_Entries[fileIndex];
                    fileOverrideFileName = $"{entry.Name} {entry.ID}";
                }

                ArchiveFile navItemParentArchiveFile;
                ArchiveFile navItemcompressedParentArchiveFile;

                // This is an archive within a compressed archive
                if (compressedParentArchiveFile != null)
                {
                    // Use the same parent as before
                    navItemParentArchiveFile = parentArchiveFile;
                    navItemcompressedParentArchiveFile = compressedParentArchiveFile;
                }
                // This is a compressed archive
                else if (archive.Pre_IsCompressed)
                {
                    // Use the same parent as before
                    navItemParentArchiveFile = parentArchiveFile;
                    navItemcompressedParentArchiveFile = archive;
                }
                else
                {
                    navItemParentArchiveFile = archive;
                    navItemcompressedParentArchiveFile = null;
                }

                AddNavigationItem(
                    parent: navItem,
                    footer: footer,
                    collection: navItem.NavigationItems,
                    title: file?.Name,
                    obj: file?.Obj,
                    parentArchiveFile: navItemParentArchiveFile,
                    compressedParentArchiveFile: navItemcompressedParentArchiveFile,
                    overrideFileName: fileOverrideFileName);
            }
        }

        public void SelectItem(NavigationItemViewModel navItem)
        {
            // Expand the parent items
            var parent = navItem;

            while (parent != null)
            {
                // Only expand if there are children
                if (parent.NavigationItems.Any())
                    parent.IsExpanded = true;

                // Get the next parent
                parent = parent.Parent;
            }

            // Select the item
            navItem.IsSelected = true;
        }

        public void SelectFontFile(KlonoaHeroesROM rom)
        {
            var pack = NavigationItems.FirstOrDefault(x => x.SerializableObject == rom.UIPack);

            if (pack == null)
                return;

            pack.IsExpanded = true;

            var file = pack.NavigationItems.FirstOrDefault(x => x.SerializableObject == rom.UIPack.Font_0);

            if (file == null)
                return;

            file.IsSelected = true;
        }

        #endregion
    }
}
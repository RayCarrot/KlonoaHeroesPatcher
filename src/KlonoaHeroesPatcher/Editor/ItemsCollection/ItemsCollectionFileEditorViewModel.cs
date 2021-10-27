using System;
using BinarySerializer.Klonoa.KH;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace KlonoaHeroesPatcher
{
    public class ItemsCollectionFileEditorViewModel : BaseTextFileEditorViewModel
    {
        public ItemsCollectionFileEditorViewModel()
        {
            ApplyValueFieldsCommand = new RelayCommand(ApplyValueFields);
        }

        public ICommand ApplyValueFieldsCommand { get; }

        public ItemsCollection_File ItemsCollectionFile => (ItemsCollection_File)SerializableObject;

        public ObservableCollection<ItemViewModel> Items { get; set; }
        public ObservableCollection<ItemValueIntFieldViewModel> ValueIntFields { get; set; }

        private ItemViewModel _selectedItem;

        public ItemViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                Load(false);
            }
        }

        protected override IEnumerable<TextItemViewModel> GetTextCommandViewModels() => new TextItemViewModel[]
        {
            new TextItemViewModel(this, SelectedItem.Item.Name, "Name"),
            new TextItemViewModel(this, SelectedItem.Item.Description, "Description"),
            new TextItemViewModel(this, SelectedItem.Item.ShopDescription, "Shop Description"),
        };

        protected override void Load(bool firstLoad)
        {
            if (firstLoad)
            {
                Items = new ObservableCollection<ItemViewModel>(ItemsCollectionFile.Items.Select(x => new ItemViewModel(x)));
                _selectedItem = Items.First();
                OnPropertyChanged(nameof(SelectedItem));
            }

            RefreshValueFields();

            // Load the text
            base.Load(firstLoad);
        }

        protected override void RelocateTextCommands()
        {
            // Relocate the data
            RelocateFile();
        }

        public void RefreshValueFields()
        {
            ValueIntFields = SelectedItem.Item.ItemCategory switch
            {
                Item_Category.W => new ObservableCollection<ItemValueIntFieldViewModel>()
                {
                    new ItemValueIntFieldViewModel("Price", (int) SelectedItem.Item.Price, x => SelectedItem.Item.Price = (uint) x),
                    new ItemValueIntFieldViewModel("AT", SelectedItem.Item.W_AT, x => SelectedItem.Item.W_AT = (ushort) x),
                    new ItemValueIntFieldViewModel("SP", SelectedItem.Item.W_SP, x => SelectedItem.Item.W_SP = (ushort) x),
                },
                Item_Category.D => new ObservableCollection<ItemValueIntFieldViewModel>()
                {
                    new ItemValueIntFieldViewModel("Price", (int) SelectedItem.Item.Price, x => SelectedItem.Item.Price = (uint) x),
                    new ItemValueIntFieldViewModel("DF", SelectedItem.Item.D_DF, x => SelectedItem.Item.D_DF = (ushort) x),
                    new ItemValueIntFieldViewModel("AG", SelectedItem.Item.D_AG, x => SelectedItem.Item.D_AG = (ushort) x),
                },
                Item_Category.A => new ObservableCollection<ItemValueIntFieldViewModel>()
                {
                    new ItemValueIntFieldViewModel("Price", (int) SelectedItem.Item.Price, x => SelectedItem.Item.Price = (uint) x),
                    new ItemValueIntFieldViewModel("Icon", SelectedItem.Item.A_IconIndex, x => SelectedItem.Item.A_IconIndex = (ushort) x),
                    new ItemValueIntFieldViewModel("AT", SelectedItem.Item.A_AT, x => SelectedItem.Item.A_AT = (short) x),
                    new ItemValueIntFieldViewModel("SP", SelectedItem.Item.A_SP, x => SelectedItem.Item.A_SP = (short) x),
                    new ItemValueIntFieldViewModel("DF", SelectedItem.Item.A_DF, x => SelectedItem.Item.A_DF = (short) x),
                    new ItemValueIntFieldViewModel("AG", SelectedItem.Item.A_AG, x => SelectedItem.Item.A_AG = (short) x),
                    new ItemValueIntFieldViewModel("HP", SelectedItem.Item.A_HP, x => SelectedItem.Item.A_HP = (short) x),
                    new ItemValueIntFieldViewModel("Unknown", SelectedItem.Item.A_Unknown1, x => SelectedItem.Item.A_Unknown1 = (ushort) x),
                    new ItemValueIntFieldViewModel("Unknown", SelectedItem.Item.A_Unknown2, x => SelectedItem.Item.A_Unknown2 = (ushort) x),
                },
                Item_Category.I => new ObservableCollection<ItemValueIntFieldViewModel>()
                {
                    new ItemValueIntFieldViewModel("Price", (int) SelectedItem.Item.Price, x => SelectedItem.Item.Price = (uint) x),
                    new ItemValueIntFieldViewModel("Icon", SelectedItem.Item.I_IconIndex, x => SelectedItem.Item.I_IconIndex = (ushort) x),
                    new ItemValueIntFieldViewModel("Unknown", SelectedItem.Item.I_Unknown, x => SelectedItem.Item.I_Unknown = (ushort) x),
                },
                Item_Category.E => new ObservableCollection<ItemValueIntFieldViewModel>()
                {
                    new ItemValueIntFieldViewModel("Icon", SelectedItem.Item.E_IconIndex, x => SelectedItem.Item.E_IconIndex = (ushort) x),
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void ApplyValueFields()
        {
            foreach (ItemValueIntFieldViewModel field in ValueIntFields)
                field.Apply();

            RefreshValueFields();

            RelocateFile();
        }

        public class ItemViewModel : BaseViewModel
        {
            public ItemViewModel(Item item)
            {
                Item = item;
                DisplayName = $"{item.Byte_02}-{item.Byte_03}-{item.Byte_04}-{item.Ushort_06} ({GetCharacterName()})";
            }

            public Item Item { get; }
            public string DisplayName { get; }

            public string GetCharacterName()
            {
                return Item.ItemCharacter switch
                {
                    Item_Character.K => "Klonoa",
                    Item_Character.G => "Guntz",
                    Item_Character.P => "Pango",
                    Item_Character.A => "All",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public class ItemValueIntFieldViewModel : BaseViewModel
        {
            public ItemValueIntFieldViewModel(string displayName, int value, Action<int> applyAction)
            {
                DisplayName = displayName;
                Value = value;
                ApplyAction = applyAction;
            }

            public string DisplayName { get; }
            public int Value { get; set; }
            public Action<int> ApplyAction { get; }

            public void Apply()
            {
                ApplyAction(Value);
            }
        }
    }
}
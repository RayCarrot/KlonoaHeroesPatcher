using System.Windows;
using System.Windows.Controls;

namespace KlonoaHeroesPatcher
{
    /// <summary>
    /// Interaction logic for FilesPage.xaml
    /// </summary>
    public partial class FilesPage : UserControl
    {
        public FilesPage()
        {
            InitializeComponent();
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (AppViewModel.Current.FilesPageViewModel != null)
                AppViewModel.Current.FilesPageViewModel.SelectedNavigationItem = e.NewValue as NavigationItemViewModel;
        }
    }
}
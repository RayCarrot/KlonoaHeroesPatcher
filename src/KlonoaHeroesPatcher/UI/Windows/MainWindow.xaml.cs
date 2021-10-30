using System.ComponentModel;
using System.Windows;

namespace KlonoaHeroesPatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = AppViewModel.Current;
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            AppViewModel.Current.SelectedNavigationItem = e.NewValue as NavigationItemViewModel;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (AppViewModel.Current.UnsavedChanges)
            {
                var result = MessageBox.Show("You have unsaved changes! Are you sure you want to exit and discard them?", "Unsaved changes", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                    return;

                e.Cancel = true;
            }
        }
    }
}
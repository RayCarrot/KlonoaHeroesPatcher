using System.Windows;
using MahApps.Metro.Controls;

namespace KlonoaHeroesPatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
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
    }
}
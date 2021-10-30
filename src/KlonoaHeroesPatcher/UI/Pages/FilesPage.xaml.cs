﻿using System.Windows;
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
            AppViewModel.Current.SelectedNavigationItem = e.NewValue as NavigationItemViewModel;
        }
    }
}
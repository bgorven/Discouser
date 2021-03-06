﻿using Discouser.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Discouser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            DataContext = new AllSites();
        }

        private void FocusNewSiteUrl(object sender, RoutedEventArgs e)
        {
            NewSiteUrl.Focus(FocusState.Programmatic);
        }

        private async void ActivateNewSiteAddButton(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var addSiteCommand = NewSiteAddButton.Command as Command;
                if (addSiteCommand != null && addSiteCommand.CanExecute(null)) await addSiteCommand.DoExecute(null);
                NewSiteCancelAddButton.Focus(FocusState.Programmatic);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Categories.Visibility = Visibility.Visible;
            CategoryGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Auto);
            CategoryGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            CategoryGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Star);
        }

        private void Topic_Selected(object sender, RoutedEventArgs e)
        {
            Categories.Visibility = Visibility.Collapsed;
            CategoryGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
            CategoryGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
            CategoryGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
        }

        private async void Refresh_ViewModel(object sender, TappedRoutedEventArgs e)
        {
            var viewmodel = ((FrameworkElement)sender).DataContext as IViewModel;
            if (viewmodel == null) return;
            if (viewmodel.CanRefresh) await viewmodel.Refresh();
        }
    }
}

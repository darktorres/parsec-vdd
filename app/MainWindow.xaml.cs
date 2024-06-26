﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace ParsecVDisplay
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            xAppName.Content += $" v{App.VERSION}";

            // prevent frame history
            xFrame.Navigating += (_, e) => { e.Cancel = e.NavigationMode != NavigationMode.New; };
            xFrame.Navigated += (_, e) => { xFrame.NavigationService.RemoveBackEntry(); };

            xDisplays.Children.Clear();
            xNoDisplay.Visibility = Visibility.Hidden;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            Shadow.ApplyShadow(hwnd);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;

            if (xFrame.Content != null)
            {
                xFrame.Visibility = Visibility.Hidden;
                xFrame.Content = null;
                xDisplays.Visibility = Visibility.Visible;
                xButtons.Visibility = Visibility.Visible;
            }
            else
            {
                this.Hide();
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Window_Loaded;

            if (App.Silent)
                Hide();

            ContextMenu.DataContext = this;
            Tray.Init(this, ContextMenu);
            ContextMenu = null;

            ParsecVDD.DisplayChanged += DisplayChanged;
            ParsecVDD.Invalidate();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            ParsecVDD.DisplayChanged -= DisplayChanged;
            Tray.Uninit();
        }

        private void DisplayChanged(List<Display> displays, bool noMonitors)
        {
            xDisplays.Children.Clear();
            xNoDisplay.Visibility = displays.Count <= 0 ? Visibility.Visible : Visibility.Hidden;

            foreach (var display in displays)
            {
                var item = new Components.DisplayItem(display);
                xDisplays.Children.Add(item);
            }

            xAdd.IsEnabled = true;

            if (noMonitors && Config.FallbackDisplay)
            {
                AddDisplay(null, EventArgs.Empty);
            }
        }

        private void AddDisplay(object sender, EventArgs e)
        {
            if (ParsecVDD.DisplayCount >= ParsecVDD.MAX_DISPLAYS)
            {
                MessageBox.Show(this,
                    $"Could not add more virtual displays, you have exceeded the maximum number ({ParsecVDD.MAX_DISPLAYS}).",
                    Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                ParsecVDD.AddDisplay();
                xAdd.IsEnabled = false;
            }
        }

        private void RemoveLastDisplay(object sender, EventArgs e)
        {
            xAdd.IsEnabled = false;
            ParsecVDD.RemoveLastDisplay();
        }

        private void OpenCustom(object sender, EventArgs e)
        {
            xDisplays.Visibility = Visibility.Hidden;
            xButtons.Visibility = Visibility.Hidden;
            xFrame.Content = new Components.CustomPage();
            xFrame.Visibility = Visibility.Visible;
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            Helper.ShellExec("ms-settings:display");
        }

        private void SyncSettings(object sender, EventArgs e)
        {
            xAdd.IsEnabled = false;
            xDisplays.Children.Clear();

            ParsecVDD.Invalidate();
        }

        private void QueryStatus(object sender, EventArgs e)
        {
            if (e is MouseEventArgs mbe)
                mbe.Handled = true;

            Tray.ShowApp();

            var status = ParsecVDD.QueryStatus();
            var version = ParsecVDD.QueryVersion();

            MessageBox.Show(this,
                $"Parsec Virtual Display v{version}\nDriver status: {status}",
                App.NAME, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExitApp(object sender, EventArgs e)
        {
            if (ParsecVDD.DisplayCount > 0)
                if (MessageBox.Show(this,
                    "All added virtual displays will be unplugged.\nDo you still want to exit?",
                    App.NAME, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

            Tray.Uninit();
            Application.Current.Shutdown();
        }

        private void OpenRepoLink(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Helper.OpenLink("https://github.com/nomi-san/parsec-vdd");
        }
    }
}
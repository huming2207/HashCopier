using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using HashCopier.Controller;
using HashCopier.Model;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;

namespace HashCopier
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static MainWindow MainWindowToInvoke;

        internal List<FileListModel> FileListLoader
        {
            set { Dispatcher.Invoke(() => { FileList.ItemsSource = value; }, DispatcherPriority.DataBind); }
        }

        public MainWindow()
        {
            InitializeComponent();
            MainWindowToInvoke = this;
        }

        private async void CopyButton_OnClick(object sender, RoutedEventArgs e)
        {
            var mainController = new MainController();
            await mainController.GetFileListModel(
                await mainController.GetFileList(SrcPathTextbox.Text),
                await mainController.GetFileList(DestPathTextbox.Text), DestPathTextbox.Text,
                new Progress<double>(value => SingleFileProgress.Value = value));
        }

        private void MoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void SrcPathButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dirDialog = new CommonOpenFileDialog { IsFolderPicker = true, Multiselect = false };
            var pathPickResult = dirDialog.ShowDialog();

            if (pathPickResult == CommonFileDialogResult.Ok)
            {
                SrcPathTextbox.Text = dirDialog.FileName;
            }
            else
            {
                MessageBox.Show("User cancelled.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DestPathButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dirDialog = new CommonOpenFileDialog { IsFolderPicker = true, Multiselect = false };
            var pathPickResult = dirDialog.ShowDialog();

            if (pathPickResult == CommonFileDialogResult.Ok)
            {
                DestPathTextbox.Text = dirDialog.FileName;
            }
            else
            {
                MessageBox.Show("User cancelled.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
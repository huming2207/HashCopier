using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using HashCopier.Controller;
using HashCopier.Model;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

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
            set { Dispatcher.InvokeAsync(() => { FileList.ItemsSource = value; }, DispatcherPriority.DataBind); }
        }

        internal void ForceRefresh()
        {
            this.FileList.Items.Refresh();
        }

        public MainWindow()
        {
            InitializeComponent();
            MainWindowToInvoke = this;
        }

        private async void CopyButton_OnClick(object sender, RoutedEventArgs e)
        {
            CopyButton.IsEnabled = false;
            MoveButton.IsEnabled = false;

            var mainController = new MainController();

            // Hash the source file list
            CopyButton.Content = "Hashing source files...";
            var srcFileList = await mainController.GetFileList(SrcPathTextbox.Text);

            // Hash the dest file list
            CopyButton.Content = "Hashing destination files...";
            var destFileList = await mainController.GetFileList(DestPathTextbox.Text);

            CopyButton.Content = "Copying...";
            await mainController.GetFileListModel(
                srcFileList, destFileList, DestPathTextbox.Text,
                new Progress<double>(value => SingleFileProgress.Value = value));

            CopyButton.Content = "Copy";
            CopyButton.IsEnabled = true;
            MoveButton.IsEnabled = true;
        }

        private async void MoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            MoveButton.IsEnabled = false;
            CopyButton.IsEnabled = false;

            var mainController = new MainController();

            // Hash the source file list
            MoveButton.Content = "Hashing source files...";
            var srcFileList = await mainController.GetFileList(SrcPathTextbox.Text);

            // Hash the dest file list
            MoveButton.Content = "Hashing destination files...";
            var destFileList = await mainController.GetFileList(DestPathTextbox.Text);

            MoveButton.Content = "Copying...";
            await mainController.GetFileListModel(
                srcFileList, destFileList, DestPathTextbox.Text,
                new Progress<double>(value => SingleFileProgress.Value = value));

            MoveButton.Content = "Copy";
            MoveButton.IsEnabled = true;
            CopyButton.IsEnabled = true;
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
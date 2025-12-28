using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TorrentHardLinkHelper.ViewModels;

namespace TorrentHardLinkHelper.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Process command line arguments
            var args = App.StartupArgs;
            if (args != null && args.Length > 0)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    // First argument: torrent file
                    if (File.Exists(args[0]) && args[0].EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
                    {
                        viewModel.LoadTorrentFile(args[0]);
                    }

                    // Second argument: source folder
                    if (args.Length > 1 && Directory.Exists(args[1]))
                    {
                        viewModel.LoadSourceFolder(args[1]);
                    }

                    // Third argument: output base folder
                    if (args.Length > 2 && Directory.Exists(args[2]))
                    {
                        viewModel.LoadOutputBaseFolder(args[2]);
                    }
                }
            }
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void TorrentFile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string file = files[0];
                    if (File.Exists(file) && file.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
                    {
                        var viewModel = DataContext as MainViewModel;
                        if (viewModel != null)
                        {
                            viewModel.LoadTorrentFile(file);
                        }
                    }
                }
            }
        }

        private void SourceFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string path = files[0];
                    if (Directory.Exists(path))
                    {
                        var viewModel = DataContext as MainViewModel;
                        if (viewModel != null)
                        {
                            viewModel.LoadSourceFolder(path);
                        }
                    }
                }
            }
        }

        private void OutputBaseFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string path = files[0];
                    if (Directory.Exists(path))
                    {
                        var viewModel = DataContext as MainViewModel;
                        if (viewModel != null)
                        {
                            viewModel.LoadOutputBaseFolder(path);
                        }
                    }
                }
            }
        }
    }
}

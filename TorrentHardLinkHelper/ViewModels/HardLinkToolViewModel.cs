using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using TorrentHardLinkHelper.HardLink;

namespace TorrentHardLinkHelper.ViewModels
{
    public class HardLinkToolViewModel : ObservableObject
    {
        private string _sourceFolder;
        private string _parentFolder;
        private string _folderName;

        private RelayCommand _selectSourceFolderCommand;
        private RelayCommand _selectParentFolderCommand;
        private RelayCommand _defaultCommand;
        private RelayCommand _linkCommand;

        public HardLinkToolViewModel()
        {
            this.InitCommands();
        }

        public void InitCommands()
        {
            this._selectSourceFolderCommand = new RelayCommand(SelectSourceFolder);

            this._selectParentFolderCommand = new RelayCommand(SelectParentFolder);

            this._defaultCommand = new RelayCommand(Default);

            this._linkCommand = new RelayCommand(Link, CanLink);
        }

        private void SelectSourceFolder()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.ShowDialog();
            if (!string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                SourceFolder = dialog.SelectedPath;
                ParentFolder = Directory.GetParent(dialog.SelectedPath).FullName;
                FolderName = Path.GetFileName(dialog.SelectedPath) + "_Copy";
            }
        }

        private void SelectParentFolder()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.ShowDialog();
            if (dialog.SelectedPath != null)
            {
                ParentFolder = dialog.SelectedPath;
            }
        }

        private void Default()
        {
            if (string.IsNullOrWhiteSpace(_sourceFolder))
            {
                FolderName = "";
            }
            else
            {
                FolderName = Path.GetDirectoryName(_folderName) + "_HLinked";
            }
        }

        private void Link()
        {
            var helper = new HardLinkHelper();
            helper.HardLink(_sourceFolder, _parentFolder, _folderName, 1024000);
            Process.Start("explorer.exe", Path.Combine(_parentFolder, _folderName));
        }

        private bool CanLink()
        {
            return !string.IsNullOrWhiteSpace(_sourceFolder) && 
                   !string.IsNullOrWhiteSpace(_parentFolder) && 
                   !string.IsNullOrWhiteSpace(_folderName);
        }

        public string SourceFolder
        {
            get { return _sourceFolder; }
            set 
            { 
                if (SetProperty(ref _sourceFolder, value))
                {
                    _linkCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string ParentFolder
        {
            get { return _parentFolder; }
            set 
            { 
                if (SetProperty(ref _parentFolder, value))
                {
                    _linkCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string FolderName
        {
            get { return _folderName; }
            set 
            { 
                if (SetProperty(ref _folderName, value))
                {
                    _linkCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public RelayCommand SelectSourceFolderCommand
        {
            get { return _selectSourceFolderCommand; }
        }

        public RelayCommand SelectParentFolderCommand
        {
            get { return _selectParentFolderCommand; }
        }

        public RelayCommand DefaultCommand
        {
            get { return _defaultCommand; }
        }

        public RelayCommand LinkCommand
        {
            get { return _linkCommand; }
        }
    }
}
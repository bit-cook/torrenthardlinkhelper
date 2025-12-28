using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using TorrentHardLinkHelper.HardLink;
using TorrentHardLinkHelper.Locate;
using TorrentHardLinkHelper.Models;
using TorrentHardLinkHelper.Torrents;
using TorrentHardLinkHelper.Views;

namespace TorrentHardLinkHelper.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private static readonly IList<string> _outputNameTypes = new[] { "Torrent Title", "Torrent Name", "Custom" };

        private string _torrentFile;
        private string _sourceFolder;
        private string _outputBaseFolder;
        private string _outputName;
        private string _status;
        private string _outputNameType;
        private bool _isOutputNameReadonly;
        private int _copyLimitSize;
        private int _maxProcess;
        private int _curProcess;
        private Torrent _torrent;
        private int _unlocatedCount = -1;
        private IList<FileSystemFileInfo> _fileSystemFileInfos;
        private IList<EntityModel> _fileSystemEntityModel;
        private IList<EntityModel> _torrentEntityModel;

        private LocateResult _locateResult;

        private Style _expandAllStyle;
        private Style _collapseAllStyle;

        private RelayCommand _selectTorrentFileCommand;
        private RelayCommand _selectSourceFolderCommand;
        private RelayCommand _selectOuptputBaseFolderCommand;
        private RelayCommand _analyseCommand;
        private RelayCommand _linkCommand;
        private RelayCommand _linkLinuxCommand;
        private RelayCommand _hardlinkLinuxCommand;
        private RelayCommand _moveLinuxCommand;
        private RelayCommand<TreeView> _expandCommand;
        private RelayCommand<TreeView> _collapseCommand;
        private RelayCommand<SelectionChangedEventArgs> _outputNameTypeChangedCommand;
        private RelayCommand _hardlinkToolCommand;

        public MainViewModel()
        {
            this.InitCommands();
            this.InitStyles();
            IsOutputNameReadonly = true;
            CopyLimitSize = 1024;
            UpdateStatusFormat("Ready.");
        }

        private void InitCommands()
        {
            this._selectTorrentFileCommand = new RelayCommand(SelectTorrentFile);

            this._selectSourceFolderCommand = new RelayCommand(SelectSourceFolder);

            this._selectOuptputBaseFolderCommand = new RelayCommand(SelectOutputBaseFolder);

            this._analyseCommand = new RelayCommand(Analyse, CanAnalyse);

            this._linkCommand = new RelayCommand(Link, CanLink);

            this._linkLinuxCommand = new RelayCommand(LinkLinux, CanLink);

            this._hardlinkLinuxCommand = new RelayCommand(HardlinkLinux, CanLink);

            this._moveLinuxCommand = new RelayCommand(MoveLinux, CanLink);

            this._outputNameTypeChangedCommand =
                new RelayCommand<SelectionChangedEventArgs>(
                    args => ChangeOutputFolderNmae(args.AddedItems[0].ToString()));

            this._expandCommand = new RelayCommand<TreeView>(tv => { tv.ItemContainerStyle = this._expandAllStyle; });
            this._collapseCommand = new RelayCommand<TreeView>(tv => { tv.ItemContainerStyle = this._collapseAllStyle; });

            this._hardlinkToolCommand = new RelayCommand(() =>
            {
                var tool = new HardLinkTool();
                tool.ShowDialog();
            });
        }

        private void InitStyles()
        {
            this._expandAllStyle = new Style(typeof(TreeViewItem));
            this._collapseAllStyle = new Style(typeof(TreeViewItem));

            this._expandAllStyle.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, true));
            this._collapseAllStyle.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, false));
        }

        private void UpdateStatusFormat(string format, params object[] args)
        {
            Status = string.Format(format, args);
        }

        private void SelectTorrentFile()
        {
            var dialog = new VistaOpenFileDialog();
            dialog.Title = "Select one torrent to open";
            dialog.Filter = "Torrent Files|*.torrent";
            dialog.Multiselect = false;
            dialog.CheckFileExists = true;
            dialog.ShowDialog();
            if (dialog.FileName != null)
            {
                TorrentFile = dialog.FileName;
                this.OpenTorrent();
            }
        }

        private void SelectSourceFolder()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.ShowDialog();
            if (dialog.SelectedPath != null)
            {
                SourceFolder = dialog.SelectedPath;
                FileSystemEntityModel = new[] { EntityModel.Load(SourceFolder) };
            }
        }

        private void SelectOutputBaseFolder()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.ShowDialog();
            if (dialog.SelectedPath != null)
            {
                OutputBaseFolder = dialog.SelectedPath;
            }
        }

        private bool CanAnalyse()
        {
            return !string.IsNullOrEmpty(_torrentFile) && !string.IsNullOrEmpty(_sourceFolder);
        }

        private bool CanLink()
        {
            return !string.IsNullOrEmpty(_outputBaseFolder) && !string.IsNullOrEmpty(_outputName) &&
                _locateResult != null;
        }

        private void OpenTorrent()
        {
            if (string.IsNullOrEmpty(_torrentFile))
            {
                return;
            }
            try
            {
                _torrent = Torrent.Load(_torrentFile);
                this.ChangeOutputFolderNmae(_outputNameType);
                TorrentEntityModel = new[] { EntityModel.Load(_torrent) };
            }
            catch (Exception ex)
            {
                UpdateStatusFormat("Load torrent failed, exception message: {0}", ex.Message);
            }
        }

        public void LoadTorrentFile(string filePath)
        {
            TorrentFile = filePath;
            this.OpenTorrent();
        }

        public void LoadSourceFolder(string folderPath)
        {
            SourceFolder = folderPath;
            FileSystemEntityModel = new[] { EntityModel.Load(SourceFolder) };
        }

        public void LoadOutputBaseFolder(string folderPath)
        {
            OutputBaseFolder = folderPath;
        }

        private void ChangeOutputFolderNmae(string nameType)
        {
            if (nameType == "Custom")
            {
                IsOutputNameReadonly = false;
            }
            else
            {
                IsOutputNameReadonly = true;
            }
            if (_torrent == null)
            {
                OutputName = "";
                return;
            }
            switch (nameType)
            {
                case "Torrent Name":
                    OutputName = Path.GetFileNameWithoutExtension(_torrentFile);
                    IsOutputNameReadonly = true;
                    break;
                case "Torrent Title":
                    OutputName = _torrent.Name;
                    IsOutputNameReadonly = true;
                    break;
            }
        }

        private void Analyse()
        {
            this.UpdateStatusFormat("Locating... This may take several minutes.");
            var func = new Func<LocateResult>(Locate);
            func.BeginInvoke(AnalyseFinish, func);
        }

        private void AnalyseFinish(IAsyncResult ar)
        {
            var func = ar.AsyncState as Func<LocateResult>;
            try
            {
                LocateResult result = func.EndInvoke(ar);

                this.UpdateStatusFormat("Successfully located {0} of {1} file(s). Matched {2} of {3} file(s) on disk.",
                    result.LocatedCount,
                    result.LocatedCount + result.UnlocatedCount,
                    result.TorrentFileLinks.Where(c => c.State == LinkState.Located)
                        .Where(c => c.LinkedFsFileInfo != null)
                        .Select(c => c.LinkedFsFileInfo.FilePath)
                        .Distinct()
                        .Count(), _fileSystemFileInfos.Count);
                _locateResult = result;
                _unlocatedCount = result.UnlocatedCount;

                EntityModel.Update(_fileSystemEntityModel[0], result.TorrentFileLinks);
                OnPropertyChanged(nameof(FileSystemEntityModel));

                TorrentEntityModel = new[] { EntityModel.Load(_torrent.Name, result) };

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    _linkCommand.NotifyCanExecuteChanged();
                    _linkLinuxCommand.NotifyCanExecuteChanged();
                    _hardlinkLinuxCommand.NotifyCanExecuteChanged();
                    _moveLinuxCommand.NotifyCanExecuteChanged();
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private LocateResult Locate()
        {
            _fileSystemFileInfos = FileSystemFileSearcher.SearchFolder(_sourceFolder);
            var locater = new TorrentFileLocater(_torrent, _fileSystemFileInfos,
                () => CurPorcess = _curProcess + 1);
            MaxProcess = _torrent.Files.Length;
            CurPorcess = 0;
            LocateResult result = locater.Locate();
            return result;
        }

        private void Link()
        {
            if (Path.GetPathRoot(_outputBaseFolder) != Path.GetPathRoot(_sourceFolder))
            {
                this.UpdateStatusFormat(
                    "Link failed, the output basefolder and the source folder must be in the same drive!");
                return;
            }
            if (_unlocatedCount != 0) {
                MessageBoxResult result = MessageBox.Show(_unlocatedCount + " files unlocated, hard link anyway?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (result != MessageBoxResult.OK) {
                    return;
                }
            }

            this.UpdateStatusFormat("Linking...");
            var helper = new HardLinkHelper();
            helper.HardLink(_locateResult.TorrentFileLinks, _copyLimitSize, _outputName,
                _outputBaseFolder);
            string targetTorrentFile = Path.Combine(Path.Combine(_outputBaseFolder, _outputName), Path.GetFileName(_torrentFile));
            helper.Copy(_torrentFile, targetTorrentFile);
            this.UpdateStatusFormat("Done.");
            Process.Start("explorer.exe", Path.Combine(_outputBaseFolder, _outputName));
        }

        private void LinkLinux()
        {
            if (_unlocatedCount != 0)
            {
                MessageBoxResult result = MessageBox.Show(_unlocatedCount + " files unlocated, generate script anyway?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }

            this.UpdateStatusFormat("Generating Linux symlink script...");
            var helper = new HardLinkHelper();
            helper.GenerateLinuxSymlinkScript(_locateResult.TorrentFileLinks, _outputName,
                _outputBaseFolder, _sourceFolder);
            this.UpdateStatusFormat("Done. Script saved as " + _outputName + "_symlink.sh");
        }

        private void HardlinkLinux()
        {
            if (_unlocatedCount != 0)
            {
                MessageBoxResult result = MessageBox.Show(_unlocatedCount + " files unlocated, generate script anyway?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }

            this.UpdateStatusFormat("Generating Linux hard link script...");
            var helper = new HardLinkHelper();
            helper.GenerateLinuxHardlinkScript(_locateResult.TorrentFileLinks, _outputName,
                _outputBaseFolder, _sourceFolder);
            this.UpdateStatusFormat("Done. Script saved as " + _outputName + "_hardlink.sh");
        }

        private void MoveLinux()
        {
            if (_unlocatedCount != 0)
            {
                MessageBoxResult result = MessageBox.Show(_unlocatedCount + " files unlocated, generate script anyway?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }

            this.UpdateStatusFormat("Generating Linux move script...");
            var helper = new HardLinkHelper();
            helper.GenerateLinuxMoveScript(_locateResult.TorrentFileLinks, _outputName,
                _outputBaseFolder, _sourceFolder);
            this.UpdateStatusFormat("Done. Script saved as " + _outputName + "_move.sh");
        }

        #region Properties

        public string TorrentFile
        {
            get { return _torrentFile; }
            set 
            { 
                if (SetProperty(ref _torrentFile, value))
                {
                    _analyseCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string SourceFolder
        {
            get { return _sourceFolder; }
            set 
            { 
                if (SetProperty(ref _sourceFolder, value))
                {
                    _analyseCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string OutputBaseFolder
        {
            get { return _outputBaseFolder; }
            set 
            { 
                if (SetProperty(ref _outputBaseFolder, value))
                {
                    _linkCommand.NotifyCanExecuteChanged();
                    _linkLinuxCommand.NotifyCanExecuteChanged();
                    _hardlinkLinuxCommand.NotifyCanExecuteChanged();
                    _moveLinuxCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string OutputName
        {
            get { return _outputName; }
            set 
            { 
                if (SetProperty(ref _outputName, value))
                {
                    _linkCommand.NotifyCanExecuteChanged();
                    _linkLinuxCommand.NotifyCanExecuteChanged();
                    _hardlinkLinuxCommand.NotifyCanExecuteChanged();
                    _moveLinuxCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string OutputNameType
        {
            get { return _outputNameType; }
            set { SetProperty(ref _outputNameType, value); }
        }

        public string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        public IList<string> OutputNameTypes
        {
            get { return _outputNameTypes; }
        }

        public bool IsOutputNameReadonly
        {
            get { return _isOutputNameReadonly; }
            set { SetProperty(ref _isOutputNameReadonly, value); }
        }

        public IList<EntityModel> FileSystemEntityModel
        {
            get { return _fileSystemEntityModel; }
            set { SetProperty(ref _fileSystemEntityModel, value); }
        }

        public IList<EntityModel> TorrentEntityModel
        {
            get { return _torrentEntityModel; }
            set { SetProperty(ref _torrentEntityModel, value); }
        }

        public int CopyLimitSize
        {
            get { return _copyLimitSize; }
            set { SetProperty(ref _copyLimitSize, value); }
        }

        public int MaxProcess
        {
            get { return _maxProcess; }
            set { SetProperty(ref _maxProcess, value); }
        }

        public int CurPorcess
        {
            get { return _curProcess; }
            set { SetProperty(ref _curProcess, value); }
        }

        #endregion

        #region Commands

        public RelayCommand SelectTorrentFileCommand
        {
            get { return _selectTorrentFileCommand; }
        }

        public RelayCommand SelectSourceFolderCommand
        {
            get { return _selectSourceFolderCommand; }
        }

        public RelayCommand SelectOutputBaseFolderCommand
        {
            get { return _selectOuptputBaseFolderCommand; }
        }

        public RelayCommand AnalyseCommand
        {
            get { return _analyseCommand; }
        }

        public RelayCommand LinkCommand
        {
            get { return _linkCommand; }
        }

        public RelayCommand LinkLinuxCommand
        {
            get { return _linkLinuxCommand; }
        }

        public RelayCommand HardlinkLinuxCommand
        {
            get { return _hardlinkLinuxCommand; }
        }

        public RelayCommand MoveLinuxCommand
        {
            get { return _moveLinuxCommand; }
        }

        public RelayCommand<SelectionChangedEventArgs> OutputNameTypeChangedCommand
        {
            get { return _outputNameTypeChangedCommand; }
        }

        public RelayCommand<TreeView> ExpandAllCommand
        {
            get { return _expandCommand; }
        }

        public RelayCommand<TreeView> CollapseAllCommand
        {
            get { return _collapseCommand; }
        }

        public RelayCommand HardlinkToolCommand
        {
            get { return _hardlinkToolCommand; }
        }

        #endregion
    }
}
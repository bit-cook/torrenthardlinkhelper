using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using TorrentHardLinkHelper.Locate;
using TorrentHardLinkHelper.Torrents;

namespace TorrentHardLinkHelper.Models
{
    public class EntityModel : ObservableObject
    {
        public EntityModel()
        {
            this.Entities = new List<EntityModel>();
        }

        protected string _name;
        protected string _fullName;
        protected bool _locked;
        protected bool _hasCounterpart;
        protected string _type;
        protected IList<EntityModel> _entities;

        public string Name
        {
            get { return this._name; }
            set { this._name = value; }
        }

        public string FullName
        {
            get { return this._fullName; }
            set { this._fullName = value; }
        }

        public bool Located
        {
            get { return this._locked; }
            set { this._locked = value; }
        }

        public bool HasCounterpart
        {
            get { return this._hasCounterpart; }
            set { this._hasCounterpart = value; }
        }

        public string Type
        {
            get { return this._type; }
            set { this._type = value; }
        }

        public IList<EntityModel> Entities
        {
            get { return this._entities; }
            set { this._entities = value; }
        }

        public Brush TextColor
        {
            get
            {
                if (this.Type == "Folder")
                {
                    return Brushes.Black;
                }
                if (this.Located)
                {
                    return Brushes.Green;
                }
                if (this.HasCounterpart)
                {
                    return Brushes.Red;
                }
                return Brushes.Blue;
            }
        }

        public override string ToString()
        {
            return string.Format("[{0,-8}]{1}", this.Type, this.Name);
        }

        public static EntityModel Load(string path)
        {
            if (!Directory.Exists(path))
            {
                return null;
            }
            var folderModel = new FolderModel(Path.GetFileName(path));
            folderModel.FullName = path;
            foreach (var file in Directory.GetFiles(path))
            {
                folderModel.Entities.Add(new FileModel(file));
            }
            foreach (var subFolder in Directory.GetDirectories(path))
            {
                folderModel.Entities.Add(Load(subFolder));
            }
            return folderModel;
        }

        public static EntityModel Load(Torrent torrent)
        {
            var root = new FolderModel(torrent.Name);
            foreach (TorrentFile file in torrent.Files)
            {
                var folder = FindOrCreateFolder(root, file.Path);
                folder.Entities.Add(new FileModel(file));
            }
            return root;
        }

        public static EntityModel Load(string title, LocateResult result)
        {
            var root = new FolderModel(title);
            foreach (TorrentFileLink file in result.TorrentFileLinks)
            {
                var folder = FindOrCreateFolder(root, file.TorrentFile.Path);
                var fileModel = new FileModel(file.TorrentFile);
                fileModel.Located = file.State == LinkState.Located;
                fileModel.HasCounterpart = file.FsFileInfos.Count > 0;
                folder.Entities.Add(fileModel);
            }
            return root;
        }

        public static void Update(EntityModel model, IList<TorrentFileLink> torrentFileLinks)
        {
            // Collect all matched file paths
            var matchedFilePaths = new HashSet<string>(
                torrentFileLinks
                    .Where(c => c.State == LinkState.Located && c.LinkedFsFileInfo != null)
                    .Select(c => c.LinkedFsFileInfo.FilePath));

            // Collect all candidate file paths (files that exist on both sides)
            var candidateFilePaths = new HashSet<string>(
                torrentFileLinks
                    .SelectMany(c => c.FsFileInfos)
                    .Where(f => f != null)
                    .Select(f => f.FilePath));

            UpdateRecursive(model, matchedFilePaths, candidateFilePaths);
        }

        private static void UpdateRecursive(EntityModel model, HashSet<string> matchedFilePaths, HashSet<string> candidateFilePaths)
        {
            foreach (EntityModel entity in model.Entities)
            {
                if (entity.Type == "File")
                {
                    if (matchedFilePaths.Contains(entity.FullName))
                    {
                        entity.Located = true;
                    }
                    if (candidateFilePaths.Contains(entity.FullName))
                    {
                        entity.HasCounterpart = true;
                    }
                    entity.RaisePropertyChanged("TextColor");
                }
                else
                {
                    UpdateRecursive(entity, matchedFilePaths, candidateFilePaths);
                }
            }
        }

        private static EntityModel FindOrCreateFolder(FolderModel rootFolder, string path)
        {
            string[] pathItems = path.Split('\\');
            if (pathItems.Length > 1)
            {
                EntityModel parentFolder = rootFolder;
                for (int i = 0; i < pathItems.Length - 1; i++)
                {
                    bool found = false;
                    foreach (EntityModel subFolder in parentFolder.Entities)
                    {
                        if (subFolder.Type == "Folder")
                        {
                            if (subFolder.Name == pathItems[i])
                            {
                                found = true;
                                parentFolder = subFolder;
                            }
                        }
                    }
                    if (!found)
                    {
                        var childFolder = new FolderModel(pathItems[i]);
                        parentFolder.Entities.Add(childFolder);
                        parentFolder = childFolder;
                    }
                }
                return parentFolder;
            }
            return rootFolder;
        }
    }
}
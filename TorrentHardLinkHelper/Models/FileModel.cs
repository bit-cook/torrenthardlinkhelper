using System.IO;
using TorrentHardLinkHelper.Torrents;

namespace TorrentHardLinkHelper.Models
{
    public sealed class FileModel : EntityModel
    {
        public FileModel()
        {
            Located = false;
            Type = "File";
        }

        public FileModel(string fullName)
            : this()
        {
            Name = Path.GetFileName(fullName);
            FullName = fullName;
        }

        public FileModel(TorrentFile torrentFile)
            : this()
        {
            Name = Path.GetFileName(torrentFile.Path);
            FullName = torrentFile.Path;
        }
    }
}
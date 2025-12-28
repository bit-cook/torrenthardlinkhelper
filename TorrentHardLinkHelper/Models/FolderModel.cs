namespace TorrentHardLinkHelper.Models
{
    public sealed class FolderModel : EntityModel
    {
        public FolderModel()
        {
            Located = false;
            Type = "Folder";
        }

        public FolderModel(string folderName)
            : this()
        {
            Name = folderName;
        }
    }
}
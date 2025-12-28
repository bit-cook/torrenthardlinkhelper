namespace TorrentHardLinkHelper.ViewModels
{
    /// <summary>
    ///     This class contains static references to all the view models in the
    ///     application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        private static MainViewModel _main;
        private static HardLinkToolViewModel _hardLinkTool;

        /// <summary>
        ///     Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
        }

        public MainViewModel Main
        {
            get 
            { 
                if (_main == null)
                {
                    _main = new MainViewModel();
                }
                return _main;
            }
        }

        public HardLinkToolViewModel HardLinkTool
        {
            get 
            { 
                if (_hardLinkTool == null)
                {
                    _hardLinkTool = new HardLinkToolViewModel();
                }
                return _hardLinkTool;
            }
        }

        public static void Cleanup()
        {
            _main = null;
            _hardLinkTool = null;
        }
    }
}
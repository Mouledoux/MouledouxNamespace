namespace Mouledoux.Debug
{
    public static class Logger
    {
        private static string _logFolderPath;

        private static void SetFolderPath(string newPath)
        {
            _logFolderPath = newPath;
        }
    }
}
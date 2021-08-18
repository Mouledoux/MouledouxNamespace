using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Mouledoux.Callback
{
    public static class Logger
    {
        public enum MesasgeLogDetail
        {
            DISABLED = -1,
            LOW = 1,
            MEDIUM = 2,
            HIGH = 3,
            DEV = 9001,
        }
        public enum MessageThreatLevel
        {
            Crash = 0,
            Error = 1,
            Warning = 2,
            System = 3,
            Message = 4,
        }

        public static MesasgeLogDetail logDetailSetting = MesasgeLogDetail.MEDIUM;
        public static readonly string logDocFilePath = $"../Logs/";
        private static readonly string logFileName = "_testStack.log";
        private static StringBuilder m_logStringBuilder = new StringBuilder();
        private static string[] m_logMessageTypes;


        private static void Initialize()
        {
            m_logMessageTypes = System.Enum.GetNames(typeof(MessageThreatLevel));
        }

        public static void SetLogGranularity(int a_logLevel)
        {
            a_logLevel = a_logLevel <= 0 ? -1 : a_logLevel >= 4 ? 9001 : a_logLevel;
            logDetailSetting = (MesasgeLogDetail)a_logLevel;
        }

        public static void Log(string a_logMessage, MessageThreatLevel a_threatLevel = MessageThreatLevel.Message)
        {
            if ((int)a_threatLevel > (int)logDetailSetting) return;

            string _logType = $"{m_logMessageTypes[(int)a_threatLevel]}";
            StringBuilder _thisMessage = new StringBuilder();
            StackTrace _stackTrace = new StackTrace(1);

            _thisMessage.AppendLine($"{_logType}: {a_logMessage}");
            _thisMessage.AppendLine($"\t{_stackTrace}");
            Log(_thisMessage.ToString());
        }

        private static void Log(string a_logMessage)
        {
            m_logStringBuilder.AppendLine($"!-{System.DateTime.Now}-!\t{a_logMessage}");
        }

        public static void SaveLogToFile()
        {
            Log("LogSaved", MessageThreatLevel.System);

            StringBuilder _logFileName = new StringBuilder($"{System.DateTime.Now.Ticks}");
            _logFileName.Remove(_logFileName.Length - 3, 3);
            _logFileName.Append(logFileName);

            string _fullLogFilePath = $"{logDocFilePath}{_logFileName}";
            string _fullLog = m_logStringBuilder.ToString();


            if (!System.IO.Directory.Exists(logDocFilePath))
            {
                System.IO.Directory.CreateDirectory(logDocFilePath);
            }

            if (logDetailSetting == MesasgeLogDetail.DISABLED)
            {
                // if logging is disabled, purge any log older than 90 days
                // PurgeOldLogFiles(logDocFilePath, 90);
            }

            System.IO.File.WriteAllText(_fullLogFilePath, _fullLog);
        }


        private static void PurgeOldLogFiles(string a_logDirectoryPath, int a_maxFileLifetimeDays)
        {
            string[] _previousLogFiles = System.IO.Directory.GetFiles(a_logDirectoryPath);
            var _staleLogs = _previousLogFiles.Where((string f, int t) => GetIfFileIsStale(f, t));

            foreach (string _sl in _staleLogs)
            {
                // make sure we dont delete anyone elses stuff in here
                if (_sl.Contains(logFileName))
                {
                    System.IO.File.Delete(_sl);
                }
            }
        }


        private static bool GetIfFileIsStale(string a_fullFilePath, int a_maxFileLifetimeDays)
        {
            if (System.IO.File.Exists(a_fullFilePath) == false) return false;
            System.DateTime _fileCreated = System.IO.File.GetLastAccessTime(a_fullFilePath);
            System.TimeSpan _fileLifetime = System.DateTime.Today - _fileCreated;
            int _fileAgeDays = _fileLifetime.Days;

            return _fileAgeDays > a_maxFileLifetimeDays;
        }
    }
}

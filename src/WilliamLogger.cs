using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using static src.WilliamTell.WilliamLogger.WPriority;
using static src.WilliamTell.WilliamLogger.WPurpose;

namespace src.WilliamTell
{
    // William
    public class WilliamLogger
    {
        /* Static fields */
        public  readonly static Encoding globalEnc = Encoding.Unicode;
        public  readonly static string WILLIAM_LOG_DECORATION = ">>> ";
        public  readonly static string WILLIAM_SIGN = "William";
        public  readonly static string DEAFULT_WILLIAM_PURPOSE = WPurpose.LOGGING;
        private readonly static string DEFAULT_LOG_FILE_NAME_ALONE = "UntitledLog";
        private readonly static string DEFAULT_LOG_FILE_PATH = $"C:\\Users\\{Environment.UserName}\\Documents\\WilliamNTFSLog.wlog";
        private static WilliamLogger globalWilliamLogger
            = new WilliamLogger(WilliamLogger.WPriority.NONE, WilliamLogger.WPurpose.NOTHING);

        private readonly static bool[] FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_ALL
            = new bool[] { true, true, true };
        private readonly static bool[] FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_NONE
            = new bool[] { false, false, false };
        private readonly static bool[] FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_READONLY
            = new bool[] { true, false, false };
        private readonly static bool[] FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_WRITEONLY
            = new bool[] { false, true, false };
        private readonly static bool[] FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_SEEKONLY
            = new bool[] { false, false, true };
        /*        private readonly static bool[][] FILE_STREAM_ACCESS_PERMISSION_RESTRICTIONS_ALL
                    = new bool[][] {
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_ALL,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_ALL,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_ALL
                    };
                private readonly static bool[][] FILE_STREAM_ACCESS_PERMISSION_RESTRICTIONS_NONE
                    = new bool[][] {
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_NONE,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_NONE,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_NONE
                    };
                private readonly static bool[][] FILE_STREAM_ACCESS_PERMISSION_RESTRICTIONS_READONLY
                    = new bool[][] {
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_READONLY,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_READONLY,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_READONLY
                    };
                private readonly static bool[][] FILE_STREAM_ACCESS_PERMISSION_RESTRICTIONS_WRITEONLY
                    = new bool[][] {
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_WRITEONLY,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_WRITEONLY,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_WRITEONLY
                    };
                private readonly static bool[][] FILE_STREAM_ACCESS_PERMISSION_RESTRICTIONS_SEEKONLY
                    = new bool[][] {
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_SEEKONLY,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_SEEKONLY,
                        FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_SEEKONLY
                    };*/
        private static readonly int BUFF_SIZE_MAX = 4096;

        /* Instance fields */
        private readonly DateTimeFormat mLogFileNameDateTimeFormat;
        private readonly DateTime mLogFileNameDateTimeValue;
        private readonly string mLogFileName;
        private readonly string mLogFilePath;
        private readonly string mLogFile;
        private readonly object[] mPriority;
        private readonly string mPurpose;
        private readonly string[] mRedirections;
        private readonly FileStream[] mStreamRedirections;
        private readonly bool mRedirectionsOnly;
        private Int16[] buffer;

        public class WPriority
        {
            public static readonly object[] NONE        = { "NONE"        , int.MinValue };
            public static readonly object[] MINOR       = { "MINOR"       , 10000        };
            public static readonly object[] NORMAL      = { "NORMAL"      , 20000        };
            public static readonly object[] MAJOR       = { "MAJOR"       , 30000        };
            public static readonly object[] SERIOUS     = { "SERIOUS"     , 40000        };
            public static readonly object[] DANDEROUS   = { "DANDEROUS"   , 50000        };
            public static readonly object[] FATAL       = { "FATAL"       , 60000        };
            public static readonly object[] DEBUG       = { "DEBUG"       , 70000        };
            public static readonly object[] ALL         = { "ALL"         , int.MaxValue };
            public static readonly object[] DEFAULT     = NONE;

            private WPriority() { }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="current"></param>
            /// <param name="other"></param>
            /// <returns>2 For incomparable;
            ///          1 For CURRENT is greater than OTHER;
            ///          0 For CURRENT is equal as OTHER;
            ///         -1 For CURRENT is less than OTHER; </returns>
            public static int Compare(object[] current, object[] other)
            {
                if (current == null || other == null)
                    return 2;

                try
                {
                    Int64 i64Current = Convert.ToInt64(current[1]);
                    Int64 i64Other = Convert.ToInt64(other[1]);
                    if (i64Current > i64Other)
                    { return 1; }
                    else if (i64Current == i64Other)
                    { return 0; }
                    else
                    { return -1; }

                }
                catch (IndexOutOfRangeException outOfRangeExcep)
                {
                    globalWilliamLogger
                        .Log(new object[]
                             {
                                 $"Illegal parameter {nameof(current)} and parameter {nameof(other)}\n" +
                                 $"had not have proper length which is used to satisfy {nameof(WPriority)}.\n" +
                                 $"In this particular case:\nParameter {nameof(current)} have length being as {current.Length}\n" +
                                 $"Parameter {nameof(other)} have length being as {current.Length}\n" +
                                 $"While members of {nameof(WPriority)} all require at least having length as much as " +
                                 $"{WPriority.NONE.Length}"
                             },
                             WilliamLogger.WPriority.SERIOUS,
                             WilliamLogger.WPurpose.LOGGING
                        );
                    return 2;
                }
            }

            public static string GetName(object[] priority)
            {
                return (string)priority[0];
            }
        }

        public class WPurpose
        {
            public static readonly string NOTHING = "Nothing";
            public static readonly string LOGGING = "Logging";
            public static readonly string TESTING = "Testing";
            public static readonly string EXCEPTION = "Exception";
            public static readonly string DEFAULT = NOTHING;
        }

        public WilliamLogger(object[] priority, string purpose)
        {
            string logFIleNameDateTimeFormatString = this.mLogFileNameDateTimeFormat.ToString();

            this.mPriority = priority;
            this.mPurpose = purpose;

            this.mLogFileNameDateTimeFormat = new DateTimeFormat("yyyyMMddHHmmss");
            this.mLogFileNameDateTimeValue = DateTime.Now;
            this.mLogFileName = (logFIleNameDateTimeFormatString is null
                                 ? DEFAULT_LOG_FILE_NAME_ALONE
                                 : logFIleNameDateTimeFormatString);
            this.mLogFilePath = DEFAULT_LOG_FILE_PATH;
            this.mLogFile = (this.mLogFilePath + this.mLogFileName);

            this.mRedirections = new string[] { "" };
            this.mStreamRedirections = new FileStream[1];
            this.mRedirectionsOnly = false;

            this.buffer = new Int16[BUFF_SIZE_MAX];
        }
        public WilliamLogger(object[] priority, string purpose, string[] redirections)
        {
            string logFIleNameDateTimeFormatString = this.mLogFileNameDateTimeFormat.ToString();

            this.mPriority = priority;
            this.mPurpose = purpose;

            this.mLogFileNameDateTimeFormat = new DateTimeFormat("yyyyMMddHHmmss");
            this.mLogFileNameDateTimeValue = DateTime.Now;
            this.mLogFileName = (logFIleNameDateTimeFormatString is null
                                 ? DEFAULT_LOG_FILE_NAME_ALONE
                                 : logFIleNameDateTimeFormatString);
            this.mLogFilePath = DEFAULT_LOG_FILE_PATH;
            this.mLogFile = (this.mLogFilePath + this.mLogFileName);

            this.mRedirections = redirections;
            this.mStreamRedirections = new FileStream[1];
            this.mRedirectionsOnly = false;

            this.buffer = new Int16[BUFF_SIZE_MAX];
        }
        public WilliamLogger(object[] priority, string purpose, string[] redirections, bool redirectionsOnly)
        {
            string logFIleNameDateTimeFormatString = this.mLogFileNameDateTimeFormat.ToString();

            this.mPriority = priority;
            this.mPurpose = purpose;

            this.mLogFileNameDateTimeFormat = new DateTimeFormat("yyyyMMddHHmmss");
            this.mLogFileNameDateTimeValue = DateTime.Now;
            this.mLogFileName = (logFIleNameDateTimeFormatString is null
                                 ? DEFAULT_LOG_FILE_NAME_ALONE
                                 : logFIleNameDateTimeFormatString);
            this.mLogFilePath = DEFAULT_LOG_FILE_PATH;
            this.mLogFile = (this.mLogFilePath + this.mLogFileName);

            this.mRedirections = redirections;
            this.mStreamRedirections = new FileStream[1];
            this.mRedirectionsOnly = redirectionsOnly;

            this.buffer = new Int16[BUFF_SIZE_MAX];
        }
        public WilliamLogger(object[] priority, string purpose, bool redirectionsOnly)
        {
            string logFIleNameDateTimeFormatString = this.mLogFileNameDateTimeFormat.ToString();

            this.mPriority = priority;
            this.mPurpose = purpose;

            this.mLogFileNameDateTimeFormat = new DateTimeFormat("yyyyMMddHHmmss");
            this.mLogFileNameDateTimeValue = DateTime.Now;
            this.mLogFileName = (logFIleNameDateTimeFormatString is null
                                 ? DEFAULT_LOG_FILE_NAME_ALONE
                                 : logFIleNameDateTimeFormatString);
            this.mLogFilePath = DEFAULT_LOG_FILE_PATH;
            this.mLogFile = (this.mLogFilePath + this.mLogFileName);

            this.mRedirections = new string[] { "" };
            this.mStreamRedirections = new FileStream[1];
            this.mRedirectionsOnly = redirectionsOnly;

            this.buffer = new Int16[BUFF_SIZE_MAX];
        }
        public WilliamLogger(object[] priority, string purpose, FileStream[] streamRedirections)
        {
            string logFIleNameDateTimeFormatString = this.mLogFileNameDateTimeFormat.ToString();

            this.mPriority = priority;
            this.mPurpose = purpose;

            this.mLogFileNameDateTimeFormat = new DateTimeFormat("yyyyMMddHHmmss");
            this.mLogFileNameDateTimeValue = DateTime.Now;
            this.mLogFileName = (logFIleNameDateTimeFormatString is null
                                 ? DEFAULT_LOG_FILE_NAME_ALONE
                                 : logFIleNameDateTimeFormatString);
            this.mLogFilePath = DEFAULT_LOG_FILE_PATH;
            this.mLogFile = (this.mLogFilePath + this.mLogFileName);

            this.mRedirections = new string[] { "" };
            this.mStreamRedirections = streamRedirections;
            this.mRedirectionsOnly = false;

            this.buffer = new Int16[BUFF_SIZE_MAX];
        }
        public WilliamLogger(object[] priority, string purpose, FileStream[] streamRedirections, bool redirectionsOnly)
        {
            string logFIleNameDateTimeFormatString = this.mLogFileNameDateTimeFormat.ToString();

            this.mPriority = priority;
            this.mPurpose = purpose;

            this.mLogFileNameDateTimeFormat = new DateTimeFormat("yyyyMMddHHmmss");
            this.mLogFileNameDateTimeValue = DateTime.Now;
            this.mLogFileName = (logFIleNameDateTimeFormatString is null
                                 ? DEFAULT_LOG_FILE_NAME_ALONE
                                 : logFIleNameDateTimeFormatString);
            this.mLogFilePath = DEFAULT_LOG_FILE_PATH;
            this.mLogFile = (this.mLogFilePath + this.mLogFileName);

            this.mRedirections = new string[] { "" };
            this.mStreamRedirections = streamRedirections;
            this.mRedirectionsOnly = redirectionsOnly;

            this.buffer = new Int16[BUFF_SIZE_MAX];
        }
        public WilliamLogger(object[] priority, string purpose, FileStream[] streamRedirections, string[] redirections)
        {
            string logFIleNameDateTimeFormatString = this.mLogFileNameDateTimeFormat.ToString();

            this.mPriority = priority;
            this.mPurpose = purpose;

            this.mLogFileNameDateTimeFormat = new DateTimeFormat("yyyyMMddHHmmss");
            this.mLogFileNameDateTimeValue = DateTime.Now;
            this.mLogFileName = (logFIleNameDateTimeFormatString is null
                                 ? DEFAULT_LOG_FILE_NAME_ALONE
                                 : logFIleNameDateTimeFormatString);
            this.mLogFilePath = DEFAULT_LOG_FILE_PATH;
            this.mLogFile = (this.mLogFilePath + this.mLogFileName);

            this.mRedirections = new string[] { "" };
            this.mStreamRedirections = streamRedirections;
            this.mRedirectionsOnly = false;

            this.buffer = new Int16[BUFF_SIZE_MAX];
        }
        public WilliamLogger(object[] priority, string purpose, FileStream[] streamRedirections, string[] redirections, bool redirectionsOnly)
        {
            string logFIleNameDateTimeFormatString = this.mLogFileNameDateTimeFormat.ToString();

            this.mPriority = priority;
            this.mPurpose = purpose;

            this.mLogFileNameDateTimeFormat = new DateTimeFormat("yyyyMMddHHmmss");
            this.mLogFileNameDateTimeValue = DateTime.Now;
            this.mLogFileName = (logFIleNameDateTimeFormatString is null
                                 ? DEFAULT_LOG_FILE_NAME_ALONE
                                 : logFIleNameDateTimeFormatString);
            this.mLogFilePath = DEFAULT_LOG_FILE_PATH;
            this.mLogFile = (this.mLogFilePath + this.mLogFileName);

            this.mRedirections = new string[] { "" };
            this.mStreamRedirections = streamRedirections;
            this.mRedirectionsOnly = redirectionsOnly;

            this.buffer = new Int16[BUFF_SIZE_MAX];
        }

        public object[] Priority { get { return mPriority; } }

        public string Purpose { get { return mPurpose; } }


        /* 00 func Log: (object[] info)                                                                                         // Uses @info, Priority.DEFAULT, Purpose.DEFAULT, stderr, Exception, false
         * 01 func Log: (object[] info, object[] priority, string purpose)                                                      // Uses @info, @priority, @purpose, stderr, Exception, false
         * 02 func Log: (object[] info, object[] priority, string purpose, string[]? redirections)                              // Uses @info, @priority, @purpose, @redirections, Exception, false
         * 03 func Log: (object[] info, object[] priority, string purpose, string[]? redirections, bool redirectionsOnly)       // Uses @info, @priority, @purpose, @redirections, Exception, @redirectionsOnly
         * 03 func Log: (object[] info, object[] priority, string purpose, Exception innerException)                            // Uses @info, @priority, @purpose, stderr, @innerException, false
         * 04 func Log: (object[] info, object[] priority, string purpose, string[]? redirections, Exception innerException)    // Uses @info, @priority, @purpose, @redirections, @innerException, false
         * 05 func Log: (object[] info, object[] priority, string purpose, string[]? redirections, Exception innerException,    // Uses @info, @priority, @purpose, @redirections, @innerException, @redirectionsOnly
         * ........................................................................................ bool redirectionsOnly)
         * 06 func Log: (object[] info, WilliamLogger logger)                                                                   // Uses @info, @logger.mPriority, @logger.mPurpose, stderr, Exception, false
         * 07 func Log: (object[] info, WilliamLogger logger, string[]? redirections)                                           // Uses @info, @logger.mPriority, @logger.mPurpose, @redirections, Exception, false
         * 08 func Log: (object[] info, WilliamLogger logger, string[]? redirections, bool redirectionsOnly)                    // Uses @info, @logger.mPriority, @logger.mPurpose, @redirections, Exception, @redirectionsOnly
         * 09 func Log: (object[] info, WilliamLogger logger, Exception innerException)                                         // Uses @info, @logger.mPriority, @logger.mPurpose, stderr, @innerException, false
         * 0A func Log: (object[] info, WilliamLogger logger, string[]? redirections, Exception innerException)                 // Uses @info, @logger.mPriority, @logger.mPurpose, @redirections, @innerException, false
         * 0B func Log: (object[] info, WilliamLogger logger, string[]? redirections, Exception innerException,                 // Uses @info, @logger.mPriority, @logger.mPurpose, @redirections, @innerException, @redirectionsOnly
         * ........................................................................... bool redirectionsOnly)
         */

        public void Log(object[] info)
        {
            Log(info, WPriority.DEFAULT, WPurpose.DEFAULT, null, false);
        }
        public void Log(object[] info, object[] priority, string purpose)
        {
            Log(info, priority, purpose, null, false);
        }
        public void Log(object[] info, object[] priority, string purpose, string[] redirections)
        {
            Log(info, priority, purpose, redirections, false);
        }
        /* ! */
        public void Log(object[] info, object[] priority, string purpose, string[] redirections, bool redirectionsOnly)
        {
            /* Null check */
            if (info is null)
            {
                info = new object[] { "" };
            }
            if (redirections is null)
            {
                redirections = new string[] { DEFAULT_LOG_FILE_PATH + DEFAULT_LOG_FILE_NAME_ALONE };
            }

            string result = GenerateLogContent(GenerateWilliamPrecontent(priority, purpose), info);

            for (int i = 0; i < redirections.Length; i++)
            {
                // YOU LEFT HERE
            }
        }
        public void Log(object[] info, object[] priority, string purpose, Exception innerException) { }
        public void Log(object[] info, object[] priority, string purpose, string[] redirections, Exception innerException) { }
        public void Log(object[] info, object[] priority, string purpose, string[] redirections, Exception innerException, bool redirectionsOnly) { }
        public void Log(object[] info, WilliamLogger logger) { }
        public void Log(object[] info, WilliamLogger logger, string[] redirections) { }
        /* ! */
        public void Log(object[] info, WilliamLogger logger, string[] redirections, bool redirectionsOnly) { }
        public void Log(object[] info, WilliamLogger logger, Exception innerException) { }
        public void Log(object[] info, WilliamLogger logger, string[] redirections, Exception innerException) { }
        public void Log(object[] info, WilliamLogger logger, string[] redirections, Exception innerException, bool redirectionsOnly) { }

        /// <summary>
        /// Make sure every line for outputing is covered with WilliamPrecontent ahead.
        /// </summary>
        /// <param name="info"></param>\
        /// <returns>A single string which contains info of generated info to be logged out.
        ///          It returns "" when info is null or empty. </returns>
        public static string GenerateLogContent(string WilliamPrecontent, object[] info)
        {
            if (info is null)
            {
                WilliamLogger.GetGlobal()
                    .Log(new object[] { $"{nameof(info)} should never be null." },
                         DEBUG,
                         EXCEPTION);
                return "";
            }

            /* Don't forget the first WilliamPreContent~~ */
            string rtn = WilliamPrecontent;

            try
            {
                for (int i = 0; i < info.Length; i++)
                {
                    if (info[i].ToString() == null)
                    {
                        throw new ArgumentNullException();
                    }

                    string currStr = info[i].ToString();
                    char currStrChar = char.MaxValue;

                    for (int j = 0; j < currStr.Length; j++)
                    {
                        currStrChar = currStr[j];
                        if (currStrChar == '\n')
                        {
                            rtn += '\n';
                            rtn += WilliamPrecontent;
                            continue;
                        }
                        rtn += currStrChar;
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                WilliamLogger.GetGlobal()
                    .Log(new object[] { e.Message },
                         WilliamLogger.WPriority.SERIOUS,
                         WilliamLogger.WPurpose.EXCEPTION);
            }

            /*Finish logging by printting a line breaker.*/
            rtn += '\n';
            return rtn; // :)
        }

        /// <summary>
        /// Make sure every line for outputing is covered with WilliamPrecontent ahead.
        /// </summary>
        /// <param name="info"></param>\
        /// <returns>A single string which contains info of generated info to be logged out.
        ///          It returns "" when info is null or empty. </returns>
        public static byte[] GenerateLogContentByteArray(string WilliamPrecontent, object[] info)
        {
            if (info is null)
            {
                WilliamLogger.GetGlobal()
                    .Log(new object[] { $"{nameof(info)} should never be null." },
                         DEBUG,
                         EXCEPTION);
                return new byte[0];
            }

            /* Don't forget the first WilliamPreContent~~ */
            byte[] rtn = Convert.FromBase64String(WilliamPrecontent);

            try
            {
                for (int i = 0; i < info.Length; i++)
                {
                    if (info[i].ToString() == null)
                    {
                        throw new ArgumentNullException();
                    }

                    string currStr = info[i].ToString();
                    char currStrChar = char.MaxValue;

                    for (int j = 0; j < currStr.Length; j++)
                    {
                        currStrChar = currStr[j];
                        if (currStrChar == '\n')
                        {
                            rtn = ByteArrayAppend(rtn, Convert.ToByte('\n'));
                            for (int k = 0; k < WilliamPrecontent.Length; k++)
                            {
                                rtn = ByteArrayAppend(rtn, Convert.ToByte(WilliamPrecontent[i]));
                            }
                            continue;
                        }
                        rtn = ByteArrayAppend(rtn, Convert.ToByte(currStrChar));
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                WilliamLogger.GetGlobal()
                    .Log(new object[] { e.Message },
                         WilliamLogger.WPriority.SERIOUS,
                         WilliamLogger.WPurpose.EXCEPTION);
            }

            /* Finish logging by printting a line breaker. */
            rtn = ByteArrayAppend(rtn, Convert.ToByte('\n'));
            return rtn; // :)
        }
        private static string GenerateWilliamPrecontent(object[] priority, string purpose)
        {
            return ($"{WILLIAM_LOG_DECORATION}[{priority[0]}]({WILLIAM_SIGN} - {purpose}): ");
        }
        private static byte[] GenerateWilliamPrecontentByteArray(object[] priority, string purpose)
        {
            /* Pick example */
            string williamPrecontentString = GenerateWilliamPrecontent(priority, purpose);

            /* Initiate result container */
            byte[] rtn = new byte[williamPrecontentString.Length];
            /* Fullfill result container */
            for (int i = 0; i < rtn.Length; i++)
            {
                rtn[i] = Convert.ToByte(williamPrecontentString[i]);
            }

            return rtn;
        }
        private static string GenerateLogFileFullName()
        {
            return (); // TODO: HERE
        }
        public static WilliamLogger GetGlobal()
        {
            return globalWilliamLogger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wLogger"></param>
        /// <param name="streams">Target indexed redirection array.</param>
        /// <exception cref="ArgumentException">Thrown once target indexed object is not Stream nor sub-Stream.</exception>
        private void LogToVariantStreams(WilliamLogger wLogger, string[] streams)
        {
            CheckRedirectionsPermissions(streams, FILE_STREAM_ACCESS_PERMISSION_RESTRICTIONS_ALL); // HERE: Issued

            for (int i = 0; i < streams.Length; i++)
            {
                if (streams[i] is null)
                {
                    throw new ArgumentNullException();
                }

                try
                {
                    /* Keyword "using" is used from caller */
                    streams[i].Write(GenerateWilliamPrecontentByteArray(wLogger.mPriority, wLogger.mPurpose));
                }
                catch (IOException ioe)
                {
                    // TODO: Completing actions after catching IOException.
                }
            }
        }
        public void CheckCreateOnFile(string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Write))
            {
                CheckRedirectionPermissions(file, FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_WRITEONLY);
            }

            if (!File.Exists(file))
            {
                File.Create(file);
            }
        }
        public void CheckCreateOnFiles(string[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {

                CheckRedirectionsPermissions(files, FILE_STREAM_ACCESS_PERMISSION_RESTRICTIONS_WRITEONLY);

                if (!File.Exists(files[i]))
                {
                    File.Create(files[i]);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="redirection"></param>
        /// <param name="restriction">A linear array stores restriction on
        /// making specified permission of item being accessed.
        /// { READING, WRITTING, SEEKING } is for item's permissions;
        /// Each item have those 3 permissions to be optionally required.
        /// Formular: restriction[ITEM][PERM] : bool 
        /// <exception cref="IOException">Thrown once errored accessing.</exception>
        private void CheckRedirectionPermissions(string redirection, bool[] restriction)
        {
            using (FileStream redirectionStream = new FileStream(redirection, FileMode.Open))
            {
                for (int i = 0; i < 3; i++)
                {
                    /* Restricted */
                    if (restriction[i])
                    {
                        /* Readable->Do nothing */
                        /* Unreadable->Exception */
                        if (!redirectionStream.CanRead)
                        {
                            throw new IOException($"File \"{redirectionStream.Name}\" cannot be read.");
                        }
                        /* Same */
                        if (!redirectionStream.CanWrite)
                        {
                            throw new IOException($"File \"{redirectionStream.Name}\" cannot be written.");
                        }
                        /* Same */
                        if (!redirectionStream.CanSeek)
                        {
                            throw new IOException($"File \"{redirectionStream.Name}\" cannot be seeken.");
                        }
                    }
                    else /* Inrestricted */
                    {
                        /* Readable->Do nothing
                           Unreadable->Log warnning */
                        if (!redirectionStream.CanRead)
                        {
                            string info = $"File \"{redirectionStream.Name}\" was neither readable nor restricted.";
                            WilliamLogger.GetGlobal()
                                .Log(new object[] { info },
                                MAJOR, LOGGING, new string[] { this.mLogFile });
                        }
                        /* Same */
                        if (!redirectionStream.CanWrite)
                        {
                            string info = $"File \"{redirectionStream.Name}\" was neither writable nor restricted.";
                            WilliamLogger.GetGlobal()
                                .Log(new object[] { info },
                                MAJOR, LOGGING, new string[] { this.mLogFile });
                        }
                        /* Same */
                        if (!redirectionStream.CanSeek)
                        {
                            string info = $"File \"{redirectionStream.Name}\" was neither seekable nor restricted.";
                            WilliamLogger.GetGlobal()
                                .Log(new object[] { info },
                                MAJOR, LOGGING, new string[] { this.mLogFile });
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redirections"></param>
        /// <param name="restrictions"></param> An 2D array stores restriction on
        /// making specified permission of item being accessed.
        /// { item1, item2, item3... } is for items;
        /// { READING, WRITTING, SEEKING } is for permissions;
        /// Each item have those 3 permissions to be optionally required.
        /// Formular: restriction[ITEM][PERM] : bool
        /// <exception cref="IOException">Thrown once errored accessing.</exception>
        private void CheckRedirectionsPermissions(string[] redirections, bool[][] restrictions)
        {
            for (int i = 0; i < redirections.Length; i++)
            {
                CheckRedirectionPermissions(redirections[i], restrictions[i]);
            }
        }
        public string GetLogFileName()
        {
            return this.mLogFileName;
        }
        public string GetLogFilePath()
        {
            return this.mLogFilePath;
        }
        public string GetLogFile()
        {
            return this.mLogFile;
        }
        private static byte[] ByteArrayAppend(byte[] array, byte content)
        {
            int len = array.Length;

            /* Create a new Array<byte> object */
            byte[] newArray = new byte[len + 1];
            /* Copy elements from original array */
            for (int i = 0; i < len; i++)
            {
                newArray[i] = array[i];
            }
            /* Have the last desired element added into the new array */
            newArray[len - 1] = content;

            return newArray;
        }
        private static bool[][] FileStreamAccessPermissionRestrictionsAdding(bool[][] A, bool[][] B)
        {
            int lenA = A.Length;
            int lenB = B.Length;
            int lenRtn = Math.Max(lenA, lenB);

            bool[][] rtn = new bool[][] { FILE_STREAM_ACCESS_PERMISSION_RESTRICTION_NONE };

            for (int i = 0; i < lenA; i++)
                for (int j = 0; j < A[0].Length; j++)
                    rtn[i][j] = (A[i][j] || B[i][j]);
            return rtn;
        }
        private static bool[][] FileStreamAccessPermissionRestrictionsMinusing(bool[][] A, bool[][] B)
        {
            bool[][] rtn = FILE_STREAM_ACCESS_PERMISSION_RESTRICTIONS_NONE;

            for (int i = 0; i < A.Length; i++)
                for (int j = 0; j < A[0].Length; j++)
                    rtn[i][j] = (A[i][j] ^ B[i][j]);
            return rtn;
        }
    }
}

namespace DirectConnectRoads.Util {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using UnityEngine;

#if !DEBUG
#if TRACE
#error TRACE is defined outside of a DEBUG build, please remove
#endif
#endif

    /// <summary>
    /// Log.Trace, Log.Debug, Log.Info, Log.Warning, Log.Error -- these format the message in place,
    ///     ✔ Cheap if there is a const string or a very simple format call with a few args.
    ///     ✔ Cheap if wrapped in an if (booleanValue) { ... }
    ///     Log.Debug and Log.Trace are optimized away if not in Debug mode
    ///     ⚠ Expensive if multiple $"string {interpolations}" are used (like breaking into multiple lines)
    ///
    /// Log.DebugFormat, Log.InfoFormat, ... - these format message later, when logging. Good for
    /// very-very long format strings with multiple complex arguments.
    ///     ✔ As they use format string literal, it can be split multiple lines without perf penalty
    ///     💲 The cost incurred: bulding args array (with pointers)
    ///     Prevents multiple calls to string.Format as opposed to multiline $"string {interpolations}"
    ///     Log.DebugFormat is optimized away, others are not, so is a good idea to wrap in if (boolValue)
    ///     ⚠ Expensive if not wrapped in a if () condition
    ///
    /// Log.DebugIf, Log.WarningIf, ... - these first check a condition, and then call a lambda,
    /// which provides a formatted string.
    ///     ✔ Lambda building is just as cheap as format args building
    ///     💲 The cost incurred: each captured value (pointer) is copied into lambda
    ///     ✔ Actual string is formatted ONLY if the condition holds true
    ///     Log.DebugIf is optimized away if not in Debug mode
    ///     ⚠ Cannot capture out and ref values
    ///
    /// Log.NotImpl logs an error if something is not implemented and only in debug mode
    /// </summary>
    public static class Log {
        private static readonly object LogLock = new object();

        // TODO refactor log filename to configuration
        private static readonly string LogFilename
            = Path.Combine(Application.dataPath, "DirectConnectRoads.debug.log");

        private enum LogLevel {
            Trace,
            Debug,
            Info,
            Warning,
            Error
        }

        private static Stopwatch _sw = Stopwatch.StartNew();

        static Log() {
            try {
                if (File.Exists(LogFilename)) {
                    File.Delete(LogFilename);
                }
            }
            catch (Exception) { }
        }

        [Conditional("DEBUG")]
        public static void Debug(string s, bool logToFile = false) {
            LogToFile(s, LogLevel.Debug, logToFile);
        }

        public static void Info(string s, bool logToFile = true) {
            LogToFile(s, LogLevel.Info, logToFile);
        }

        public static void Error(string s, bool logToFile = true) {
            LogToFile(s, LogLevel.Error, logToFile);
        }

        public static void Warning(string s, bool logToFile = true) {
            LogToFile(s, LogLevel.Warning, logToFile);
        }

        private static void LogToFile(string log, LogLevel level, bool logToFile) {
            try {
                Monitor.Enter(LogLock);

                using (StreamWriter w = File.AppendText(LogFilename)) {
                    long secs = _sw.ElapsedTicks / Stopwatch.Frequency;
                    long fraction = _sw.ElapsedTicks % Stopwatch.Frequency;
                    string m =
                        $"{level.ToString()} " +
                        $"{secs:n0}.{fraction:D7}: " +
                        $"{log}";
                    w.WriteLine(m);
                    if(logToFile)UnityEngine.Debug.Log(m);

                    if (level == LogLevel.Warning || level == LogLevel.Error) {
                        w.WriteLine(Environment.StackTrace);
                        if (logToFile)UnityEngine.Debug.Log(Environment.StackTrace);
                    }
                    //w.WriteLine();
                }
            }
            finally {
                Monitor.Exit(LogLock);
            }
        }
    }
}

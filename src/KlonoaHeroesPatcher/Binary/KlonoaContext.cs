using System;
using System.IO;
using System.Text;
using System.Windows;
using BinarySerializer;

namespace KlonoaHeroesPatcher
{
    public class KlonoaContext : Context
    {
        public KlonoaContext(string basePath, string logPath, bool showWarnings) 
            : base(
                basePath: basePath, 
                settings: null, 
                serializerLog: logPath == null ? null : new KlonoaSerializerLog(logPath),
                logger: showWarnings ? new DebugBinaryLogger() : null) { }

        public class KlonoaSerializerLog : ISerializerLog
        {
            public KlonoaSerializerLog(string logFile)
            {
                LogFile = logFile;
            }

            private static bool _hasBeenCreated;
            private StreamWriter _logWriter;

            protected StreamWriter LogWriter => _logWriter ??= GetFile();
            public bool IsEnabled => true;
            public string OverrideLogPath { get; set; }
            public string LogFile { get; }

            public StreamWriter GetFile()
            {
                var w = new StreamWriter(File.Open(LogFile, _hasBeenCreated ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8);
                _hasBeenCreated = true;
                return w;
            }

            public void Log(object obj)
            {
                LogWriter.WriteLine(obj != null ? obj.ToString() : String.Empty);
            }

            public void Dispose()
            {
                _logWriter?.Dispose();
                _logWriter = null;
            }
        }

        public class DebugBinaryLogger : ILogger
        {
            public void Log(object log) { }

            public void LogWarning(object log) => MessageBox.Show(log.ToString(), "Warning");

            public void LogError(object log) => MessageBox.Show(log.ToString(), "Error");
        }
    }
}
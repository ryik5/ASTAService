using System;
using System.Reflection;
using System.Threading;

namespace ASTAWebClient
{

    public class DirectoryWatchLogger
    {
        System.IO.FileSystemWatcher watcher;
        public string pathToDir { get; private set; }
        readonly object obj = new object();
        bool enabled = true;
        public delegate void InfoMessage(object sender, TextEventArgs e);
        public event InfoMessage EvntInfoMessage;
        public bool IsReady { get { return pathToDir != null && System.IO.Directory.Exists(pathToDir); } }

        public DirectoryWatchLogger() { }
        public DirectoryWatchLogger(string pathToDir)
        {
            SetDirWatcher(pathToDir);
        }

        public void SetDirWatcher(string pathToDir)
        {
            watcher = new System.IO.FileSystemWatcher(pathToDir);
            watcher.IncludeSubdirectories = true;
            watcher.Deleted += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Changed += Watcher_Changed;
            watcher.Renamed += Watcher_Renamed;
            this.pathToDir = pathToDir;
        }

        public void StartWatcher()
        {
            watcher.EnableRaisingEvents = true;
            while (enabled)
            {
                Thread.Sleep(1000);
            }
        }
        public void StopWatcher()
        {
            watcher.EnableRaisingEvents = false;
            enabled = false;
        }

        // переименование файлов
        private void Watcher_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            string filePath = $"'{e.OldFullPath}' => '{e.FullPath}'";
            RecordEntry(filePath, e.ChangeType);
        }
        // изменение файлов
        // создание файлов
        // удаление файлов
        private void Watcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            string filePath = e.FullPath;
            RecordEntry(filePath, e.ChangeType);
        }

        private void RecordEntry(string filePath, System.IO.WatcherChangeTypes evntChanging)
        {
            string path = Assembly.GetExecutingAssembly().Location;
            string pathToLog = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".log");

            lock (obj)
            {
                string message = $"{DateTime.Now.ToString("yyyy.MM.dd|hh:mm:ss")}|{evntChanging}|{filePath}";

                EvntInfoMessage?.Invoke(this, new TextEventArgs(message));

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(pathToLog, true))
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
        }
    }


    public class Logger
    {
        readonly object obj = new object();

        public Logger() { }

        public void WriteString(string text, string eventText="Message")
        {
            RecordEntry(eventText, text);
        }
        private void RecordEntry(string eventText, string text)
        {
            string path = Assembly.GetExecutingAssembly().Location;
            string pathToLog = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".log");
            lock (obj)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(pathToLog, true))
                {
                    writer.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd|hh:mm:ss")}|{eventText}|{text}");
                    writer.Flush();
                }
            }
        }
    }
}
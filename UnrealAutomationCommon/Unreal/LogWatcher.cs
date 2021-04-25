﻿using System.IO;

namespace UnrealAutomationCommon.Unreal
{
    public delegate void LineLoggedEventHandler(string output);

    public class LogWatcher
    {
        private string _registeredLogFile = null;
        private StreamReader _reader = null;

        public event LineLoggedEventHandler LineLogged;

        public LogWatcher(Project project)
        {
            FileSystemWatcher directoryWatcher = new FileSystemWatcher(project.GetLogsPath());
            directoryWatcher.Filter = project.Name + "*.log";
            directoryWatcher.Created += (Sender, Args) =>
            {
                if (ShouldRegisterLogFile(Args.FullPath))
                {
                    RegisterLogFile(Args.FullPath);
                }
            };
            directoryWatcher.Changed += (Sender, Args) =>
            {
                if (ShouldRegisterLogFile(Args.FullPath))
                {
                    RegisterLogFile( Args.FullPath);
                }

                if (Args.FullPath != _registeredLogFile)
                {
                    // Ignore logs other than the registered one
                    return;
                }

                ReadToEnd();
            };
            directoryWatcher.EnableRaisingEvents = true;
        }

        private bool HasRegisteredLogFile => _registeredLogFile != null;

        private bool ShouldRegisterLogFile(string logFile)
        {
            return !HasRegisteredLogFile && !Path.GetFileNameWithoutExtension(logFile).Contains("-backup-");
        }

        private void RegisterLogFile(string logFile)
        {
            _registeredLogFile = logFile;

            FileStream stream = new System.IO.FileStream(_registeredLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _reader = new System.IO.StreamReader(stream);
            ReadToEnd();
        }

        private void ReadToEnd()
        {
            while (!_reader.EndOfStream)
            {
                string line = _reader.ReadLine();
                LineLogged?.Invoke(line);
            }
        }
    }
}
﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnrealAutomationCommon.Unreal;

namespace UnrealAutomationCommon.Operations
{
    public delegate void OperationOutputEventHandler(string output, LogVerbosity verbosity);
    public delegate void OperationEndedEventHandler(OperationResult result);

    public class OperationRunner
    {
        private Operation _operation;
        private OperationParameters _operationParameters;
        private Process _process;
        private bool _isWaitingForLogs = false;

        public event OperationOutputEventHandler Output;
        public event OperationEndedEventHandler Ended;

        public OperationRunner(Operation operation, OperationParameters operationParameters)
        {
            _operation = operation;
            _operationParameters = operationParameters;
        }

        public void Run()
        {
            string outputPath = _operation.GetOutputPath(_operationParameters);
            FileUtils.DeleteDirectoryIfExists(outputPath);

            bool readLogFile = _operation.ShouldReadOutputFromLogFile() && _operationParameters.Target is Project;

            if (readLogFile)
            {
                Project project = _operationParameters.Target as Project;
                LogWatcher logWatcher = new LogWatcher(project, _operation.GetLogsPath(_operationParameters));
                logWatcher.LineLogged += HandleLogLine;
            }

            _process = _operation.Execute(_operationParameters, (o, args) =>
            {
                HandleLogLine(args.Data);
            }, (o, args) =>
            {
                Output?.Invoke(args.Data, LogVerbosity.Error);
            }, (o, args) =>
            {
                OnProcessEnded();
            });

            if(_operationParameters.WaitForAttach)
            {
                Output?.Invoke("-WaitForAttach was specified, attach now", LogVerbosity.Log);
            }
        }

        public void Terminate()
        {
            Output?.Invoke("Operation terminated", LogVerbosity.Log);
            _process.Kill();
        }

        void OnProcessEnded()
        {
            if (_isWaitingForLogs)
            {
                return;
            }

            // Wait a little for logs to finish reading

            _isWaitingForLogs = true;

            Task.Delay(100).ContinueWith(t =>
            {
                HandleProcessEnded();
            });
        }

        void HandleLogLine(string line)
        {
            if (line == null)
            {
                return;
            }

            string[] split = line.Split(new []{": "}, StringSplitOptions.None);
            LogVerbosity verbosity = LogVerbosity.Log;
            if (split.Length > 1)
            {
                if (split[0] == "ERROR")
                {
                    // UBT error format
                    // "ERROR: Some message"
                    verbosity = LogVerbosity.Error;
                }
                else if (split[1] == "Error")
                {
                    // Unreal error format
                    // "LogCategory: Error: Some message"
                    verbosity = LogVerbosity.Error;
                }
                else if (split[1] == "Warning")
                {
                    // Unreal warning format
                    verbosity = LogVerbosity.Warning;
                }
            }
            Output?.Invoke(line, verbosity);
        }

        void HandleProcessEnded()
        {
            OperationResult result = new OperationResult();
            result.ExitCode = _process.ExitCode;

            Output?.Invoke("Process exited with code " + result.ExitCode, result.ExitCode == 0 ? LogVerbosity.Log : LogVerbosity.Error);

            if (_operationParameters.RunTests)
            {
                string reportFilePath = OutputPaths.GetTestReportFilePath(_operation.GetOutputPath(_operationParameters));
                TestReport report = TestReport.Load(reportFilePath);
                if (report != null)
                {
                    result.TestReport = report;
                }
                else
                {
                    Output?.Invoke("Expected test report at " + reportFilePath + " but didn't find one", LogVerbosity.Error);
                }

                if (result.TestReport != null)
                {
                    foreach (Test test in result.TestReport.Tests)
                    {
                        Output?.Invoke(EnumUtils.GetName(test.State).ToUpperInvariant().PadRight(7) + " - " + test.FullTestPath, test.State == TestState.Success ? LogVerbosity.Log : LogVerbosity.Error);
                        foreach(TestEntry entry in test.Entries)
                        {
                            if(entry.Event.Type != TestEventType.Info)
                            {
                                Output?.Invoke("".PadRight(9) + " - " + entry.Event.Message, entry.Event.Type == TestEventType.Error ? LogVerbosity.Error : LogVerbosity.Warning);
                            }
                        }
                    }
                    int testsPassed = result.TestReport.Tests.Count(t => t.State == TestState.Success);
                    bool allPassed = testsPassed == result.TestReport.Tests.Count;
                    Output?.Invoke(testsPassed + " of " + result.TestReport.Tests.Count + " tests passed", allPassed ? LogVerbosity.Log : LogVerbosity.Error);
                }
            }

            Ended?.Invoke(result);
        }
    }
}

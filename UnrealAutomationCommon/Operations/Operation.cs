﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnrealAutomationCommon.Operations.OperationTypes;

namespace UnrealAutomationCommon.Operations
{
    public abstract class Operation
    {
        public static Operation CreateOperation(OperationType operationType)
        {
            switch (operationType)
            {
                case OperationType.BuildEditorTarget:
                    return new BuildEditorTarget();
                case OperationType.BuildEditor:
                    return new BuildEditor();
                case OperationType.OpenEditor:
                    return new OpenEditor();
                case OperationType.PackageProject:
                    return new PackageProject();
                case OperationType.BuildPlugin:
                    return new BuildPlugin();
                default:
                    throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null);
            }
        }

        public string OperationName => GetOperationName();

        public Process Execute(OperationParameters operationParameters, DataReceivedEventHandler outputHandler, EventHandler exitHandler )
        {
            Command command = BuildCommand(operationParameters);

            if (command == null)
            {
                throw new Exception("No command");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command.File,
                Arguments = command.Arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = startInfo };
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += outputHandler;
            process.ErrorDataReceived += outputHandler;
            process.Exited += exitHandler;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        public Command GetCommand(OperationParameters operationParameters)
        {
            if (!RequirementsSatisfied(operationParameters))
            {
                return null;
            }

            return BuildCommand(operationParameters);
        }

        public bool RequirementsSatisfied(OperationParameters operationParameters)
        {
            if (RequiresProject() && operationParameters.Project == null)
            {
                return false;
            }

            if (RequiresPlugin() && operationParameters.Plugin == null)
            {
                return false;
            }

            return true;
        }

        protected string GetOutputPath(OperationParameters operationParameters)
        {
            string path = operationParameters.OutputPathRoot;
            if (operationParameters.UseOutputPathProjectSubfolder)
            {
                string subfolderName = IsPluginOnlyOperation()
                    ? operationParameters.Plugin.Name
                    : operationParameters.Project.Name;
                path = Path.Combine(path, subfolderName.Replace(" ", ""));
            }
            if (operationParameters.UseOutputPathOperationSubfolder)
            {
                path = Path.Combine(path, OperationName.Replace(" ",""));
            }
            return path;
        }

        protected abstract Command BuildCommand(OperationParameters operationParameters );

        protected virtual bool RequiresProject()
        {
            return !IsPluginOnlyOperation();
        }

        protected virtual bool RequiresPlugin()
        {
            return IsPluginOnlyOperation();
        }

        protected virtual bool IsPluginOnlyOperation()
        {
            return false;
        }

        protected virtual string GetOperationName()
        {
            string name = GetType().Name;
            return string.Concat(name.Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
        }
    }
}

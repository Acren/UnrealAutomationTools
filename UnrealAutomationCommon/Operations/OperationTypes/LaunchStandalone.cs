﻿namespace UnrealAutomationCommon.Operations.OperationTypes
{
    public class LaunchStandalone : ProjectOperation
    {
        protected override Command BuildCommand(OperationParameters operationParameters)
        {
            Arguments args = UnrealArguments.MakeArguments(operationParameters, GetOutputPath(operationParameters), true);
            args.SetFlag("game");
            args.SetFlag("windowed");
            args.SetKeyValue("resx", "1920");
            args.SetKeyValue("resy", "1080");
            return new Command(EnginePaths.GetEditorExe(GetProject(operationParameters).ProjectDescriptor.GetEngineInstallDirectory(), operationParameters), args);
        }
    }
}

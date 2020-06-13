﻿using System;
using System.Collections.Generic;
using System.Text;
using UnrealAutomationCommon.Operations;

namespace UnrealAutomationCommon
{
    public class BuildEditor : Operation
    {
        public override Command GetCommand(OperationParameters operationParameters)
        {
            return new Command(operationParameters.Project.ProjectDefinition.GetRunUAT(), UATArguments.ToString(operationParameters) );
        }

        public override string GetOperationName()
        {
            return "Build Editor";
        }
    }
}
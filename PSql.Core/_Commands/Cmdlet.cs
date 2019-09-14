﻿using System;
using System.Management.Automation;
using System.Management.Automation.Host;

namespace PSql
{
    public class Cmdlet : System.Management.Automation.Cmdlet
    {
        private static readonly string[]
            HostTag = { "PSHOST" };

        public void WriteHost(string message,
            bool          newLine         = true,
            ConsoleColor? foregroundColor = null,
            ConsoleColor? backgroundColor = null)
        {
            // Technique learned from PSv5+ Write-Host implementation, which
            // works by sending specially-marked messages to the information
            // stream.
            //
            // https://github.com/PowerShell/PowerShell/blob/v6.1.3/src/Microsoft.PowerShell.Commands.Utility/commands/utility/WriteConsoleCmdlet.cs

            var data = new HostInformationMessage
            {
                Message   = message,
                NoNewLine = !newLine
            };

            if (foregroundColor.HasValue || backgroundColor.HasValue)
            {
                try
                {
                    data.ForegroundColor = foregroundColor;
                    data.BackgroundColor = backgroundColor;
                }
                catch (HostException)
                {
                    // Host is non-interactive or does not support colors.
                }
            }

            WriteInformation(data, HostTag);
        }
    }
}
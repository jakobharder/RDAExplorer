﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ManyConsole.Internal;

namespace ManyConsole
{
    public class ConsoleCommandDispatcher
    {
        public static int DispatchCommand(ConsoleCommand command, string[] arguments, TextWriter consoleOut, TextWriter consoleErr)
        {
            return DispatchCommand(new [] {command}, arguments, consoleOut, consoleErr);
        }

        public static int DispatchCommand(IEnumerable<ConsoleCommand> commands, string[] arguments, TextWriter consoleOut, TextWriter consoleErr, bool skipExeInExpectedUsage = false)
        {
            ConsoleCommand selectedCommand = null;

            foreach (var command in commands)
            {
                ValidateConsoleCommand(command);
            }

            try
            {
                List<string> remainingArguments;

                if (commands.Count() == 1)
                {
                    selectedCommand = commands.First();

                    if (arguments.Count() > 0 && arguments.First().ToLower() == selectedCommand.Command.ToLower())
                    {
                        remainingArguments = selectedCommand.GetActualOptions().Parse(arguments.Skip(1));
                    }
                    else
                    {
                        remainingArguments = selectedCommand.GetActualOptions().Parse(arguments);
                    }
                }
                else
                {
                    if (arguments.Count() < 1)
                        throw new ConsoleHelpAsException("No arguments specified.");

                    if (arguments[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                    {
                        selectedCommand = GetMatchingCommand(commands, arguments.Skip(1).FirstOrDefault());

                        if (selectedCommand == null)
                            ConsoleHelp.ShowSummaryOfCommands(commands, consoleOut);
                        else
                            ConsoleHelp.ShowCommandHelp(selectedCommand, consoleOut, skipExeInExpectedUsage);

                        return -1;
                    }

                    selectedCommand = GetMatchingCommand(commands, arguments.First());

                    if (selectedCommand == null)
                        throw new ConsoleHelpAsException("Command name not recognized.");

                    remainingArguments = selectedCommand.GetActualOptions().Parse(arguments.Skip(1));
                }

                selectedCommand.CheckRequiredArguments();

                CheckRemainingArguments(remainingArguments, selectedCommand.RemainingArgumentsCount);

                var preResult = selectedCommand.OverrideAfterHandlingArgumentsBeforeRun(remainingArguments.ToArray());

                if (preResult.HasValue)
                    return preResult.Value;

                ConsoleHelp.ShowParsedCommand(selectedCommand, consoleOut);

                return selectedCommand.Run(remainingArguments.ToArray());
            }
            catch (ConsoleHelpAsException e)
            {
                return DealWithException(e, consoleErr, skipExeInExpectedUsage, selectedCommand, commands);
            }
            catch (NDesk.Options.OptionException e)
            {
                return DealWithException(e, consoleErr, skipExeInExpectedUsage, selectedCommand, commands);
            }
        }

        private static int DealWithException(Exception e, TextWriter console, bool skipExeInExpectedUsage, ConsoleCommand selectedCommand, IEnumerable<ConsoleCommand> commands)
        {
            if (selectedCommand != null)
            {
                console.WriteLine();
                console.WriteLine(e.Message);
                ConsoleHelp.ShowCommandHelp(selectedCommand, console, skipExeInExpectedUsage);
            }
            else
            {
                ConsoleHelp.ShowSummaryOfCommands(commands, console);
            }

            return -1;
        }
  
        private static ConsoleCommand GetMatchingCommand(IEnumerable<ConsoleCommand> command, string name)
        {
            return command.FirstOrDefault(c => c.Command.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static void ValidateConsoleCommand(ConsoleCommand command)
        {
            if (string.IsNullOrEmpty(command.Command))
            {
                throw new InvalidOperationException(String.Format(
                    "Command {0} did not call IsCommand in its constructor to indicate its name and description.",
                    command.GetType().Name));
            }
        }

        private static void CheckRemainingArguments(List<string> remainingArguments, int? parametersRequiredAfterOptions)
        {
            if (parametersRequiredAfterOptions.HasValue)
                ConsoleUtil.VerifyNumberOfArguments(remainingArguments.ToArray(),
                    parametersRequiredAfterOptions.Value);
        }

        public static IEnumerable<ConsoleCommand> FindCommandsInSameAssemblyAs(Type typeInSameAssembly)
        {
            if (typeInSameAssembly == null)
                throw new ArgumentNullException("typeInSameAssembly");

            return FindCommandsInAssembly(typeInSameAssembly.GetTypeInfo().Assembly);
        }

        public static IEnumerable<ConsoleCommand> FindCommandsInAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            var commandTypes = assembly.GetTypes()
                .Where(t => t.GetTypeInfo().IsSubclassOf(typeof(ConsoleCommand)))
                .Where(t => !t.GetTypeInfo().IsAbstract)
                .OrderBy(t => t.FullName);

            List<ConsoleCommand> result = new List<ConsoleCommand>();

            foreach(var commandType in commandTypes)
            {
                var constructor = commandType.GetTypeInfo().GetConstructor(new Type[] { });

                if (constructor == null)
                    continue;

                result.Add((ConsoleCommand)constructor.Invoke(new object[] { }));
            }

            return result;
        }
    }
}

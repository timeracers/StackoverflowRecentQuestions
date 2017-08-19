using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace StackoverflowRecentQuestions.UI
{
    public static class Program
    {
        private static Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
        private static Dictionary<string, string[]> _commandsHelp = new Dictionary<string, string[]>();

        private static FormattedGetRecentQuestions _questionGetter;
        private static IntervalQuestionsPresenter _intervalQuestions;
        public static ConcurrentStack<Action<string[]>> ConsoleReadLineStack;

        private static void Add(string name, Action<string[]> command, params string[] help)
        {
            _commands.Add(name.ToUpper(), command);
            _commandsHelp.Add(name.ToUpper(), help);
        }

        public static void Main(string[] args)
        {
            ConsoleReadLineStack = new ConcurrentStack<Action<string[]>>();
            var store = new IO(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\StackoverflowRecentQuestions");
            var web = new WebRequester();
            _questionGetter = new FormattedGetRecentQuestions(store, web);
            _questionGetter.Initialize();
            _intervalQuestions = new IntervalQuestionsPresenter(_questionGetter, store);
            _intervalQuestions.Initialize();

            Add("SetIntervalForQuestions", _intervalQuestions.SetInterval, "Syntax: SetIntervalForQuestions <Minutes>",
                "Sets the interval for when you are presented questions*",
                "*It will ask if you are ready for them before making the web request",
                "Syntax: SetIntervalForQuestions",
                "Disables automatic presentation of questions");
            Add("EnableReminder", (_) => _intervalQuestions.EnableReminder(), "Syntax: EnableReminder",
                "Makes the program play a sound and flash its icon when you are presented questions*");
            Add("DisableReminder", (_) => _intervalQuestions.DisableReminder(), "Syntax: DisableReminder",
                "Makes the program no longer play a sound and flash its icon when you are presented questions*");
            Add("SetDefaultTags", _questionGetter.SetDefaultTags, "Syntax: SetDefaultTags [<Tag1>] [<Tag2>]...",
                "Sets the default tags used in queries");
            Add("SetDefaultSite", _questionGetter.SetDefaultSite, "Syntax: SetDefaultSite <Site>",
                "Sets the default site used in queries");
            Add("GetRecentQuestions", (s) => _questionGetter.Get(s).Wait(),
               "Syntax: GetRecentQuestions [<Within Last X Days>] [<Page>]",
               "Gets questions within the specified time with the default tags, and site",
               "Syntax: GetRecentQuestions <Within Last X Days> <Page> <Site> [<Tag1>] [<Tag2>]...",
               "Gets questions within the specified time with any of the specified tags if any, and the site");

            Add("Exit", (_) => notQuitting = false, "Syntax: Exit", "Exits the program");
            Add("Quit", (_) => notQuitting = false, "Syntax: Quit", "Exits the program");
            Add("Clear", (_) => Console.Clear(), "Syntax: Clear", "Clears the console");
            Add("Help", (s) =>
            {
                if (s.Length == 0)
                {
                    Console.WriteLine("Valid Commands:");
                    foreach (var help in _commandsHelp)
                        WriteHelpSection(help);
                }
                else
                {
                    foreach (var command in s)
                    {
                        if (_commands.ContainsKey(command.ToUpper()))
                            WriteHelpSection(new KeyValuePair<string, string[]>(command.ToUpper(), _commandsHelp[command.ToUpper()]));
                        else
                            Console.WriteLine(command + " not recognized");
                    }
                }
            }, "Syntax: Help", "Writes the help section of each command",
                "Syntax: Help <Command> [<Command>] [<Command>]...", "Writes the help section of those commands");

            if (args.Length == 0)
                while (notQuitting)
                    ResolveCommand(ReadCommandLineArgs(Console.ReadLine()));
            else
                ResolveCommand(ReadCommandLineArgs(string.Join(" ", args)));
            Environment.Exit(Environment.ExitCode);
        }

        private static void ResolveCommand(string[] input)
        {
            if (!ConsoleReadLineStack.IsEmpty)
            {
                Action<string[]> action;
                ConsoleReadLineStack.TryPop(out action);
                action(input);
            }
            else
            {
                var commandFound = _commands.ContainsKey(input[0].ToUpper());
                if (commandFound)
                {
                    try
                    {
                        _commands[input[0].ToUpper()](input.SubArray(1, input.Length - 1));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception was unhandled in the command");
                        Console.WriteLine("Exception: " + ex.ToString());
                    }
                }
                else
                    Console.WriteLine("Invalid Command, type help to display all commands with their help sections");
            }
        }

        private static string[] ReadCommandLineArgs(string input)
        {
            if (input.IndexOf(" ") == -1)
                return new[] { input };
            else
            {
                var inputs = new List<string>() { input.Substring(0, input.IndexOf(" ")) };
                var remains = input.Substring(input.IndexOf(" "));
                while (remains.Length > 1)
                {
                    if (remains.Substring(0, 2) == " \"")
                    {
                        remains = remains.Substring(2);
                        var index = remains.IndexOf("\"");
                        inputs.Add(index != -1 ? remains.Substring(0, index) : remains);
                        remains = index != -1 ? remains.Substring(index + 1) : "";
                    }
                    else
                    {
                        remains = remains.Substring(1);
                        var index = remains.IndexOf(" ");
                        inputs.Add(index != -1 ? remains.Substring(0, index) : remains);
                        remains = index != -1 ? remains.Substring(index) : "";
                    }
                }
                return inputs.ToArray();
            }
        }

        private static void WriteHelpSection(KeyValuePair<string, string[]> help)
        {
            Console.WriteLine("    " + help.Key);
            foreach (var st in help.Value)
                Console.WriteLine(st);
        }

        private static bool notQuitting = true;
    }
}
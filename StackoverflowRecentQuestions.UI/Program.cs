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

        private static GetRecentQuestionsConsoleAdapter _questionGetter;
        private static IntervalQuestionsPresenter _intervalQuestions;
        private static SingleStore<Action<string[]>> _currentQuestion;
        private static JsonStore _store;
        private static bool Quiting = false;

        private static void Add(string name, Action<string[]> command, params string[] help)
        {
            _commands.Add(name.ToUpper(), command);
            _commandsHelp.Add(name.ToUpper(), help);
        }

        public static void Main(string[] args)
        {
            _currentQuestion = new SingleStore<Action<string[]>>();
            _store = new JsonStore(new HardDrive(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                + "\\StackoverflowRecentQuestions"));
            var web = new WebRequester();
            _questionGetter = GetRecentQuestionsConsoleAdapter.Create(_store, web, _currentQuestion);
            _intervalQuestions = IntervalQuestionsPresenter.Create(_questionGetter, _store);

            Add("ViewSettings", (_) => { Console.WriteLine(_store.Read<Options>("Options").ToString()); }, 
                "Syntax: ViewSettings", "Writes the current settings to the console");
            Add("SetIntervalForQuestions", _intervalQuestions.SetInterval, "Syntax: SetIntervalForQuestions <Minutes>",
                "Sets the interval for when you are presented questions",
                "Syntax: SetIntervalForQuestions",
                "Disables automatic presentation of questions");
            Add("EnableReminder", (_) => _intervalQuestions.EnableReminder(), "Syntax: EnableReminder",
                "Makes the program play a sound when you are presented questions");
            Add("DisableReminder", (_) => _intervalQuestions.DisableReminder(), "Syntax: DisableReminder",
                "Makes the program no longer play a sound when you are presented questions");
            Add("SetDefaultTags", _questionGetter.SetDefaultTags, "Syntax: SetDefaultTags [<Tag1>] [<Tag2>]...",
                "Sets the default tags used in queries");
            Add("SetDefaultSite", _questionGetter.SetDefaultSite, "Syntax: SetDefaultSite <Site>",
                "Sets the default site used in queries");
            Add("SetDefaultAmountOfQuestions", _questionGetter.SetDefaultAmountOfQuestions, "Syntax: SetDefaultSite <Number of questions>",
                "Sets the default amount of questions. The number has to within 1 and 100");
            Add("GetRecentQuestions", async (s) => await _questionGetter.Get(s),
               "Syntax: GetRecentQuestions [<Within Last X Days>] [<Page>]",
               "Gets questions within the specified time with the default tags, and site",
               "Syntax: GetRecentQuestions <Within Last X Days> <Page> <Site> [<Tag1>] [<Tag2>]...",
               "Gets questions within the specified time with any of the specified tags if any, and the site");

            Add("Exit", (_) => Quiting = true, "Syntax: Exit", "Exits the program");
            Add("Quit", (_) => Quiting = true, "Syntax: Quit", "Exits the program");
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
            {
                Console.WriteLine("Type commands. Use the Help command for information about commands");
                while (!Quiting)
                    ResolveCommand(ReadCommandLineArgs(Console.ReadLine()));
            }
            else
                ResolveCommand(ReadCommandLineArgs(string.Join(" ", args)));
            Environment.Exit(Environment.ExitCode);
        }

        private static void ResolveCommand(string[] input)
        {
            lock (_currentQuestion)
            {
                if (_currentQuestion.HasValue)
                {
                    Action<string[]> action = _currentQuestion.Value;
                    _currentQuestion.Clear();
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
    }
}
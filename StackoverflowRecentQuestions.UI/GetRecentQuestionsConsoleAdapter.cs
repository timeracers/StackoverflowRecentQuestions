using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackoverflowRecentQuestions.UI
{
    public class GetRecentQuestionsConsoleAdapter
    {
        private JsonStore _store;
        private ThrottleChecker _throttle;
        private RecentQuestionsGetter _questions;
        private IWebRequester _web;
        private SingleStore<Action<string[]>> _currentQuestion;
        
        public GetRecentQuestionsConsoleAdapter(JsonStore store, IWebRequester web, SingleStore<Action<string[]>> currentQuestion)
        {
            _store = store;
            _throttle = new ThrottleChecker(_store);
            _web = web;
            _currentQuestion = currentQuestion;
        }

        public void Initialize()
        {
            var options = _store.Read<Options>("Options");
            _questions = new RecentQuestionsGetter(_store, _web, options.DefaultTags, options.DefaultSite, options.DefaultAmountOfQuestions);
        }

        public static GetRecentQuestionsConsoleAdapter Create(JsonStore store, IWebRequester web, SingleStore<Action<string[]>> currentQuestion)
        {
            var getter = new GetRecentQuestionsConsoleAdapter(store, web, currentQuestion);
            getter.Initialize();
            return getter;
        }

        public async Task Get(string[] args)
        {
            if (args.Length < 3)
                await Get(args.Length > 0
                    ? UnixEpoch.Now + (long)TimeSpan.FromDays(-1 * long.Parse(args[0])).TotalSeconds
                    : UnixEpoch.Now + (long)TimeSpan.FromDays(-90).TotalSeconds,
                    args.Length > 1 ? int.Parse(args[1]) : 1);
            else
                await Get(UnixEpoch.Now + (long)TimeSpan.FromDays(-1 * long.Parse(args[0])).TotalSeconds, int.Parse(args[1]), args[2],
                    args.SubArray(3, args.Length - 3));
        }

        public async Task Get(long unixEpoch, int page = 1)
        {
            var throttleException = _throttle.IfICanNotMakeRequestsWhy();
            if (throttleException.HasValue)
                Console.WriteLine(throttleException.Value);
            else
                WriteQuestions(await _questions.GetSince(unixEpoch, page));
        }

        public async Task Get(long unixEpoch, int page, string site, params string[] tags)
        {
            var throttleException = _throttle.IfICanNotMakeRequestsWhy();
            if (throttleException.HasValue)
                Console.WriteLine(throttleException.Value);
            else
                WriteQuestions(await _questions.GetSince(unixEpoch, page, site, tags));
        }

        private void WriteQuestions(Optional<List<Question>> optionalQuestions)
        {
            if (!optionalQuestions.HasValue)
                Console.WriteLine("Throttle was Violated!");
            else if(optionalQuestions.Value.Count > 0)
            {
                lock (_currentQuestion)
                {
                    for (var i = 0; i < optionalQuestions.Value.Count; i++)
                    {
                        Console.Write((i + 1).ToString() + ": ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(optionalQuestions.Value[i].Title);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("    " + string.Join(", ", optionalQuestions.Value[i].Tags));
                        Console.ResetColor();
                        Console.WriteLine("    " + optionalQuestions.Value[i].Link);
                    }
                    Console.WriteLine("Open link in browser? Type a number to open one or don't type anything to not.");
                    _currentQuestion.Value = (a) => OpenLink(optionalQuestions, a);
                }
            }
            else
            {
                Console.WriteLine("No questions found");
            }
        }

        private void OpenLink(Optional<List<Question>> optionalQuestions, string[] args)
        {
            if (args.Length > 0)
            {
                uint number;
                if (uint.TryParse(args[0], out number))
                {
                    if (number <= optionalQuestions.Value.Count && number != 0)
                        Process.Start(optionalQuestions.Value[(int)number - 1].Link);
                    else
                        Console.WriteLine("No question has that number");
                    Console.WriteLine("Open another?");
                    _currentQuestion.Value = (a) => OpenLink(optionalQuestions, a);
                }
            }
        }

        public void SetDefaultSite(string[] args)
        {
            if (args.Length < 1)
                Console.WriteLine("Site name required");
            else
            {
                _questions.Site = args[0];
                var options = _store.Read<Options>("Options");
                options.DefaultSite = args[0];
                _store.Write("Options", options);
                Console.WriteLine("Default Site changed");
            }
        }

        public void SetDefaultTags(string[] args)
        {
            _questions.Tags = args;
            var options = _store.Read<Options>("Options");
            options.DefaultTags = args;
            _store.Write("Options", options);
            Console.WriteLine("Default Tags changed");
        }

        public void SetDefaultAmountOfQuestions(string[] args)
        {
            int number;
            if (args.Length < 1 || !int.TryParse(args[0], out number) || number > 100 || number < 1)
                Console.WriteLine("A number within 1 and 100 is required");
            else
            {
                _questions.Pagesize = number;
                var options = _store.Read<Options>("Options");
                options.DefaultAmountOfQuestions = number;
                _store.Write("Options", options);
                Console.WriteLine("Amount of questions changed");
            }
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackoverflowRecentQuestions.UI
{
    public class FormattedGetRecentQuestions
    {
        private IStore _store;
        private ThrottleChecker _throttle;
        private RecentQuestionsGetter _questions;
        private IWebRequester _web;

        public FormattedGetRecentQuestions(IStore store, IWebRequester web)
        {
            _store = store;
            _throttle = new ThrottleChecker(_store);
            _web = web;
        }

        public void Initialize()
        {
            var options = _store.Exists("Options.json") ? JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(_store.Read("Options.json")))
                : new JObject();
            _questions = new RecentQuestionsGetter(_store, _web,
                options["DefaultTags"] != null ? options["DefaultTags"].ToObject<string[]>() : new string[0],
                options["DefaultSite"] != null ? options["DefaultSite"].Value<string>() : "stackoverflow");
        }

        public async Task Get(string[] args)
        {
            if (args.Length < 3)
                await Get(args.Length > 0 ? DateTimeOffset.UtcNow.AddDays(-1 * long.Parse(args[0])).ToUnixTimeSeconds() : 0,
                    args.Length > 1 ? int.Parse(args[1]) : 1);
            else
                await Get(DateTimeOffset.UtcNow.AddDays(-1 * long.Parse(args[0])).ToUnixTimeSeconds(), int.Parse(args[1]), args[2],
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
            else
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
                var response = Console.ReadLine();
                uint number;
                while (uint.TryParse(response, out number))
                {
                    if (number <= optionalQuestions.Value.Count && number != 0)
                        Process.Start(optionalQuestions.Value[(int)number - 1].Link);
                    else
                        Console.WriteLine("No question has that number");
                    Console.WriteLine("Open another?");
                    response = Console.ReadLine();
                }
            }
        }

        public void SetDefaultSite(string[] args)
        {
            if (args.Length < 1)
                Console.WriteLine("Site name required.");
            else
            {
                _questions.Site = args[0];
                var options = _store.Exists("Options.json")
                    ? JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(_store.Read("Options.json"))) : new JObject();
                options["DefaultSite"] = args[0];
                _store.Write("Options.json", Encoding.UTF8.GetBytes(options.ToString()));
            }
        }

        public void SetDefaultTags(string[] args)
        {
            _questions.Tags = args;
            var options = _store.Exists("Options.json") ? JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(_store.Read("Options.json")))
                : new JObject();
            options["DefaultTags"] = JToken.FromObject(args);
            _store.Write("Options.json", Encoding.UTF8.GetBytes(options.ToString()));
        }
    }
}

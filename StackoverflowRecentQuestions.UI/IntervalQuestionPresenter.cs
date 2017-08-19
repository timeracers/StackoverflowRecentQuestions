using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace StackoverflowRecentQuestions.UI
{
    public class IntervalQuestionsPresenter
    {
        private bool _responded = false;
        private FormattedGetRecentQuestions _getter;
        private IStore _store;
        private int _intervalInMinutes;
        private bool _remind;
        private long _intervalStart;

        public IntervalQuestionsPresenter(FormattedGetRecentQuestions questionGetter, IStore store)
        {
            _store = store;
            _getter = questionGetter;
        }

        public void Initialize()
        {
            var options = _store.Exists("Options.json") ? JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(_store.Read("Options.json")))
                : new JObject();
            _intervalInMinutes = options["Interval"] != null ? options["Interval"].ToObject<int>() : 0;
            _remind = options["Remind"] != null && options["Remind"].ToObject<bool>();
            new Thread(new ThreadStart(IntervallyPresentQuestions)).Start();
        }
        
        public void SetInterval(string[] args)
        {
            if (args.Length > 0 && int.Parse(args[0]) < 15)
                Console.WriteLine("Minimum interval is 15 minutes");
            else
            {
                _intervalInMinutes = args.Length > 0 ? int.Parse(args[0]) : 0;
                var options = _store.Exists("Options.json")
                    ? JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(_store.Read("Options.json"))) : new JObject();
                options["Interval"] = _intervalInMinutes;
                _store.Write("Options.json", Encoding.UTF8.GetBytes(options.ToString()));
            }
        }

        public void EnableReminder()
        {
            _remind = true;
            var options = _store.Exists("Options.json") ? JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(_store.Read("Options.json")))
                : new JObject();
            options["Remind"] = true;
            _store.Write("Options.json", Encoding.UTF8.GetBytes(options.ToString()));
        }

        public void DisableReminder()
        {
            _remind = false;
            var options = _store.Exists("Options.json") ? JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(_store.Read("Options.json")))
                : new JObject();
            options["Remind"] = false;
            _store.Write("Options.json", Encoding.UTF8.GetBytes(options.ToString()));
        }

        private void IntervallyPresentQuestions()
        {
            _intervalStart = DateTimeOffset.Now.ToUnixTimeSeconds();
            while (true)
            {
                while (_intervalInMinutes == 0 || _intervalInMinutes * 60 > DateTimeOffset.Now.ToUnixTimeSeconds() - _intervalStart)
                    Thread.Sleep(20000);
                Console.WriteLine("Ready for questions? Type yes to continue");
                if (_remind)
                    SystemSounds.Exclamation.Play();
                Program.ConsoleReadLineStack.Push((s) => PresentQuestions(s).Wait());
                while (!_responded)
                    Thread.Sleep(25);
                _responded = false;
                _intervalStart = DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }
        private async Task PresentQuestions(string[] commandLineArgs)
        {
            if (commandLineArgs.Length > 0 && commandLineArgs[0].ToUpper() == "YES")
                await _getter.Get(_intervalStart);
            _responded = true;
        }
    }
}

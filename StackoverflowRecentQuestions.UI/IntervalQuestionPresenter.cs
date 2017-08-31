using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StackoverflowRecentQuestions.UI
{
    public class IntervalQuestionsPresenter
    {
        private GetRecentQuestionsConsoleAdapter _getter;
        private JsonStore _store;
        private int _intervalInMinutes;
        private bool _remind;
        private long _intervalStart;
        private Thread _thread;

        public IntervalQuestionsPresenter(GetRecentQuestionsConsoleAdapter questionGetter, JsonStore store)
        {
            _store = store;
            _getter = questionGetter;
        }

        public void Initialize()
        {
            var options = _store.Read<Options>("Options");
            _intervalInMinutes = options.Interval;
            _remind = options.Remind;
            _thread = new Thread(new ThreadStart(IntervallyPresentQuestions));
            _thread.Start();
        }

        public static IntervalQuestionsPresenter Create(GetRecentQuestionsConsoleAdapter questionGetter, JsonStore store)
        {
            var presenter = new IntervalQuestionsPresenter(questionGetter, store);
            presenter.Initialize();
            return presenter;
        }
        
        public void SetInterval(string[] args)
        {
            if (args.Length > 0 && int.Parse(args[0]) < 15)
                Console.WriteLine("Minimum interval is 15 minutes");
            else
            {
                _intervalInMinutes = args.Length > 0 ? int.Parse(args[0]) : 0;
                var options = _store.Read<Options>("Options");
                options.Interval = _intervalInMinutes;
                _store.Write("Options", options);
                Console.WriteLine("Interval changed");
            }
        }

        public void EnableReminder()
        {
            _remind = true;
            var options = _store.Read<Options>("Options");
            options.Remind = true;
            _store.Write("Options", options);
            Console.WriteLine("Reminder enabled");
        }

        public void DisableReminder()
        {
            _remind = false;
            var options = _store.Read<Options>("Options");
            options.Remind = false;
            _store.Write("Options", options);
            Console.WriteLine("Reminder disabled");
        }

        private void IntervallyPresentQuestions()
        {
            _intervalStart = UnixEpoch.Now;
            while (true)
            {
                while (_intervalInMinutes == 0 || _intervalInMinutes * 60 > UnixEpoch.Now - _intervalStart)
                    Thread.Sleep(15000);
                if (_remind)
                    SystemSounds.Exclamation.Play();
                Task.Run(() => _getter.Get(_intervalStart));
                _intervalStart = UnixEpoch.Now;
            }
        }
    }
}

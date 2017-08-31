using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace StackoverflowRecentQuestions
{
    public class JsonStore
    {
        IStore _store;

        public JsonStore(IStore store)
        {
            _store = store;
        }

        public T Read<T>(string path)
        {
            return _store.Exists(path + ".json")
                ? JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(_store.Read(path + ".json")))
                : new JObject().ToObject<T>();
        }

        public void Write(string path, object obj)
        {
            WriteJson(path, JsonConvert.SerializeObject(obj));
        }

        public void WriteJson(string path, string json)
        {
            _store.Write(path + ".json", Encoding.UTF8.GetBytes(json));
        }
    }
}

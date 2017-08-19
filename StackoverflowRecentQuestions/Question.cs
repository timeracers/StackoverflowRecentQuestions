using Newtonsoft.Json;

namespace StackoverflowRecentQuestions
{
    public class Question
    {
        public string[] Tags { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        [JsonProperty("creation_date")]
        public long CreationDate { get; set; }
        [JsonProperty("question_id")]
        public long Id { get; set; }

        private Question() { }

        public Question(string title, string link, long creationDate, long id, params string[] tags)
        {
            Tags = tags;
            Title = title;
            Link = link;
            CreationDate = creationDate;
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return obj is Question && Id == ((Question)obj).Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Title + "  " + string.Join(", ", Tags) + "  " + Link;
        }
    }
}
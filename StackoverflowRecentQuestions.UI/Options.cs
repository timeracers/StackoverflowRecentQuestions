namespace StackoverflowRecentQuestions.UI
{
    public class Options
    {
        public bool Remind { get; set; } = false;
        public int Interval { get; set; } = 0;
        public string DefaultSite { get; set; } = "stackoverflow";
        public string[] DefaultTags { get; set; } = new string[0];
        public int DefaultAmountOfQuestions { get; set; } = 30;

        public override string ToString()
        {
            return "Default Site: " + DefaultSite
                + "\nDefault Tags: " + (DefaultTags.Length > 0 ? string.Join(", ", DefaultTags) : "None")
                + "\nInterval: " + Interval
                + "\nRemind: " + Remind
                + "\nDefault Amount of Questions: " + DefaultAmountOfQuestions;
        }
    }
}

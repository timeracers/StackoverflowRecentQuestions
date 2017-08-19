How to use this console app:
  To get recent questions use
    GetRecentQuestions [<Within Last X Days>] [<Page>]
    GetRecentQuestions <Within Last X Days> <Page> <Site> [<Tag1>] [<Tag2>]...
  To periodly get recent questions use
    SetIntervalForQuestions <Minutes>
    SetIntervalForQuestions
  To enable reminders use
    EnableReminder
  To disable use
    DisableReminder
  To exit use
    Exit
  To see other commands or more information about each command use
    Help
    Help <Command> [<Command>] [<Command>]...
    
To use the class RecentQuestionsGetter
First Construct it
  RecentQuestionsGetter(IStore store, IWebRequester web, string[] tags, string site = "stackoverflow")
    Store should likely be IO. Web should be the regular WebRequester class unless you want to fake the internet.
    Tags are the tags you want questions to have*. Site is the site you want to search.
    *If you mention multiple then it look for questions that have any of those tags, if you mention 0 it will look for any question.
Example: new RecentQuestionsGetter(new IO(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\StackoverflowRecentQuestions"), new WebRequester(), "c#", "java", "stackoverflow")
Then when you want to get questions call
  public async Task<Optional<List<Question>>> GetSince(long unixEpoch, int page = 1)
    UnixEpoch is the time you want question since in the format of a unix timestamp.
    Since a query can only return up to 30 questions normally, you will need to page if you want more then 30 questions.
RecentQuestionsGetter won't allow you to get questions if you are out of queries for today or the server told you to backoff.
You can check that by using ThrottleChecker.

namespace StackoverflowRecentQuestions
{
    public interface IStore
    {
        byte[] Read(string path);
        void Write(string path, byte[] bytes);
        bool Exists(string path);
    }
}
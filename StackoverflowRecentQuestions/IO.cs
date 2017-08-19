using System.IO;

namespace StackoverflowRecentQuestions
{
    public class IO : IStore
    {
        private string _basePath;

        public IO(string basePath)
        {
            _basePath = basePath.EndsWith("\\") ? basePath: basePath + "\\";
            Directory.CreateDirectory(basePath);
        }

        public bool Exists(string path)
        {
            return File.Exists(_basePath + path);
        }

        public byte[] Read(string path)
        {
            return File.ReadAllBytes(_basePath + path);
        }
        
        public void Write(string path, byte[] bytes)
        {
            File.WriteAllBytes(_basePath + path, bytes);
        }
    }
}

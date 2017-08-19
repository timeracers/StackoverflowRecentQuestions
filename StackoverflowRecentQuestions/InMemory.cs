using System.Collections.Generic;

namespace StackoverflowRecentQuestions
{
    public class InMemory : IStore
    {
        private Dictionary<string, byte[]> addedValues;
        
        public InMemory()
        {
            addedValues = new Dictionary<string, byte[]>();
        }

        public InMemory(Dictionary<string, byte[]> startingValues)
        {
            addedValues = startingValues;
        }

        public byte[] Read(string path)
        {
            return addedValues[path];
        }

        public void Write(string path, byte[] contents)
        {
            if (addedValues.ContainsKey(path))
                addedValues[path] = contents;
            else
                addedValues.Add(path, contents);
        }

        public bool Exists(string path)
        {
            return addedValues.ContainsKey(path);
        }
    }
}

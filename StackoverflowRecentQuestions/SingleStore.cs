using System;

namespace StackoverflowRecentQuestions.UI
{
    public class SingleStore<T>
    {
        private T _value;

        public bool HasValue { get; private set; }

        public T Value
        {
            set
            {
                HasValue = value != null;
                _value = value;
            }
            get
            {
                if (!HasValue)
                    throw new InvalidOperationException("This contains no value.");
                return _value;
            }
        }

        public SingleStore()
        {
            HasValue = false;
        }

        public SingleStore(T item)
        {
            Value = item;
        }

        public void Clear()
        {
            HasValue = false;
        }
    }
}

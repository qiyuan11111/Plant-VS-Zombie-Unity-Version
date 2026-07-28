namespace Script
{
    public class Nullable<T>
    {
        private T _value;
        public T value
        {
            set
            {
                isNull = value == null;
                _value = value;
            }
            get => _value;
        }

        public bool isNull;
    }
}
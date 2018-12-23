using System;

namespace BepisPlugins
{
    public class Lazy<T>
    {
        private T _object;
        private Func<T> Factory { get; set; }

        private Lazy(Func<T> factory)
        {
            Factory = factory;
        }

        public T Instance
        {
            get
            {
                Initialize();
                return _object;
            }
        }

        public void Initialize()
        {
            if (_object == null || _object.ToString() == "null")
                _object = Factory();
        }

        public static implicit operator T(Lazy<T> lazy)
        {
            return lazy.Instance;
        }

        public static Lazy<TClass> Create<TClass>()
            where TClass : new()
        {
            return new Lazy<TClass>(() => new TClass());
        }

        public static Lazy<TClass> Create<TClass>(Func<TClass> factory)
        {
            return new Lazy<TClass>(factory);
        }
    }
}

using System;

namespace Shared
{
    public class Lazy<T>
    {
        private T _object;
        private bool _initialized;
        private Func<T> _factory;

        private Lazy(Func<T> factory)
        {
            _factory = factory;
        }

        public T Instance
        {
            get {
                Initialize();
                return _object;
            }
        }

        public bool Initialized => _initialized;

        public void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            _object = _factory();
        }

        public static implicit operator T (Lazy<T> lazy)
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

using System;

namespace Shared
{
    internal class LazyCustom<T>
    {
        private T _object;
        private Func<T> Factory { get; set; }

        private LazyCustom(Func<T> factory) => Factory = factory;

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

        public static implicit operator T(LazyCustom<T> lazy) => lazy.Instance;

        public static LazyCustom<TClass> Create<TClass>()
            where TClass : new() => new LazyCustom<TClass>(() => new TClass());

        public static LazyCustom<TClass> Create<TClass>(Func<TClass> factory) => new LazyCustom<TClass>(factory);
    }
}

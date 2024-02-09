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
#if !RG
            if (_object == null || _object.ToString() == "null")
#else
            if (_object == null || _object.ToString() == "null" ||
                _object is UnityEngine.Object obj1 && !UnityEngine.Object.IsNativeObjectAlive(obj1) ||
                _object is Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase obj2 && obj2.WasCollected)
#endif
                _object = Factory();
        }

        public static implicit operator T(LazyCustom<T> lazy) => lazy.Instance;

        public static LazyCustom<TClass> Create<TClass>()
            where TClass : new() => new LazyCustom<TClass>(() => new TClass());

        public static LazyCustom<TClass> Create<TClass>(Func<TClass> factory) => new LazyCustom<TClass>(factory);
    }
}

using System;

namespace BepisPlugins
{
    internal class HashedWeakReference
    {
        private readonly int _targetHashCode;
        private readonly WeakReference _reference;

        public HashedWeakReference(object target)
        {
            _targetHashCode = target.GetHashCode();
            _reference = new WeakReference(target);
        }

        public object Target => _reference.Target;

        public bool IsAlive => _reference.IsAlive;

        public override int GetHashCode() => _targetHashCode;

        public override bool Equals(object obj) => _targetHashCode == obj?.GetHashCode();
    }
}

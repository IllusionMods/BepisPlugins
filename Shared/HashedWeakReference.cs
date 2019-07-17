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

        public object Target
        {
            get { return _reference.Target; }
        }

        public bool IsAlive
        {
            get
            {
                return _reference.IsAlive;
            }
        }

        public override int GetHashCode()
        {
            return _targetHashCode;
        }

        public override bool Equals(object obj)
        {
            return _targetHashCode == obj?.GetHashCode();
        }
    }
}

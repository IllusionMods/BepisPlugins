using System;
using System.Collections.Generic;
using System.Text;

namespace BepisPlugins
{
    internal class HashedWeakReference
    {
        private int TargetHashCode;
        private WeakReference Reference;

        private void setTarget(object target)
        {
            TargetHashCode = target.GetHashCode();
            Reference = new WeakReference(target);
        }

        public HashedWeakReference(object target)
        {
            setTarget(target);
        }

        public object Target
        {
            get { return Reference.Target; }
            set
            {
                setTarget(value);
            }
        }

        public bool IsAlive
        {
            get
            {
                return Reference.IsAlive;
            }
        }

        public override int GetHashCode()
        {
            return TargetHashCode;
        }

        public override bool Equals(object obj)
        {
            return TargetHashCode == obj.GetHashCode();// && Reference.IsAlive && Target == obj;
        }
    }
}

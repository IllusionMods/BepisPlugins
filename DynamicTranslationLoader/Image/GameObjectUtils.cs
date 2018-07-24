using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DynamicTranslationLoader.Image
{
    internal class GameObjectUtils
    {
        internal static string AbsoluteTransform(GameObject go)
        {
            var st = new Stack<Transform>();
            var t = go.transform;
            st.Push(t);
            while (t.parent)
            {
                t = t.parent;
                st.Push(t);
            }
            return string.Join("/", st.Select(x => x.name).ToArray());
        }
    }
}

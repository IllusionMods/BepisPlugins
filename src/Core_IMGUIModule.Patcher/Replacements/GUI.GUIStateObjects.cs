using System;
using System.Collections.Generic;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal partial class GUI
    {
        private class GUIStateObjects
        {
            private static Dictionary<int, object> s_StateCache = new Dictionary<int, object>();

            public static object GetStateObject(Type t, int controlID)
            {
                object obj;
                if (!s_StateCache.TryGetValue(controlID, out obj) || obj.GetType() != t)
                {
                    obj = Activator.CreateInstance(t);
                    s_StateCache[controlID] = obj;
                }
                return obj;
            }
        }
    }
}

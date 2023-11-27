using static UnityEngine.GUIStateObjects;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal class GUIStateObjects
    {
        public static Il2CppSystem.Object QueryStateObject(Il2CppSystem.Type t, int controlID)
        {
            Il2CppSystem.Object o = s_StateCache[controlID];
            return t.IsInstanceOfType(o) ? o : null;
        }
    }
}

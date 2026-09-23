using BepInEx.Preloader.Core.Patching;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    public partial class Patcher : BasePatcher
    {
        [TargetType("UnityEngine.IMGUIModule.dll", "UnityEngine.GUI")]
        public void AvoidBrokenPinnableRefGet(TypeDefinition type)
        {
            foreach (var method in type.Methods)
            {
                // Completely broken in AmaLoca as of be.788 because of messed up generation
                // of Il2CppSystem.ReadOnlySpan`1<System.Char>::GetPinnableReference() references
                if (method.Name is "SetNextControlName" or "FocusControl")
                {
                    method.Body.Instructions.Clear();
                    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    method.Body.ExceptionHandlers.Clear();
                }
            }
        }
    }
}

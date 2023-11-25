using HarmonyLib;
using QudUX.Concepts;

namespace QudUX.HarmonyPatches
{
    // Using CanAutoget instead of ShouldAutoget so more item types can be
    // taken into account (like artifacts)
    [HarmonyPatch(typeof(XRL.World.GameObject))]
    class Patch_XRL_World_GameObject_ShouldAutoget
    {
        [HarmonyPostfix]
        [HarmonyPatch("CanAutoget")]
        static void Postfix(XRL.World.GameObject __instance, ref bool __result)
        {
            if (__result == true && Options.UI.EnableAutogetExclusions)
            {
                __result = !XRL.World.Parts.QudUX_AutogetHelper.IsAutogetDisabledByQudUX(__instance);
            }
        }
    }
}

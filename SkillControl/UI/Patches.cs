using HarmonyLib;

namespace SkillControl.UI;

public static class Patches
{
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
    private static class PlayerController_UI_Patch
    {
        private static bool Prefix() => !LoadUI.IsUIActive();
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    private static class IsUI_Visible_Patch1 
    {
        private static void Postfix(ref bool __result)
        {
            __result |= LoadUI.IsUIActive();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    private static class IsUI_Visible_Patch2
    {
        private static bool Prefix() => !LoadUI.IsUIActive();
    }
}
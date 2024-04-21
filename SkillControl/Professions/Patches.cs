using HarmonyLib;
using SkillControl.UI;

namespace SkillControl.Professions;

public static class Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    private static class RaiseSkill_Patch
    {
        private static void Prefix(Skills.SkillType skill, ref float value)
        {
            if (skill is Skills.SkillType.None) return;
            if (!JobManager.GetCollectedModifiers().TryGetValue(skill, out float modifier)) return;
            value *= modifier;
        }
    }
    

    [HarmonyPatch(typeof(Player), nameof(Player.Save))]
    private static class SaveJobData_Patch
    {
        private static void Postfix() => JobManager.SaveJobsToPlayer();
    }

    private static bool initiated;

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class LoadJobData_Patch
    {
        private static void Postfix()
        {
            if (initiated) return;
            JobManager.RetrieveJobsFromPlayer();
            initiated = true;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    private static class Game_Logout_Patch
    {
        private static void Postfix() => initiated = false;
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
    private static class Initialize_ServerSynced_JobData
    {
        private static void Postfix(ZNet __instance)
        {
            if (!__instance) return;
            if (!__instance.IsServer())
            {
                JobManager.ServerJobData.ValueChanged += JobManager.OnServerDataChange;
            }
            else
            {
                JobManager.UpdateServerData();
            }
        }
    }
}
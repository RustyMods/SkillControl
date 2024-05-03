using HarmonyLib;
using UnityEngine;

namespace SkillControl.Professions;

public static class Patches
{
    private static float originalIncreaseStep;
    
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    private static class RaiseSkill_Patch
    {
        private static void Prefix(Skills.SkillType skillType, ref float factor)
        {
            if (skillType is Skills.SkillType.None) return;
            if (!JobManager.GetCollectedModifiers().TryGetValue(skillType, out float modifier)) return;
            Skills.Skill skill = Player.m_localPlayer.m_skills.GetSkill(skillType);
            if (SkillControlPlugin._OverrideDefaults.Value is SkillControlPlugin.Toggle.On)
            {
                originalIncreaseStep = skill.m_info.m_increseStep;
                skill.m_info.m_increseStep = modifier;
            }
            else
            {
                factor *= modifier;
            }
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    private static class RaiseSkill_Patch2
    {
        private static void Postfix(Skills.SkillType skillType)
        {
            if (skillType is Skills.SkillType.None) return;
            var skill = Player.m_localPlayer.m_skills.GetSkill(skillType);
            if (SkillControlPlugin._OverrideDefaults.Value is SkillControlPlugin.Toggle.Off) return;
            skill.m_info.m_increseStep = originalIncreaseStep;
        }
    }

    private static float CalculateTotalExperience(float currentLevel)
    {
        float totalExperience = 0f;
        for (float level = 1; level <= currentLevel; level++)
        {
            float levelRequirement = Mathf.Pow(Mathf.Floor(level), 1.5f) * 0.5f + 0.5f;
            totalExperience += levelRequirement;
        }
        return totalExperience;
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
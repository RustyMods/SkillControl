using HarmonyLib;
using UnityEngine;

namespace SkillControl.Professions;

public static class Patches
{
    private static float originalIncreaseStep;
    private static bool m_modified;
    
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
                m_modified = true;
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
            if (!JobManager.GetCollectedModifiers().TryGetValue(skillType, out float modifier)) return;
            if (SkillControlPlugin._OverrideDefaults.Value is SkillControlPlugin.Toggle.Off) return;
            if (!m_modified) return;
            if (originalIncreaseStep <= 0f) return;
            skill.m_info.m_increseStep = originalIncreaseStep;
            m_modified = false;
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
    
    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.LoadPlayerData))]
    private static class LoadJobData_Patch
    {
        private static void Postfix()
        {
            JobManager.RetrieveJobsFromPlayer();
        }
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using ServerSync;
using SkillControl.UI;
using UnityEngine;
using YamlDotNet.Serialization;

namespace SkillControl.Professions;

public static class JobManager
{
    public static readonly CustomSyncedValue<string> ServerJobData = new(SkillControlPlugin.ConfigSync, "ServerSkillControlData", "");
    private static readonly string m_dataKey = "SkillControlData";
    public static Dictionary<string, JobData> RegisteredJobs = new();
    public static List<JobData> m_jobs = new();

    public static void SaveJobsToPlayer()
    {
        if (!Player.m_localPlayer) return;
        List<string> jobs = m_jobs.Select(job => job.Name).ToList();
        var serializer = new SerializerBuilder().Build();
        var data = serializer.Serialize(jobs);
        Player.m_localPlayer.m_customData[m_dataKey] = data;
        SkillControlPlugin.SkillControlLogger.LogDebug("Saved data to player file");
    }

    public static void RetrieveJobsFromPlayer()
    {
        if (!Player.m_localPlayer) return;
        if (!Player.m_localPlayer.m_customData.TryGetValue(m_dataKey, out string serial)) return;
        m_jobs.Clear();
        var deserializer = new DeserializerBuilder().Build();
        var data = deserializer.Deserialize<List<string>>(serial);
        foreach (string job in data)
        {
            if (!RegisteredJobs.TryGetValue(Regex.Replace(job, "<.*?>", string.Empty), out JobData jobData)) continue;
            m_jobs.Add(jobData);
        }
        SkillControlPlugin.SkillControlLogger.LogDebug("Retrieved data from player file");
    }

    public static Dictionary<Skills.SkillType, float> GetCollectedModifiers()
    {
        Dictionary<Skills.SkillType, float> output = new();
        foreach (var modifier in m_jobs.SelectMany(job => job.SkillModifiers))
        {
            if (!GetSkillType(modifier.SkillName, out Skills.SkillType skill)) continue;
            if (output.TryGetValue(skill, out float mod))
            {
                output[skill] = Mathf.Clamp(mod + modifier.Modifier, 0f, 5f);
            }
            else
            {
                output[skill] = modifier.Modifier;
            }
        }

        return output;
    }
    
    public static void InitJobs()
    {
        string[] files = Directory.GetFiles(PluginPaths.folderPath, "*.yml");
        if (files.Length == 0)
        {
            CreateExampleFile();
        }
        else
        {
            var deserializer = new DeserializerBuilder().Build();
            foreach (var file in files)
            {
                var serial = File.ReadAllText(file);
                var data = deserializer.Deserialize<JobData>(serial);
                RegisteredJobs[Regex.Replace(data.Name, "<.*?>", string.Empty)] = data;
            }
        }
        SkillControlPlugin.SkillControlLogger.LogDebug("Initialized skill controller");
    }

    public static void UpdateServerData()
    {
        var serializer = new SerializerBuilder().Build();
        var serial = serializer.Serialize(RegisteredJobs);
        ServerJobData.Value = serial;
        SkillControlPlugin.SkillControlLogger.LogDebug("Updated server data");
    }

    public static void OnServerDataChange()
    {
        if (ServerJobData.Value.IsNullOrWhiteSpace()) return;
        var deserializer = new DeserializerBuilder().Build();
        var data = deserializer.Deserialize<Dictionary<string, JobData>>(ServerJobData.Value);
        RegisteredJobs = data;
        SkillControlPlugin.SkillControlLogger.LogDebug("Client: received server data");
        UpdateCurrentJobs();
        if (LoadUI.IsUIActive())
        {
            LoadUI.ReloadUI();
        }
    }

    private static void UpdateCurrentJobs()
    {
        m_jobs = m_jobs.Select(job => RegisteredJobs.Values.FirstOrDefault(x => Regex.Replace(x.Name, "<.*?>",string.Empty) == Regex.Replace(job.Name, "<.*?>",string.Empty))).ToList();;
    }

    private static void CreateExampleFile()
    {
        var data = new List<JobData>()
        {
            new JobData()
            {
                Name = "Peasant",
                Description = "For the one that cares for the well-being of the populace",
                SkillModifiers = new()
                {
                    new SkillData() { SkillName = "Farming", Modifier = 1.2f },
                    new SkillData() { SkillName = "Foraging", Modifier = 1.2f },
                    new SkillData() { SkillName = "Ranching", Modifier = 1.2f },
                    new SkillData() { SkillName = "Blacksmithing", Modifier = 0f },
                    new SkillData() { SkillName = "Sailing", Modifier = 0.5f },
                    new SkillData() { SkillName = "Knives", Modifier = 1.3f}
                }
            },
            new JobData()
            {
                Name = "Blacksmith",
                Description = "For whom has a great deal of attention to the details of survival",
                SkillModifiers = new ()
                {
                    new SkillData(){SkillName = "Blacksmithing", Modifier = 1.2f},
                    new SkillData(){SkillName = "Farming", Modifier = 0.5f},
                    new SkillData(){SkillName = "Foraging", Modifier = 0.5f},
                    new SkillData(){SkillName = "Sailing", Modifier = 0.5f},
                    new SkillData(){SkillName = "Swords",Modifier = 1.1f},
                    new SkillData(){SkillName = "Axes",Modifier = 1.1f},
                    new SkillData(){SkillName = "Clubs",Modifier = 1.1f}
                }
            },
            new JobData()
            {
                Name = "Sailor",
                Description = "The heavy toll of sea-fairing is one that comes with great honor",
                SkillModifiers = new()
                {
                    new SkillData(){SkillName = "Blacksmithing", Modifier = 0f},
                    new SkillData(){SkillName = "Farming",Modifier = 0f},
                    new SkillData(){SkillName = "Foraging", Modifier = 1.1f},
                    new SkillData(){SkillName = "Sailing",Modifier = 1.5f},
                    new SkillData(){SkillName = "Spears",Modifier = 1.3f}
                }
            }
        };
        var serializer = new SerializerBuilder().Build();
        foreach (var job in data)
        {
            string filePath = PluginPaths.folderPath + Path.DirectorySeparatorChar + job.Name + ".yml";
            var serial = serializer.Serialize(job);
            File.WriteAllText(filePath, serial);
            RegisteredJobs[Regex.Replace(job.Name, "<.*?>", string.Empty)] = job;
        }
        
    }

    public static bool GetSkillType(string input, out Skills.SkillType output)
    {
        if (Enum.TryParse(input, out Skills.SkillType type))
        {
            output = type;
            return true;
        }

        int hash = input.GetStableHashCode();
        output = (Skills.SkillType)Math.Abs(hash);
        
        return !Player.m_localPlayer || Player.m_localPlayer.m_skills.GetSkillDef(output) != null;
    }
}
using System.IO;
using BepInEx;
using SkillControl.Professions;
using YamlDotNet.Serialization;

namespace SkillControl.Managers;

public static class FileWaterManager
{
    public static void InitFileWatcher()
    {
        FileSystemWatcher JobWatcher = new FileSystemWatcher(PluginPaths.folderPath)
        {
            Filter = "*.yml",
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            SynchronizingObject = ThreadingHelper.SynchronizingObject,
            NotifyFilter = NotifyFilters.LastWrite
        };
        JobWatcher.Created += OnFileChange;
        JobWatcher.Changed += OnFileChange;
        JobWatcher.Deleted += OnFileDelete;

        FileSystemWatcher ImageWatcher = new FileSystemWatcher(PluginPaths.iconPath)
        {
            Filter = "*.png",
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            SynchronizingObject = ThreadingHelper.SynchronizingObject,
            NotifyFilter = NotifyFilters.LastWrite
        };
        ImageWatcher.Created += OnImageChange;
        ImageWatcher.Changed += OnImageChange;
        ImageWatcher.Deleted += OnImageDelete;

    }

    private static void OnImageChange(object sender, FileSystemEventArgs e)
    {
        SpriteManager.RegisterSprite(e.FullPath);
    }

    private static void OnImageDelete(object sender, FileSystemEventArgs e)
    {
        string fileName = Path.GetFileName(e.Name);
        fileName = fileName.Replace(".png", string.Empty);
        SpriteManager.RegisteredSprites.Remove(fileName);
    }

    private static void OnFileChange(object sender, FileSystemEventArgs e)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        var deserializer = new DeserializerBuilder().Build();
        var serial = File.ReadAllText(e.FullPath);
        var data = deserializer.Deserialize<JobData>(serial);
        JobManager.RegisteredJobs[data.Name] = data;
        JobManager.UpdateServerData();
    }

    private static void OnFileDelete(object sender, FileSystemEventArgs e)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        JobManager.RegisteredJobs.Clear();
        var files = Directory.GetFiles(PluginPaths.folderPath, "*.yml");
        var deserializer = new DeserializerBuilder().Build();
        foreach (var file in files)
        {
            var serial = File.ReadAllText(file);
            var data = deserializer.Deserialize<JobData>(serial);
            JobManager.RegisteredJobs[data.Name] = data;
        }
        JobManager.UpdateServerData();
    }

}
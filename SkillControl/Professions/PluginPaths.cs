using System.IO;
using BepInEx;

namespace SkillControl.Professions;

public static class PluginPaths
{
    public static readonly string folderPath = Paths.ConfigPath + Path.DirectorySeparatorChar + "SkillControl";
    public static readonly string iconPath = folderPath + Path.DirectorySeparatorChar + "Icons";

    public static void CreateDirectories()
    {
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        if (!Directory.Exists(iconPath)) Directory.CreateDirectory(iconPath);
    }
}
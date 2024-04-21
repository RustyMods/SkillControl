using System.Collections.Generic;
using System.IO;
using SkillControl.Professions;
using UnityEngine;

namespace SkillControl.Managers;

public static class SpriteManager
{
    public static readonly Dictionary<string, Sprite> RegisteredSprites = new();

    public static Sprite? TryGetIcon(string name) => RegisteredSprites.TryGetValue(name, out Sprite sprite) ? sprite : null;
    
    public static void GetIcons()
    {
        string[] filePaths = Directory.GetFiles(PluginPaths.iconPath, "*.png");
        foreach(var path in filePaths) RegisterSprite(path);
    }
    
    public static void RegisterSprite(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);

        Sprite? sprite = texture.LoadImage(fileData)
            ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero)
            : null;
        if (sprite == null) return;
        string name = filePath.Replace(".png", string.Empty).Replace(PluginPaths.iconPath, string.Empty).Replace("\\",string.Empty);
        sprite.name = name;
        RegisteredSprites[name] = sprite;
        SkillControlPlugin.SkillControlLogger.LogDebug("Successfully registered sprite: " + name);
    }
}
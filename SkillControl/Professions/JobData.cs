using System;
using System.Collections.Generic;

namespace SkillControl.Professions;

[Serializable]
public class JobData
{
    public string Name = "";
    public string Description = "";
    public string Image = "";
    public List<SkillData> SkillModifiers = new();
}

[Serializable]
public class SkillData
{
    public string SkillName = "";
    public string DisplayName = "";
    public float Modifier;
    public string type = "";
}
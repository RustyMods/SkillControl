using System;
using System.Collections.Generic;

namespace SkillControl.Professions;

[Serializable]
public class JobData
{
    public string Name = "";
    public string Description = "";
    public List<SkillData> SkillModifiers = new();
}

[Serializable]
public class SkillData
{
    public string SkillName = "";
    public float Modifier;
}
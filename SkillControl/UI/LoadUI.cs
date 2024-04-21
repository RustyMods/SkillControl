using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SkillControl.Managers;
using SkillControl.Professions;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SkillControl.UI;

public static class LoadUI
{
    private static Text m_title = null!;
    private static Image m_icon = null!;
    private static Text m_description = null!;
    private static Text m_availableSkills = null!;
    private static Text m_lockedSkills = null!;
    private static Text m_selectedTitle = null!;
    private static Text m_selectButtonText = null!;
    
    private static Button m_selectButton = null!;
    private static Button m_closeButton = null!;

    private static Transform m_partAvailable = null!;
    private static Transform m_partLocked = null!;
    private static Transform m_partTabs = null!;
    private static Transform m_selected = null!;
    private static GameObject m_selectedContainer = null!;

    private static GameObject m_UI = null!;
    private static GameObject m_skillElement = null!;
    private static GameObject m_tabElement = null!;

    private static RectTransform m_position = null!;

    private static Sprite m_defaultBackground = null!;
    private static ItemDrop m_defaultCurrency = null!;

    private static JobData m_selectedJob = new()
    {
        Name = "$msg_select_job",
        Description = ""
    };

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    private static class Load_Jobs_UI
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (!__instance) return;
            InitUI(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static class Override_ForceEmployment
    {
        private static void Postfix()
        {
            OverrideUI();
        }
    }

    private static void GetGameAssets(InventoryGui instance, out ButtonSfx sfx, out Button button)
    {
        Transform vanilla = instance.m_trophiesPanel.transform.Find("TrophiesFrame/Closebutton");
        sfx = vanilla.GetComponent<ButtonSfx>();
        button = vanilla.GetComponent<Button>();
    }

    private static void AddGameAssets(List<Button> buttons, ButtonSfx sfx, Button button)
    {
        foreach (var item in buttons)
        {
            item.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = sfx.m_sfxPrefab;
            item.transition = Selectable.Transition.SpriteSwap;
            item.spriteState = button.spriteState;
        }
    }

    private static void OverrideUI()
    {
        if (SkillControlPlugin._ForceJob.Value is not SkillControlPlugin.Toggle.On) return;
        if (JobManager.m_jobs.Count == 0) ShowUI();
    }

    public static void OnPositionChange(object sender, EventArgs e)
    {
        if (sender is ConfigEntry<Vector2> config)
        {
            m_position.anchoredPosition = config.Value;
        }
    }

    private static void InitUI(InventoryGui instance)
    {
        GetGameAssets(instance, out ButtonSfx sfx, out Button button);

        m_UI = Object.Instantiate(SkillControlPlugin._AssetBundle.LoadAsset<GameObject>("UI"), instance.transform);
        m_position = m_UI.GetComponent<RectTransform>();
        m_position.anchoredPosition = SkillControlPlugin._UIPosition.Value;
        m_UI.SetActive(false);
        m_skillElement = SkillControlPlugin._AssetBundle.LoadAsset<GameObject>("Skill_Element");
        m_tabElement = SkillControlPlugin._AssetBundle.LoadAsset<GameObject>("Tab_Element");
        
        Object.DontDestroyOnLoad(m_UI);
        Object.DontDestroyOnLoad(m_skillElement);
        Object.DontDestroyOnLoad(m_tabElement);

        Text[] UI_Text = m_UI.GetComponentsInChildren<Text>();
        Text[] Element_Text = m_skillElement.GetComponentsInChildren<Text>();
        Text[] Tab_Text = m_tabElement.GetComponentsInChildren<Text>();
        Font? NorseBold = GetFont("Norsebold");
        AddFonts(UI_Text, NorseBold);
        AddFonts(Element_Text, NorseBold);
        AddFonts(Tab_Text, NorseBold);

        var UI_Buttons = m_UI.GetComponentsInChildren<Button>().ToList();
        UI_Buttons.Add(m_tabElement.GetComponent<Button>());
        AddGameAssets(UI_Buttons, sfx, button);

        m_title = Utils.FindChild(m_UI.transform, "$text_title").GetComponent<Text>();
        m_icon = Utils.FindChild(m_UI.transform, "$image_icon").GetComponent<Image>();
        m_description = Utils.FindChild(m_UI.transform, "$text_description").GetComponent<Text>();
        m_availableSkills = Utils.FindChild(m_UI.transform, "$text_available").GetComponent<Text>();
        m_lockedSkills = Utils.FindChild(m_UI.transform, "$text_locked").GetComponent<Text>();
        m_partAvailable = Utils.FindChild(m_UI.transform, "$part_available_content");
        m_partLocked = Utils.FindChild(m_UI.transform, "$part_locked_content");
        m_partTabs = Utils.FindChild(m_UI.transform, "$part_tabs_content");
        var acceptButton = Utils.FindChild(m_UI.transform, "$button_accept");
        m_selectButtonText = acceptButton.Find("$text_button").GetComponent<Text>();
        m_selectButton = acceptButton.GetComponent<Button>();
        m_closeButton = Utils.FindChild(m_UI.transform, "$button_close").GetComponent<Button>();
        m_selectedTitle = Utils.FindChild(m_UI.transform, "$text_selected_title").GetComponent<Text>();
        m_selected = Utils.FindChild(m_UI.transform, "$part_selected_content");
        m_selectedContainer = m_UI.transform.Find("Select").gameObject;
        
        SetStaticEvents();
        SkillControlPlugin.SkillControlLogger.LogDebug("Initialized skill control UI");
    }

    private static void SetStaticEvents()
    {
        m_closeButton.onClick.AddListener(HideUI);
        m_selectButton.onClick.AddListener(SelectJob);
        m_availableSkills.text = Localization.instance.Localize("$text_benefit");
        m_lockedSkills.text = Localization.instance.Localize("$text_detriment");
        m_selectedTitle.text = Localization.instance.Localize("$text_my_jobs");
        
        if (ZNetScene.instance)
        {
            GameObject coins = ZNetScene.instance.GetPrefab("Coins");
            if (!coins.TryGetComponent(out ItemDrop itemDrop)) return;
            m_defaultCurrency = itemDrop;
        }
        Sprite? background = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == "biome_blackforest");
        if (background == null) return;
        m_defaultBackground = background;
        m_icon.sprite = m_defaultBackground;
    }

    private static void SelectJob()
    {
        if (JobManager.m_jobs.Contains(m_selectedJob))
        {
            if (!CheckCost()) return;
            if (SkillControlPlugin._RemoveSkillExperience.Value is SkillControlPlugin.Toggle.On)
            {
                foreach (var kvp in m_selectedJob.SkillModifiers)
                {
                    var skill = kvp.SkillName;
                    if (kvp.Modifier > 0f) continue;
                    if (!JobManager.GetSkillType(skill, out Skills.SkillType type)) continue;
                    Player.m_localPlayer.m_skills.ResetSkill(type);
                    SkillControlPlugin.SkillControlLogger.LogDebug($"Reset {skill} to zero");
                }
            }
            JobManager.m_jobs.Remove(m_selectedJob);
        }
        else
        {
            if (m_selectedJob.Name == "$msg_select_job")
            {
                ShowUI();
                return;
            }
            if (JobManager.m_jobs.Count >= SkillControlPlugin._JobLimit.Value)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_job_limit");
                return;
            }
            
            JobManager.m_jobs.Add(m_selectedJob);
        }
        ShowUI();
    }

    private static void AddFonts(Text[] array, Font? font)
    {
        foreach (Text text in array) text.font = font;
    }

    private static Font? GetFont(string name)
    {
        Font[]? fonts = Resources.FindObjectsOfTypeAll<Font>();
        return fonts.FirstOrDefault(x => x.name == name);
    }

    public static bool IsUIActive() => m_UI && m_UI.activeInHierarchy;

    private static void ToggleUI(bool value)
    {
        if (m_UI) m_UI.SetActive(value);
    }

    public static void UpdateUI()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) HideUI();
        if (!Input.GetKeyDown(SkillControlPlugin._UIKey.Value)) return;
        if (IsUIActive()) HideUI();
        else ShowUI();
        
    }

    private static void DestroyUIElements()
    {
        foreach(Transform element in m_partTabs) Object.Destroy(element.gameObject);
        foreach(Transform element in m_partAvailable) Object.Destroy(element.gameObject);
        foreach(Transform element in m_partLocked) Object.Destroy(element.gameObject);
        foreach(Transform element in m_selected) Object.Destroy(element.gameObject);
    }

    private static void CreateTabs()
    {
        foreach (var kvp in JobManager.RegisteredJobs)
        {
            var job = kvp.Value;
            GameObject tab = Object.Instantiate(m_tabElement, m_partTabs);
            tab.GetComponentInChildren<Text>().text = Localization.instance.Localize(job.Name);
            if (tab.TryGetComponent(out Button button))
            {
                button.onClick.AddListener(() =>
                {
                    m_selectedJob = job;
                    ShowUI();
                });
            }
        }
    }

    private static void SetStaticElements()
    {
        if (!m_title || !m_icon || !m_description) return;
        m_title.text = Localization.instance.Localize(m_selectedJob.Name);
        Sprite? background = SpriteManager.TryGetIcon(m_selectedJob.Name);
        m_icon.sprite = background ? background : m_defaultBackground;
        string tooltip = SkillControlPlugin._RemoveSkillExperience.Value is SkillControlPlugin.Toggle.On ? "<color=red>$text_skill_loss</color>" : "";
        m_description.text = Localization.instance.Localize(m_selectedJob.Description + $"\n{tooltip}");
    }

    private static void CreateContentElements()
    {
        foreach (SkillData? details in m_selectedJob.SkillModifiers)
        {
            if (!JobManager.GetSkillType(details.SkillName, out Skills.SkillType skill)) continue;
            GameObject element = Object.Instantiate(m_skillElement, details.Modifier < 1f ? m_partLocked : m_partAvailable);
            Skills.SkillDef definition = Player.m_localPlayer.m_skills.GetSkillDef(skill);
            if (definition != null)
            {
                if (Utils.FindChild(element.transform, "$image_icon").TryGetComponent(out Image image))
                {
                    image.sprite = definition.m_icon;
                }
            }

            if (Utils.FindChild(element.transform, "$text_skill_name").TryGetComponent(out Text skillText))
            {
                skillText.text = Localization.instance.Localize(details.SkillName);
            }

            if (Utils.FindChild(element.transform, "$text_skill_description").TryGetComponent(out Text descriptionText))
            {
                descriptionText.text = GetTooltip(details);
            }
        }
    }

    private static void ShowUI()
    {
        if (!m_selectButtonText) return;
        if (m_selectedJob.Name.IsNullOrWhiteSpace())
        {
            if (JobManager.RegisteredJobs.Count > 0)
            {
                m_selectedJob = JobManager.RegisteredJobs.Values.First();
            }
        }

        m_selectButtonText.text = Localization.instance.Localize(JobManager.m_jobs.Contains(m_selectedJob) ? "$text_remove" : "$text_select");

        DestroyUIElements();
        UpdateSelectedJobs();
        CreateTabs();
        SetStaticElements();
        CreateContentElements();
        ToggleUI(true);
    }

    public static void ReloadUI()
    {
        m_selectedJob = JobManager.RegisteredJobs.Values.FirstOrDefault(x => x.Name == m_selectedJob.Name) ?? m_selectedJob;
        ShowUI();
    }

    private static void UpdateSelectedJobs()
    {
        if (!m_selectedContainer) return;
        if (JobManager.m_jobs.Count > 0)
        {
            m_selectedContainer.SetActive(true);
            foreach (var job in JobManager.m_jobs)
            {
                GameObject element = Object.Instantiate(m_tabElement, m_selected);
                if (element.transform.GetChild(0).TryGetComponent(out Text text))
                {
                    text.text = Localization.instance.Localize(
                        $"<color=yellow>{job.Name}</color>" 
                        + $"\n$text_cost_to $text_remove <color=orange>{SkillControlPlugin._CostToRemove.Value}</color> {GetCurrency().m_itemData.m_shared.m_name}");
                }
                if (element.TryGetComponent(out Button button))
                {
                    button.onClick.AddListener(() =>
                    {
                        if (!CheckCost()) return;
                        JobManager.m_jobs.Remove(job);
                        if (SkillControlPlugin._RemoveSkillExperience.Value is SkillControlPlugin.Toggle.On)
                        {
                            foreach (var kvp in job.SkillModifiers)
                            {
                                var skill = kvp.SkillName;
                                if (kvp.Modifier > 0f) continue;
                                if (JobManager.GetSkillType(skill, out Skills.SkillType type))
                                {
                                    Player.m_localPlayer.m_skills.ResetSkill(type);
                                    SkillControlPlugin.SkillControlLogger.LogDebug($"Reset {skill} to zero");
                                }
                            }
                        }
                        ShowUI();
                    });
                }
            }
        }
        else
        { 
            m_selectedContainer.SetActive(false);
        }
    }

    private static ItemDrop GetCurrency()
    {
        var currency = ObjectDB.instance.GetItemPrefab(SkillControlPlugin._Currency.Value);
        if (currency == null) return m_defaultCurrency;
        return currency.TryGetComponent(out ItemDrop component) ? component : m_defaultCurrency;
    }

    private static bool CheckCost()
    {
        if (Player.m_localPlayer.GetInventory().CountItems(GetCurrency().m_itemData.m_shared.m_name) > SkillControlPlugin._CostToRemove.Value)
        {
            Player.m_localPlayer.GetInventory().RemoveItem(GetCurrency().m_itemData.m_shared.m_name, SkillControlPlugin._CostToRemove.Value);
            return true;
        }
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{GetCurrency().m_itemData.m_shared.m_name} $msg_cost_required");
        return false;
    }

    private static string GetTooltip(SkillData data)
    {
        return Localization.instance.Localize($"$text_modifier: <color=orange>{data.Modifier * 100}</color>%");
    }

    private static void HideUI()
    {
        if (SkillControlPlugin._ForceJob.Value is SkillControlPlugin.Toggle.On)
        {
            if (JobManager.m_jobs.Count == 0)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_job_required");
            }
        }
        ToggleUI(false);
    }
}
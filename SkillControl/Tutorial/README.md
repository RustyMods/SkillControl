# Skill Controller

Plugin adds the ability to modify skill experience gain. Created with ItemControl in mind,
so admins can effectively create professions.

https://thunderstore.io/c/valheim/p/RustyMods/ItemControl/

With <b>ItemControl</b> and <b>SkillControl</b>, you can control which skills can be leveled, in turn, because <b>ItemControl</b>
can block craft, equip and or consume based on skill level, you can control what is craft-able, equip-able and consumable,
depending on the "job" that the player chooses.

## Changelog
```yaml
1.0.0 - Initial release
```

## Credits

Commissioned by <b>Aldhari</b>

## Configurations
```yaml
Lock Configuration: Only admins are allowed to change synced values
Cost To Remove: Cost to remove job
Currency: Input PrefabName of currency you wish to use
Force Employment: If on, window will appear everytime player opens inventory to remind them to choose a job
Key: Hotkey to open menu
Limit: Amount of jobs one can have at a time
Position: Move UI
Lose Skills: If on, then removing a job, resets skills that are modified at 0 
```

## File Structure

Upon initial load of plugin. You will find a new folder in your BepinEx/config directory

FolderName: <b>SkillControl</b>

Inside, you will find default generated example jobs and another folder:

FolderName: <b>Icons</b>

You can manipulate YML files while in-game and it will update accordingly.

You can add/change/remove images from icons folder and it will update accordingly.

## Icons

You must share your .png files with your friends if you wish for them to see your custom backgrounds.

They must share the same name as your job.

Example:
```yaml
Name: Sword Master
Description: ...
SkillModifiers: ...
PNG file name: Sword Master.png
```

## YML Structure
```yaml
Name: Blacksmith
Description: For whom has a great deal of attention to the details of survival
SkillModifiers:
- SkillName: Blacksmithing
  Modifier: 1.2
- SkillName: Farming
  Modifier: 0.5
- SkillName: Foraging
  Modifier: 0
- SkillName: Sailing
  Modifier: 0.5
- SkillName: Swords
  Modifier: 1.5
```

## Modifiers

Once player selects a job, the modifiers are applied whenever they gain skill. If player has multiple jobs, the modifiers are additive.
```
If modifier is 0, 
    then player does not gain any experience for said skill.
If modifier is less than 1, 
    then player gains less experience for said skill.
If modifier is above 1, 
    then player gains more experience for said skill.
```

## Localization
```yaml
text_my_jobs: "my jobs"
text_benefit: "benefits"
text_detriment: "detriments"
text_modifier: "modifier"
text_select: "select"
text_remove: "remove"
text_cost_to: "cost to"
text_skill_loss: "<color=yellow>Warning:</color> removing job, removes skill experience for any with <color=orange>0</color>% modifier"
msg_select_job: "Choose your employment"
msg_job_required: "employment required"
msg_job_limit: "you are already employed"
msg_cost_required: "required to remove"
```
If you wish to translate to your own language, simply create a YML file in your config directory and add these entries with the name of your language followed by .yml
`example: SkillControl.French.yml`

![](https://i.imgur.com/CR7s30A.png)

## Contact information
For Questions or Comments, find <span style="color:orange">Rusty</span> in the Odin Plus Team Discord

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/v89DHnpvwS)

Or come find me at the [Modding Corner](https://discord.gg/fB8aHSfA8B)

##
If you enjoy this mod and want to support me:
[PayPal](https://paypal.me/mpei)

<span>
<img src="https://i.imgur.com/rbNygUc.png" alt="" width="150">
<img src="https://i.imgur.com/VZfZR0k.png" alt="https://www.buymeacoffee.com/peimalcolm2" width="150">
</span>

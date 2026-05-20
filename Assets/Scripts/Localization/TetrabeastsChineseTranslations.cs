using System;
using System.Collections.Generic;
using System.Text;

public static class TetrabeastsChineseTranslations
{
    static readonly Dictionary<string, string> ExactText = BuildExactText();

    static readonly Dictionary<string, string> RunModifierTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["decrease the amount of special gained from all sources."] = "{0}降低所有来源获得的特殊能量。",
        ["decreases the amount of special gained from all sources."] = "{0}降低所有来源获得的特殊能量。",
        ["increase the amount of special gained from all sources."] = "{0}提高所有来源获得的特殊能量。",
        ["increases the amount of special gained from all sources."] = "{0}提高所有来源获得的特殊能量。",
        ["decrease the special gained from each monster."] = "{0}降低每个怪物获得的特殊能量。",
        ["decreases the special gained from each monster."] = "{0}降低每个怪物获得的特殊能量。",
        ["increase the special gained from each monster."] = "{0}提高每个怪物获得的特殊能量。",
        ["increases the special gained from each monster."] = "{0}提高每个怪物获得的特殊能量。",
        ["decreases the attack value for all monsters in your current roster."] = "{0}降低当前队伍中所有怪物的攻击力。",
        ["decrease the attack value for all monsters in your current roster."] = "{0}降低当前队伍中所有怪物的攻击力。",
        ["increases the attack value for all monsters in your current roster."] = "{0}提高当前队伍中所有怪物的攻击力。",
        ["increase the attack value for all monsters in your current roster."] = "{0}提高当前队伍中所有怪物的攻击力。",
        ["decrease the damage of enemy projectiles."] = "{0}降低敌方投射物伤害。",
        ["decreases the damage of enemy projectiles."] = "{0}降低敌方投射物伤害。",
        ["incecrease the damage of enemy projectiles."] = "{0}提高敌方投射物伤害。",
        ["incecreases the damage of enemy projectiles."] = "{0}提高敌方投射物伤害。",
        ["increase the damage of enemy projectiles."] = "{0}提高敌方投射物伤害。",
        ["increases the damage of enemy projectiles."] = "{0}提高敌方投射物伤害。",
        ["increases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0}延长敌人攻击间隔（不包括 Boss 技能冷却）。",
        ["increase the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0}延长敌人攻击间隔（不包括 Boss 技能冷却）。",
        ["decreases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0}缩短敌人攻击间隔（不包括 Boss 技能冷却）。",
        ["decrease the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0}缩短敌人攻击间隔（不包括 Boss 技能冷却）。",
        ["increase the hit points of all future enemy fortifications."] = "{0}提高之后所有敌方防御工事的生命值。",
        ["increases the hit points of all future enemy fortifications."] = "{0}提高之后所有敌方防御工事的生命值。",
        ["decreases the rate falling speed builds up over time for blocks."] = "{0}降低方块下落速度随时间提升的速率。",
        ["decrease the rate falling speed builds up over time for blocks."] = "{0}降低方块下落速度随时间提升的速率。",
        ["increases the rate falling speed builds up over time for blocks."] = "{0}提高方块下落速度随时间提升的速率。",
        ["increase the rate falling speed builds up over time for blocks."] = "{0}提高方块下落速度随时间提升的速率。",
        ["decreases the initial falling speed of blocks."] = "{0}降低方块初始下落速度。",
        ["decrease the initial falling speed of blocks."] = "{0}降低方块初始下落速度。",
        ["increases the initial falling speed of blocks."] = "{0}提高方块初始下落速度。",
        ["increase the initial falling speed of blocks."] = "{0}提高方块初始下落速度。",
        ["increase the healing power of all friendly monsters."] = "{0}提高所有友方怪物的治疗强度。",
        ["increases the healing power of all friendly monsters."] = "{0}提高所有友方怪物的治疗强度。",
        ["decrease friendly monster pieces maximum hit points."] = "{0}降低友方怪物单位的最大生命值。",
        ["decreases friendly monster pieces maximum hit points."] = "{0}降低友方怪物单位的最大生命值。",
        ["increase friendly monster pieces maximum hit points."] = "{0}提高友方怪物单位的最大生命值。",
        ["increases friendly monster pieces maximum hit points."] = "{0}提高友方怪物单位的最大生命值。",
        ["increase luck raising the chance of getting higher rarity buffs."] = "{0}提高幸运值，增加获得更高稀有度增益的概率。",
        ["increases luck raising the chance of getting higher rarity buffs."] = "{0}提高幸运值，增加获得更高稀有度增益的概率。",
        ["increases the likelihood of finding higher rarity debuffs."] = "{0}提高遭遇更高稀有度减益的概率。",
        ["increase the likelihood of finding higher rarity debuffs."] = "{0}提高遭遇更高稀有度减益的概率。",
        ["decrease the number of reinforcement units added after winning a round."] = "{0}降低每回合获胜后获得的援军数量。",
        ["decreases the number of reinforcement units added after winning a round."] = "{0}降低每回合获胜后获得的援军数量。",
        ["increase the number of reinforcement units added after winning a round."] = "{0}提高每回合获胜后获得的援军数量。",
        ["increases the number of reinforcement units added after winning a round."] = "{0}提高每回合获胜后获得的援军数量。",
        ["decreases the chance of special blocks appearing."] = "{0}降低特殊方块出现概率。",
        ["decrease the chance of special blocks appearing."] = "{0}降低特殊方块出现概率。",
        ["incecreases the chance of special blocks appearing."] = "{0}提高特殊方块出现概率。",
        ["incecrease the chance of special blocks appearing."] = "{0}提高特殊方块出现概率。",
        ["increases the chance of special blocks appearing."] = "{0}提高特殊方块出现概率。",
        ["increase the chance of special blocks appearing."] = "{0}提高特殊方块出现概率。",
        ["decrease the chance a buff will drop from a stone obstacle."] = "{0}降低石块障碍掉落增益的概率。",
        ["decreases the chance a buff will drop from a stone obstacle."] = "{0}降低石块障碍掉落增益的概率。",
        ["increase the chance a buff will drop from a stone obstacle."] = "{0}提高石块障碍掉落增益的概率。",
        ["increases the chance a buff will drop from a stone obstacle."] = "{0}提高石块障碍掉落增益的概率。",
        ["decrease the maximum limit of the unit reserve."] = "{0}降低单位预备队上限。",
        ["decreases the maximum limit of the unit reserve."] = "{0}降低单位预备队上限。",
        ["increase the maximum limit of the unit reserve."] = "{0}提高单位预备队上限。",
        ["increases the maximum limit of the unit reserve."] = "{0}提高单位预备队上限。",
        ["reduces the amount of currency the player gains after winning a round."] = "{0}降低玩家回合获胜后获得的金币。",
        ["reduce the amount of currency the player gains after winning a round."] = "{0}降低玩家回合获胜后获得的金币。",
        ["increases the amount of currency the player gains after winning a round."] = "{0}提高玩家回合获胜后获得的金币。",
        ["increase the amount of currency the player gains after winning a round."] = "{0}提高玩家回合获胜后获得的金币。",
        ["increase the chance currency will be earned when clearing lines."] = "{0}提高消除行时获得金币的概率。",
        ["increases the chance currency will be earned when clearing lines."] = "{0}提高消除行时获得金币的概率。"
    };

    static readonly (string English, string Chinese)[] DegreePrefixes =
    {
        ("Slightly", "略微"),
        ("Modestly", "小幅"),
        ("Moderatley", "中等"),
        ("Moderately", "中等"),
        ("Significantly", "显著"),
        ("Massivley", "大幅"),
        ("Massively", "大幅")
    };

    static readonly (string English, string Chinese)[] LinePrefixes =
    {
        ("Special Gauge Gain", "特殊能量获取"),
        ("Enemy Damage", "敌人伤害"),
        ("Enemy HP", "敌人生命"),
        ("Score Gain", "分数获取"),
        ("EXP Gain", "经验获取"),
        ("Misfortune", "厄运"),
        ("Gravity", "重力"),
        ("Score", "分数"),
        ("Level", "关卡"),
        ("Reset", "重置")
    };

    public static bool TryGetText(string englishText, out string chineseText)
    {
        chineseText = null;

        if (string.IsNullOrWhiteSpace(englishText))
            return false;

        string lookupKey = NormalizeLookupKey(englishText);
        if (ExactText.TryGetValue(lookupKey, out chineseText))
            return true;

        if (TryTranslateRunModifierDescription(lookupKey, out chineseText))
            return true;

        if (TryTranslateLabelValueLines(englishText, out chineseText))
            return true;

        return false;
    }

    static Dictionary<string, string> BuildExactText()
    {
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddTopLevelText(text);
        AddCommonLabels(text);
        AddXpBreakdownText(text);
        AddWarningAndTutorialText(text);
        AddCharacterText(text);
        AddHelpTopicText(text);
        AddLevelModifierText(text);
        AddRunModifierNames(text);
        AddRunModifierFixedDescriptions(text);
        AddStatText(text);
        AddRecentFixupText(text);

        return text;
    }

    static void AddRecentFixupText(Dictionary<string, string> text)
    {
        Add(text, "Starting a new game will erase the saved run. Continue?", "开始新游戏将删除已保存的征战。继续？");
        Add(text, "Current Level: {0}", "当前等级：{0}");
        Add(text, "Current Level: 0", "当前等级：0");
        Add(text, " x{0}", " ×{0}");
        Add(text, "Refund", "退款");
        Add(text, "Unlock", "解锁");
        Add(text, " Unlock", " 解锁");
        Add(text, "Selected:", "已选择：");
        Add(text, "Selected: ", "已选择：");
        Add(text, "{0}  Lv.{1}", "{0}  等级{1}");
        Add(text, "Luck Up", "幸运提升");
        Add(text, "Gravity Down", "重力降低");
        Add(text, "Velocity Down", "速度降低");
        Add(text, "Gold Up", "金币提升");
        Add(text, "Attack Up", "攻击提升");
        Add(text, "HP Up", "生命提升");
        Add(text, "Unit Lives Up", "单位生命提升");
        Add(text, "Role: {0}", "定位：{0}");
        Add(text, "Level: {0}  ({1:0.#}/{2})", "等级：{0}  ({1:0.#}/{2})");
        Add(text, "Max HP: {0:0.#}  (+{1}) = {2:0.#}", "最大生命：{0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Attack: {0:0.#}  (+{1}) = {2:0.#}", "攻击：{0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Special Gain: {0:0.#}", "特殊能量获取：{0:0.#}");
        Add(text, "Heal: {0:0.#}  (+{1}) = {2:0.#}", "治疗：{0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Heal Range: {0:0.#}", "治疗范围：{0:0.#}");
        Add(text, "Heal Speed: {0:0.#}s", "治疗速度：{0:0.#}秒");
        Add(text, "Heal: -", "治疗：-");
        Add(text, "Base Stats + Shop Buff = Total Stats", "基础属性 + 商店增益 = 总属性");
        Add(text, "Attack", "攻击");
        Add(text, "Defense", "防御");
        Add(text, "Healer", "治疗者");
        Add(text, "Beefy boy that deals very little damage.", "结实耐打，但伤害很低。");
        Add(text, "Healer with a wide range, but weak spells.", "治疗范围广，但法术较弱。");
        Add(text, "A well rounded attacker unit with decent damage, but enough health to survive weaker attacks.", "均衡的攻击单位，伤害不错，也有足够生命承受较弱攻击。");
        Add(text, "A specialized attacker unit with high base damage and lower health.", "专精攻击单位，基础伤害高但生命较低。");
        Add(text, "Can take a hit and keep going, but won't deal much damage.", "能挨打并继续作战，但伤害不高。");
        Add(text, "Healer with a short range, but powerful healing spells.", "治疗范围短，但治疗法术强力。");
        Add(text, "Combo Extension", "连击延长");
        Add(text, "Chain Surge", "连锁涌动");
        Add(text, "Stone Scrounger", "石块搜刮者");
        Add(text, "Reserve Stockpile", "预备队储备");
        Add(text, "Reserve Recovery", "预备队恢复");
        Add(text, "Bulwark Aura", "壁垒光环");
        Add(text, "Increase combo timer duration by {0}.", "连击计时持续时间增加 {0}。");
        Add(text, "Each row clear has a {0} chance to increase combo count one additional time.", "每次消行有 {0} 概率额外增加一次连击数。");
        Add(text, "Increase chance of buff drop from stone obstacle destruction by {0}.", "摧毁石块障碍时掉落增益的概率增加 {0}。");
        Add(text, "Increase the number of starting reserve units by {0}.", "初始预备单位数量增加 {0}。");
        Add(text, "Increase the number of reserve units restored on round win by {0}.", "回合胜利时恢复的预备单位数量增加 {0}。");
        Add(text, "Decrease damage taken and damage done for all ally monster units by {0}.", "所有友方怪物单位受到和造成的伤害降低 {0}。");
        Add(text, "Passive - {0}", "被动 - {0}");
        Add(text, "Passive - {0}:", "被动 - {0}：");
        Add(text, "Next upgrade at Lv.{0}:", "下次升级于等级{0}：");
        Add(text, "Passive is fully upgraded.", "被动已升满。");
        Add(text, "1 second", "1 秒");
        Add(text, "{0} seconds", "{0} 秒");
        Add(text, "A = Shift Left", "A = 左移");
        Add(text, "D = Shift Right", "D = 右移");
        Add(text, "D = Shft Right", "D = 右移");
        Add(text, "R = Character Special", "R = 角色特殊能力");
        Add(text, "= Character Special", "= 角色特殊能力");
        Add(text, "Boss", "首领");
        Add(text, "Boss Ability", "首领能力");
        Add(text, "Boss Abilities", "首领能力");

        Add(text, "R = Character Special (Special Guage 100%)", "R = 角色特殊能力（特殊能量 100%）");
        Add(text, "R = Character Special (Special Gauge 100%)", "R = 角色特殊能力（特殊能量 100%）");
        Add(text, "R = Character Special (Specaial Gauge 100%)", "R = 角色特殊能力（特殊能量 100%）");
        Add(text, "= Character Special (Special Guage 100%)", "= 角色特殊能力（特殊能量 100%）");
        Add(text, "= Character Special (Special Gauge 100%)", "= 角色特殊能力（特殊能量 100%）");

        Add(text, "Gold Won This Round:", "本回合获得金币:");
        Add(text, "Gold Won This Round", "本回合获得金币");
        Add(text, "Rerolls", "重抽次数");
        Add(text, "Rerolls: {0}", "重抽次数: {0}");
        Add(text, "Rerolls: 0", "重抽次数: 0");
        Add(text, "Modifier", "修改器");
        Add(text, "x{0}", "×{0}");

        Add(text, "Level Up", "升级");
        Add(text, "Level Up!", "升级！");
        Add(text, "LEVEL UP!", "升级！");
        Add(text, "LEVEL UP! x{0}", "升级！x{0}");
        Add(text, "Level {0} -> Level {1}", "等级 {0} -> 等级 {1}");
        Add(text, "+{0} Exp", "+{0} 经验");
        Add(text, "{0} permanent EXP ({1}% of {2} transferable EXP)", "{0} 永久经验（{2} 可转移经验的 {1}%）");
        Add(text, "Converted from {0} run EXP at {1}%", "从 {0} 征战经验按 {1}% 转换");

        Add(text, "Passive+", "被动+");
        Add(text, "+ 5 HP", "+ 5 生命");
        Add(text, "+ 1 Attack", "+ 1 攻击");
        Add(text, "+ 1 Special", "+ 1 特殊能量");
        Add(text, "+ 5 Heal", "+ 5 治疗");
        Add(text, "+ 1 Range", "+ 1 范围");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "接下来，请按[空格键]让当前方块快速落下。这会立刻放下方块，让你快速锁定当前位置并生成新方块。");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "接下来，请按[空格键]让当前方块快速落下。这会立刻放下方块，让你快速锁定当前位置并生成新方块。");
        Add(text, "This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "这会立刻放下方块，让你快速锁定当前位置并生成新方块。");
        Add(text, "This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "这会立刻放下方块，让你快速锁定当前位置并生成新方块。");
        AddAchievementText(text);
    }

    static void AddAchievementText(Dictionary<string, string> text)
    {
        Add(text, "I Want Every Able Body", "我要所有可用战力");
        Add(text, "Unlock all monster units.", "解锁所有怪物单位。");
        Add(text, "0 Star Victory", "0星胜利");
        Add(text, "Beat the final boss.", "击败最终Boss。");
        Add(text, "One Star Victory", "一星胜利");
        Add(text, "Beat the final boss on 1-Star difficulty.", "在1星难度击败最终Boss。");
        Add(text, "Pay To Win", "付费变强");
        Add(text, "Upgrade any shop buff to level 5.", "将任意商店增益升到5级。");
        Add(text, "Gangs All Here", "全员到齐");
        Add(text, "Unlock all Commanders.", "解锁所有指挥官。");
        Add(text, "Two Star Victory", "二星胜利");
        Add(text, "Beat the final boss on 2-Star difficulty.", "在2星难度击败最终Boss。");
        Add(text, "Three Star Victory", "三星胜利");
        Add(text, "Beat the final boss on 3-Star difficulty.", "在3星难度击败最终Boss。");
        Add(text, "Four Star Victory", "四星胜利");
        Add(text, "Beat the final boss on 4-Star difficulty.", "在4星难度击败最终Boss。");
        Add(text, "Five Star Victory", "五星胜利");
        Add(text, "Beat the final boss on 5-Star difficulty.", "在5星难度击败最终Boss。");
        Add(text, "This is Fine", "这没事");
        Add(text, "Take 1,000 burn damage from fire floor effects.", "承受1,000点火焰地面灼烧伤害。");
        Add(text, "That Escalated Quickly", "升级得真快");
        Add(text, "Remove 1,000 units using the Death Special Block.", "使用死亡特殊方块移除1,000个单位。");
        Add(text, "First Time Raider", "初次突袭者");
        Add(text, "Take 1,000 damage from traps.", "承受1,000点陷阱伤害。");
        Add(text, "I think I Stepped in Something", "我好像踩到东西了");
        Add(text, "Take 1,000 toxic damage from posioned floor effects.", "承受1,000点中毒地面毒素伤害。");
        Add(text, "Take 1,000 toxic damage from poisoned floor effects.", "承受1,000点中毒地面毒素伤害。");
        Add(text, "Shake It Until You Break It", "摇到它碎掉");
        Add(text, "Clear 250 rows by using the earthquake special block.", "使用地震特殊方块清除250行。");
        Add(text, "A Little Jiggle Goes A Long Way", "小小震动，大有作为");
        Add(text, "Clear 25 rows by using the earthquake special block.", "使用地震特殊方块清除25行。");
        Add(text, "Girthquake", "巨震");
        Add(text, "Clear 1,000 rows by using the earthquake special block.", "使用地震特殊方块清除1,000行。");
        Add(text, "Get in Loser, We're Going Shopping", "上车，我们去购物");
        Add(text, "Accumulate 1,000 gold.", "累计获得1,000金币。");
        Add(text, "Is I Rich Now?", "我现在有钱了吗？");
        Add(text, "Accumulate 100 gold.", "累计获得100金币。");
        Add(text, "This Lasted Longer Than Some Collectible Fads", "比某些收藏热潮还持久");
        Add(text, "Take more than 5 minutes to conquer a castle.", "征服一座城堡用时超过5分钟。");
        Add(text, "Anything You Can Do, I Can Do Slower", "你会的我都能更慢");
        Add(text, "Take more than 3 minutes to conquer a castle.", "征服一座城堡用时超过3分钟。");
        Add(text, "Sloth Lord", "懒惰领主");
        Add(text, "Take more than 4 minutes to conquer a castle.", "征服一座城堡用时超过4分钟。");
        Add(text, "My Fingers Hurt", "我的手指疼");
        Add(text, "Survive with gravity at 10 for 60 seconds.", "在重力10下生存60秒。");
        Add(text, "Nevermind...", "算了……");
        Add(text, "Unlock your first first temporary debuff.", "解锁你的第一个临时减益。");
        Add(text, "A Record That Would Make Lions Blush", "令人脸红的纪录");
        Add(text, "Lose 50 Times.", "失败50次。");
        Add(text, "Thrive Under Pressure", "压力下茁壮成长");
        Add(text, "Survive with gravity at 10 for 30 seconds.", "在重力10下生存30秒。");
        Add(text, "Is I Strong Now?", "我现在强了吗？");
        Add(text, "Unlock your first first temporary buff.", "解锁你的第一个临时增益。");
        Add(text, "I Say This Not As An Insult, But As A Statement Of Fact", "这不是侮辱，只是陈述事实");
        Add(text, "Lose 100 Times.", "失败100次。");
        Add(text, "I Think We Need A Bigger Vault", "我觉得需要更大的金库");
        Add(text, "Accumulate 10,000 gold.", "累计获得10,000金币。");
        Add(text, "GG EZ", "GG EZ");
        Add(text, "Beat the final level with every Commander.", "使用每位指挥官通关最终关卡。");
        Add(text, "Participation Trophy", "参与奖");
        Add(text, "Lose for the first time.", "第一次失败。");
        Add(text, "Immortal Army", "不朽军团");
        Add(text, "Conquer 100 castles with your Unit Reserve at max capacity.", "在单位预备队满员时征服100座城堡。");
        Add(text, "I Guess That Was The Wrong Wire", "看来剪错线了");
        Add(text, "Lose 100 monster units to magic explosives.", "让100个怪物单位死于魔法爆炸物。");
        Add(text, "I Think It's Dead Now", "我觉得它现在死了");
        Add(text, "Clear 1,000 rows.", "清除1,000行。");
        Add(text, "Tis But A Scratch", "只是擦伤");
        Add(text, "Conquer a castle with your Unit Reserve at max capacity.", "在单位预备队满员时征服一座城堡。");
        Add(text, "Meat Shield Tactics", "肉盾战术");
        Add(text, "Conquer 25 castles with your Unit Reserve at max capacity.", "在单位预备队满员时征服25座城堡。");
        Add(text, "You Got Your Marching Orders", "行军命令已下达");
        Add(text, "Clear 100 rows.", "清除100行。");
        Add(text, "Overwhelming Power", "压倒性力量");
        Add(text, "Accumulate 15 buffs in a single run.", "单次征战累计15个增益。");
        Add(text, "Is There Anything Left To Attack?", "还有什么能打的吗？");
        Add(text, "Clear 10,000 rows.", "清除10,000行。");
        Add(text, "Shut Up And Takey My Money", "别说了，拿走我的钱");
        Add(text, "Buy an upgrade from the shop for the first time.", "第一次在商店购买升级。");
        Add(text, "Turning Small Numbers Into Big Numbers", "把小数字变成大数字");
        Add(text, "Get a combo of 10 or higher.", "达成10或更高连击。");
        Add(text, "Vewwy Stwong", "超强壮");
        Add(text, "Deal 100 damage or more in a single attack.", "单次攻击造成100点或更多伤害。");
        Add(text, "I'll Follow You Anywhere", "我愿追随你到任何地方");
        Add(text, "Unlock a Commander for the first time.", "第一次解锁一名指挥官。");
        Add(text, "Eenie Meenie Miney Mo", "点兵点将");
        Add(text, "Unlock a monster unit for the first time.", "第一次解锁一个怪物单位。");
        Add(text, "Tell Me I'm Pretty", "说我好看");
        Add(text, "Unlock a monster units skin variant for the first time.", "第一次解锁怪物单位的皮肤变体。");
        Add(text, "Are We There Yet?", "到了吗？");
        Add(text, "Conquer 10 castles in a single run.", "单次征战征服10座城堡。");
        Add(text, "It's Called Fashion Brenda, Look It Up", "这叫时尚，去查查");
        Add(text, "Unlock ten skin variants.", "解锁十个皮肤变体。");
        Add(text, "New Skin Who Dis?", "新皮肤，谁呀？");
        Add(text, "Unlock five skin variants.", "解锁五个皮肤变体。");
        Add(text, "A Little Special", "有点特殊");
        Add(text, "Use a special for the first time.", "第一次使用特殊能力。");
        Add(text, "Mama's Special Boy", "妈妈的特别孩子");
        Add(text, "Use a special 100 times.", "使用特殊能力100次。");
        Add(text, "Some Are More Special Than Others", "有些更特殊");
        Add(text, "Use every Commanders special 100 times.", "每位指挥官的特殊能力都使用100次。");
        Add(text, "General Got Me Workin'", "将军让我干活");
        Add(text, "Destory 100 stone obstacles.", "摧毁100个石块障碍。");
        Add(text, "Destroy 100 stone obstacles.", "摧毁100个石块障碍。");
        Add(text, "Certified Glue Eater", "认证胶水食客");
        Add(text, "Use a special 1000 times.", "使用特殊能力1000次。");
        Add(text, "We're All Special", "我们都很特殊");
        Add(text, "Use every Commanders special 20 times.", "每位指挥官的特殊能力都使用20次。");
        Add(text, "That's A Lot Of Rubble", "好多废墟");
        Add(text, "Conquer 50 castles.", "征服50座城堡。");
        Add(text, "King of Rubble", "废墟之王");
        Add(text, "Conquer 100 castles.", "征服100座城堡。");
        Add(text, "I. AM. SPEED!", "我，就是速度！");
        Add(text, "Conquer a castle in 30 seconds or less.", "在30秒或更短时间内征服一座城堡。");
        Add(text, "Can't Stop, Won't Stop", "停不下来，也不想停");
        Add(text, "Conquer your first castle.", "征服你的第一座城堡。");
        Add(text, "I Ran Track In Highschool", "我高中练过田径");
        Add(text, "Conquer a castle in 45 seconds or less.", "在45秒或更短时间内征服一座城堡。");
        Add(text, "Gotta Go Fast", "必须快起来");
        Add(text, "Conquer a castle in 60 seconds or less.", "在60秒或更短时间内征服一座城堡。");
    }

    static void AddXpBreakdownText(Dictionary<string, string> text)
    {
        Add(text, "Level {0} Complete", "关卡 {0} 完成");
        Add(text, "Base Level XP", "关卡基础经验");
        Add(text, "Clear time {0}", "通关时间 {0}");
        Add(text, "Units lost {0}", "损失单位 {0}");
        Add(text, "Largest Combo {0}", "最高连击 {0}");
        Add(text, "Obstacles Cleared {0}", "清除障碍 {0}");
        Add(text, "Star Difficulty ({0}):", "星级难度（{0}）：");
        Add(text, "Total XP Earned {0}", "获得总经验 {0}");
    }

    static void AddTopLevelText(Dictionary<string, string> text)
    {
        Add(text, "OK", "确定");
        Add(text, "Cancel", "取消");
        Add(text, "Continue", "继续");
        Add(text, "Confirm", "确认");
        Add(text, "Close", "关闭");
        Add(text, "Start", "开始");
        Add(text, "PAUSED", "已暂停");
        Add(text, "Resume", "继续");
        Add(text, "Main Menu", "主菜单");
        Add(text, "Restart", "重新开始");
        Add(text, "Save & Quit", "保存并退出");
        Add(text, "Quit", "退出");
        Add(text, "New Game", "新游戏");
        Add(text, "Shop", "商店");
        Add(text, "Codex", "图鉴");
        Add(text, "Help", "帮助");
        Add(text, "HighScore", "最高分");
        Add(text, "Select Monsters", "选择怪物");
        Add(text, "Select Commander", "选择指挥官");
        Add(text, "Combat Log", "战斗日志");
        Add(text, "Skip Trailer", "跳过预告");
        Add(text, "BGM Genre", "背景音乐类型");
        Add(text, "EDM", "电子舞曲");
        Add(text, "Metal", "金属乐");
        Add(text, "Random", "随机");
        Add(text, "Modifiers", "修改器");
        Add(text, "Active Run Modifiers", "当前征战修改器");
        Add(text, "Back", "返回");
        Add(text, "None", "无");
        Add(text, "Yes", "是");
        Add(text, "No", "否");
        Add(text, "Locked", "已锁定");
        Add(text, "Blocked", "已阻挡");
        Add(text, "Active", "已激活");
        Add(text, "(Missing)", "（缺失）");
        Add(text, "???", "???");
        Add(text, "Leaderboard", "排行榜");
        Add(text, "LEADERBOARDS", "排行榜");
        Add(text, "Global", "全球");
        Add(text, "Friends", "好友");
        Add(text, "Current", "当前");
        Add(text, "Rank", "排名");
        Add(text, "Player", "玩家");
        Add(text, "Refresh", "刷新");
        Add(text, "Loading...", "正在加载...");
        Add(text, "Refreshing leaderboards...", "正在刷新排行榜...");
        Add(text, "Steam leaderboard ready.", "Steam 排行榜已就绪。");
        Add(text, "Leaderboard refresh failed.", "排行榜刷新失败。");
        Add(text, "No leaderboard data.", "没有排行榜数据。");
        Add(text, "No global scores yet.", "还没有全球分数。");
        Add(text, "No friend scores yet.", "还没有好友分数。");
        Add(text, "You are not ranked yet.", "你尚未上榜。");
        Add(text, "Help Menu", "帮助菜单");
        Add(text, "Modifier not yet discovered.", "尚未发现该词条。");
        Add(text, "No level Modifier", "无关卡修改器");
        Add(text, "Level modifier: {0}.", "关卡修改器：{0}。");
        Add(text, "Secret Achievements", "隐藏成就");
        Add(text, "1 secret achievement remaining", "还剩 1 个隐藏成就");
        Add(text, "{0} secret achievements remaining", "还剩 {0} 个隐藏成就");
        Add(text, "No GameController found.", "未找到 GameController。");
        Add(text, "No monsters in roster.", "队伍中没有怪物。");
        Add(text, "Reinforcements", "援军");
        Add(text, "Special Ability: {0}", "特殊能力：{0}");
        Add(text, "{0} is broken.", "{0} 已损坏。");
        Add(text, "{0}: {1} shield(s) remain.", "{0}：还剩 {1} 个护盾。");
        Add(text, "Language", "语言");
        Add(text, "Settings", "设置");
        Add(text, "Master Volume", "主音量");
        Add(text, "Music Volume", "音乐音量");
        Add(text, "SFX Volume", "音效音量");
        Add(text, "Cursor Size", "光标大小");
        Add(text, "Choose Language", "选择语言");
        Add(text, "Pick the language Tetrabeasts should use. You can change this later in Settings.", "选择 Tetrabeasts 使用的语言。之后可在设置中更改。");
        Add(text, "Press Any Key", "按任意键");
        Add(text, "Achievements", "成就");
        Add(text, "Submit Name", "提交名称");
        Add(text, "Buffs", "增益");
        Add(text, "Debuffs", "减益");
        Add(text, "Level Mod", "关卡修改");
        Add(text, "Units", "单位");
        Add(text, "Choose A Buff", "选择一个增益");
        Add(text, "Choose A Debuff", "选择一个减益");
        Add(text, "Next Piece", "下一个方块");
        Add(text, "Reroll", "重抽");
        Add(text, "EXP Gained", "获得经验");
        Add(text, "Permanent EXP Gained", "获得永久经验");
        Add(text, "EXP Preserved", "保留经验");
        Add(text, "Unit Reserve", "单位预备队");
        Add(text, "New Difficulty Unlocked!", "新难度已解锁！");
        Add(text, "Do not show this message again", "不再显示此消息");
        Add(text, "Active Squad", "当前小队");
    }

    static void AddCommonLabels(Dictionary<string, string> text)
    {
        Add(text, "Run", "征战");
        Add(text, "Level", "关卡");
        Add(text, "Controls", "控制");
        Add(text, "Gravity", "重力");
        Add(text, "Combo", "连击");
        Add(text, "Score", "分数");
        Add(text, "Reset", "重置");
        Add(text, "Score: {0}", "分数：{0}");
        Add(text, "Gravity: {0:0.0}", "重力：{0:0.0}");
        Add(text, "Star Difficulty", "星级难度");
        Add(text, "0 Star: Recruit Difficulty", "0 星：新兵难度");
        Add(text, "1 Star: Soldier Difficulty", "1 星：士兵难度");
        Add(text, "2 Star: Veteran Difficulty", "2 星：老兵难度");
        Add(text, "3 Star: Lieutenant Difficulty", "3 星：中尉难度");
        Add(text, "4 Star: General Difficulty", "4 星：将军难度");
        Add(text, "5 Star: War God Difficulty", "5 星：战神难度");
        Add(text, "0-Star Difficulty", "0 星难度");
        Add(text, "1 Star Difficulty", "1 星难度");
        Add(text, "2 Stars Difficulty", "2 星难度");
        Add(text, "3 Stars Difficulty", "3 星难度");
        Add(text, "4 Stars Difficulty", "4 星难度");
        Add(text, "5 Stars Difficulty", "5 星难度");
        Add(text, "Normal difficulty.", "普通难度。");
        Add(text, "No gameplay modifiers.", "没有玩法修改。");
        Add(text, "All star difficulties unlocked.", "所有星级难度均已解锁。");
        Add(text, "0 Stars is always available.", "0 星始终可用。");
        Add(text, "Beat the final level on 0 Stars to unlock 1 Star.", "以 0 星通关最终关卡以解锁 1 星。");
        Add(text, "Beat the final level on 1 Star to unlock 2 Stars.", "以 1 星通关最终关卡以解锁 2 星。");
        Add(text, "Beat the final level on 2 Stars to unlock 3 Stars.", "以 2 星通关最终关卡以解锁 3 星。");
        Add(text, "Beat the final level on 3 Stars to unlock 4 Stars.", "以 3 星通关最终关卡以解锁 4 星。");
        Add(text, "Beat the final level on 4 Stars to unlock 5 Stars.", "以 4 星通关最终关卡以解锁 5 星。");
        Add(text, "0 Stars", "0 星");
        Add(text, "1 Star", "1 星");
        Add(text, "2 Stars", "2 星");
        Add(text, "3 Stars", "3 星");
        Add(text, "4 Stars", "4 星");
        Add(text, "5 Stars", "5 星");
        Add(text, "Score Gain", "分数获取");
        Add(text, "EXP Gain", "经验获取");
        Add(text, "Level Modifier", "关卡修改器");
        Add(text, "Reserves And Rewards", "预备队与奖励");
        Add(text, "Reserve Units", "预备单位");
        Add(text, "Max Reserve Units", "预备单位上限");
        Add(text, "Reserve Restored On Win", "胜利恢复预备队");
        Add(text, "Round Win Currency", "回合胜利金币");
        Add(text, "Line Clear Currency Chance", "消行金币概率");
        Add(text, "Line Clear Currency Amount", "消行金币数量");
        Add(text, "Monster Combat", "怪物战斗");
        Add(text, "Monster Damage", "怪物伤害");
        Add(text, "Monster Special Gain", "怪物特殊能量获取");
        Add(text, "Monster Max HP", "怪物最大生命");
        Add(text, "Healing Power", "治疗强度");
        Add(text, "Healing Range Bonus", "治疗范围加成");
        Add(text, "Ally Damage Dealt", "友方造成伤害");
        Add(text, "Ally Damage Taken", "友方承受伤害");
        Add(text, "Combo And Passives", "连击与被动");
        Add(text, "Combo Window", "连击窗口");
        Add(text, "Bonus Combo Chance", "额外连击概率");
        Add(text, "Stone Buff Drop Chance", "石块增益掉落率");
        Add(text, "Starting Reserve Passive", "初始预备队被动");
        Add(text, "Round Win Reserve Passive", "回合胜利预备队被动");
        Add(text, "Enemy", "敌人");
        Add(text, "Enemy Castle HP", "敌方城堡生命");
        Add(text, "Enemy Damage", "敌人伤害");
        Add(text, "Enemy Attack Interval", "敌人攻击间隔");
        Add(text, "Enemy Projectile Speed", "敌方投射物速度");
        Add(text, "Castle Projectile Damage", "城堡投射物伤害");
        Add(text, "Castle Attack Interval", "城堡攻击间隔");
        Add(text, "Piece And Special", "方块与特殊");
        Add(text, "Piece Gravity", "方块重力");
        Add(text, "Gravity Ramp Rate", "重力提升速率");
        Add(text, "Special Block Chance", "特殊方块概率");
        Add(text, "Commander Special Gain", "指挥官特殊能量获取");
        Add(text, "Special Drain", "特殊能量流失");
        Add(text, "Next Preview Disabled", "禁用下个预览");
        Add(text, "Landing Hint Disabled", "禁用落点提示");
        Add(text, "Special Usage Locked", "特殊能力锁定");
        Add(text, "Special Blocks Blocked", "阻止特殊方块");
        Add(text, "Run Modifier Drops", "征战修改器掉落");
        Add(text, "Stone Drops Debuffs Only", "石块仅掉落减益");
        Add(text, "Luck", "幸运");
        Add(text, "Misfortune", "厄运");
        Add(text, "Active Level Modifier", "当前关卡修改器");
        Add(text, "Effect", "效果");
        Add(text, "Outgoing Damage", "造成伤害");
        Add(text, "Incoming Damage", "承受伤害");
        Add(text, "Overgrowth Target Interval", "蔓生目标间隔");
        Add(text, "Initial Target Rows", "初始目标行");
        Add(text, "Partial Growth Time", "部分生长时间");
        Add(text, "Full Growth Time", "完全生长时间");
        Add(text, "Storm Strike Damage", "风暴打击伤害");
        Add(text, "Storm Floor Tick Damage", "风暴地面持续伤害");
        Add(text, "Storm Floor Duration", "风暴地面持续时间");
        Add(text, "Rear Ambush Interval", "后方伏击间隔");
        Add(text, "Rations Tick Interval", "口粮伤害间隔");
        Add(text, "Low Reserve Damage", "低预备队伤害");
        Add(text, "High Reserve Damage", "高预备队伤害");
        Add(text, "Infection Chance", "感染概率");
        Add(text, "Damage Per Tick", "每跳伤害");
        Add(text, "Damage Increase Per Tick", "每跳伤害增加");
        Add(text, "Spread Chance", "传播概率");
        Add(text, "Special Gauge Gain", "特殊能量获取");
        Add(text, "Death Explosion Damage", "死亡爆炸伤害");
        Add(text, "Swamp Poison Damage", "沼泽毒素伤害");
        Add(text, "Swamp Poison Interval", "沼泽毒素间隔");
        Add(text, "Manual Rotation", "手动旋转");
        Add(text, "Auto Rotate Interval", "自动旋转间隔");
        Add(text, "Manual Horizontal Shift", "手动横移");
        Add(text, "Auto Shift Interval", "自动横移间隔");
        Add(text, "Combo Threshold", "连击阈值");
        Add(text, "Below Threshold Damage", "低于阈值伤害");
        Add(text, "Shield Combo Threshold", "护盾连击阈值");
        Add(text, "Blocked Damage", "被阻挡伤害");
        Add(text, "Shield Count", "护盾数量");
        Add(text, "Special Pieces", "特殊方块");
        Add(text, "Starting Monster Health", "怪物初始生命");
        Add(text, "Monster Damage Sharing", "怪物伤害共享");
        Add(text, "Roster", "队伍");
        Add(text, "Max HP", "最大生命");
        Add(text, "Starting HP", "初始生命");
        Add(text, "Attack", "攻击");
        Add(text, "Special Gain", "特殊能量获取");
        Add(text, "Heal Power", "治疗强度");
        Add(text, "Heal Range", "治疗范围");
        Add(text, "Heal Speed", "治疗速度");
        Add(text, "Spawn Weight", "生成权重");
        Add(text, "Stats", "属性");
        Add(text, "Monsters", "怪物");
        Add(text, "Shop Buff", "商店增益");
        Add(text, "Passive", "被动");
        Add(text, "Run Mod Buff", "征战增益");
        Add(text, "Run Mod Debuff", "征战减益");
        Add(text, "Level Mod", "关卡修改");
        Add(text, "Boss Ability", "Boss 能力");
        Add(text, "Defender", "防御者");
        Add(text, "Healer", "治疗者");
        Add(text, "Role", "定位");
        Add(text, "HP", "生命");
        Add(text, "Special Rate", "特殊速率");
        Add(text, "Passive Lv", "被动等级");
        Add(text, "Esc = Pause", "Esc = 暂停");
        Add(text, "Q = Rotate Counter Clockwise", "Q = 逆时针旋转");
        Add(text, "E = Rotate Clockwise", "E = 顺时针旋转");
        Add(text, "= Shift Left", "= 左移");
        Add(text, "D = Shft Right", "D = 右移");
        Add(text, "S = Shift Down", "S = 下移");
        Add(text, "Spacebar = Drop Instantly", "空格 = 立即下落");
        Add(text, "= Character Special (Specaial Gauge 100%)", "= 角色特殊能力（特殊能量 100%）");
        Add(text, "Castle", "城堡");
        Add(text, "Starting Village", "起始村庄");
        Add(text, "Tribe", "部落");
        Add(text, "Tribe Chieftain", "部落酋长");
        Add(text, "Shanty Town", "棚户镇");
        Add(text, "Fortified Hamlet", "设防小村");
        Add(text, "Industrial City", "工业城市");
        Add(text, "Thriving Metropolis", "繁荣大都会");
        Add(text, "Grand Dukedom", "大公国");
        Add(text, "Royal Villa", "皇家别墅");
        Add(text, "Ruk, Tribal Chieftain", "鲁克，部落酋长");
        Add(text, "Fionne, Village Protector", "菲奥妮，村庄守护者");
        Add(text, "Sir Ralphie, Captain of the Guard", "拉尔菲爵士，卫队队长");
        Add(text, "Eris, City Priestess", "艾莉丝，城市女祭司");
        Add(text, "His Holiness Isaeh, Metropolis Pope", "圣座以赛亚，大都会教皇");
        Add(text, "Vivica, Dukedom Arch Mage", "薇薇卡，公国大法师");
        Add(text, "Emperor Reginald P. Exford IV, Emperor's Palace", "雷金纳德 P. 埃克斯福德四世皇帝，皇宫");
        Add(text, "Esora, Guardian to the Gates of Heaven?", "艾索拉，天门守护者？");
    }

    static void AddWarningAndTutorialText(Dictionary<string, string> text)
    {
        Add(text, "Purchases are disabled in the demo. Your earned progress will still carry into the full game.", "试玩版已禁用购买。你已获得的进度仍会继承到完整版。");
        Add(text, "A saved run is waiting. Continue that run or delete the temp save before changing your commander, squad, or shop buffs.", "有一个已保存的征战正在等待。请先继续该征战或删除临时存档，然后才能更改指挥官、小队或商店增益。");
        Add(text, "Deleting the current temp run will permanently erase that saved run. After deleting it, you will be able to change your commander, monsters, and access the shop again. Continue?", "删除当前临时征战会永久清除该存档。删除后，你将可以再次更改指挥官、怪物并进入商店。继续？");
        Add(text, "Thank you for playing the Tetrabeasts demo! You have cleared the final demo level. If you enjoyed your time with the game, please consider buying the full version.", "感谢游玩 Tetrabeasts 试玩版！你已经通关试玩版最终关卡。如果你喜欢这款游戏，请考虑购买完整版。");
        Add(text, "The Castle Has Fallen", "城堡已陷落");
        Add(text, "Conquest Failed", "征服失败");
        Add(text, "Endless Survival This final battle cannot be won. The enemy has endless health, and the run continues until a loss condition is met. Survive as long as you can.", "无尽生存：这场最终战无法获胜。敌人拥有无尽生命，征战会持续到触发失败条件为止。尽可能活得更久。");
        Add(text, "Quit without saving? Your current run will be lost and will not be available to continue later.", "不保存就退出？当前征战将会丢失，之后无法继续。");
        Add(text, "Restarting will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?", "重新开始会将本次征战视为失败。当前临时存档将被删除，本次征战不会保存。继续？");
        Add(text, "Returning to the main menu will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?", "返回主菜单会将本次征战视为失败。当前临时存档将被删除，本次征战不会保存。继续？");
        Add(text, "Save this run and quit the game? Continuing later will resume from the start of the current level checkpoint. While a run is saved, you will not be able to change your commander, squad, or shop buffs from the title menu.", "保存本次征战并退出游戏？之后继续时会从当前关卡检查点开始。保存征战期间，你无法在标题菜单更改指挥官、小队或商店增益。");
        Add(text, "The run could not be temp-saved, so the game will stay open.", "无法临时保存征战，因此游戏会保持开启。");
        Add(text, "Welcome Overlord, my name is Lilith. I have done my best to rally the few monster I could find to assist in your conquest of the human domain. (Press [F] to Continue)", "欢迎，霸主。我叫莉莉丝。我已尽力召集能找到的少数怪物，协助你征服人类领地。（按 [F] 继续）");
        Add(text, "Before going into battle, we will first have to select our commander. Press the commander button in the bottom right to see who is available. (Press [F] to Continue)", "进入战斗前，我们必须先选择指挥官。点击右下角的指挥官按钮查看可用人选。（按 [F] 继续）");
        Add(text, "We only have one Commander that we can use at the moment, but with enough gold we can get a few more. Each commander has their own special ability. The details of each ability can be seen in the Commander preview on the right. (Press [F] to Continue)", "目前只有一名可用指挥官，但金币足够后还能获得更多。每位指挥官都有自己的特殊能力，能力详情可在右侧指挥官预览中查看。（按 [F] 继续）");
        Add(text, "Since our commander is already selected lets confirm the selection at the bottom and head back to the main lobby.", "既然指挥官已经选好，就点击底部确认选择，然后返回大厅。");
        Add(text, "Now we will set up our monster squadron that will go into battle with our Commander. Press the select monster button in the bottom right.", "现在设置与指挥官一同出战的怪物小队。点击右下角的选择怪物按钮。");
        Add(text, "As you can see we only have so many monsters to work with to creat our squadron. A Squadron must have at least two monsters and can have a maximum of 4. (Press [F] to Continue)", "如你所见，可用于组队的怪物数量有限。一个小队至少需要 2 只怪物，最多 4 只。（按 [F] 继续）");
        Add(text, "A war can hardly be fought with such pitiful numbers, but that's where I can be of assistance. (Press [F] to Continue)", "靠这么点兵力几乎打不了仗，不过这正是我能帮上忙的地方。（按 [F] 继续）");
        Add(text, "I can make clones of your squadron to be sent into battle. This will allow multiple copies to be made to fill out your ranks. However, this will come with some limitations. (Press [F] to Continue)", "我可以复制你的小队并派入战场。这样能制造多份副本来补充队伍。不过，这也有一些限制。（按 [F] 继续）");
        Add(text, "I will only be able to make so many copies and if we lose too many in battle we will be forced to retreat. Second, the copies will gain their own experience, but I will only be able to safely convert a small fraction of that experience from the copy back to the original monster after a campaign is finished. (Press [F] to Continue)", "我能制造的复制体数量有限；若战斗中损失太多，我们就必须撤退。其次，复制体会获得自己的经验，但战役结束后，我只能安全地将其中一小部分经验转回原本的怪物。（按 [F] 继续）");
        Add(text, "You can click on any monster and see a preview of it's current stats and level in the preview section on the right. Click on the arrow button in the preview section to swap between it's stats and it's passive ability description.", "你可以点击任意怪物，在右侧预览区查看它当前的属性和等级。点击预览区的箭头按钮，可在属性与被动能力说明之间切换。");
        Add(text, "More monsters can be unlocked with gold and cosmetic skins can also be purchased once you have gained some extra funds. Now, Prress the confirm button at the bottom of the screen to lock in your team.", "更多怪物可用金币解锁，资金充足后也能购买外观皮肤。现在点击屏幕底部的确认按钮锁定队伍。");
        Add(text, "Now you are ready to start your first campaign. Press the Start button to begin a new run.", "现在你已准备好开始第一场战役。点击开始按钮开启新的征战。");
        Add(text, "Welcome to the battlefield. We will begin by going over a few of the basic controls and battle mechanics. (Press [F] to Continue)", "欢迎来到战场。我们先讲解一些基础控制和战斗机制。（按 [F] 继续）");
        Add(text, "First, try moving your piece to the left by pressing [A].", "首先，按 [A] 试着把方块向左移动。");
        Add(text, "Next, try moving your piece to the right by pressing [D].", "接下来，按 [D] 试着把方块向右移动。");
        Add(text, "Next, try moving your piece down a single row by pressing [S].", "接下来，按 [S] 试着让方块下移一行。");
        Add(text, "Now, try rotating your piece counter-clockwise by pressing [Q].", "现在，按 [Q] 试着逆时针旋转方块。");
        Add(text, "Now, try rotating your piece clockwise by pressing [E].", "现在，按 [E] 试着顺时针旋转方块。");
        Add(text, "Look at the bottom of the board and you will see four cells with a bright red tint in the same shape as your current piece, that isn't a coincidence. (Press [F] to Continue)", "看看棋盘底部，你会看到四个亮红色格子，形状与你当前方块相同。这可不是巧合。（按 [F] 继续）");
        Add(text, "This is the landing indicator and will help you see exactly where your current piece will lock in place. A piece will lock in place automatically when it come in contact with an obstacle, another locked unit piece, or when it reaches the bottom of the board. (Press [F] to Continue)", "这是落点提示，可帮助你看清当前方块会锁定在哪里。方块接触障碍、其他已锁定单位方块，或到达棋盘底部时，会自动锁定。（按 [F] 继续）");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar].", "接下来，请按 [空格键] 让当前方块快速落下。");
        Add(text, "Here you can keep track of level information such as any active level modifiers, the curent level number, how long you have been in the level, and the current gravity pulling your pieces down. (Press [F] to Continue)", "这里可以查看关卡信息，例如当前生效的关卡修改器、当前关卡数、已在关卡中停留的时间，以及拉动方块下落的当前重力。（按 [F] 继续）");
        Add(text, "Here you can see a preview of the next piece that will be spawned. Knowing what will come next will help you plan out your next step. (Press [F] to Continue)", "这里可以预览下一个生成的方块。提前知道下一步会出现什么，有助于规划行动。（按 [F] 继续）");
        Add(text, "Reserve Units - Loss Condition When any of your units die your current reserve will be reduced. You only have so many soldiers and when your reserve hits 0 you will lose. (Press [F] to Continue)", "预备单位 - 失败条件：任意单位死亡时，当前预备队会减少。你的兵力有限，当预备队降至 0 时你就会失败。（按 [F] 继续）");
        Add(text, "Loss Condition If a piece is locked in place above the top row of the board you will instantly lose, regardless of the number of reserve units left. Be caureful not to build to high. (Press [F] to Continue)", "失败条件：如果方块锁定在棋盘顶行以上，无论剩余多少预备单位，都会立刻失败。小心不要堆得太高。（按 [F] 继续）");
        Add(text, "Win Condition Reduce the Enemy Castle's HP to zero. (Press [F] to Continue)", "胜利条件：将敌方城堡生命值降至 0。（按 [F] 继续）");
        Add(text, "If you need a break press [Esc]. This will pause the game and bring up the pause menu.", "如果需要休息，按 [Esc]。这会暂停游戏并打开暂停菜单。");
        Add(text, "Here you can change settings, look through the help menu, check your current modifiers, or end your run early. For now, lets close the pause menu by pressing [Esc] again.", "在这里你可以更改设置、查看帮助菜单、检查当前修改器，或提前结束征战。现在再次按 [Esc] 关闭暂停菜单。");
        Add(text, "Now lets try filling an entire row on the board next to launch an attack. (Press [F] to Continue)", "现在试着填满棋盘上的一整行来发动攻击。（按 [F] 继续）");
    }

    static void AddCharacterText(Dictionary<string, string> text)
    {
        Add(text, "Charge!", "冲锋！");
        Add(text, "Send all units from the bottom 3 rows to attack immediatley, no need for fully formed lines. Damage based on the number of units cleared.", "立即派出底部 3 行的所有单位进攻，无需形成完整行。伤害取决于清除的单位数量。");
        Add(text, "Time Shift", "时间偏移");
        Add(text, "Reduce unit fall speed by 1/3 of its current speed fo 15 seconds.", "使单位下落速度降低为当前速度的 1/3，持续 15 秒。");
        Add(text, "Natures Embrace", "自然拥抱");
        Add(text, "Heal all units on the board back to full health including those that have died.", "将棋盘上所有单位治疗至满生命，包括已死亡单位。");
        Add(text, "Grock SMASH!", "格洛克粉碎！");
        Add(text, "Let loose a war cry doubling all units on the boards health and attack damage for 10 seconds.", "发出战吼，使棋盘上所有单位的生命和攻击伤害翻倍，持续 10 秒。");
        Add(text, "Immutable Bulwark", "不动壁垒");
        Add(text, "All units become immune to damage for 12 seconds.", "所有单位免疫伤害，持续 12 秒。");
    }

    static void AddHelpTopicText(Dictionary<string, string> text)
    {
        Add(text, "Boss Abilities", "Boss 能力");
        Add(text, "Floor Effects", "地面效果");
        Add(text, "Game Mechanics", "游戏机制");
        Add(text, "Obstacles", "障碍");
        Add(text, "Special Blocks", "特殊方块");
        Add(text, "Traps", "陷阱");
        Add(text, "Other", "其他");
        Add(text, "Full Board Blast", "全棋盘轰击");
        Add(text, "The boss will target all monster units set on the board dealing a small amount of damage to each one.", "Boss 会瞄准棋盘上所有怪物单位，并对每个单位造成少量伤害。");
        Add(text, "Increased Gravity", "重力增强");
        Add(text, "The boss will temporarily increase gravity causing blocks to fall significantly faster for a set period of time.", "Boss 会暂时增强重力，使方块在一段时间内明显更快下落。");
        Add(text, "Invulnerable", "无敌");
        Add(text, "The boss will temporarily become invulnerable. While invulnerable, the boss will take no damage from any sources.", "Boss 会暂时进入无敌状态。无敌期间，Boss 不会受到任何来源的伤害。");
        Add(text, "Lightning Strike", "雷击");
        Add(text, "The boss will target 1-3 individual cells with lightning bolts that will deal siginficant damage to any monster unit in that cell. Afterwards that cell will have a lightning floor effect that does continuous damage to any monster unit that occupies the tile.", "Boss 会用闪电瞄准 1-3 个单独格子，对格内怪物单位造成大量伤害。之后该格会留下雷电地面效果，对占据该格的怪物单位造成持续伤害。");
        Add(text, "Magic Explosive", "魔法炸弹");
        Add(text, "The boss will spawn a single magical explosive as low on the board as possible. This explosive will detonate after 15 seconds and can only be safley removed by clearing the row it occupies.", "Boss 会在棋盘尽可能低的位置生成一个魔法炸弹。炸弹会在 15 秒后爆炸，只能通过消除其所在行来安全移除。");
        Add(text, "Magic Shield", "魔法护盾");
        Add(text, "The boss will spawn multiple magical pylon obstacles on the board. As long as the pylons remain on the board the boss will take 50% reduced damage from all sources.", "Boss 会在棋盘上生成多个魔法塔障碍。只要魔法塔仍在棋盘上，Boss 受到的所有来源伤害都会降低 50%。");
        Add(text, "Spawn FE's", "生成地面效果");
        Add(text, "The boss will spawn multiple floor effects on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. Possible floor effect types are posion, fire, and lightning.", "Boss 会在棋盘上生成多个地面效果。它们可能零散出现，也可能以 2x2、1x4 或 4x1 图案出现。可能的地面效果包括毒、火和雷电。");
        Add(text, "Spawn Obstacles", "生成障碍");
        Add(text, "The boss will spawn multiple obstacles on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns stone obstacles.", "Boss 会在棋盘上生成多个障碍。它们可能零散出现，也可能以 2x2、1x4 或 4x1 图案出现。此能力只会生成石块障碍。");
        Add(text, "Spawn Traps", "生成陷阱");
        Add(text, "The boss will spawn multiple traps on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns spike traps.", "Boss 会在棋盘上生成多个陷阱。它们可能零散出现，也可能以 2x2、1x4 或 4x1 图案出现。此能力只会生成尖刺陷阱。");
        Add(text, "Row Blast", "行轰击");
        Add(text, "The boss will target the top three rows the player has set monster units. All monster units in the selected rows will recieve moderate damage from this attack.", "Boss 会瞄准玩家已放置怪物单位的最上方三行。选中行中的所有怪物单位都会受到中等伤害。");
        Add(text, "Activate Special", "激活特殊能力");
        Add(text, "R - Activates the players special ability when the special gauge is charged to 100%", "R - 当特殊能量充满至 100% 时激活玩家特殊能力");
        Add(text, "Movement", "移动");
        Add(text, "A - shifts the active piece one column to the left. S - shifts the active piece one row down. D - shifts the active piece one column to the right.", "A - 将当前方块左移一列。S - 将当前方块下移一行。D - 将当前方块右移一列。");
        Add(text, "Pause", "暂停");
        Add(text, "Escape - Will open the pause menu and pause all gameplay functions. Presseing escape while the pause menu is open will close the pause menu and resume gameplay.", "Escape - 打开暂停菜单并暂停所有游戏功能。暂停菜单打开时再次按 Escape 会关闭菜单并恢复游戏。");
        Add(text, "Quick Drop", "快速下落");
        Add(text, "Pressing spacebar will quick drop your active piece, setting it in place immediatley.", "按空格键会让当前方块快速下落并立即放置。");
        Add(text, "Rotation", "旋转");
        Add(text, "Q - Rotate the active piece 90 degrees counter-clockwise. E - Rotate the active piece 90 degrees clockwise.", "Q - 将当前方块逆时针旋转 90 度。E - 将当前方块顺时针旋转 90 度。");
        Add(text, "Fire", "火焰");
        Add(text, "Any monster unit set in a cell with the fire floor effect will take constant damage. The damage is realtivley low but occurs often.", "任何位于火焰地面效果格中的怪物单位都会持续受到伤害。伤害较低，但触发频繁。");
        Add(text, "Lightning", "雷电");
        Add(text, "Any monster unit set in a cell with the lightning floor effect will take constant damage. The damage is moderate but the floor effect will disappears after a period of time.", "任何位于雷电地面效果格中的怪物单位都会持续受到伤害。伤害中等，但地面效果会在一段时间后消失。");
        Add(text, "Poison", "毒素");
        Add(text, "Any monster unit set in a cell with the poison floor effect will take continuous damage. The damage is realtivley low but occurs often.", "任何位于毒素地面效果格中的怪物单位都会持续受到伤害。伤害较低，但触发频繁。");
        Add(text, "Attack Units", "攻击单位");
        Add(text, "Attack units tend to have a higher attack stat than other unit types. They are best used to deal maximum damage to enemies to end levels more quickly. They cannot heal and tend to have average health stats.", "攻击单位通常比其他单位拥有更高攻击力。它们最适合对敌人造成最大伤害，从而更快结束关卡。它们无法治疗，生命值通常普通。");
        Add(text, "Currency", "金币");
        Add(text, "The player can gain currency from completing levels and rarely from clearing rows. Currency can be used to purchase various cosmetics, monster units, player characters, and permanent buffs to improve future runs. Your current currency can be found in the top right of the screen.", "玩家可以通过完成关卡获得金币，消除行时也有小概率获得金币。金币可用于购买各种外观、怪物单位、玩家角色和永久增益，以改善未来征战。当前金币显示在屏幕右上角。");
        Add(text, "Enemy Attack", "敌人攻击");
        Add(text, "The enemy castle will send constant attacks at the players monster units in an attempt to cull them. The enemies attacks will increase in power and frequency at higher levels.", "敌方城堡会不断攻击玩家的怪物单位，试图削减你的兵力。关卡越高，敌人的攻击威力和频率越高。");
        Add(text, "Healing Units", "治疗单位");
        Add(text, "Some units can heal other monster units to varying degrees. They tend to have much lower health and attack stats.", "某些单位可以不同程度地治疗其他怪物单位。它们通常生命和攻击较低。");
        Add(text, "Landing Hint", "落点提示");
        Add(text, "A red tint overlay appears in the location where the current active piece will fall.", "红色半透明提示会显示当前活动方块将落下的位置。");
        Add(text, "Loss Condition 1", "失败条件 1");
        Add(text, "If the unit reserve reaches 0, the run will end with your loss! The unit reserve is reduced by one for every monster unit that dies on the board.", "如果单位预备队降至 0，本次征战将以失败结束！棋盘上每有一个怪物单位死亡，单位预备队就会减少 1。");
        Add(text, "Loss Condition 2", "失败条件 2");
        Add(text, "If a piece is set above the top row of the grid, the run will end with a loss!", "如果方块被放置在网格顶行以上，本次征战将失败！");
        Add(text, "Monster Units", "怪物单位");
        Add(text, "Choose from multiple units to make up your warband. Each unit has their own individual stats that will make them more suitable for different roles. Some units have balanced stats and others are more specialized.", "从多个单位中选择，组成你的战团。每个单位都有独立属性，更适合不同职责。有些单位属性均衡，有些则更专精。");
        Add(text, "Commander", "指挥官");
        Add(text, "Each Commander has their own unquie special ability that can be used in battle. New Commanders can be unlocked and set from the main menu.", "每位指挥官都有可在战斗中使用的独特特殊能力。新的指挥官可在主菜单中解锁并设置。");
        Add(text, "Row Clear", "消行");
        Add(text, "Fill each grid cell in a row to launch an attack. Monster Units and Obstacles count as a filled cell. Floor effects and traps do not count as a filled cell. Monster units in the cleared row contribute their attack stats for damage calculation and partially fill the player's special gauge.", "填满一行中的每个网格即可发动攻击。怪物单位和障碍算作已填充格；地面效果和陷阱不算。被消除行中的怪物单位会将其攻击属性计入伤害计算，并为玩家特殊能量充能。");
        Add(text, "Run Buffs", "征战增益");
        Add(text, "After succesfully completing a level you will be given three random buffs to choose from to enhance your current run. All buff modifiers will be reset when the run ends.", "成功完成关卡后，你将从三个随机增益中选择一个来强化当前征战。所有增益修改器会在征战结束时重置。");
        Add(text, "Run Debuffs", "征战减益");
        Add(text, "After succesfully completing a level you will be given three random debuffs to choose from to increase the difficulty of your current run. All debuff modifiers will be reset when the run ends.", "成功完成关卡后，你将从三个随机减益中选择一个来提高当前征战难度。所有减益修改器会在征战结束时重置。");
        Add(text, "Clearing rows will earn the player points and add to their overall score. This can be used to measure the success of a run and compete with other players.", "消除行会获得分数并计入总分。分数可用来衡量征战表现，也可与其他玩家竞争。");
        Add(text, "Shop Buffs", "商店增益");
        Add(text, "The shop offers different buffs that can be purchased. Each buff is permanent and can be purchased multiple times. Each purchase will increase the price of the buffs next purchase level.", "商店提供多种可购买增益。每个增益都是永久的，并可多次购买。每次购买都会提高该增益下一等级的价格。");
        Add(text, "Special Gauge", "特殊能量");
        Add(text, "Fills with every row cleared. When it reaches 100% you can use your Commander's unique special ability, resetting the special gauge back 0%.", "每次消行都会填充。当达到 100% 时，可使用指挥官的独特特殊能力，并将特殊能量重置为 0%。");
        Add(text, "Tank Units", "坦克单位");
        Add(text, "Tank units tend to have significantly more health than other unit types. They are best used to outlast enemy attacks and protect weaker units. They cannot heal and tend to have low attack stats.", "坦克单位通常比其他单位拥有显著更高生命值。它们最适合承受敌人攻击并保护较弱单位。它们无法治疗，攻击通常较低。");
        Add(text, "Unit Death", "单位死亡");
        Add(text, "When a monster units health drops to zero it dies. Dead units do not contribute their stats to an attack when their row is cleared or help fill up the players Special Gauge.", "当怪物单位生命降至 0 时会死亡。死亡单位所在行被消除时不会提供攻击属性，也不会帮助填充玩家特殊能量。");
        Add(text, "When starting a new run the player will have a set limit of how many units they can afford to lose throughout the run. When a unit dies the reserve bar will be decreased. Suuccesfully completing a level will award the player up to 5 reinforcements for each victory up to the max unit reserve.", "开始新征战时，玩家会有一个整场征战可承受损失的单位上限。单位死亡时预备队条会减少。成功完成关卡会为每次胜利奖励最多 5 个援军，直到达到预备队上限。");
        Add(text, "Victory Condition", "胜利条件");
        Add(text, "Reduce the health of the enemy castle by clearing rows. When the enemy castle reaches 0 health, you win!", "通过消行削减敌方城堡生命。当敌方城堡生命降至 0 时，你获胜！");
        Add(text, "Explosive", "爆炸物");
        Add(text, "The explosive obstacle will explode after a period of time killing all surrounding monster units. Can be safley disposed of by clearing its row. When safley disposed of it add 25 damage to that row clears attack. CAUTION: Using a bomb or lightning special block on the explosive will cause it to detonate.", "爆炸物障碍会在一段时间后爆炸，杀死周围所有怪物单位。可通过消除其所在行安全处理。安全处理时会为该行攻击增加 25 点伤害。注意：对爆炸物使用炸弹或雷电特殊方块会使其引爆。");
        Add(text, "Magic Pylon", "魔法塔");
        Add(text, "When magic pylon obstacles are on the board enemies will take 50% reduced damage from all sources. Magic pylon can bee destroyed by clearing the row they occupy or using bomb and lightning special blocks.", "棋盘上存在魔法塔障碍时，敌人受到的所有来源伤害降低 50%。魔法塔可通过消除其所在行，或使用炸弹和雷电特殊方块摧毁。");
        Add(text, "Stone", "石块");
        Add(text, "Stone obstacles can be spawned at the beggining of a level or by the boss. A lightning special block or clearing a row containing a stone obstacle will deal one damage to it. Stone obstacle need to be damaged 3 times to be removed. Exception: Using a bomb special block will instantly destroy a stone obstacle.", "石块障碍可能在关卡开始时或由 Boss 生成。雷电特殊方块或消除包含石块障碍的行会对其造成 1 点伤害。石块障碍需要受到 3 次伤害才会移除。例外：使用炸弹特殊方块会立刻摧毁石块障碍。");
        Add(text, "Bomb", "炸弹");
        Add(text, "The bomb special will detonate immediatley when set. All blocks in the surrounding tiles of its blast will be destroyed!", "炸弹特殊方块放置后会立即引爆！爆炸范围周围格子中的所有方块都会被摧毁。");
        Add(text, "Death", "死亡");
        Add(text, "The Death special will activate immediatley when set on top of a monster unit. All monster units of the same type will be safley removed from the board with out decreasing your unit reserve.", "死亡特殊方块放置在怪物单位上时会立即激活。所有同类型怪物单位都会从棋盘安全移除，且不会减少单位预备队。");
        Add(text, "Earthquake", "地震");
        Add(text, "The Earthquake special will activate immediatley when set. All blocks on the board will be dropped if not being supported by another tile beneath them. This effects obstacles as well that may otherwise not be able to be moved by other means.", "地震特殊方块放置后会立即激活。棋盘上所有下方没有其他格子支撑的方块都会下落。这也会影响原本无法用其他方式移动的障碍。");
        Add(text, "Lightning Bolt", "闪电箭");
        Add(text, "The Lightning Bolt special will activate immediatley when set. All monster units and traps will be destroyed in that column. Stone obstacles will take partial damage if they are in the affected area.", "闪电箭特殊方块放置后会立即激活。该列中的所有怪物单位和陷阱都会被摧毁。范围内的石块障碍会受到部分伤害。");
        Add(text, "Slow Gravity", "缓慢重力");
        Add(text, "The Slow Gravity special block will activate immedialtey upon being set. It will significantly reduce the speed at which pieces fall and the how quickly gravity increases over time.", "缓慢重力特殊方块放置后会立即激活。它会显著降低方块下落速度，以及重力随时间增强的速度。");
        Add(text, "Spike Trap", "尖刺陷阱");
        Add(text, "Spike traps will deal a high amount of damage to any monster unit that is set on the trapped cell when they are placed. The only way to destroyed spike traps are with the lightning special block.", "尖刺陷阱会对放置在陷阱格上的怪物单位造成大量伤害。摧毁尖刺陷阱的唯一方式是使用雷电特殊方块。");
    }

    static void AddLevelModifierText(Dictionary<string, string> text)
    {
        Add(text, "Spin to Win", "旋转制胜");
        Add(text, "Active pieces will continuously rotate until set in place. Manual rotation will be locked.", "活动方块会持续旋转，直到放置完成。手动旋转将被锁定。");
        Add(text, "Timing is Everything", "时机就是一切");
        Add(text, "Active pieces will continuously shift horizontally back and and forth across the board. Manual shifting will be locked.", "活动方块会在棋盘上持续左右横移。手动横移将被锁定。");
        Add(text, "Go Big or Go Home", "不成大事便回家");
        Add(text, "Damage is significantly reduced for all attacks when the combo streak is less than 3.", "连击数低于 3 时，所有攻击伤害大幅降低。");
        Add(text, "Break Out The Big Guns", "亮出重炮");
        Add(text, "The enemy has fortified their position. A combo attack at 4 or higher will be required to remove each enemy shield. Damage dealt to the enemy while shielded is significantly reduced.", "敌人已加固阵地。每个敌方护盾都需要 4 连击或更高的攻击才能移除。敌人有护盾时受到的伤害大幅降低。");
        Add(text, "Contagion Outbreak", "传染爆发");
        Add(text, "Disease has begun to spread through the ranks. Close proximitiy has a chance to transfer from afflicted units to healthy units and will spread with certainity on the death of infected units.", "疾病开始在队伍中传播。近距离接触有概率从患病单位传播给健康单位，感染单位死亡时必定传播。");
        Add(text, "Double Down", "孤注一掷");
        Add(text, "All damage taken and dealt will be doubled!", "所有受到和造成的伤害都会翻倍！");
        Add(text, "Exploding Corpses", "爆炸尸体");
        Add(text, "Units will explode on death dealing damage to all surrounding units. Damage dealt is a percentage based off of the max health of the exploding unit.", "单位死亡时会爆炸，对周围所有单位造成伤害。伤害基于爆炸单位最大生命的一定比例。");
        Add(text, "Tis A Flesh Wound", "不过是皮肉伤");
        Add(text, "All ally units will start with half health.", "所有友方单位开局只有一半生命。");
        Add(text, "Rations Running Low", "口粮告急");
        Add(text, "Rations have begun to run out. Ally units have begun to starve trying to share the remaining rations. Units will take continuous damage proportional to the number of current reserve units.", "口粮开始耗尽。友方单位为了分享剩余口粮而挨饿。单位会持续受到伤害，伤害与当前预备单位数量成比例。");
        Add(text, "Overgrowth", "蔓生");
        Add(text, "Overgrowth has taken over the area consuming tiles and monsters. Overgrowth becomes more resilent to destruction once fully grown. Defeat the enemy before your army becomes mulch!", "蔓生吞噬了这片区域的格子和怪物。完全生长后，蔓生会更难摧毁。在你的军队变成肥料前击败敌人！");
        Add(text, "No Retreat", "无路可退");
        Add(text, "Enemy ambush will cut off any retreat. Rows will slowly fill with enemy units progressivley limiting space to maneuver.", "敌人的伏击切断了退路。行会逐渐被敌方单位填满，慢慢压缩可操作空间。");
        Add(text, "Soul Link", "灵魂链接");
        Add(text, "All four units in a piece share a single health pool.", "一个方块中的四个单位共享同一个生命池。");
        Add(text, "Back to the Basics", "回归基础");
        Add(text, "Special blocks will not spawn.", "不会生成特殊方块。");
        Add(text, "Commander Special Lock", "指挥官特殊能力锁定");
        Add(text, "Special ability gauge will be set to zero and locked.", "特殊能力槽会被设为 0 并锁定。");
        Add(text, "Catastrophic Storm", "灾厄风暴");
        Add(text, "An unrelenting storm has arrived and will blast the area with devestating lightning strikes.", "无情风暴已经降临，将用毁灭性的雷击轰击这片区域。");
        Add(text, "Miasma Marsh", "瘴气沼泽");
        Add(text, "The battlefield has shifted to the nearby marshes where deadly miasma drifts across the terrain.", "战场转移到了附近沼泽，致命瘴气在地形中飘荡。");
    }

    static void AddRunModifierNames(Dictionary<string, string> text)
    {
        Add(text, "All Special Gain Down", "总特殊能量获取下降");
        Add(text, "All Special Gain Up", "总特殊能量获取上升");
        Add(text, "ATK Down", "攻击下降");
        Add(text, "ATK Up", "攻击上升");
        Add(text, "Currency Drop Up", "金币掉落上升");
        Add(text, "Debuffs Only", "仅减益");
        Add(text, "Enemy ATK Down", "敌人攻击下降");
        Add(text, "Enemy ATK SPD Down", "敌人攻速下降");
        Add(text, "Enemy ATK SPD Up", "敌人攻速上升");
        Add(text, "Enemy ATK Up", "敌人攻击上升");
        Add(text, "Enemy HP Up", "敌人生命上升");
        Add(text, "Gravity Accel SPD Down", "重力加速度下降");
        Add(text, "Gravity Accel SPD Up", "重力加速度上升");
        Add(text, "Gravity Base SPD Down", "基础重力速度下降");
        Add(text, "Gravity SPD Up", "重力速度上升");
        Add(text, "Healing Range Up", "治疗范围上升");
        Add(text, "Healing STR Up", "治疗强度上升");
        Add(text, "HP Down", "生命下降");
        Add(text, "HP Up", "生命上升");
        Add(text, "Luck Up", "幸运上升");
        Add(text, "Misfortune Up", "厄运上升");
        Add(text, "No Landing Indicator", "无落点提示");
        Add(text, "No Next Block Preview", "无下个方块预览");
        Add(text, "No Reinforcements", "无援军");
        Add(text, "Reinforcements Down", "援军下降");
        Add(text, "Reinforcements Up", "援军上升");
        Add(text, "Special Block Down", "特殊方块下降");
        Add(text, "Special Block Up", "特殊方块上升");
        Add(text, "Special Gain Stat Down", "怪物特殊能量获取下降");
        Add(text, "Special Gauge Stat Up", "怪物特殊能量获取上升");
        Add(text, "Stone Buff Drop Down", "石块增益掉落下降");
        Add(text, "Stone Buff Drop Up", "石块增益掉落上升");
        Add(text, "Unit Reserve Down", "单位预备队下降");
        Add(text, "Unit Reserve Up", "单位预备队上升");
        Add(text, "Win Currency Down", "胜利金币下降");
        Add(text, "Win Currency Up", "胜利金币上升");
    }

    static void AddRunModifierFixedDescriptions(Dictionary<string, string> text)
    {
        Add(text, "A red tinted outline will no longer be shown where your pieces will land.", "不再显示方块落点位置的红色轮廓。");
        Add(text, "The next block will no longer be shown.", "不再显示下一个方块。");
        Add(text, "Reinforcements will no longer arrive after winning a round.", "回合获胜后不再获得援军。");
        Add(text, "Stone obstacles no longer have a chance of dropping buffs and now only drop debuffs. Debuff drop chance is the same as prior buff drop chance.", "石块障碍不再有概率掉落增益，现在只会掉落减益。减益掉落概率与此前增益掉落概率相同。");
        Add(text, "Double the amount of currency gained occasionally when clearing lines.", "消行时偶尔获得双倍金币。");
        Add(text, "Triple the amount of currency gained occasionally when clearing lines.", "消行时偶尔获得三倍金币。");
        Add(text, "Qunituple the amount of currency gained occasionally when clearing lines.", "消行时偶尔获得五倍金币。");
        Add(text, "Increases the healing range of all friendly monsters by 1.", "所有友方怪物治疗范围 +1。");
        Add(text, "Increases the healing range of all friendly monsters by 2.", "所有友方怪物治疗范围 +2。");
        Add(text, "Increases the healing range of all friendly monsters by 3.", "所有友方怪物治疗范围 +3。");
    }

    static void AddStatText(Dictionary<string, string> text)
    {
        Add(text, "Lines Cleared:", "消除行数：");
        Add(text, "Special Used:", "使用特殊能力：");
        Add(text, "Obstacles Destroyed:", "摧毁障碍：");
        Add(text, "Highest Combo:", "最高连击：");
        Add(text, "Highest Single Attack:", "最高单次攻击：");
        Add(text, "Units Died:", "单位死亡：");
        Add(text, "Units Healed:", "单位治疗：");
        Add(text, "Total Damage Dealt:", "造成总伤害：");
        Add(text, "Clear Time:", "通关时间：");
        Add(text, "Final Score:", "最终分数：");
        Add(text, "Lines", "行");
        Add(text, "Times", "次");
        Add(text, "Obstacles", "障碍");
        Add(text, "Damage", "伤害");
        Add(text, "Units", "单位");
        Add(text, "Health", "生命");
        Add(text, "Level {0}", "关卡 {0}");
        Add(text, "{0} of {1} {2} discovered. Total codex unlocked {3}%", "已发现 {1} 个{2}中的 {0} 个。图鉴总解锁率 {3}%");
        Add(text, "Level Modifiers", "关卡修改器");
    }

    static bool TryTranslateRunModifierDescription(string lookupKey, out string chineseText)
    {
        chineseText = null;

        for (int i = 0; i < DegreePrefixes.Length; i++)
        {
            string englishPrefix = DegreePrefixes[i].English;
            if (!lookupKey.StartsWith(englishPrefix + " ", StringComparison.OrdinalIgnoreCase))
                continue;

            string remainder = lookupKey.Substring(englishPrefix.Length + 1).Trim();
            if (TryGetRunModifierTemplate(remainder, out string template))
            {
                chineseText = string.Format(template, DegreePrefixes[i].Chinese);
                return true;
            }
        }

        return false;
    }

    static bool TryGetRunModifierTemplate(string remainder, out string template)
    {
        if (RunModifierTemplates.TryGetValue(remainder, out template))
            return true;

        string normalized = remainder
            .Replace("winning the round", "winning a round")
            .Replace(" from a stone obstacle upon destruction.", " from a stone obstacle.");

        return !string.Equals(normalized, remainder, StringComparison.OrdinalIgnoreCase)
            && RunModifierTemplates.TryGetValue(normalized, out template);
    }

    static bool TryTranslateLabelValueLines(string englishText, out string chineseText)
    {
        chineseText = null;

        string normalizedNewlines = englishText.Replace("\r\n", "\n").Replace('\r', '\n');
        if (string.IsNullOrWhiteSpace(normalizedNewlines))
            return false;

        string[] lines = normalizedNewlines.Split('\n');
        bool changed = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (TryTranslateColonLine(lines[i], out string translatedLine))
            {
                lines[i] = translatedLine;
                changed = true;
                continue;
            }

            string lookup = NormalizeLookupKey(lines[i]);
            if (!string.IsNullOrEmpty(lookup) && ExactText.TryGetValue(lookup, out translatedLine))
            {
                lines[i] = translatedLine;
                changed = true;
                continue;
            }

            if (TryTranslateLinePrefix(lines[i], out translatedLine))
            {
                lines[i] = translatedLine;
                changed = true;
            }
        }

        if (!changed)
            return false;

        chineseText = string.Join("\n", lines);
        return true;
    }

    static bool TryTranslateColonLine(string line, out string translatedLine)
    {
        translatedLine = null;

        int colonIndex = line.IndexOf(':');
        if (colonIndex <= 0)
            return false;

        string label = line.Substring(0, colonIndex).Trim();
        if (!ExactText.TryGetValue(NormalizeLookupKey(label), out string translatedLabel))
            return false;

        string value = line.Substring(colonIndex + 1).TrimStart();
        if (!string.IsNullOrEmpty(value) && ExactText.TryGetValue(NormalizeLookupKey(value), out string translatedValue))
            value = translatedValue;

        translatedLine = $"{translatedLabel}：{value}";
        return true;
    }

    static bool TryTranslateLinePrefix(string line, out string translatedLine)
    {
        translatedLine = null;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        string trimmed = line.TrimStart();
        string leading = line.Substring(0, line.Length - trimmed.Length);

        for (int i = 0; i < LinePrefixes.Length; i++)
        {
            string englishPrefix = LinePrefixes[i].English;
            if (!trimmed.StartsWith(englishPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (trimmed.Length > englishPrefix.Length && char.IsLetterOrDigit(trimmed[englishPrefix.Length]))
                continue;

            translatedLine = leading + LinePrefixes[i].Chinese + trimmed.Substring(englishPrefix.Length);
            return true;
        }

        return false;
    }

    static void Add(Dictionary<string, string> text, string english, string chinese)
    {
        string key = NormalizeLookupKey(english);
        if (!string.IsNullOrEmpty(key))
            text[key] = chinese;
    }

    static string NormalizeLookupKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        bool previousWhitespace = false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsWhiteSpace(c))
            {
                if (!previousWhitespace)
                    builder.Append(' ');

                previousWhitespace = true;
                continue;
            }

            if ((c == '\'' || c == '"') && builder.Length == 0)
                continue;

            builder.Append(c);
            previousWhitespace = false;
        }

        return builder.ToString().Trim();
    }
}

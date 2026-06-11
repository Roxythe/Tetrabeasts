using System;
using System.Collections.Generic;
using System.Text;

public static class TetrabeastsRussianTranslations
{
    static readonly Dictionary<string, string> ExactText = BuildExactText();

    static readonly Dictionary<string, string> RunModifierTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["decrease the amount of special gained from all sources."] = "{0} снижает количество особой энергии, получаемой из всех источников.",
        ["decreases the amount of special gained from all sources."] = "{0} снижает количество особой энергии, получаемой из всех источников.",
        ["increase the amount of special gained from all sources."] = "{0} повышает количество особой энергии, получаемой из всех источников.",
        ["increases the amount of special gained from all sources."] = "{0} повышает количество особой энергии, получаемой из всех источников.",
        ["decrease the special gained from each monster."] = "{0} снижает особую энергию, получаемую от каждого монстра.",
        ["decreases the special gained from each monster."] = "{0} снижает особую энергию, получаемую от каждого монстра.",
        ["increase the special gained from each monster."] = "{0} повышает особую энергию, получаемую от каждого монстра.",
        ["increases the special gained from each monster."] = "{0} повышает особую энергию, получаемую от каждого монстра.",
        ["decreases the attack value for all monsters in your current roster."] = "{0} снижает атаку всех монстров в текущем отряде.",
        ["decrease the attack value for all monsters in your current roster."] = "{0} снижает атаку всех монстров в текущем отряде.",
        ["increases the attack value for all monsters in your current roster."] = "{0} повышает атаку всех монстров в текущем отряде.",
        ["increase the attack value for all monsters in your current roster."] = "{0} повышает атаку всех монстров в текущем отряде.",
        ["decrease the damage of enemy projectiles."] = "{0} снижает урон вражеских снарядов.",
        ["decreases the damage of enemy projectiles."] = "{0} снижает урон вражеских снарядов.",
        ["incecrease the damage of enemy projectiles."] = "{0} повышает урон вражеских снарядов.",
        ["incecreases the damage of enemy projectiles."] = "{0} повышает урон вражеских снарядов.",
        ["increase the damage of enemy projectiles."] = "{0} повышает урон вражеских снарядов.",
        ["increases the damage of enemy projectiles."] = "{0} повышает урон вражеских снарядов.",
        ["increases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} увеличивает время между атаками врага, не считая перезарядку умений босса.",
        ["increase the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} увеличивает время между атаками врага, не считая перезарядку умений босса.",
        ["decreases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} уменьшает время между атаками врага, не считая перезарядку умений босса.",
        ["decrease the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} уменьшает время между атаками врага, не считая перезарядку умений босса.",
        ["increase the hit points of all future enemy fortifications."] = "{0} повышает здоровье всех будущих вражеских укреплений.",
        ["increases the hit points of all future enemy fortifications."] = "{0} повышает здоровье всех будущих вражеских укреплений.",
        ["decreases the rate falling speed builds up over time for blocks."] = "{0} снижает скорость роста падения фигур со временем.",
        ["decrease the rate falling speed builds up over time for blocks."] = "{0} снижает скорость роста падения фигур со временем.",
        ["increases the rate falling speed builds up over time for blocks."] = "{0} повышает скорость роста падения фигур со временем.",
        ["increase the rate falling speed builds up over time for blocks."] = "{0} повышает скорость роста падения фигур со временем.",
        ["decreases the initial falling speed of blocks."] = "{0} снижает начальную скорость падения фигур.",
        ["decrease the initial falling speed of blocks."] = "{0} снижает начальную скорость падения фигур.",
        ["increases the initial falling speed of blocks."] = "{0} повышает начальную скорость падения фигур.",
        ["increase the initial falling speed of blocks."] = "{0} повышает начальную скорость падения фигур.",
        ["increase the healing power of all friendly monsters."] = "{0} повышает силу лечения всех союзных монстров.",
        ["increases the healing power of all friendly monsters."] = "{0} повышает силу лечения всех союзных монстров.",
        ["decrease friendly monster pieces maximum hit points."] = "{0} снижает максимальное здоровье фигур союзных монстров.",
        ["decreases friendly monster pieces maximum hit points."] = "{0} снижает максимальное здоровье фигур союзных монстров.",
        ["increase friendly monster pieces maximum hit points."] = "{0} повышает максимальное здоровье фигур союзных монстров.",
        ["increases friendly monster pieces maximum hit points."] = "{0} повышает максимальное здоровье фигур союзных монстров.",
        ["increase luck raising the chance of getting higher rarity buffs."] = "{0} повышает удачу, увеличивая шанс получить усиления более высокой редкости.",
        ["increases luck raising the chance of getting higher rarity buffs."] = "{0} повышает удачу, увеличивая шанс получить усиления более высокой редкости.",
        ["increases the likelihood of finding higher rarity debuffs."] = "{0} повышает шанс найти ослабления более высокой редкости.",
        ["increase the likelihood of finding higher rarity debuffs."] = "{0} повышает шанс найти ослабления более высокой редкости.",
        ["decrease the number of reinforcement units added after winning a round."] = "{0} снижает количество подкреплений после победы в раунде.",
        ["decreases the number of reinforcement units added after winning a round."] = "{0} снижает количество подкреплений после победы в раунде.",
        ["increase the number of reinforcement units added after winning a round."] = "{0} повышает количество подкреплений после победы в раунде.",
        ["increases the number of reinforcement units added after winning a round."] = "{0} повышает количество подкреплений после победы в раунде.",
        ["decreases the chance of special blocks appearing."] = "{0} снижает шанс появления особых блоков.",
        ["decrease the chance of special blocks appearing."] = "{0} снижает шанс появления особых блоков.",
        ["incecreases the chance of special blocks appearing."] = "{0} повышает шанс появления особых блоков.",
        ["incecrease the chance of special blocks appearing."] = "{0} повышает шанс появления особых блоков.",
        ["increases the chance of special blocks appearing."] = "{0} повышает шанс появления особых блоков.",
        ["increase the chance of special blocks appearing."] = "{0} повышает шанс появления особых блоков.",
        ["decrease the chance a buff will drop from a stone obstacle."] = "{0} снижает шанс выпадения усиления из каменного препятствия.",
        ["decreases the chance a buff will drop from a stone obstacle."] = "{0} снижает шанс выпадения усиления из каменного препятствия.",
        ["increase the chance a buff will drop from a stone obstacle."] = "{0} повышает шанс выпадения усиления из каменного препятствия.",
        ["increases the chance a buff will drop from a stone obstacle."] = "{0} повышает шанс выпадения усиления из каменного препятствия.",
        ["decrease the maximum limit of the unit reserve."] = "{0} снижает максимальный лимит резерва отряда.",
        ["decreases the maximum limit of the unit reserve."] = "{0} снижает максимальный лимит резерва отряда.",
        ["increase the maximum limit of the unit reserve."] = "{0} повышает максимальный лимит резерва отряда.",
        ["increases the maximum limit of the unit reserve."] = "{0} повышает максимальный лимит резерва отряда.",
        ["reduces the amount of currency the player gains after winning a round."] = "{0} снижает количество золота, получаемого игроком после победы в раунде.",
        ["reduce the amount of currency the player gains after winning a round."] = "{0} снижает количество золота, получаемого игроком после победы в раунде.",
        ["increases the amount of currency the player gains after winning a round."] = "{0} повышает количество золота, получаемого игроком после победы в раунде.",
        ["increase the amount of currency the player gains after winning a round."] = "{0} повышает количество золота, получаемого игроком после победы в раунде.",
        ["increase the chance currency will be earned when clearing lines."] = "{0} повышает шанс получить золото при очистке линий.",
        ["increases the chance currency will be earned when clearing lines."] = "{0} повышает шанс получить золото при очистке линий."
    };

    static readonly (string English, string Russian)[] DegreePrefixes =
    {
        ("Slightly", "Немного"),
        ("Modestly", "Умеренно"),
        ("Moderatley", "Средне"),
        ("Moderately", "Средне"),
        ("Significantly", "Значительно"),
        ("Massivley", "Сильно"),
        ("Massively", "Сильно")
    };

    static readonly (string English, string Russian)[] LinePrefixes =
    {
        ("Special Gauge Gain", "Получение особой энергии"),
        ("Enemy Damage", "Урон врага"),
        ("Enemy HP", "ОЗ врага"),
        ("Score Gain", "Получение очков"),
        ("EXP Gain", "Получение опыта"),
        ("Misfortune", "Невезение"),
        ("Gravity", "Гравитация"),
        ("Score", "Очки"),
        ("Level", "Уровень"),
        ("Reset", "Сброс")
    };

    public static bool TryGetText(string englishText, out string russianText)
    {
        russianText = null;

        if (string.IsNullOrWhiteSpace(englishText))
            return false;

        string lookupKey = NormalizeLookupKey(englishText);
        if (ExactText.TryGetValue(lookupKey, out russianText))
            return true;

        if (TryTranslateRunModifierDescription(lookupKey, out russianText))
            return true;

        if (TryTranslateLabelValueLines(englishText, out russianText))
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

    static void AddTopLevelText(Dictionary<string, string> text)
    {
        Add(text, "OK", "ОК");
        Add(text, "Cancel", "Отмена");
        Add(text, "Continue", "Продолжить");
        Add(text, "Confirm", "Подтвердить");
        Add(text, "Close", "Закрыть");
        Add(text, "Start", "Начать");
        Add(text, "PAUSED", "ПАУЗА");
        Add(text, "Resume", "Продолжить");
        Add(text, "Main Menu", "Главное меню");
        Add(text, "Restart", "Начать заново");
        Add(text, "Save & Quit", "Сохранить и выйти");
        Add(text, "Quit", "Выйти");
        Add(text, "New Game", "Новая игра");
        Add(text, "Shop", "Магазин");
        Add(text, "Codex", "Кодекс");
        Add(text, "Help", "Помощь");
        Add(text, "HighScore", "Рекорд");
        Add(text, "Select Monsters", "Выбрать монстров");
        Add(text, "Select Commander", "Выбрать командира");
        Add(text, "Tip: Special blocks activate as soon as they are placed.", "Совет: специальные блоки активируются сразу после размещения.");
        Add(text, "Tip: Rerolls can be saved and used on future reward screens during the same run.", "Совет: перебросы можно сохранять и использовать на будущих экранах наград в том же забеге.");
        Add(text, "Tip: Temporary monster copies earn EXP during a run, and some of it becomes permanent after the run ends.", "Совет: временные копии монстров получают опыт во время забега, и часть его становится постоянной после завершения забега.");
        Add(text, "Tip: Full rows launch attacks at the enemy castle.", "Совет: заполненные ряды запускают атаки по вражескому замку.");
        Add(text, "Tip: Keep an eye on your unit reserve. If it reaches 0, the run is over.", "Совет: следите за резервом отрядов. Если он достигнет 0, забег закончится.");
        Add(text, "Tip: Level modifiers stack with your run buffs and debuffs.", "Совет: модификаторы уровня складываются с усилениями и ослаблениями вашего забега.");
        Add(text, "Combat Log", "Журнал боя");
        Add(text, "{0} takes {1} damage.", "{0} получает {1} урона.");
        Add(text, "{0} heals {1}.", "{0} восстанавливает {1}.");
        Add(text, "{0} dies.", "{0} погибает.");
        Add(text, "{0} uses {1}.", "{0} использует {1}.");
        Add(text, "{0} casts {1}.", "{0} применяет {1}.");
        Add(text, "{0} took {1}{2} damage{3}.", "{0} получил {1}{2} урона{3}.");
        Add(text, "{0} restored {1} health for {2}.", "{0} восстановил {1} здоровья для {2}.");
        Add(text, "{0} dealt {1} damage to {2}.{3}", "{0} нанес {1} урона {2}.{3}");
        Add(text, " from {0}", " от {0}");
        Add(text, "(shielded)", "(под щитом)");
        Add(text, "poison", "яд");
        Add(text, "fire", "огонь");
        Add(text, "lightning", "молния");
        Add(text, "contagion", "заражение");
        Add(text, "starvation", "голод");
        Add(text, "burst", "взрыв");
        Add(text, "floor effect", "эффект пола");
        Add(text, "storm", "буря");
        Add(text, "infection", "инфекция");
        Add(text, "low rations", "нехватка пайков");
        Add(text, "death burst", "предсмертный взрыв");
        Add(text, "spikes", "шипы");
        Add(text, "Enemy Archer", "вражеский лучник");
        Add(text, "rear ambush", "засада с тыла");
        Add(text, "Castle", "Замок");
        Add(text, "Skybreaker Edict", "Эдикт неболома");
        Add(text, "Heaven's Judgement", "Небесный суд");
        Add(text, "Stormcaller's Verdict", "Вердикт призывателя бурь");
        Add(text, "Hex of the Warped Ground", "Порча искаженной земли");
        Add(text, "Aegis of the Unbroken Crown", "Эгида нерушимой короны");
        Add(text, "Temporal Distortion", "Искажение времени");
        Add(text, "Ward of the Arcane Pylons", "Оберег арканных пилонов");
        Add(text, "Rune of Ruin", "Руна погибели");
        Add(text, "Summon Earthen Rampart", "Призвать земляной вал");
        Add(text, "Raise Iron Thorns", "Поднять железные шипы");
        Add(text, "Sow Venomous Miasma", "Посеять ядовитую миазму");
        Add(text, "Kindle Infernal Sigils", "Зажечь адские печати");
        Add(text, "Call Stormbound Sigils", "Призвать грозовые печати");
        Add(text, "Skip Trailer", "Пропустить ролик");
        Add(text, "BGM Genre", "Жанр музыки");
        Add(text, "EDM", "EDM");
        Add(text, "Metal", "Метал-музыка");
        Add(text, "Random", "Случайно");
        Add(text, "Language", "Язык");
        Add(text, "Settings", "Настройки");
        Add(text, "Master Volume", "Общая громкость");
        Add(text, "Music Volume", "Громкость музыки");
        Add(text, "SFX Volume", "Громкость эффектов");
        Add(text, "Cursor Size", "Размер курсора");
        Add(text, "Choose Language", "Выберите язык");
        Add(text, "Pick the language Tetrabeasts should use. You can change this later in Settings.", "Выберите язык Tetrabeasts. Позже его можно изменить в настройках.");
        Add(text, "Press Any Key", "Нажмите любую клавишу");
        Add(text, "Modifiers", "Модификаторы");
        Add(text, "Active Run Modifiers", "Активные модификаторы забега");
        Add(text, "Back", "Назад");
        Add(text, "None", "Нет");
        Add(text, "Yes", "Да");
        Add(text, "No", "Нет");
        Add(text, "Locked", "Заблокировано");
        Add(text, "Blocked", "Заблокировано");
        Add(text, "Active", "Активно");
        Add(text, "(Missing)", "(Отсутствует)");
        Add(text, "???", "???");
        Add(text, "Leaderboard", "Таблица лидеров");
        Add(text, "LEADERBOARDS", "ТАБЛИЦЫ ЛИДЕРОВ");
        Add(text, "Global", "Глобальная");
        Add(text, "Friends", "Друзья");
        Add(text, "Current", "Текущая");
        Add(text, "Rank", "Место");
        Add(text, "Player", "Игрок");
        Add(text, "Refresh", "Обновить");
        Add(text, "Loading...", "Загрузка...");
        Add(text, "Refreshing leaderboards...", "Обновление таблиц лидеров...");
        Add(text, "Steam leaderboard ready.", "Таблица лидеров Steam готова.");
        Add(text, "Leaderboard refresh failed.", "Не удалось обновить таблицу лидеров.");
        Add(text, "No leaderboard data.", "Нет данных таблицы лидеров.");
        Add(text, "No global scores yet.", "Глобальных результатов пока нет.");
        Add(text, "No friend scores yet.", "Результатов друзей пока нет.");
        Add(text, "You are not ranked yet.", "Вы пока не в рейтинге.");
        Add(text, "Achievements", "Достижения");
        Add(text, "Submit Name", "Отправить имя");
        Add(text, "Help Menu", "Меню помощи");
        Add(text, "Modifier not yet discovered.", "Модификатор еще не открыт.");
        Add(text, "No level Modifier", "Нет модификатора уровня");
        Add(text, "Level modifier: {0}.", "Модификатор уровня: {0}.");
        Add(text, "Secret Achievements", "Секретные достижения");
        Add(text, "1 secret achievement remaining", "Осталось 1 секретное достижение");
        Add(text, "{0} secret achievements remaining", "Осталось секретных достижений: {0}");
        Add(text, "No GameController found.", "GameController не найден.");
        Add(text, "No monsters in roster.", "В отряде нет монстров.");
        Add(text, "Reinforcements", "Подкрепления");
        Add(text, "Active Squad", "Активный отряд");
        Add(text, "Choose A Buff", "Выберите усиление");
        Add(text, "Choose A Debuff", "Выберите ослабление");
        Add(text, "Next Piece", "Следующая фигура");
        Add(text, "Reroll", "Перебросить");
        Add(text, "EXP Gained", "Получено опыта");
        Add(text, "Permanent EXP Gained", "Получено постоянного опыта");
        Add(text, "EXP Preserved", "Опыт сохранен");
        Add(text, "New Difficulty Unlocked!", "Новая сложность открыта!");
        Add(text, "Special Ability: {0}", "Особая способность: {0}");
        Add(text, "{0} is broken.", "{0} сломан.");
        Add(text, "{0}: {1} shield(s) remain.", "{0}: осталось щитов: {1}.");
    }

    static void AddRecentFixupText(Dictionary<string, string> text)
    {
        Add(text, "Starting a new game will erase the saved run. Continue?", "Новая игра удалит сохраненный забег. Продолжить?");
        Add(text, "Current Level: {0}", "Текущий уровень: {0}");
        Add(text, "Current Level: 0", "Текущий уровень: 0");
        Add(text, " x{0}", " x{0}");
        Add(text, "Refund", "Возврат");
        Add(text, "Unlock", "Разблокировать");
        Add(text, " Unlock", " Разблокировать");
        Add(text, "Selected:", "Выбрано:");
        Add(text, "Selected: ", "Выбрано: ");
        Add(text, "{0}  Lv.{1}", "{0}  ур.{1}");
        Add(text, "Luck Up", "Удача +");
        Add(text, "Gravity Down", "Гравитация -");
        Add(text, "Velocity Down", "Скорость -");
        Add(text, "Gold Up", "Золото +");
        Add(text, "Attack Up", "Атака +");
        Add(text, "HP Up", "ОЗ +");
        Add(text, "Unit Lives Up", "Жизни отряда +");
        Add(text, "Role: {0}", "Роль: {0}");
        Add(text, "Level: {0}  ({1:0.#}/{2})", "Уровень: {0}  ({1:0.#}/{2})");
        Add(text, "Max HP: {0:0.#}  (+{1}) = {2:0.#}", "Макс. ОЗ: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Attack: {0:0.#}  (+{1}) = {2:0.#}", "Атака: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Special Gain: {0:0.#}", "Особая энергия: {0:0.#}");
        Add(text, "Heal: {0:0.#}  (+{1}) = {2:0.#}", "Лечение: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Heal Range: {0:0.#}", "Дальность лечения: {0:0.#}");
        Add(text, "Heal Speed: {0:0.#}s", "Скорость лечения: {0:0.#} с");
        Add(text, "Heal: -", "Лечение: -");
        Add(text, "Base Stats + Shop Buff = Total Stats", "Базовые характеристики + усиление магазина = итоговые характеристики");
        Add(text, "Defense", "Защита");
        Add(text, "Beefy boy that deals very little damage.", "Крепкий боец, который наносит очень мало урона.");
        Add(text, "Healer with a wide range, but weak spells.", "Лекарь с большой дальностью, но слабыми заклинаниями.");
        Add(text, "A well rounded attacker unit with decent damage, but enough health to survive weaker attacks.", "Сбалансированный атакующий боец с хорошим уроном и достаточным здоровьем, чтобы пережить слабые атаки.");
        Add(text, "A specialized attacker unit with high base damage and lower health.", "Специализированный атакующий боец с высоким базовым уроном и низким здоровьем.");
        Add(text, "Can take a hit and keep going, but won't deal much damage.", "Может выдержать удар и продолжить бой, но наносит мало урона.");
        Add(text, "Healer with a short range, but powerful healing spells.", "Лекарь с малой дальностью, но сильными лечебными заклинаниями.");
        Add(text, "Combo Extension", "Продление комбо");
        Add(text, "Chain Surge", "Цепной всплеск");
        Add(text, "Stone Scrounger", "Каменный добытчик");
        Add(text, "Reserve Stockpile", "Запас резерва");
        Add(text, "Reserve Recovery", "Восстановление резерва");
        Add(text, "Bulwark Aura", "Аура бастиона");
        Add(text, "Increase combo timer duration by {0}.", "Увеличивает длительность таймера комбо на {0}.");
        Add(text, "Each row clear has a {0} chance to increase combo count one additional time.", "Каждая очищенная строка имеет шанс {0} дополнительно увеличить счетчик комбо.");
        Add(text, "Increase chance of buff drop from stone obstacle destruction by {0}.", "Увеличивает шанс выпадения усиления при разрушении каменного препятствия на {0}.");
        Add(text, "Increase the number of starting reserve units by {0}.", "Увеличивает число стартовых резервных бойцов на {0}.");
        Add(text, "Increase the number of reserve units restored on round win by {0}.", "Увеличивает число резервных бойцов, восстанавливаемых при победе в раунде, на {0}.");
        Add(text, "Decrease damage taken and damage done for all ally monster units by {0}.", "Снижает получаемый и наносимый урон всех союзных монстров на {0}.");
        Add(text, "Passive - {0}", "Пассивно - {0}");
        Add(text, "Passive - {0}:", "Пассивно - {0}:");
        Add(text, "Next upgrade at Lv.{0}:", "Следующее улучшение на ур.{0}:");
        Add(text, "Passive is fully upgraded.", "Пассивная способность полностью улучшена.");
        Add(text, "1 second", "1 секунда");
        Add(text, "{0} seconds", "{0} сек.");
        Add(text, "A = Shift Left", "A = сдвиг влево");
        Add(text, "D = Shift Right", "D = сдвиг вправо");
        Add(text, "D = Shft Right", "D = сдвиг вправо");
        Add(text, "R = Character Special", "R = особая способность персонажа");
        Add(text, "= Character Special", "= особая способность персонажа");
        Add(text, "R = Character Special (Special Guage 100%)", "R = особая способность персонажа (особая энергия 100%)");
        Add(text, "R = Character Special (Special Gauge 100%)", "R = особая способность персонажа (особая энергия 100%)");
        Add(text, "R = Character Special (Specaial Gauge 100%)", "R = особая способность персонажа (особая энергия 100%)");
        Add(text, "= Character Special (Special Guage 100%)", "= особая способность персонажа (особая энергия 100%)");
        Add(text, "= Character Special (Special Gauge 100%)", "= особая способность персонажа (особая энергия 100%)");
        Add(text, "Boss", "Босс");
        Add(text, "Boss Ability", "Способность босса");
        Add(text, "Boss Abilities", "Способности босса");

        Add(text, "Gold Won This Round:", "Золото за этот раунд:");
        Add(text, "Gold Won This Round", "Золото за этот раунд");
        Add(text, "Rerolls", "Перебросы");
        Add(text, "Rerolls: {0}", "Перебросы: {0}");
        Add(text, "Rerolls: 0", "Перебросы: 0");
        Add(text, "Rerolls ({0})", "Перебросы ({0})");
        Add(text, "Modifier", "Модификатор");
        Add(text, "x{0}", "x{0}");

        Add(text, "Level Up", "Уровень повышен");
        Add(text, "Level Up!", "Уровень повышен!");
        Add(text, "LEVEL UP!", "УРОВЕНЬ ПОВЫШЕН!");
        Add(text, "LEVEL UP! x{0}", "УРОВЕНЬ ПОВЫШЕН! x{0}");
        Add(text, "Level {0} -> Level {1}", "Уровень {0} -> уровень {1}");
        Add(text, "+{0} Exp", "+{0} опыта");
        Add(text, "{0} permanent EXP ({1}% of {2} transferable EXP)", "{0} постоянного опыта ({1}% от {2} переносимого опыта)");
        Add(text, "Converted from {0} run EXP at {1}%", "Преобразовано из {0} опыта забега по ставке {1}%");

        Add(text, "Passive+", "Пассивно+");
        Add(text, "+ 5 HP", "+ 5 ОЗ");
        Add(text, "+ 1 Attack", "+ 1 атака");
        Add(text, "+ 1 Special", "+ 1 особая энергия");
        Add(text, "+ 5 Heal", "+ 5 лечение");
        Add(text, "+ 1 Range", "+ 1 дальность");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Теперь быстро сбросьте текущую фигуру клавишей [Пробел]. Это сразу опустит фигуру, чтобы вы могли быстро поставить ее на место, закрепить и создать новую.");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Теперь быстро сбросьте текущую фигуру клавишей [Пробел]. Это сразу опустит фигуру, чтобы вы могли быстро поставить ее на место, закрепить и создать новую.");
        Add(text, "This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Это сразу опустит фигуру, чтобы вы могли быстро поставить ее на место, закрепить и создать новую.");
        Add(text, "This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Это сразу опустит фигуру, чтобы вы могли быстро поставить ее на место, закрепить и создать новую.");
        AddAchievementText(text);
    }

    static void AddAchievementText(Dictionary<string, string> text)
    {
        Add(text, "I Want Every Able Body", "Мне нужен каждый боец");
        Add(text, "Unlock all monster units.", "Разблокируйте всех монстров.");
        Add(text, "0 Star Victory", "Победа на 0 звездах");
        Add(text, "Beat the final boss.", "Победите финального босса.");
        Add(text, "One Star Victory", "Победа на одной звезде");
        Add(text, "Beat the final boss on 1-Star difficulty.", "Победите финального босса на сложности 1 звезда.");
        Add(text, "Pay To Win", "Заплати и побеждай");
        Add(text, "Upgrade any shop buff to level 5.", "Улучшите любое усиление магазина до 5 уровня.");
        Add(text, "Gangs All Here", "Все в сборе");
        Add(text, "Unlock all Commanders.", "Разблокируйте всех командиров.");
        Add(text, "Two Star Victory", "Победа на двух звездах");
        Add(text, "Beat the final boss on 2-Star difficulty.", "Победите финального босса на сложности 2 звезды.");
        Add(text, "Three Star Victory", "Победа на трех звездах");
        Add(text, "Beat the final boss on 3-Star difficulty.", "Победите финального босса на сложности 3 звезды.");
        Add(text, "Four Star Victory", "Победа на четырех звездах");
        Add(text, "Beat the final boss on 4-Star difficulty.", "Победите финального босса на сложности 4 звезды.");
        Add(text, "Five Star Victory", "Победа на пяти звездах");
        Add(text, "Beat the final boss on 5-Star difficulty.", "Победите финального босса на сложности 5 звезд.");
        Add(text, "This is Fine", "Все нормально");
        Add(text, "Take 1,000 burn damage from fire floor effects.", "Получите 1,000 урона от горения на огненном полу.");
        Add(text, "That Escalated Quickly", "Быстро же все обострилось");
        Add(text, "Remove 1,000 units using the Death Special Block.", "Уберите 1,000 бойцов с помощью особого блока смерти.");
        Add(text, "First Time Raider", "Первый налет");
        Add(text, "Take 1,000 damage from traps.", "Получите 1,000 урона от ловушек.");
        Add(text, "I think I Stepped in Something", "Кажется, я во что-то наступил");
        Add(text, "Take 1,000 toxic damage from posioned floor effects.", "Получите 1,000 токсичного урона от отравленного пола.");
        Add(text, "Take 1,000 toxic damage from poisoned floor effects.", "Получите 1,000 токсичного урона от отравленного пола.");
        Add(text, "Shake It Until You Break It", "Тряси, пока не сломается");
        Add(text, "Clear 250 rows by using the earthquake special block.", "Очистите 250 строк с помощью особого блока землетрясения.");
        Add(text, "A Little Jiggle Goes A Long Way", "Маленькая встряска творит чудеса");
        Add(text, "Clear 25 rows by using the earthquake special block.", "Очистите 25 строк с помощью особого блока землетрясения.");
        Add(text, "Girthquake", "Мегатрясение");
        Add(text, "Clear 1,000 rows by using the earthquake special block.", "Очистите 1,000 строк с помощью особого блока землетрясения.");
        Add(text, "Get in Loser, We're Going Shopping", "Залезай, едем за покупками");
        Add(text, "Accumulate 1,000 gold.", "Накопите 1,000 золота.");
        Add(text, "Is I Rich Now?", "Я теперь богат?");
        Add(text, "Accumulate 100 gold.", "Накопите 100 золота.");
        Add(text, "This Lasted Longer Than Some Collectible Fads", "Это продержалось дольше многих модных коллекций");
        Add(text, "Take more than 5 minutes to conquer a castle.", "Потратьте больше 5 минут на захват замка.");
        Add(text, "Anything You Can Do, I Can Do Slower", "Все, что умеешь ты, я сделаю медленнее");
        Add(text, "Take more than 3 minutes to conquer a castle.", "Потратьте больше 3 минут на захват замка.");
        Add(text, "Sloth Lord", "Повелитель лени");
        Add(text, "Take more than 4 minutes to conquer a castle.", "Потратьте больше 4 минут на захват замка.");
        Add(text, "My Fingers Hurt", "Пальцы болят");
        Add(text, "Survive with gravity at 10 for 60 seconds.", "Продержитесь 60 секунд при гравитации 10.");
        Add(text, "Nevermind...", "Неважно...");
        Add(text, "Unlock your first first temporary debuff.", "Разблокируйте свое первое временное ослабление.");
        Add(text, "A Record That Would Make Lions Blush", "Рекорд, за который можно покраснеть");
        Add(text, "Lose 50 Times.", "Проиграйте 50 раз.");
        Add(text, "Thrive Under Pressure", "Расцвет под давлением");
        Add(text, "Survive with gravity at 10 for 30 seconds.", "Продержитесь 30 секунд при гравитации 10.");
        Add(text, "Is I Strong Now?", "Я теперь сильный?");
        Add(text, "Unlock your first first temporary buff.", "Разблокируйте свое первое временное усиление.");
        Add(text, "I Say This Not As An Insult, But As A Statement Of Fact", "Это не оскорбление, а констатация факта");
        Add(text, "Lose 100 Times.", "Проиграйте 100 раз.");
        Add(text, "I Think We Need A Bigger Vault", "Похоже, нужна сокровищница побольше");
        Add(text, "Accumulate 10,000 gold.", "Накопите 10,000 золота.");
        Add(text, "GG EZ", "GG EZ");
        Add(text, "Beat the final level with every Commander.", "Пройдите финальный уровень каждым командиром.");
        Add(text, "Participation Trophy", "Награда за участие");
        Add(text, "Lose for the first time.", "Проиграйте впервые.");
        Add(text, "Immortal Army", "Бессмертная армия");
        Add(text, "Conquer 100 castles with your Unit Reserve at max capacity.", "Захватите 100 замков с полным резервом отряда.");
        Add(text, "I Guess That Was The Wrong Wire", "Кажется, это был не тот провод");
        Add(text, "Lose 100 monster units to magic explosives.", "Потеряйте 100 монстров от магической взрывчатки.");
        Add(text, "I Think It's Dead Now", "Теперь оно точно мертво");
        Add(text, "Clear 1,000 rows.", "Очистите 1,000 строк.");
        Add(text, "Tis But A Scratch", "Просто царапина");
        Add(text, "Conquer a castle with your Unit Reserve at max capacity.", "Захватите замок с полным резервом отряда.");
        Add(text, "Meat Shield Tactics", "Тактика живого щита");
        Add(text, "Conquer 25 castles with your Unit Reserve at max capacity.", "Захватите 25 замков с полным резервом отряда.");
        Add(text, "You Got Your Marching Orders", "Приказ к походу получен");
        Add(text, "Clear 100 rows.", "Очистите 100 строк.");
        Add(text, "Overwhelming Power", "Подавляющая сила");
        Add(text, "Accumulate 15 buffs in a single run.", "Соберите 15 усилений за один забег.");
        Add(text, "Is There Anything Left To Attack?", "Осталось ли что-нибудь для атаки?");
        Add(text, "Clear 10,000 rows.", "Очистите 10,000 строк.");
        Add(text, "Shut Up And Takey My Money", "Молчи и забирай мои деньги");
        Add(text, "Buy an upgrade from the shop for the first time.", "Впервые купите улучшение в магазине.");
        Add(text, "Turning Small Numbers Into Big Numbers", "Превращаем маленькие числа в большие");
        Add(text, "Get a combo of 10 or higher.", "Получите комбо 10 или выше.");
        Add(text, "Vewwy Stwong", "Очэнь сильный");
        Add(text, "Deal 100 damage or more in a single attack.", "Нанесите 100 или больше урона одной атакой.");
        Add(text, "I'll Follow You Anywhere", "Я пойду за тобой куда угодно");
        Add(text, "Unlock a Commander for the first time.", "Впервые разблокируйте командира.");
        Add(text, "Eenie Meenie Miney Mo", "Эники-беники");
        Add(text, "Unlock a monster unit for the first time.", "Впервые разблокируйте монстра.");
        Add(text, "Tell Me I'm Pretty", "Скажи, что я красивый");
        Add(text, "Unlock a monster units skin variant for the first time.", "Впервые разблокируйте вариант облика монстра.");
        Add(text, "Are We There Yet?", "Мы уже пришли?");
        Add(text, "Conquer 10 castles in a single run.", "Захватите 10 замков за один забег.");
        Add(text, "It's Called Fashion Brenda, Look It Up", "Это называется мода, почитай");
        Add(text, "Unlock ten skin variants.", "Разблокируйте десять вариантов облика.");
        Add(text, "New Skin Who Dis?", "Новый облик, кто это?");
        Add(text, "Unlock five skin variants.", "Разблокируйте пять вариантов облика.");
        Add(text, "A Little Special", "Немного особенный");
        Add(text, "Use a special for the first time.", "Впервые используйте особую способность.");
        Add(text, "Mama's Special Boy", "Мамин особенный");
        Add(text, "Use a special 100 times.", "Используйте особую способность 100 раз.");
        Add(text, "Some Are More Special Than Others", "Некоторые особенные чуть больше");
        Add(text, "Use every Commanders special 100 times.", "Используйте особую способность каждого командира 100 раз.");
        Add(text, "General Got Me Workin'", "Генерал заставил работать");
        Add(text, "Destory 100 stone obstacles.", "Разрушьте 100 каменных препятствий.");
        Add(text, "Destroy 100 stone obstacles.", "Разрушьте 100 каменных препятствий.");
        Add(text, "Certified Glue Eater", "Сертифицированный любитель клея");
        Add(text, "Use a special 1000 times.", "Используйте особую способность 1000 раз.");
        Add(text, "We're All Special", "Мы все особенные");
        Add(text, "Use every Commanders special 20 times.", "Используйте особую способность каждого командира 20 раз.");
        Add(text, "That's A Lot Of Rubble", "Немало обломков");
        Add(text, "Conquer 50 castles.", "Захватите 50 замков.");
        Add(text, "King of Rubble", "Король обломков");
        Add(text, "Conquer 100 castles.", "Захватите 100 замков.");
        Add(text, "I. AM. SPEED!", "Я. ЕСТЬ. СКОРОСТЬ!");
        Add(text, "Conquer a castle in 30 seconds or less.", "Захватите замок за 30 секунд или быстрее.");
        Add(text, "Can't Stop, Won't Stop", "Не могу остановиться и не хочу");
        Add(text, "Conquer your first castle.", "Захватите свой первый замок.");
        Add(text, "I Ran Track In Highschool", "Я бегал в школе");
        Add(text, "Conquer a castle in 45 seconds or less.", "Захватите замок за 45 секунд или быстрее.");
        Add(text, "Gotta Go Fast", "Нужно быть быстрым");
        Add(text, "Conquer a castle in 60 seconds or less.", "Захватите замок за 60 секунд или быстрее.");
    }

    static void AddXpBreakdownText(Dictionary<string, string> text)
    {
        Add(text, "Level {0} Complete", "Уровень {0} завершен");
        Add(text, "Base Level XP", "Базовый опыт уровня");
        Add(text, "Clear time {0}", "Время прохождения {0}");
        Add(text, "Units lost {0}", "Потеряно бойцов {0}");
        Add(text, "Largest Combo {0}", "Лучшее комбо {0}");
        Add(text, "Obstacles Cleared {0}", "Уничтожено препятствий {0}");
        Add(text, "Star Difficulty ({0}):", "Звездная сложность ({0}):");
        Add(text, "Total XP Earned {0}", "Всего опыта получено {0}");
    }

    static void AddCommonLabels(Dictionary<string, string> text)
    {
        Add(text, "Run", "Забег");
        Add(text, "Level", "Уровень");
        Add(text, "Controls", "Управление");
        Add(text, "Gravity", "Гравитация");
        Add(text, "Combo", "Комбо");
        Add(text, "Score", "Очки");
        Add(text, "Reset", "Сброс");
        Add(text, "Score: {0}", "Очки: {0}");
        Add(text, "Gravity: {0:0.0}", "Гравитация: {0:0.0}");
        Add(text, "Star Difficulty", "Звездная сложность");
        Add(text, "0 Star: Recruit Difficulty", "0 звезд: сложность новобранца");
        Add(text, "1 Star: Soldier Difficulty", "1 звезда: сложность солдата");
        Add(text, "2 Star: Veteran Difficulty", "2 звезды: сложность ветерана");
        Add(text, "3 Star: Lieutenant Difficulty", "3 звезды: сложность лейтенанта");
        Add(text, "4 Star: General Difficulty", "4 звезды: сложность генерала");
        Add(text, "5 Star: War God Difficulty", "5 звезд: сложность бога войны");
        Add(text, "0-Star Difficulty", "Сложность 0 звезд");
        Add(text, "1 Star Difficulty", "Сложность 1 звезда");
        Add(text, "2 Stars Difficulty", "Сложность 2 звезды");
        Add(text, "3 Stars Difficulty", "Сложность 3 звезды");
        Add(text, "4 Stars Difficulty", "Сложность 4 звезды");
        Add(text, "5 Stars Difficulty", "Сложность 5 звезд");
        Add(text, "Normal difficulty.", "Обычная сложность.");
        Add(text, "No gameplay modifiers.", "Без игровых модификаторов.");
        Add(text, "All star difficulties unlocked.", "Все звездные сложности разблокированы.");
        Add(text, "0 Stars is always available.", "0 звезд доступны всегда.");
        Add(text, "Beat the final level on 0 Stars to unlock 1 Star.", "Пройдите финальный уровень на 0 звездах, чтобы открыть 1 звезду.");
        Add(text, "Beat the final level on 1 Star to unlock 2 Stars.", "Пройдите финальный уровень на 1 звезде, чтобы открыть 2 звезды.");
        Add(text, "Beat the final level on 2 Stars to unlock 3 Stars.", "Пройдите финальный уровень на 2 звездах, чтобы открыть 3 звезды.");
        Add(text, "Beat the final level on 3 Stars to unlock 4 Stars.", "Пройдите финальный уровень на 3 звездах, чтобы открыть 4 звезды.");
        Add(text, "Beat the final level on 4 Stars to unlock 5 Stars.", "Пройдите финальный уровень на 4 звездах, чтобы открыть 5 звезд.");
        Add(text, "0 Stars", "0 звезд");
        Add(text, "1 Star", "1 звезда");
        Add(text, "2 Stars", "2 звезды");
        Add(text, "3 Stars", "3 звезды");
        Add(text, "4 Stars", "4 звезды");
        Add(text, "5 Stars", "5 звезд");
        Add(text, "Score Gain", "Получение очков");
        Add(text, "EXP Gain", "Получение опыта");
        Add(text, "Level Modifier", "Модификатор уровня");
        Add(text, "Reserves And Rewards", "Резервы и награды");
        Add(text, "Reserve Units", "Резервные бойцы");
        Add(text, "Max Reserve Units", "Макс. резерв бойцов");
        Add(text, "Reserve Restored On Win", "Резерв после победы");
        Add(text, "Round Win Currency", "Золото за победу в раунде");
        Add(text, "Line Clear Currency Chance", "Шанс золота за очистку линии");
        Add(text, "Line Clear Currency Amount", "Золото за очистку линии");
        Add(text, "Monster Combat", "Бой монстров");
        Add(text, "Monster Damage", "Урон монстров");
        Add(text, "Monster Special Gain", "Особая энергия монстров");
        Add(text, "Monster Max HP", "Макс. ОЗ монстров");
        Add(text, "Healing Power", "Сила лечения");
        Add(text, "Healing Range Bonus", "Бонус дальности лечения");
        Add(text, "Ally Damage Dealt", "Урон союзников");
        Add(text, "Ally Damage Taken", "Урон по союзникам");
        Add(text, "Combo And Passives", "Комбо и пассивные способности");
        Add(text, "Combo Window", "Окно комбо");
        Add(text, "Bonus Combo Chance", "Шанс бонусного комбо");
        Add(text, "Stone Buff Drop Chance", "Шанс усиления из камня");
        Add(text, "Starting Reserve Passive", "Пассивный стартовый резерв");
        Add(text, "Round Win Reserve Passive", "Пассивный резерв за победу");
        Add(text, "Enemy", "Враг");
        Add(text, "Enemy Castle HP", "ОЗ вражеского замка");
        Add(text, "Enemy Damage", "Урон врага");
        Add(text, "Enemy Attack Interval", "Интервал атак врага");
        Add(text, "Enemy Projectile Speed", "Скорость снарядов врага");
        Add(text, "Castle Projectile Damage", "Урон снаряда замка");
        Add(text, "Castle Attack Interval", "Интервал атак замка");
        Add(text, "Piece And Special", "Фигура и особое");
        Add(text, "Piece Gravity", "Гравитация фигуры");
        Add(text, "Gravity Ramp Rate", "Рост гравитации");
        Add(text, "Special Block Chance", "Шанс особого блока");
        Add(text, "Commander Special Gain", "Особая энергия командира");
        Add(text, "Special Drain", "Расход особой энергии");
        Add(text, "Next Preview Disabled", "Превью следующей фигуры отключено");
        Add(text, "Landing Hint Disabled", "Подсказка падения отключена");
        Add(text, "Special Usage Locked", "Использование особого заблокировано");
        Add(text, "Special Blocks Blocked", "Особые блоки заблокированы");
        Add(text, "Run Modifier Drops", "Выпадение модификаторов забега");
        Add(text, "Stone Drops Debuffs Only", "Камни дают только ослабления");
        Add(text, "Luck", "Удача");
        Add(text, "Misfortune", "Невезение");
        Add(text, "Active Level Modifier", "Активный модификатор уровня");
        Add(text, "Effect", "Эффект");
        Add(text, "Outgoing Damage", "Исходящий урон");
        Add(text, "Incoming Damage", "Входящий урон");
        Add(text, "Overgrowth Target Interval", "Интервал целей зарастания");
        Add(text, "Initial Target Rows", "Начальные целевые ряды");
        Add(text, "Partial Growth Time", "Время частичного роста");
        Add(text, "Full Growth Time", "Время полного роста");
        Add(text, "Storm Strike Damage", "Урон удара бури");
        Add(text, "Storm Floor Tick Damage", "Периодический урон пола бури");
        Add(text, "Storm Floor Duration", "Длительность пола бури");
        Add(text, "Rear Ambush Interval", "Интервал тыловой засады");
        Add(text, "Rations Tick Interval", "Интервал пайков");
        Add(text, "Low Reserve Damage", "Урон при низком резерве");
        Add(text, "High Reserve Damage", "Урон при высоком резерве");
        Add(text, "Infection Chance", "Шанс заражения");
        Add(text, "Damage Per Tick", "Урон за тик");
        Add(text, "Damage Increase Per Tick", "Рост урона за тик");
        Add(text, "Spread Chance", "Шанс распространения");
        Add(text, "Special Gauge Gain", "Получение особой энергии");
        Add(text, "Death Explosion Damage", "Урон взрыва при смерти");
        Add(text, "Swamp Poison Damage", "Урон болотного яда");
        Add(text, "Swamp Poison Interval", "Интервал болотного яда");
        Add(text, "Manual Rotation", "Ручное вращение");
        Add(text, "Auto Rotate Interval", "Интервал автовращения");
        Add(text, "Manual Horizontal Shift", "Ручной сдвиг по горизонтали");
        Add(text, "Auto Shift Interval", "Интервал автосдвига");
        Add(text, "Combo Threshold", "Порог комбо");
        Add(text, "Below Threshold Damage", "Урон ниже порога");
        Add(text, "Shield Combo Threshold", "Порог комбо щита");
        Add(text, "Blocked Damage", "Заблокированный урон");
        Add(text, "Shield Count", "Количество щитов");
        Add(text, "Special Pieces", "Особые фигуры");
        Add(text, "Starting Monster Health", "Начальное здоровье монстров");
        Add(text, "Monster Damage Sharing", "Разделение урона монстрами");
        Add(text, "Roster", "Отряд");
        Add(text, "Max HP", "Макс. ОЗ");
        Add(text, "Starting HP", "Начальные ОЗ");
        Add(text, "Attack", "Атака");
        Add(text, "Special Gain", "Особая энергия");
        Add(text, "Heal Power", "Сила лечения");
        Add(text, "Heal Range", "Дальность лечения");
        Add(text, "Heal Speed", "Скорость лечения");
        Add(text, "Spawn Weight", "Вес появления");
        Add(text, "Stats", "Характеристики");
        Add(text, "Monsters", "Монстры");
        Add(text, "Shop Buff", "Усиление магазина");
        Add(text, "Passive", "Пассивно");
        Add(text, "Run Mod Buff", "Усиление забега");
        Add(text, "Run Mod Debuff", "Ослабление забега");
        Add(text, "Level Mod", "Мод. уровня");
        Add(text, "Boss Ability", "Способность босса");
        Add(text, "Defender", "Защитник");
        Add(text, "Healer", "Лекарь");
        Add(text, "Role", "Роль");
        Add(text, "HP", "ОЗ");
        Add(text, "Special Rate", "Скорость особого");
        Add(text, "Passive Lv", "Ур. пассивного");
        Add(text, "Esc = Pause", "Esc = пауза");
        Add(text, "Q = Rotate Counter Clockwise", "Q = вращать против часовой");
        Add(text, "E = Rotate Clockwise", "E = вращать по часовой");
        Add(text, "= Shift Left", "= сдвиг влево");
        Add(text, "S = Shift Down", "S = сдвиг вниз");
        Add(text, "Spacebar = Drop Instantly", "Пробел = мгновенно сбросить");
        Add(text, "= Character Special (Specaial Gauge 100%)", "= особая способность персонажа (особая энергия 100%)");
        Add(text, "Castle", "Замок");
        Add(text, "Starting Village", "Стартовая деревня");
        Add(text, "Tribe", "Племя");
        Add(text, "Tribe Chieftain", "Вождь племени");
        Add(text, "Shanty Town", "Бедный городок");
        Add(text, "Fortified Hamlet", "Укрепленный поселок");
        Add(text, "Industrial City", "Промышленный город");
        Add(text, "Thriving Metropolis", "Процветающий мегаполис");
        Add(text, "Grand Dukedom", "Великое герцогство");
        Add(text, "Royal Villa", "Королевская вилла");
        Add(text, "Ruk, Tribal Chieftain", "Рук, вождь племени");
        Add(text, "Fionne, Village Protector", "Фионн, защитница деревни");
        Add(text, "Sir Ralphie, Captain of the Guard", "Сэр Ральфи, капитан стражи");
        Add(text, "Eris, City Priestess", "Эрис, городская жрица");
        Add(text, "His Holiness Isaeh, Metropolis Pope", "Его Святейшество Исаэ, папа мегаполиса");
        Add(text, "Vivica, Dukedom Arch Mage", "Вивика, архимаг герцогства");
        Add(text, "Emperor Reginald P. Exford IV, Emperor's Palace", "Император Реджинальд П. Эксфорд IV, императорский дворец");
        Add(text, "Esora, Guardian to the Gates of Heaven?", "Эсора, страж небесных врат?");
    }

    static void AddWarningAndTutorialText(Dictionary<string, string> text)
    {
        Add(text, "Purchases are disabled in the demo. Your earned progress will still carry into the full game.",
            "Покупки отключены в демоверсии. Полученный прогресс все равно перенесется в полную игру.");
        Add(text, "A saved run is waiting. Continue that run or delete the temp save before changing your commander, squad, or shop buffs.",
            "Есть сохраненный забег. Продолжите его или удалите временное сохранение, прежде чем менять командира, отряд или усиления магазина.");
        Add(text, "Deleting the current temp run will permanently erase that saved run. After deleting it, you will be able to change your commander, monsters, and access the shop again. Continue?",
            "Удаление текущего временного забега навсегда сотрет это сохранение. После удаления вы снова сможете менять командира, монстров и открывать магазин. Продолжить?");
        Add(text, "Thank you for playing the Tetrabeasts demo! You have cleared the final demo level. If you enjoyed your time with the game, please consider buying the full version.",
            "Спасибо за игру в демоверсию Tetrabeasts!\n\nВы прошли последний уровень демо. Если игра вам понравилась, подумайте о покупке полной версии.");
        Add(text, "The Castle Has Fallen", "Замок пал");
        Add(text, "Conquest Failed", "Завоевание провалено");
        Add(text, "Endless Survival This final battle cannot be won. The enemy has endless health, and the run continues until a loss condition is met. Survive as long as you can.",
            "Бесконечное выживание\n\nЭту финальную битву нельзя выиграть. У врага бесконечное здоровье, а забег продолжается до выполнения условия поражения.\n\nВыживайте как можно дольше.");
        Add(text, "Do not show this message again", "Больше не показывать это сообщение");
        Add(text, "Increase luck, improving favorable random outcomes during runs.",
            "Повышает удачу и улучшает благоприятные случайные исходы во время забега.");
        Add(text, "Reduce the starting gravity speed of falling pieces.",
            "Снижает начальную скорость гравитации падающих фигур.");
        Add(text, "Reduce how quickly gravity ramps up during a level.",
            "Снижает скорость роста гравитации в течение уровня.");
        Add(text, "Increase the chance to earn gold from cleared rows.",
            "Повышает шанс получить золото за очищенные ряды.");
        Add(text, "Increase monster attack power.",
            "Повышает силу атаки монстров.");
        Add(text, "Increase monster maximum HP.",
            "Повышает максимальное здоровье монстров.");
        Add(text, "Increase monster healing power.",
            "Повышает силу лечения монстров.");
        Add(text, "Increase starting unit reserves.",
            "Увеличивает начальный резерв отрядов.");
        Add(text, "Now you are ready to start your first campaign. Press [F] to Continue",
            "Теперь вы готовы начать первую кампанию. Нажмите [F], чтобы продолжить");
        Add(text, "You found a Death special block. Drop it in a column to destroy all units matching the first monster below it. (Press [F] to Continue)",
            "Вы нашли особый блок Смерти. Сбросьте его в столбец, чтобы уничтожить все отряды, совпадающие с первым монстром под ним. (Нажмите [F], чтобы продолжить)");
        Add(text, "You found a Bomb special block. Drop it to blast a 3x3 area and damage nearby obstacles. (Press [F] to Continue)",
            "Вы нашли особый блок Бомбы. Сбросьте его, чтобы взорвать область 3x3 и повредить ближайшие препятствия. (Нажмите [F], чтобы продолжить)");
        Add(text, "You found a Bolt special block. Drop it to strike an entire column. (Press [F] to Continue)",
            "Вы нашли особый блок Молнии. Сбросьте его, чтобы ударить по целому столбцу. (Нажмите [F], чтобы продолжить)");
        Add(text, "You found an Earthquake special block. Drop it to shake loose unsupported units across the board. (Press [F] to Continue)",
            "Вы нашли особый блок Землетрясения. Сбросьте его, чтобы стряхнуть неподдерживаемые отряды по всему полю. (Нажмите [F], чтобы продолжить)");
        Add(text, "You found a Slow Gravity special block. Drop it to slow falling pieces for a short time. (Press [F] to Continue)",
            "Вы нашли особый блок Замедления гравитации. Сбросьте его, чтобы ненадолго замедлить падающие фигуры. (Нажмите [F], чтобы продолжить)");
        Add(text, "Quit without saving? Your current run will be lost and will not be available to continue later.",
            "Выйти без сохранения? Текущий забег будет потерян, и его нельзя будет продолжить позже.");
        Add(text, "Restarting will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
            "Перезапуск засчитает этот забег как поражение. Текущее временное сохранение будет стерто, а забег не сохранится. Продолжить?");
        Add(text, "Returning to the main menu will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
            "Возврат в главное меню засчитает этот забег как поражение. Текущее временное сохранение будет стерто, а забег не сохранится. Продолжить?");
        Add(text, "Save this run and quit the game? Continuing later will resume from the start of the current level checkpoint. While a run is saved, you will not be able to change your commander, squad, or shop buffs from the title menu.",
            "Сохранить этот забег и выйти из игры? При продолжении позже вы начнете с контрольной точки текущего уровня. Пока забег сохранен, вы не сможете менять командира, отряд или усиления магазина из главного меню.");
        Add(text, "The run could not be temp-saved, so the game will stay open.",
            "Не удалось временно сохранить забег, поэтому игра останется открытой.");
        Add(text, "After completing a level you will be allowed to choose one of three buffs that will empower your units. Press [F] to Continue",
            "После завершения уровня вы сможете выбрать одно из трех усилений, которое укрепит ваши отряды. Нажмите [F], чтобы продолжить");
        Add(text, "Most buffs come in multiple rarities that will determine their strength. From weakest to strongest (White -> Green -> Blue -> Purple -> Orange). Press [F] to Continue",
            "У большинства усилений есть несколько редкостей, определяющих их силу. От слабых к сильным (Белый -> Зеленый -> Синий -> Фиолетовый -> Оранжевый). Нажмите [F], чтобы продолжить");
        Add(text, "You will receive a single reroll per level that can be used on your buffs or debuffs. Rerolls can be saved throughout your run, but will be reset at the start of a new run. Press [F] to Continue",
            "Вы будете получать один переброс за уровень, который можно использовать для усилений или ослаблений. Перебросы можно сохранять в течение забега, но они сбрасываются в начале нового забега. Нажмите [F], чтобы продолжить");
        Add(text, "As you grow stronger your enemies will too. Choose one of three debuffs that will empower your enemies during this run. All buffs and debuffs will stay active and stack throughout the run, but will be reset upon starting a new game. Press [F] to Continue",
            "По мере того как вы становитесь сильнее, враги тоже усиливаются. Выберите одно из трех ослаблений, которое усилит врагов в этом забеге. Все усиления и ослабления остаются активными и суммируются на протяжении забега, но сбрасываются при начале новой игры. Нажмите [F], чтобы продолжить");
        Add(text, "Not all levels are created equal, here you will let lady luck decide what the next battlefield will be like. Pull the lever to reveal it, use rerolls if you have them, then continue into the fight. (Press [F] to Continue)",
            "Не все уровни одинаковы. Здесь удача решит, каким будет следующее поле боя. Потяните рычаг, чтобы открыть модификатор, используйте перебросы, если они есть, а затем продолжайте бой. (Нажмите [F], чтобы продолжить)");
        Add(text, "During a run, your monsters are temporary copies. This screen drains the EXP those copies earned so a portion can be preserved for their permanent versions. (Press [F] to Continue)",
            "Во время забега ваши монстры являются временными копиями. На этом экране опыт, полученный копиями, извлекается, чтобы часть его можно было сохранить для их постоянных версий. (Нажмите [F], чтобы продолжить)");
        Add(text, "Preserved EXP is added to your permanent monsters. Permanent levels make your units stronger at the start of future runs. (Press [F] to Continue)",
            "Сохраненный опыт добавляется вашим постоянным монстрам. Постоянные уровни делают ваши отряды сильнее в начале будущих забегов. (Нажмите [F], чтобы продолжить)");

        Add(text, "Welcome Overlord, my name is Lilith. I have done my best to rally the few monster I could find to assist in your conquest of the human domain. (Press [F] to Continue)",
            "Добро пожаловать, Владыка. Меня зовут Лилит. Я собрала тех немногих монстров, которых смогла найти, чтобы помочь вам покорить владения людей. (Нажмите [F], чтобы продолжить)");
        Add(text, "Before going into battle, we will first have to select our commander. Press the commander button in the bottom right to see who is available. (Press [F] to Continue)",
            "Перед боем нужно выбрать командира. Нажмите кнопку командира справа внизу, чтобы посмотреть доступных. (Нажмите [F], чтобы продолжить)");
        Add(text, "We only have one Commander that we can use at the moment, but with enough gold we can get a few more. Each commander has their own special ability. The details of each ability can be seen in the Commander preview on the right. (Press [F] to Continue)",
            "Сейчас у нас доступен только один командир, но за золото можно получить еще. У каждого командира есть своя особая способность. Подробности видны в предпросмотре командира справа. (Нажмите [F], чтобы продолжить)");
        Add(text, "Since our commander is already selected lets confirm the selection at the bottom and head back to the main lobby.",
            "Командир уже выбран, так что подтвердите выбор внизу и вернитесь в главное лобби.");
        Add(text, "Now we will set up our monster squadron that will go into battle with our Commander. Press the select monster button in the bottom right.",
            "Теперь соберем отряд монстров, который пойдет в бой с командиром. Нажмите кнопку выбора монстров справа внизу.");
        Add(text, "As you can see we only have so many monsters to work with to creat our squadron. A Squadron must have at least two monsters and can have a maximum of 4. (Press [F] to Continue)",
            "Как видите, монстров для отряда пока немного. В отряде должно быть минимум два монстра и максимум четыре. (Нажмите [F], чтобы продолжить)");
        Add(text, "A war can hardly be fought with such pitiful numbers, but that's where I can be of assistance. (Press [F] to Continue)",
            "С такими жалкими силами войну не выиграть, но тут я могу помочь. (Нажмите [F], чтобы продолжить)");
        Add(text, "I can make clones of your squadron to be sent into battle. This will allow multiple copies to be made to fill out your ranks. However, this will come with some limitations. (Press [F] to Continue)",
            "Я могу создавать клонов вашего отряда и отправлять их в бой. Так мы заполним ряды несколькими копиями, но у этого есть ограничения. (Нажмите [F], чтобы продолжить)");
        Add(text, "I will only be able to make so many copies and if we lose too many in battle we will be forced to retreat. Second, the copies will gain their own experience, but I will only be able to safely convert a small fraction of that experience from the copy back to the original monster after a campaign is finished. (Press [F] to Continue)",
            "Я смогу создать лишь ограниченное число копий, и если мы потеряем слишком много в бою, придется отступить. Кроме того, копии получают собственный опыт, но после кампании я смогу безопасно перенести оригинальному монстру только малую часть этого опыта. (Нажмите [F], чтобы продолжить)");
        Add(text, "You can click on any monster and see a preview of it's current stats and level in the preview section on the right. Click on the arrow button in the preview section to swap between it's stats and it's passive ability description.",
            "Нажмите на любого монстра, чтобы справа увидеть его текущие характеристики и уровень. Кнопка со стрелкой в предпросмотре переключает характеристики и описание пассивной способности.");
        Add(text, "More monsters can be unlocked with gold and cosmetic skins can also be purchased once you have gained some extra funds. Now, Prress the confirm button at the bottom of the screen to lock in your team.",
            "Новых монстров можно разблокировать за золото, а когда появятся лишние средства, можно купить косметические облики. Теперь нажмите кнопку подтверждения внизу экрана, чтобы закрепить команду.");
        Add(text, "Now you are ready to start your first campaign. Press the Start button to begin a new run.",
            "Теперь вы готовы начать первую кампанию. Нажмите кнопку начала, чтобы начать новый забег.");
        Add(text, "Welcome to the battlefield. We will begin by going over a few of the basic controls and battle mechanics. (Press [F] to Continue)",
            "Добро пожаловать на поле боя. Сначала разберем базовое управление и боевые механики. (Нажмите [F], чтобы продолжить)");
        Add(text, "First, try moving your piece to the left by pressing [A].",
            "Сначала попробуйте сдвинуть фигуру влево клавишей [A].");
        Add(text, "Next, try moving your piece to the right by pressing [D].",
            "Теперь попробуйте сдвинуть фигуру вправо клавишей [D].");
        Add(text, "Next, try moving your piece down a single row by pressing [S].",
            "Теперь попробуйте опустить фигуру на одну строку клавишей [S].");
        Add(text, "Now, try rotating your piece counter-clockwise by pressing [Q].",
            "Теперь попробуйте повернуть фигуру против часовой стрелки клавишей [Q].");
        Add(text, "Now, try rotating your piece clockwise by pressing [E].",
            "Теперь попробуйте повернуть фигуру по часовой стрелке клавишей [E].");
        Add(text, "Look at the bottom of the board and you will see four cells with a bright red tint in the same shape as your current piece, that isn't a coincidence. (Press [F] to Continue)",
            "Посмотрите в нижнюю часть доски: вы увидите четыре ярко-красные клетки формы вашей текущей фигуры. Это не совпадение. (Нажмите [F], чтобы продолжить)");
        Add(text, "This is the landing indicator and will help you see exactly where your current piece will lock in place. A piece will lock in place automatically when it come in contact with an obstacle, another locked unit piece, or when it reaches the bottom of the board. (Press [F] to Continue)",
            "Это индикатор приземления. Он показывает, где текущая фигура закрепится. Фигура закрепляется автоматически при контакте с препятствием, другой закрепленной фигурой или нижней частью доски. (Нажмите [F], чтобы продолжить)");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar].",
            "Теперь быстро сбросьте текущую фигуру клавишей [Пробел].");
        Add(text, "Here you can keep track of level information such as any active level modifiers, the curent level number, how long you have been in the level, and the current gravity pulling your pieces down. (Press [F] to Continue)",
            "Здесь отображается информация об уровне: активные модификаторы, номер уровня, проведенное время и текущая гравитация, тянущая фигуры вниз. (Нажмите [F], чтобы продолжить)");
        Add(text, "Here you can see a preview of the next piece that will be spawned. Knowing what will come next will help you plan out your next step. (Press [F] to Continue)",
            "Здесь видно следующую фигуру. Зная, что появится дальше, проще планировать следующий ход. (Нажмите [F], чтобы продолжить)");
        Add(text, "Reserve Units - Loss Condition When any of your units die your current reserve will be reduced. You only have so many soldiers and when your reserve hits 0 you will lose. (Press [F] to Continue)",
            "Резервные бойцы - условие поражения\n\nКогда любой ваш боец погибает, текущий резерв уменьшается. Бойцов ограниченное число, и если резерв дойдет до 0, вы проиграете. (Нажмите [F], чтобы продолжить)");
        Add(text, "Loss Condition If a piece is locked in place above the top row of the board you will instantly lose, regardless of the number of reserve units left. Be caureful not to build to high. (Press [F] to Continue)",
            "Условие поражения\n\nЕсли фигура закрепится выше верхней строки доски, вы мгновенно проиграете независимо от оставшегося резерва. Не стройте слишком высоко. (Нажмите [F], чтобы продолжить)");
        Add(text, "Win Condition Reduce the Enemy Castle's HP to zero. (Press [F] to Continue)",
            "Условие победы\n\nСнизьте ОЗ вражеского замка до нуля. (Нажмите [F], чтобы продолжить)");
        Add(text, "If you need a break press [Esc]. This will pause the game and bring up the pause menu.",
            "Если нужен перерыв, нажмите [Esc]. Игра поставится на паузу и откроется меню паузы.");
        Add(text, "Here you can change settings, look through the help menu, check your current modifiers, or end your run early. For now, lets close the pause menu by pressing [Esc] again.",
            "Здесь можно изменить настройки, открыть помощь, посмотреть текущие модификаторы или закончить забег досрочно. Пока закройте меню паузы, снова нажав [Esc].");
        Add(text, "Now lets try filling an entire row on the board next to launch an attack. (Press [F] to Continue)",
            "Теперь попробуем заполнить целую строку на доске, чтобы начать атаку. (Нажмите [F], чтобы продолжить)");
    }

    static void AddCharacterText(Dictionary<string, string> text)
    {
        Add(text, "Charge!", "В атаку!");
        Add(text, "Send all units from the bottom 3 rows to attack immediatley, no need for fully formed lines. Damage based on the number of units cleared.",
            "Немедленно отправляет в атаку всех бойцов из нижних 3 строк без необходимости собирать полные линии. Урон зависит от числа убранных бойцов.");
        Add(text, "Time Shift", "Сдвиг времени");
        Add(text, "Reduce unit fall speed by 1/3 of its current speed fo 15 seconds.",
            "Снижает скорость падения бойцов на треть от текущей скорости на 15 секунд.");
        Add(text, "Natures Embrace", "Объятия природы");
        Add(text, "Heal all units on the board back to full health including those that have died.",
            "Полностью исцеляет всех бойцов на доске, включая погибших.");
        Add(text, "Grock SMASH!", "Грок КРУШИТ!");
        Add(text, "Let loose a war cry doubling all units on the boards health and attack damage for 10 seconds.",
            "Издает боевой клич, удваивая здоровье и урон атаки всех бойцов на доске на 10 секунд.");
        Add(text, "Immutable Bulwark", "Нерушимый бастион");
        Add(text, "All units become immune to damage for 12 seconds.",
            "Все бойцы получают иммунитет к урону на 12 секунд.");
    }

    static void AddHelpTopicText(Dictionary<string, string> text)
    {
        Add(text, "Boss Abilities", "Способности босса");
        Add(text, "Controls", "Управление");
        Add(text, "Floor Effects", "Эффекты пола");
        Add(text, "Game Mechanics", "Игровые механики");
        Add(text, "Obstacles", "Препятствия");
        Add(text, "Special Blocks", "Особые блоки");
        Add(text, "Traps", "Ловушки");
        Add(text, "Other", "Другое");

        Add(text, "Full Board Blast", "Взрыв всей доски");
        Add(text, "The boss will target all monster units set on the board dealing a small amount of damage to each one.",
            "Босс выбирает всех монстров на доске и наносит каждому небольшой урон.");
        Add(text, "Increased Gravity", "Повышенная гравитация");
        Add(text, "The boss will temporarily increase gravity causing blocks to fall significantly faster for a set period of time.",
            "Босс временно повышает гравитацию, из-за чего блоки некоторое время падают значительно быстрее.");
        Add(text, "Invulnerable", "Неуязвимость");
        Add(text, "The boss will temporarily become invulnerable. While invulnerable, the boss will take no damage from any sources.",
            "Босс временно становится неуязвимым. Пока он неуязвим, он не получает урон из любых источников.");
        Add(text, "Lightning Strike", "Удар молнии");
        Add(text, "The boss will target 1-3 individual cells with lightning bolts that will deal siginficant damage to any monster unit in that cell. Afterwards that cell will have a lightning floor effect that does continuous damage to any monster unit that occupies the tile.",
            "Босс поражает молниями от 1 до 3 отдельных клеток, нанося значительный урон любому монстру в этих клетках. Затем клетка получает эффект электрического пола, который постоянно наносит урон стоящему на ней монстру.");
        Add(text, "Magic Explosive", "Магическая взрывчатка");
        Add(text, "The boss will spawn a single magical explosive as low on the board as possible. This explosive will detonate after 15 seconds and can only be safley removed by clearing the row it occupies.",
            "Босс создает одну магическую взрывчатку как можно ниже на доске. Она взорвется через 15 секунд, а безопасно убрать ее можно только очистив строку, которую она занимает.");
        Add(text, "Magic Shield", "Магический щит");
        Add(text, "The boss will spawn multiple magical pylon obstacles on the board. As long as the pylons remain on the board the boss will take 50% reduced damage from all sources.",
            "Босс создает на доске несколько магических пилонов. Пока пилоны остаются на доске, босс получает на 50% меньше урона из всех источников.");
        Add(text, "Spawn FE's", "Создать эффекты пола");
        Add(text, "The boss will spawn multiple floor effects on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. Possible floor effect types are posion, fire, and lightning.",
            "Босс создает на доске несколько эффектов пола. Они могут появляться отдельно в случайных местах или шаблонами 2x2, 1x4 или 4x1. Возможные типы: яд, огонь и молния.");
        Add(text, "Spawn Obstacles", "Создать препятствия");
        Add(text, "The boss will spawn multiple obstacles on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns stone obstacles.",
            "Босс создает на доске несколько препятствий. Они могут появляться отдельно в случайных местах или шаблонами 2x2, 1x4 или 4x1. Эта способность создает только каменные препятствия.");
        Add(text, "Spawn Traps", "Создать ловушки");
        Add(text, "The boss will spawn multiple traps on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns spike traps.",
            "Босс создает на доске несколько ловушек. Они могут появляться отдельно в случайных местах или шаблонами 2x2, 1x4 или 4x1. Эта способность создает только шипованные ловушки.");
        Add(text, "Row Blast", "Взрыв рядов");
        Add(text, "The boss will target the top three rows the player has set monster units. All monster units in the selected rows will recieve moderate damage from this attack.",
            "Босс выбирает три верхних ряда, где игрок поставил монстров. Все монстры в выбранных рядах получают умеренный урон.");
        Add(text, "Activate Special", "Активировать особое");
        Add(text, "R - Activates the players special ability when the special gauge is charged to 100%",
            "R - активирует особую способность игрока, когда шкала особой энергии заряжена до 100%.");
        Add(text, "Movement", "Передвижение");
        Add(text, "A - shifts the active piece one column to the left. S - shifts the active piece one row down. D - shifts the active piece one column to the right.",
            "A - сдвигает активную фигуру на один столбец влево.\n\nS - сдвигает активную фигуру на одну строку вниз.\n\nD - сдвигает активную фигуру на один столбец вправо.");
        Add(text, "Pause", "Пауза");
        Add(text, "Escape - Will open the pause menu and pause all gameplay functions. Presseing escape while the pause menu is open will close the pause menu and resume gameplay.",
            "Escape - открывает меню паузы и приостанавливает все игровые функции. Повторное нажатие Escape при открытом меню паузы закроет его и продолжит игру.");
        Add(text, "Quick Drop", "Быстрый сброс");
        Add(text, "Pressing spacebar will quick drop your active piece, setting it in place immediatley.",
            "Пробел быстро сбрасывает активную фигуру и сразу ставит ее на место.");
        Add(text, "Rotation", "Вращение");
        Add(text, "Q - Rotate the active piece 90 degrees counter-clockwise. E - Rotate the active piece 90 degrees clockwise.",
            "Q - повернуть активную фигуру на 90 градусов против часовой стрелки.\n\nE - повернуть активную фигуру на 90 градусов по часовой стрелке.");
        Add(text, "Fire", "Огонь");
        Add(text, "Any monster unit set in a cell with the fire floor effect will take constant damage. The damage is realtivley low but occurs often.",
            "Любой монстр, поставленный на клетку с огненным полом, будет постоянно получать урон. Урон сравнительно мал, но наносится часто.");
        Add(text, "Lightning", "Молния");
        Add(text, "Any monster unit set in a cell with the lightning floor effect will take constant damage. The damage is moderate but the floor effect will disappears after a period of time.",
            "Любой монстр, поставленный на клетку с электрическим полом, будет постоянно получать урон. Урон умеренный, но эффект пола исчезает через некоторое время.");
        Add(text, "Poison", "Яд");
        Add(text, "Any monster unit set in a cell with the poison floor effect will take continuous damage. The damage is realtivley low but occurs often.",
            "Любой монстр, поставленный на клетку с ядовитым полом, будет постоянно получать урон. Урон сравнительно мал, но наносится часто.");
        Add(text, "Attack Units", "Атакующие бойцы");
        Add(text, "Attack units tend to have a higher attack stat than other unit types. They are best used to deal maximum damage to enemies to end levels more quickly. They cannot heal and tend to have average health stats.",
            "Атакующие бойцы обычно имеют более высокую атаку, чем другие типы. Лучше всего использовать их для максимального урона врагам и быстрого завершения уровней. Они не лечат и обычно имеют среднее здоровье.");
        Add(text, "Currency", "Золото");
        Add(text, "The player can gain currency from completing levels and rarely from clearing rows. Currency can be used to purchase various cosmetics, monster units, player characters, and permanent buffs to improve future runs. Your current currency can be found in the top right of the screen.",
            "Игрок получает золото за прохождение уровней и иногда за очистку строк. Золото используется для покупки косметики, монстров, персонажей и постоянных усилений для будущих забегов. Текущее золото показано справа вверху.");
        Add(text, "Enemy Attack", "Атака врага");
        Add(text, "The enemy castle will send constant attacks at the players monster units in an attempt to cull them. The enemies attacks will increase in power and frequency at higher levels.",
            "Вражеский замок постоянно атакует монстров игрока, пытаясь сократить их число. На высоких уровнях атаки врага становятся сильнее и чаще.");
        Add(text, "Healing Units", "Лечащие бойцы");
        Add(text, "Some units can heal other monster units to varying degrees. They tend to have much lower health and attack stats.",
            "Некоторые бойцы могут лечить других монстров с разной эффективностью. Обычно у них гораздо меньше здоровья и атаки.");
        Add(text, "Landing Hint", "Подсказка падения");
        Add(text, "A red tint overlay appears in the location where the current active piece will fall.",
            "Красная подсветка показывает место, куда упадет текущая активная фигура.");
        Add(text, "Loss Condition 1", "Условие поражения 1");
        Add(text, "If the unit reserve reaches 0, the run will end with your loss! The unit reserve is reduced by one for every monster unit that dies on the board.",
            "Если резерв отряда достигнет 0, забег закончится поражением. Резерв уменьшается на один за каждого монстра, погибшего на доске.");
        Add(text, "Loss Condition 2", "Условие поражения 2");
        Add(text, "If a piece is set above the top row of the grid, the run will end with a loss!",
            "Если фигура будет поставлена выше верхней строки сетки, забег закончится поражением.");
        Add(text, "Monster Units", "Монстры");
        Add(text, "Choose from multiple units to make up your warband. Each unit has their own individual stats that will make them more suitable for different roles. Some units have balanced stats and others are more specialized.",
            "Выбирайте разных бойцов для своего отряда. У каждого свои характеристики, подходящие для разных ролей. Некоторые бойцы сбалансированы, другие более специализированы.");
        Add(text, "Commander", "Командир");
        Add(text, "Each Commander has their own unquie special ability that can be used in battle. New Commanders can be unlocked and set from the main menu.",
            "У каждого командира есть уникальная особая способность для боя. Новых командиров можно разблокировать и выбрать в главном меню.");
        Add(text, "Row Clear", "Очистка строки");
        Add(text, "Fill each grid cell in a row to launch an attack. Monster Units and Obstacles count as a filled cell. Floor effects and traps do not count as a filled cell. Monster units in the cleared row contribute their attack stats for damage calculation and partially fill the player's special gauge.",
            "Заполните все клетки строки, чтобы начать атаку. Монстры и препятствия считаются заполненными клетками. Эффекты пола и ловушки не считаются. Монстры в очищенной строке добавляют свою атаку к расчету урона и частично заполняют особую шкалу игрока.");
        Add(text, "Run Buffs", "Усиления забега");
        Add(text, "After succesfully completing a level you will be given three random buffs to choose from to enhance your current run. All buff modifiers will be reset when the run ends.",
            "После успешного завершения уровня вы получите три случайных усиления на выбор для текущего забега. Все модификаторы усилений сбросятся в конце забега.");
        Add(text, "Run Debuffs", "Ослабления забега");
        Add(text, "After succesfully completing a level you will be given three random debuffs to choose from to increase the difficulty of your current run. All debuff modifiers will be reset when the run ends.",
            "После успешного завершения уровня вы получите три случайных ослабления на выбор, чтобы повысить сложность текущего забега. Все модификаторы ослаблений сбросятся в конце забега.");
        Add(text, "Clearing rows will earn the player points and add to their overall score. This can be used to measure the success of a run and compete with other players.",
            "Очистка строк приносит очки и увеличивает общий счет. По счету можно оценивать успех забега и соревноваться с другими игроками.");
        Add(text, "Shop Buffs", "Усиления магазина");
        Add(text, "The shop offers different buffs that can be purchased. Each buff is permanent and can be purchased multiple times. Each purchase will increase the price of the buffs next purchase level.",
            "В магазине продаются разные усиления. Каждое усиление постоянно и может покупаться несколько раз. Каждая покупка повышает цену следующего уровня этого усиления.");
        Add(text, "Special Gauge", "Особая шкала");
        Add(text, "Fills with every row cleared. When it reaches 100% you can use your Commander's unique special ability, resetting the special gauge back 0%.",
            "Заполняется при каждой очищенной строке. Когда шкала достигает 100%, можно использовать уникальную особую способность командира, после чего шкала сбрасывается до 0%.");
        Add(text, "Tank Units", "Танки");
        Add(text, "Tank units tend to have significantly more health than other unit types. They are best used to outlast enemy attacks and protect weaker units. They cannot heal and tend to have low attack stats.",
            "Танки обычно имеют гораздо больше здоровья, чем другие типы. Они лучше всего выдерживают атаки врага и защищают слабых бойцов. Они не лечат и обычно имеют низкую атаку.");
        Add(text, "Unit Death", "Гибель бойца");
        Add(text, "When a monster units health drops to zero it dies. Dead units do not contribute their stats to an attack when their row is cleared or help fill up the players Special Gauge.",
            "Когда здоровье монстра падает до нуля, он погибает. Погибшие бойцы не добавляют характеристики к атаке при очистке их строки и не заполняют особую шкалу игрока.");
        Add(text, "Unit Reserve", "Резерв отряда");
        Add(text, "When starting a new run the player will have a set limit of how many units they can afford to lose throughout the run. When a unit dies the reserve bar will be decreased. Suuccesfully completing a level will award the player up to 5 reinforcements for each victory up to the max unit reserve.",
            "В начале нового забега у игрока есть лимит бойцов, которых можно потерять за весь забег. Когда боец погибает, шкала резерва уменьшается. За успешное завершение уровня игрок получает до 5 подкреплений за победу, пока не достигнут максимум резерва.");
        Add(text, "Victory Condition", "Условие победы");
        Add(text, "Reduce the health of the enemy castle by clearing rows. When the enemy castle reaches 0 health, you win!",
            "Уменьшайте здоровье вражеского замка, очищая строки. Когда здоровье замка достигнет 0, вы победите!");
        Add(text, "Explosive", "Взрывчатка");
        Add(text, "The explosive obstacle will explode after a period of time killing all surrounding monster units. Can be safley disposed of by clearing its row. When safley disposed of it add 25 damage to that row clears attack. CAUTION: Using a bomb or lightning special block on the explosive will cause it to detonate.",
            "Взрывное препятствие через некоторое время взорвется и убьет всех ближайших монстров. Его можно безопасно убрать, очистив его строку. При безопасном удалении оно добавит 25 урона к атаке этой строки.\n\nОСТОРОЖНО: использование бомбы или молнии на взрывчатке вызовет детонацию.");
        Add(text, "Magic Pylon", "Магический пилон");
        Add(text, "When magic pylon obstacles are on the board enemies will take 50% reduced damage from all sources. Magic pylon can bee destroyed by clearing the row they occupy or using bomb and lightning special blocks.",
            "Пока на доске есть магические пилоны, враги получают на 50% меньше урона из всех источников. Пилон можно уничтожить, очистив его строку, либо с помощью бомбы или молнии.");
        Add(text, "Stone", "Камень");
        Add(text, "Stone obstacles can be spawned at the beggining of a level or by the boss. A lightning special block or clearing a row containing a stone obstacle will deal one damage to it. Stone obstacle need to be damaged 3 times to be removed. Exception: Using a bomb special block will instantly destroy a stone obstacle.",
            "Каменные препятствия могут появляться в начале уровня или создаваться боссом. Особый блок молнии или очистка строки с камнем наносит ему 1 урон. Чтобы убрать камень, нужно нанести ему 3 урона. Исключение: особый блок бомбы мгновенно уничтожает камень.");
        Add(text, "Bomb", "Бомба");
        Add(text, "The bomb special will detonate immediatley when set. All blocks in the surrounding tiles of its blast will be destroyed!",
            "Особая бомба взорвется сразу после установки. Все блоки в окружающих клетках взрыва будут уничтожены!");
        Add(text, "Death", "Смерть");
        Add(text, "The Death special will activate immediatley when set on top of a monster unit. All monster units of the same type will be safley removed from the board with out decreasing your unit reserve.",
            "Особый блок смерти активируется сразу, если поставить его на монстра. Все монстры того же типа будут безопасно убраны с доски без уменьшения резерва.");
        Add(text, "Earthquake", "Землетрясение");
        Add(text, "The Earthquake special will activate immediatley when set. All blocks on the board will be dropped if not being supported by another tile beneath them. This effects obstacles as well that may otherwise not be able to be moved by other means.",
            "Особое землетрясение активируется сразу после установки. Все блоки на доске упадут, если под ними нет опоры. Это влияет и на препятствия, которые иначе нельзя сдвинуть.");
        Add(text, "Lightning Bolt", "Молния");
        Add(text, "The Lightning Bolt special will activate immediatley when set. All monster units and traps will be destroyed in that column. Stone obstacles will take partial damage if they are in the affected area.",
            "Особая молния активируется сразу после установки. Все монстры и ловушки в этом столбце будут уничтожены. Каменные препятствия в зоне действия получат частичный урон.");
        Add(text, "Slow Gravity", "Медленная гравитация");
        Add(text, "The Slow Gravity special block will activate immedialtey upon being set. It will significantly reduce the speed at which pieces fall and the how quickly gravity increases over time.",
            "Особый блок медленной гравитации активируется сразу после установки. Он значительно снижает скорость падения фигур и скорость роста гравитации со временем.");
        Add(text, "Spike Trap", "Шипованная ловушка");
        Add(text, "Spike traps will deal a high amount of damage to any monster unit that is set on the trapped cell when they are placed. The only way to destroyed spike traps are with the lightning special block.",
            "Шипованные ловушки наносят большой урон любому монстру, поставленному на клетку с ловушкой. Уничтожить такие ловушки можно только особым блоком молнии.");
    }

    static void AddLevelModifierText(Dictionary<string, string> text)
    {
        Add(text, "Spin to Win", "Крутись к победе");
        Add(text, "Active pieces will continuously rotate until set in place. Manual rotation will be locked.",
            "Активные фигуры будут непрерывно вращаться, пока не будут поставлены. Ручное вращение заблокировано.");
        Add(text, "Timing is Everything", "Время решает все");
        Add(text, "Active pieces will continuously shift horizontally back and and forth across the board. Manual shifting will be locked.",
            "Активные фигуры будут постоянно двигаться по горизонтали туда-сюда по доске. Ручной сдвиг заблокирован.");
        Add(text, "Go Big or Go Home", "Все или ничего");
        Add(text, "Damage is significantly reduced for all attacks when the combo streak is less than 3.",
            "Урон всех атак значительно снижен, если серия комбо меньше 3.");
        Add(text, "Break Out The Big Guns", "Достать тяжелую артиллерию");
        Add(text, "The enemy has fortified their position. A combo attack at 4 or higher will be required to remove each enemy shield. Damage dealt to the enemy while shielded is significantly reduced.",
            "Враг укрепил позицию. Чтобы снять каждый щит врага, нужна атака с комбо 4 или выше. Урон по врагу под щитом значительно снижен.");
        Add(text, "Contagion Outbreak", "Вспышка заразы");
        Add(text, "Disease has begun to spread through the ranks. Close proximitiy has a chance to transfer from afflicted units to healthy units and will spread with certainity on the death of infected units.",
            "Болезнь начала распространяться по рядам. Близость может передать ее от больных бойцов здоровым, а при смерти зараженного бойца она распространяется гарантированно.");
        Add(text, "Double Down", "Удвоить ставку");
        Add(text, "All damage taken and dealt will be doubled!",
            "Весь получаемый и наносимый урон удваивается!");
        Add(text, "Exploding Corpses", "Взрывающиеся тела");
        Add(text, "Units will explode on death dealing damage to all surrounding units. Damage dealt is a percentage based off of the max health of the exploding unit.",
            "Бойцы взрываются при смерти, нанося урон всем вокруг. Урон зависит от максимального здоровья взорвавшегося бойца.");
        Add(text, "Tis A Flesh Wound", "Всего лишь царапина");
        Add(text, "All ally units will start with half health.",
            "Все союзные бойцы начинают с половиной здоровья.");
        Add(text, "Rations Running Low", "Пайки на исходе");
        Add(text, "Rations have begun to run out. Ally units have begun to starve trying to share the remaining rations. Units will take continuous damage proportional to the number of current reserve units.",
            "Пайки начали заканчиваться. Союзники голодают, пытаясь делить остатки. Бойцы получают постоянный урон, пропорциональный текущему числу резервных бойцов.");
        Add(text, "Overgrowth", "Зарастание");
        Add(text, "Overgrowth has taken over the area consuming tiles and monsters. Overgrowth becomes more resilent to destruction once fully grown. Defeat the enemy before your army becomes mulch!",
            "Зарастание поглощает клетки и монстров. Полностью выросшее зарастание становится более стойким к разрушению. Победите врага, пока ваша армия не стала удобрением!");
        Add(text, "No Retreat", "Отступать некуда");
        Add(text, "Enemy ambush will cut off any retreat. Rows will slowly fill with enemy units progressivley limiting space to maneuver.",
            "Вражеская засада отрежет путь к отступлению. Ряды будут медленно заполняться врагами, постепенно ограничивая пространство для маневра.");
        Add(text, "Soul Link", "Связь душ");
        Add(text, "All four units in a piece share a single health pool.",
            "Все четыре бойца в фигуре имеют общий запас здоровья.");
        Add(text, "Back to the Basics", "Назад к основам");
        Add(text, "Special blocks will not spawn.",
            "Особые блоки не будут появляться.");
        Add(text, "Commander Special Lock", "Блокировка особого командира");
        Add(text, "Special ability gauge will be set to zero and locked.",
            "Шкала особой способности будет обнулена и заблокирована.");
        Add(text, "Catastrophic Storm", "Катастрофическая буря");
        Add(text, "An unrelenting storm has arrived and will blast the area with devestating lightning strikes.",
            "Пришла беспощадная буря, которая будет поражать область разрушительными ударами молний.");
        Add(text, "Miasma Marsh", "Миазматическое болото");
        Add(text, "The battlefield has shifted to the nearby marshes where deadly miasma drifts across the terrain.",
            "Поле боя сместилось к ближайшим болотам, где по местности стелется смертоносная миазма.");
    }

    static void AddRunModifierNames(Dictionary<string, string> text)
    {
        Add(text, "All Special Gain Down", "Вся особая энергия снижена");
        Add(text, "All Special Gain Up", "Вся особая энергия повышена");
        Add(text, "ATK Down", "Атака снижена");
        Add(text, "ATK Up", "Атака повышена");
        Add(text, "Currency Drop Up", "Выпадение золота повышено");
        Add(text, "Debuffs Only", "Только ослабления");
        Add(text, "Enemy ATK Down", "Атака врага снижена");
        Add(text, "Enemy ATK SPD Down", "Скорость атаки врага снижена");
        Add(text, "Enemy ATK SPD Up", "Скорость атаки врага повышена");
        Add(text, "Enemy ATK Up", "Атака врага повышена");
        Add(text, "Enemy HP Up", "ОЗ врага повышены");
        Add(text, "Gravity Accel SPD Down", "Ускорение гравитации снижено");
        Add(text, "Gravity Accel SPD Up", "Ускорение гравитации повышено");
        Add(text, "Gravity Base SPD Down", "Базовая скорость гравитации снижена");
        Add(text, "Gravity SPD Up", "Скорость гравитации повышена");
        Add(text, "Healing Range Up", "Дальность лечения повышена");
        Add(text, "Healing STR Up", "Сила лечения повышена");
        Add(text, "HP Down", "ОЗ снижены");
        Add(text, "HP Up", "ОЗ повышены");
        Add(text, "Luck Up", "Удача повышена");
        Add(text, "Misfortune Up", "Невезение повышено");
        Add(text, "No Landing Indicator", "Без индикатора падения");
        Add(text, "No Next Block Preview", "Без превью следующего блока");
        Add(text, "No Reinforcements", "Без подкреплений");
        Add(text, "Reinforcements Down", "Подкрепления снижены");
        Add(text, "Reinforcements Up", "Подкрепления повышены");
        Add(text, "Special Block Down", "Особые блоки снижены");
        Add(text, "Special Block Up", "Особые блоки повышены");
        Add(text, "Special Gain Stat Down", "Особая энергия монстров снижена");
        Add(text, "Special Gauge Stat Up", "Особая энергия монстров повышена");
        Add(text, "Stone Buff Drop Down", "Усиления из камня снижены");
        Add(text, "Stone Buff Drop Up", "Усиления из камня повышены");
        Add(text, "Unit Reserve Down", "Резерв отряда снижен");
        Add(text, "Unit Reserve Up", "Резерв отряда повышен");
        Add(text, "Win Currency Down", "Золото за победу снижено");
        Add(text, "Win Currency Up", "Золото за победу повышено");
    }

    static void AddRunModifierFixedDescriptions(Dictionary<string, string> text)
    {
        Add(text, "A red tinted outline will no longer be shown where your pieces will land.",
            "Красный контур больше не будет показывать, куда приземлятся фигуры.");
        Add(text, "The next block will no longer be shown.",
            "Следующий блок больше не будет показываться.");
        Add(text, "Reinforcements will no longer arrive after winning a round.",
            "Подкрепления больше не будут прибывать после победы в раунде.");
        Add(text, "Stone obstacles no longer have a chance of dropping buffs and now only drop debuffs. Debuff drop chance is the same as prior buff drop chance.",
            "Каменные препятствия больше не могут ронять усиления и теперь дают только ослабления. Шанс выпадения ослабления равен прежнему шансу выпадения усиления.");
        Add(text, "Double the amount of currency gained occasionally when clearing lines.",
            "Иногда удваивает золото, получаемое при очистке линий.");
        Add(text, "Triple the amount of currency gained occasionally when clearing lines.",
            "Иногда утраивает золото, получаемое при очистке линий.");
        Add(text, "Qunituple the amount of currency gained occasionally when clearing lines.",
            "Иногда упятеряет золото, получаемое при очистке линий.");
        Add(text, "Increases the healing range of all friendly monsters by 1.",
            "Увеличивает дальность лечения всех союзных монстров на 1.");
        Add(text, "Increases the healing range of all friendly monsters by 2.",
            "Увеличивает дальность лечения всех союзных монстров на 2.");
        Add(text, "Increases the healing range of all friendly monsters by 3.",
            "Увеличивает дальность лечения всех союзных монстров на 3.");
    }

    static void AddStatText(Dictionary<string, string> text)
    {
        Add(text, "Lines Cleared:", "Очищено линий:");
        Add(text, "Special Used:", "Особое использовано:");
        Add(text, "Obstacles Destroyed:", "Препятствий уничтожено:");
        Add(text, "Highest Combo:", "Лучшее комбо:");
        Add(text, "Highest Single Attack:", "Лучшая одиночная атака:");
        Add(text, "Units Died:", "Бойцов погибло:");
        Add(text, "Units Healed:", "Бойцов исцелено:");
        Add(text, "Total Damage Dealt:", "Всего нанесено урона:");
        Add(text, "Clear Time:", "Время прохождения:");
        Add(text, "Final Score:", "Итоговый счет:");
        Add(text, "Lines", "Линии");
        Add(text, "Times", "Раз");
        Add(text, "Obstacles", "Препятствия");
        Add(text, "Damage", "Урон");
        Add(text, "Units", "Бойцы");
        Add(text, "Health", "Здоровье");
        Add(text, "Level {0}", "Уровень {0}");
        Add(text, "{0} of {1} {2} discovered. Total codex unlocked {3}%",
            "Открыто {0} из {1} {2}. Всего кодекса открыто: {3}%");
        Add(text, "Buffs", "Усиления");
        Add(text, "Debuffs", "Ослабления");
        Add(text, "Level Modifiers", "Модификаторы уровня");
    }

    static bool TryTranslateRunModifierDescription(string lookupKey, out string russianText)
    {
        russianText = null;

        for (int i = 0; i < DegreePrefixes.Length; i++)
        {
            string englishPrefix = DegreePrefixes[i].English;
            if (!lookupKey.StartsWith(englishPrefix + " ", StringComparison.OrdinalIgnoreCase))
                continue;

            string remainder = lookupKey.Substring(englishPrefix.Length + 1).Trim();
            if (TryGetRunModifierTemplate(remainder, out string template))
            {
                russianText = string.Format(template, DegreePrefixes[i].Russian);
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

    static bool TryTranslateLabelValueLines(string englishText, out string russianText)
    {
        russianText = null;

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

        russianText = string.Join("\n", lines);
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

        translatedLine = $"{translatedLabel}: {value}";
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

            translatedLine = leading + LinePrefixes[i].Russian + trimmed.Substring(englishPrefix.Length);
            return true;
        }

        return false;
    }

    static void Add(Dictionary<string, string> text, string english, string russian)
    {
        string key = NormalizeLookupKey(english);
        if (!string.IsNullOrEmpty(key))
            text[key] = russian;
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

using System;
using System.Collections.Generic;
using System.Text;

public static class TetrabeastsGermanTranslations
{
    static readonly Dictionary<string, string> ExactText = BuildExactText();

    static readonly Dictionary<string, string> RunModifierTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["decrease the amount of special gained from all sources."] = "{0} verringert die aus allen Quellen erhaltene Spezialenergie.",
        ["decreases the amount of special gained from all sources."] = "{0} verringert die aus allen Quellen erhaltene Spezialenergie.",
        ["increase the amount of special gained from all sources."] = "{0} erhöht die aus allen Quellen erhaltene Spezialenergie.",
        ["increases the amount of special gained from all sources."] = "{0} erhöht die aus allen Quellen erhaltene Spezialenergie.",
        ["decrease the special gained from each monster."] = "{0} verringert die von jedem Monster erhaltene Spezialenergie.",
        ["decreases the special gained from each monster."] = "{0} verringert die von jedem Monster erhaltene Spezialenergie.",
        ["increase the special gained from each monster."] = "{0} erhöht die von jedem Monster erhaltene Spezialenergie.",
        ["increases the special gained from each monster."] = "{0} erhöht die von jedem Monster erhaltene Spezialenergie.",
        ["decreases the attack value for all monsters in your current roster."] = "{0} verringert den Angriff aller Monster in deinem aktuellen Trupp.",
        ["decrease the attack value for all monsters in your current roster."] = "{0} verringert den Angriff aller Monster in deinem aktuellen Trupp.",
        ["increases the attack value for all monsters in your current roster."] = "{0} erhöht den Angriff aller Monster in deinem aktuellen Trupp.",
        ["increase the attack value for all monsters in your current roster."] = "{0} erhöht den Angriff aller Monster in deinem aktuellen Trupp.",
        ["decrease the damage of enemy projectiles."] = "{0} verringert den Schaden feindlicher Projektile.",
        ["decreases the damage of enemy projectiles."] = "{0} verringert den Schaden feindlicher Projektile.",
        ["incecrease the damage of enemy projectiles."] = "{0} erhöht den Schaden feindlicher Projektile.",
        ["incecreases the damage of enemy projectiles."] = "{0} erhöht den Schaden feindlicher Projektile.",
        ["increase the damage of enemy projectiles."] = "{0} erhöht den Schaden feindlicher Projektile.",
        ["increases the damage of enemy projectiles."] = "{0} erhöht den Schaden feindlicher Projektile.",
        ["increases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} erhöht die Zeit zwischen feindlichen Angriffen, ohne Boss-Abklingzeiten.",
        ["increase the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} erhöht die Zeit zwischen feindlichen Angriffen, ohne Boss-Abklingzeiten.",
        ["decreases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} verringert die Zeit zwischen feindlichen Angriffen, ohne Boss-Abklingzeiten.",
        ["decrease the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} verringert die Zeit zwischen feindlichen Angriffen, ohne Boss-Abklingzeiten.",
        ["increase the hit points of all future enemy fortifications."] = "{0} erhöht die Trefferpunkte aller künftigen feindlichen Befestigungen.",
        ["increases the hit points of all future enemy fortifications."] = "{0} erhöht die Trefferpunkte aller künftigen feindlichen Befestigungen.",
        ["decreases the rate falling speed builds up over time for blocks."] = "{0} verringert, wie schnell die Fallgeschwindigkeit von Blöcken mit der Zeit zunimmt.",
        ["decrease the rate falling speed builds up over time for blocks."] = "{0} verringert, wie schnell die Fallgeschwindigkeit von Blöcken mit der Zeit zunimmt.",
        ["increases the rate falling speed builds up over time for blocks."] = "{0} erhöht, wie schnell die Fallgeschwindigkeit von Blöcken mit der Zeit zunimmt.",
        ["increase the rate falling speed builds up over time for blocks."] = "{0} erhöht, wie schnell die Fallgeschwindigkeit von Blöcken mit der Zeit zunimmt.",
        ["decreases the initial falling speed of blocks."] = "{0} verringert die anfängliche Fallgeschwindigkeit von Blöcken.",
        ["decrease the initial falling speed of blocks."] = "{0} verringert die anfängliche Fallgeschwindigkeit von Blöcken.",
        ["increases the initial falling speed of blocks."] = "{0} erhöht die anfängliche Fallgeschwindigkeit von Blöcken.",
        ["increase the initial falling speed of blocks."] = "{0} erhöht die anfängliche Fallgeschwindigkeit von Blöcken.",
        ["increase the healing power of all friendly monsters."] = "{0} erhöht die Heilkraft aller verbündeten Monster.",
        ["increases the healing power of all friendly monsters."] = "{0} erhöht die Heilkraft aller verbündeten Monster.",
        ["decrease friendly monster pieces maximum hit points."] = "{0} verringert die maximalen Trefferpunkte verbündeter Monsterteile.",
        ["decreases friendly monster pieces maximum hit points."] = "{0} verringert die maximalen Trefferpunkte verbündeter Monsterteile.",
        ["increase friendly monster pieces maximum hit points."] = "{0} erhöht die maximalen Trefferpunkte verbündeter Monsterteile.",
        ["increases friendly monster pieces maximum hit points."] = "{0} erhöht die maximalen Trefferpunkte verbündeter Monsterteile.",
        ["increase luck raising the chance of getting higher rarity buffs."] = "{0} erhöht das Glück und damit die Chance auf Buffs höherer Seltenheit.",
        ["increases luck raising the chance of getting higher rarity buffs."] = "{0} erhöht das Glück und damit die Chance auf Buffs höherer Seltenheit.",
        ["increases the likelihood of finding higher rarity debuffs."] = "{0} erhöht die Chance, Debuffs höherer Seltenheit zu finden.",
        ["increase the likelihood of finding higher rarity debuffs."] = "{0} erhöht die Chance, Debuffs höherer Seltenheit zu finden.",
        ["decrease the number of reinforcement units added after winning a round."] = "{0} verringert die Zahl der Verstärkungen nach einem Rundensieg.",
        ["decreases the number of reinforcement units added after winning a round."] = "{0} verringert die Zahl der Verstärkungen nach einem Rundensieg.",
        ["increase the number of reinforcement units added after winning a round."] = "{0} erhöht die Zahl der Verstärkungen nach einem Rundensieg.",
        ["increases the number of reinforcement units added after winning a round."] = "{0} erhöht die Zahl der Verstärkungen nach einem Rundensieg.",
        ["decreases the chance of special blocks appearing."] = "{0} verringert die Chance, dass Spezialblöcke erscheinen.",
        ["decrease the chance of special blocks appearing."] = "{0} verringert die Chance, dass Spezialblöcke erscheinen.",
        ["incecreases the chance of special blocks appearing."] = "{0} erhöht die Chance, dass Spezialblöcke erscheinen.",
        ["incecrease the chance of special blocks appearing."] = "{0} erhöht die Chance, dass Spezialblöcke erscheinen.",
        ["increases the chance of special blocks appearing."] = "{0} erhöht die Chance, dass Spezialblöcke erscheinen.",
        ["increase the chance of special blocks appearing."] = "{0} erhöht die Chance, dass Spezialblöcke erscheinen.",
        ["decrease the chance a buff will drop from a stone obstacle."] = "{0} verringert die Chance, dass ein Buff aus einem Steinhindernis fällt.",
        ["decreases the chance a buff will drop from a stone obstacle."] = "{0} verringert die Chance, dass ein Buff aus einem Steinhindernis fällt.",
        ["increase the chance a buff will drop from a stone obstacle."] = "{0} erhöht die Chance, dass ein Buff aus einem Steinhindernis fällt.",
        ["increases the chance a buff will drop from a stone obstacle."] = "{0} erhöht die Chance, dass ein Buff aus einem Steinhindernis fällt.",
        ["decrease the maximum limit of the unit reserve."] = "{0} verringert das Maximum der Einheitenreserve.",
        ["decreases the maximum limit of the unit reserve."] = "{0} verringert das Maximum der Einheitenreserve.",
        ["increase the maximum limit of the unit reserve."] = "{0} erhöht das Maximum der Einheitenreserve.",
        ["increases the maximum limit of the unit reserve."] = "{0} erhöht das Maximum der Einheitenreserve.",
        ["reduces the amount of currency the player gains after winning a round."] = "{0} verringert die Menge Gold, die der Spieler nach einem Rundensieg erhält.",
        ["reduce the amount of currency the player gains after winning a round."] = "{0} verringert die Menge Gold, die der Spieler nach einem Rundensieg erhält.",
        ["increases the amount of currency the player gains after winning a round."] = "{0} erhöht die Menge Gold, die der Spieler nach einem Rundensieg erhält.",
        ["increase the amount of currency the player gains after winning a round."] = "{0} erhöht die Menge Gold, die der Spieler nach einem Rundensieg erhält.",
        ["increase the chance currency will be earned when clearing lines."] = "{0} erhöht die Chance, beim Löschen von Linien Gold zu erhalten.",
        ["increases the chance currency will be earned when clearing lines."] = "{0} erhöht die Chance, beim Löschen von Linien Gold zu erhalten."
    };

    static readonly (string English, string German)[] DegreePrefixes =
    {
        ("Slightly", "Leicht"),
        ("Modestly", "Mäßig"),
        ("Moderatley", "Mäßig"),
        ("Moderately", "Mäßig"),
        ("Significantly", "Deutlich"),
        ("Massivley", "Stark"),
        ("Massively", "Stark")
    };

    static readonly (string English, string German)[] LinePrefixes =
    {
        ("Special Gauge Gain", "Spezialenergie-Gewinn"),
        ("Enemy Damage", "Feindschaden"),
        ("Enemy HP", "Feind-TP"),
        ("Score Gain", "Punktegewinn"),
        ("EXP Gain", "EP-Gewinn"),
        ("Misfortune", "Unglück"),
        ("Gravity", "Schwerkraft"),
        ("Score", "Punkte"),
        ("Level", "Level"),
        ("Reset", "Zurücksetzen")
    };

    public static bool TryGetText(string englishText, out string germanText)
    {
        germanText = null;

        if (string.IsNullOrWhiteSpace(englishText))
            return false;

        string lookupKey = NormalizeLookupKey(englishText);
        if (ExactText.TryGetValue(lookupKey, out germanText))
            return true;

        if (TryTranslateRunModifierDescription(lookupKey, out germanText))
            return true;

        if (TryTranslateLabelValueLines(englishText, out germanText))
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
        Add(text, "OK", "OK");
        Add(text, "Cancel", "Abbrechen");
        Add(text, "Continue", "Weiter");
        Add(text, "Confirm", "Bestätigen");
        Add(text, "Close", "Schließen");
        Add(text, "Start", "Starten");
        Add(text, "PAUSED", "PAUSIERT");
        Add(text, "Resume", "Fortsetzen");
        Add(text, "Main Menu", "Hauptmenü");
        Add(text, "Restart", "Neustart");
        Add(text, "Save & Quit", "Speichern & beenden");
        Add(text, "Quit", "Beenden");
        Add(text, "New Game", "Neues Spiel");
        Add(text, "Shop", "Laden");
        Add(text, "Codex", "Kodex");
        Add(text, "Help", "Hilfe");
        Add(text, "HighScore", "Bestwert");
        Add(text, "Select Monsters", "Monster wählen");
        Add(text, "Select Commander", "Kommandanten wählen");
        Add(text, "Tip: Special blocks activate as soon as they are placed.", "Tipp: Spezialblöcke werden aktiviert, sobald sie platziert werden.");
        Add(text, "Tip: Rerolls can be saved and used on future reward screens during the same run.", "Tipp: Neuwürfe können gespeichert und in späteren Belohnungsbildschirmen desselben Laufs genutzt werden.");
        Add(text, "Tip: Temporary monster copies earn EXP during a run, and some of it becomes permanent after the run ends.", "Tipp: Temporäre Monsterkopien verdienen während eines Laufs EP, und ein Teil davon wird nach dem Lauf permanent.");
        Add(text, "Tip: Full rows launch attacks at the enemy castle.", "Tipp: Vollständige Reihen starten Angriffe auf die feindliche Burg.");
        Add(text, "Tip: Keep an eye on your unit reserve. If it reaches 0, the run is over.", "Tipp: Behalte deine Einheitenreserve im Auge. Wenn sie 0 erreicht, ist der Lauf vorbei.");
        Add(text, "Tip: Level modifiers stack with your run buffs and debuffs.", "Tipp: Levelmodifikatoren stapeln sich mit deinen Buffs und Debuffs des Laufs.");
        Add(text, "Combat Log", "Kampfprotokoll");
        Add(text, "{0} takes {1} damage.", "{0} erleidet {1} Schaden.");
        Add(text, "{0} heals {1}.", "{0} heilt {1}.");
        Add(text, "{0} dies.", "{0} stirbt.");
        Add(text, "{0} uses {1}.", "{0} benutzt {1}.");
        Add(text, "{0} casts {1}.", "{0} wirkt {1}.");
        Add(text, "{0} took {1}{2} damage{3}.", "{0} erlitt {1}{2} Schaden{3}.");
        Add(text, "{0} restored {1} health for {2}.", "{0} stellte {1} Gesundheit f\u00fcr {2} wieder her.");
        Add(text, "{0} dealt {1} damage to {2}.{3}", "{0} verursachte {1} Schaden an {2}.{3}");
        Add(text, " from {0}", " durch {0}");
        Add(text, "(shielded)", "(gesch\u00fctzt)");
        Add(text, "poison", "Gift");
        Add(text, "fire", "Feuer");
        Add(text, "lightning", "Blitz");
        Add(text, "contagion", "Ansteckung");
        Add(text, "starvation", "Hunger");
        Add(text, "burst", "Explosion");
        Add(text, "floor effect", "Bodeneffekt");
        Add(text, "storm", "Sturm");
        Add(text, "infection", "Infektion");
        Add(text, "low rations", "knappe Rationen");
        Add(text, "death burst", "Todesexplosion");
        Add(text, "spikes", "Stacheln");
        Add(text, "Enemy Archer", "feindlicher Bogensch\u00fctze");
        Add(text, "rear ambush", "Hinterhalt von hinten");
        Add(text, "Castle", "Burg");
        Add(text, "Skybreaker Edict", "Himmelsbrecher-Edikt");
        Add(text, "Heaven's Judgement", "Himmlisches Urteil");
        Add(text, "Stormcaller's Verdict", "Urteil des Sturmvaters");
        Add(text, "Hex of the Warped Ground", "Fluch des verzerrten Bodens");
        Add(text, "Aegis of the Unbroken Crown", "\u00c4gis der ungebrochenen Krone");
        Add(text, "Temporal Distortion", "Zeitverzerrung");
        Add(text, "Ward of the Arcane Pylons", "Schutz der arkanen Pylonen");
        Add(text, "Rune of Ruin", "Rune des Verderbens");
        Add(text, "Summon Earthen Rampart", "Erdwall beschw\u00f6ren");
        Add(text, "Raise Iron Thorns", "Eiserne Dornen erheben");
        Add(text, "Sow Venomous Miasma", "Giftiges Miasma s\u00e4en");
        Add(text, "Kindle Infernal Sigils", "H\u00f6llische Siegel entfachen");
        Add(text, "Call Stormbound Sigils", "Sturmgebundene Siegel rufen");
        Add(text, "Skip Trailer", "Trailer überspringen");
        Add(text, "BGM Genre", "Musikgenre");
        Add(text, "EDM", "EDM");
        Add(text, "Metal", "Metal-Musik");
        Add(text, "Random", "Zufällig");
        Add(text, "Language", "Sprache");
        Add(text, "Settings", "Einstellungen");
        Add(text, "Master Volume", "Gesamtlautstärke");
        Add(text, "Music Volume", "Musiklautstärke");
        Add(text, "SFX Volume", "Effektlautstärke");
        Add(text, "Cursor Size", "Cursorgröße");
        Add(text, "Choose Language", "Sprache wählen");
        Add(text, "Pick the language Tetrabeasts should use. You can change this later in Settings.", "Wähle die Sprache, die Tetrabeasts verwenden soll. Du kannst sie später in den Einstellungen ändern.");
        Add(text, "Press Any Key", "Beliebige Taste drücken");
        Add(text, "Modifiers", "Modifikatoren");
        Add(text, "Active Run Modifiers", "Aktive Laufmodifikatoren");
        Add(text, "Back", "Zurück");
        Add(text, "None", "Keine");
        Add(text, "Yes", "Ja");
        Add(text, "No", "Nein");
        Add(text, "Locked", "Gesperrt");
        Add(text, "Blocked", "Blockiert");
        Add(text, "Active", "Aktiv");
        Add(text, "(Missing)", "(Fehlt)");
        Add(text, "???", "???");
        Add(text, "Leaderboard", "Bestenliste");
        Add(text, "LEADERBOARDS", "BESTENLISTEN");
        Add(text, "Global", "Global");
        Add(text, "Friends", "Freunde");
        Add(text, "Current", "Aktuell");
        Add(text, "Rank", "Rang");
        Add(text, "Player", "Spieler");
        Add(text, "Refresh", "Aktualisieren");
        Add(text, "Loading...", "Lädt...");
        Add(text, "Refreshing leaderboards...", "Bestenlisten werden aktualisiert...");
        Add(text, "Steam leaderboard ready.", "Steam-Bestenliste bereit.");
        Add(text, "Leaderboard refresh failed.", "Bestenliste konnte nicht aktualisiert werden.");
        Add(text, "No leaderboard data.", "Keine Bestenlistendaten.");
        Add(text, "No global scores yet.", "Noch keine globalen Ergebnisse.");
        Add(text, "No friend scores yet.", "Noch keine Freundesergebnisse.");
        Add(text, "You are not ranked yet.", "Du bist noch nicht platziert.");
        Add(text, "Achievements", "Erfolge");
        Add(text, "Submit Name", "Namen senden");
        Add(text, "Help Menu", "Hilfemenü");
        Add(text, "Modifier not yet discovered.", "Modifikator noch nicht entdeckt.");
        Add(text, "No level Modifier", "Kein Levelmodifikator");
        Add(text, "Level modifier: {0}.", "Levelmodifikator: {0}.");
        Add(text, "Secret Achievements", "Geheime Erfolge");
        Add(text, "1 secret achievement remaining", "Noch 1 geheimer Erfolg übrig");
        Add(text, "{0} secret achievements remaining", "Noch {0} geheime Erfolge übrig");
        Add(text, "No GameController found.", "Kein GameController gefunden.");
        Add(text, "No monsters in roster.", "Keine Monster im Trupp.");
        Add(text, "Reinforcements", "Verstärkungen");
        Add(text, "Active Squad", "Aktiver Trupp");
        Add(text, "Choose A Buff", "Buff wählen");
        Add(text, "Choose A Debuff", "Debuff wählen");
        Add(text, "Next Piece", "Nächste Figur");
        Add(text, "Reroll", "Neu würfeln");
        Add(text, "EXP Gained", "EP erhalten");
        Add(text, "Permanent EXP Gained", "Permanente EP erhalten");
        Add(text, "EXP Preserved", "EP erhalten");
        Add(text, "New Difficulty Unlocked!", "Neue Schwierigkeit freigeschaltet!");
        Add(text, "Special Ability: {0}", "Spezialfähigkeit: {0}");
        Add(text, "{0} is broken.", "{0} ist zerbrochen.");
        Add(text, "{0}: {1} shield(s) remain.", "{0}: {1} Schild(er) verbleiben.");
    }

    static void AddRecentFixupText(Dictionary<string, string> text)
    {
        Add(text, "Starting a new game will erase the saved run. Continue?", "Ein neues Spiel löscht den gespeicherten Lauf. Fortfahren?");
        Add(text, "Current Level: {0}", "Aktuelles Level: {0}");
        Add(text, "Current Level: 0", "Aktuelles Level: 0");
        Add(text, " x{0}", " x{0}");
        Add(text, "Refund", "Erstatten");
        Add(text, "Unlock", "Freischalten");
        Add(text, " Unlock", " Freischalten");
        Add(text, "Selected:", "Ausgewählt:");
        Add(text, "Selected: ", "Ausgewählt: ");
        Add(text, "{0}  Lv.{1}", "{0}  St.{1}");
        Add(text, "Luck Up", "Glück +");
        Add(text, "Gravity Down", "Schwerkraft -");
        Add(text, "Velocity Down", "Geschwindigkeit -");
        Add(text, "Gold Up", "Gold +");
        Add(text, "Attack Up", "Angriff +");
        Add(text, "HP Up", "TP +");
        Add(text, "Unit Lives Up", "Einheitenleben +");
        Add(text, "Role: {0}", "Rolle: {0}");
        Add(text, "Level: {0}  ({1:0.#}/{2})", "Level: {0}  ({1:0.#}/{2})");
        Add(text, "Max HP: {0:0.#}  (+{1}) = {2:0.#}", "Max. TP: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Attack: {0:0.#}  (+{1}) = {2:0.#}", "Angriff: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Special Gain: {0:0.#}", "Spezialgewinn: {0:0.#}");
        Add(text, "Heal: {0:0.#}  (+{1}) = {2:0.#}", "Heilung: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Heal Range: {0:0.#}", "Heilreichweite: {0:0.#}");
        Add(text, "Heal Speed: {0:0.#}s", "Heiltempo: {0:0.#} s");
        Add(text, "Heal: -", "Heilung: -");
        Add(text, "Base Stats + Shop Buff = Total Stats", "Basiswerte + Shop-Buff = Gesamtwerte");
        Add(text, "Defense", "Verteidigung");
        Add(text, "Beefy boy that deals very little damage.", "Robuster Kerl, der sehr wenig Schaden verursacht.");
        Add(text, "Healer with a wide range, but weak spells.", "Heilerin mit großer Reichweite, aber schwachen Zaubern.");
        Add(text, "A well rounded attacker unit with decent damage, but enough health to survive weaker attacks.", "Ausgewogene Angriffseinheit mit ordentlichem Schaden und genug Leben für schwächere Angriffe.");
        Add(text, "A specialized attacker unit with high base damage and lower health.", "Spezialisierte Angriffseinheit mit hohem Grundschaden und wenig Leben.");
        Add(text, "Can take a hit and keep going, but won't deal much damage.", "Kann Treffer einstecken und weitermachen, verursacht aber wenig Schaden.");
        Add(text, "Healer with a short range, but powerful healing spells.", "Heilerin mit kurzer Reichweite, aber starken Heilzaubern.");
        Add(text, "Combo Extension", "Kombo-Verlängerung");
        Add(text, "Chain Surge", "Kettenwoge");
        Add(text, "Stone Scrounger", "Steinsammler");
        Add(text, "Reserve Stockpile", "Reservevorrat");
        Add(text, "Reserve Recovery", "Reserveerholung");
        Add(text, "Bulwark Aura", "Bollwerk-Aura");
        Add(text, "Increase combo timer duration by {0}.", "Erhöht die Dauer des Kombo-Timers um {0}.");
        Add(text, "Each row clear has a {0} chance to increase combo count one additional time.", "Jede gelöschte Reihe hat eine Chance von {0}, den Kombozähler ein weiteres Mal zu erhöhen.");
        Add(text, "Increase chance of buff drop from stone obstacle destruction by {0}.", "Erhöht die Chance auf Buffs beim Zerstören von Steinhindernissen um {0}.");
        Add(text, "Increase the number of starting reserve units by {0}.", "Erhöht die Anzahl der Start-Reserveeinheiten um {0}.");
        Add(text, "Increase the number of reserve units restored on round win by {0}.", "Erhöht die bei Rundensieg wiederhergestellten Reserveeinheiten um {0}.");
        Add(text, "Decrease damage taken and damage done for all ally monster units by {0}.", "Verringert erlittenen und verursachten Schaden aller verbündeten Monster um {0}.");
        Add(text, "Passive - {0}", "Passiv - {0}");
        Add(text, "Passive - {0}:", "Passiv - {0}:");
        Add(text, "Next upgrade at Lv.{0}:", "Nächstes Upgrade auf St.{0}:");
        Add(text, "Passive is fully upgraded.", "Passivfähigkeit ist vollständig verbessert.");
        Add(text, "1 second", "1 Sekunde");
        Add(text, "{0} seconds", "{0} Sekunden");
        Add(text, "A = Shift Left", "A = nach links schieben");
        Add(text, "D = Shift Right", "D = nach rechts schieben");
        Add(text, "D = Shft Right", "D = nach rechts schieben");
        Add(text, "R = Character Special", "R = Charakter-Spezial");
        Add(text, "= Character Special", "= Charakter-Spezial");
        Add(text, "R = Character Special (Special Guage 100%)", "R = Charakter-Spezial (Spezialenergie 100%)");
        Add(text, "R = Character Special (Special Gauge 100%)", "R = Charakter-Spezial (Spezialenergie 100%)");
        Add(text, "R = Character Special (Specaial Gauge 100%)", "R = Charakter-Spezial (Spezialenergie 100%)");
        Add(text, "= Character Special (Special Guage 100%)", "= Charakter-Spezial (Spezialenergie 100%)");
        Add(text, "= Character Special (Special Gauge 100%)", "= Charakter-Spezial (Spezialenergie 100%)");
        Add(text, "Boss", "Boss");
        Add(text, "Boss Ability", "Bossfähigkeit");
        Add(text, "Boss Abilities", "Bossfähigkeiten");

        Add(text, "Gold Won This Round:", "Gold in dieser Runde:");
        Add(text, "Gold Won This Round", "Gold in dieser Runde");
        Add(text, "Rerolls", "Neuwürfe");
        Add(text, "Rerolls: {0}", "Neuwürfe: {0}");
        Add(text, "Rerolls: 0", "Neuwürfe: 0");
        Add(text, "Rerolls ({0})", "Neuwürfe ({0})");
        Add(text, "Modifier", "Modifikator");
        Add(text, "x{0}", "x{0}");

        Add(text, "Level Up", "Stufenaufstieg");
        Add(text, "Level Up!", "Stufenaufstieg!");
        Add(text, "LEVEL UP!", "STUFENAUFSTIEG!");
        Add(text, "LEVEL UP! x{0}", "STUFENAUFSTIEG! x{0}");
        Add(text, "Level {0} -> Level {1}", "Level {0} -> Level {1}");
        Add(text, "+{0} Exp", "+{0} EP");
        Add(text, "{0} permanent EXP ({1}% of {2} transferable EXP)", "{0} permanente EP ({1}% von {2} übertragbaren EP)");
        Add(text, "Converted from {0} run EXP at {1}%", "Aus {0} Lauf-EP mit {1}% umgewandelt");

        Add(text, "Passive+", "Passiv+");
        Add(text, "+ 5 HP", "+ 5 TP");
        Add(text, "+ 1 Attack", "+ 1 Angriff");
        Add(text, "+ 1 Special", "+ 1 Spezial");
        Add(text, "+ 5 Heal", "+ 5 Heilung");
        Add(text, "+ 1 Range", "+ 1 Reichweite");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Als Nächstes sollst du deine aktuelle Figur mit der [Leertaste] hart fallen lassen. Dadurch fällt sie sofort, sodass du sie schnell platzieren, einrasten lassen und eine neue erzeugen kannst.");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Als Nächstes sollst du deine aktuelle Figur mit der [Leertaste] hart fallen lassen. Dadurch fällt sie sofort, sodass du sie schnell platzieren, einrasten lassen und eine neue erzeugen kannst.");
        Add(text, "This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Dadurch fällt deine Figur sofort, sodass du sie schnell platzieren, einrasten lassen und eine neue erzeugen kannst.");
        Add(text, "This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Dadurch fällt deine Figur sofort, sodass du sie schnell platzieren, einrasten lassen und eine neue erzeugen kannst.");
        AddAchievementText(text);
    }

    static void AddAchievementText(Dictionary<string, string> text)
    {
        Add(text, "I Want Every Able Body", "Ich will jeden verfügbaren Kämpfer");
        Add(text, "Unlock all monster units.", "Schalte alle Monstereinheiten frei.");
        Add(text, "0 Star Victory", "0-Sterne-Sieg");
        Add(text, "Beat the final boss.", "Besiege den Endboss.");
        Add(text, "One Star Victory", "Ein-Stern-Sieg");
        Add(text, "Beat the final boss on 1-Star difficulty.", "Besiege den Endboss auf 1-Stern-Schwierigkeit.");
        Add(text, "Pay To Win", "Pay to Win");
        Add(text, "Upgrade any shop buff to level 5.", "Verbessere einen beliebigen Shop-Buff auf Level 5.");
        Add(text, "Gangs All Here", "Die ganze Bande ist da");
        Add(text, "Unlock all Commanders.", "Schalte alle Kommandanten frei.");
        Add(text, "Two Star Victory", "Zwei-Sterne-Sieg");
        Add(text, "Beat the final boss on 2-Star difficulty.", "Besiege den Endboss auf 2-Sterne-Schwierigkeit.");
        Add(text, "Three Star Victory", "Drei-Sterne-Sieg");
        Add(text, "Beat the final boss on 3-Star difficulty.", "Besiege den Endboss auf 3-Sterne-Schwierigkeit.");
        Add(text, "Four Star Victory", "Vier-Sterne-Sieg");
        Add(text, "Beat the final boss on 4-Star difficulty.", "Besiege den Endboss auf 4-Sterne-Schwierigkeit.");
        Add(text, "Five Star Victory", "Fünf-Sterne-Sieg");
        Add(text, "Beat the final boss on 5-Star difficulty.", "Besiege den Endboss auf 5-Sterne-Schwierigkeit.");
        Add(text, "This is Fine", "Alles in Ordnung");
        Add(text, "Take 1,000 burn damage from fire floor effects.", "Erleide 1.000 Brandschaden durch Feuerbodeneffekte.");
        Add(text, "That Escalated Quickly", "Das eskalierte schnell");
        Add(text, "Remove 1,000 units using the Death Special Block.", "Entferne 1.000 Einheiten mit dem Todes-Spezialblock.");
        Add(text, "First Time Raider", "Erstürmer zum ersten Mal");
        Add(text, "Take 1,000 damage from traps.", "Erleide 1.000 Schaden durch Fallen.");
        Add(text, "I think I Stepped in Something", "Ich glaube, ich bin in etwas getreten");
        Add(text, "Take 1,000 toxic damage from posioned floor effects.", "Erleide 1.000 Giftschaden durch vergiftete Bodeneffekte.");
        Add(text, "Take 1,000 toxic damage from poisoned floor effects.", "Erleide 1.000 Giftschaden durch vergiftete Bodeneffekte.");
        Add(text, "Shake It Until You Break It", "Schütteln, bis es bricht");
        Add(text, "Clear 250 rows by using the earthquake special block.", "Lösche 250 Reihen mit dem Erdbeben-Spezialblock.");
        Add(text, "A Little Jiggle Goes A Long Way", "Ein kleines Wackeln hilft viel");
        Add(text, "Clear 25 rows by using the earthquake special block.", "Lösche 25 Reihen mit dem Erdbeben-Spezialblock.");
        Add(text, "Girthquake", "Megabeben");
        Add(text, "Clear 1,000 rows by using the earthquake special block.", "Lösche 1.000 Reihen mit dem Erdbeben-Spezialblock.");
        Add(text, "Get in Loser, We're Going Shopping", "Steig ein, wir gehen shoppen");
        Add(text, "Accumulate 1,000 gold.", "Sammle 1.000 Gold.");
        Add(text, "Is I Rich Now?", "Bin ich jetzt reich?");
        Add(text, "Accumulate 100 gold.", "Sammle 100 Gold.");
        Add(text, "This Lasted Longer Than Some Collectible Fads", "Das hielt länger als manche Sammeltrends");
        Add(text, "Take more than 5 minutes to conquer a castle.", "Brauche mehr als 5 Minuten, um eine Burg zu erobern.");
        Add(text, "Anything You Can Do, I Can Do Slower", "Alles, was du kannst, kann ich langsamer");
        Add(text, "Take more than 3 minutes to conquer a castle.", "Brauche mehr als 3 Minuten, um eine Burg zu erobern.");
        Add(text, "Sloth Lord", "Herr der Trägheit");
        Add(text, "Take more than 4 minutes to conquer a castle.", "Brauche mehr als 4 Minuten, um eine Burg zu erobern.");
        Add(text, "My Fingers Hurt", "Meine Finger tun weh");
        Add(text, "Survive with gravity at 10 for 60 seconds.", "Überlebe 60 Sekunden bei Schwerkraft 10.");
        Add(text, "Nevermind...", "Schon gut...");
        Add(text, "Unlock your first first temporary debuff.", "Schalte deinen ersten temporären Debuff frei.");
        Add(text, "A Record That Would Make Lions Blush", "Ein Rekord, der rot werden lässt");
        Add(text, "Lose 50 Times.", "Verliere 50 Mal.");
        Add(text, "Thrive Under Pressure", "Unter Druck aufblühen");
        Add(text, "Survive with gravity at 10 for 30 seconds.", "Überlebe 30 Sekunden bei Schwerkraft 10.");
        Add(text, "Is I Strong Now?", "Bin ich jetzt stark?");
        Add(text, "Unlock your first first temporary buff.", "Schalte deinen ersten temporären Buff frei.");
        Add(text, "I Say This Not As An Insult, But As A Statement Of Fact", "Das ist keine Beleidigung, nur eine Tatsache");
        Add(text, "Lose 100 Times.", "Verliere 100 Mal.");
        Add(text, "I Think We Need A Bigger Vault", "Ich glaube, wir brauchen einen größeren Tresor");
        Add(text, "Accumulate 10,000 gold.", "Sammle 10.000 Gold.");
        Add(text, "GG EZ", "GG EZ");
        Add(text, "Beat the final level with every Commander.", "Schließe das letzte Level mit jedem Kommandanten ab.");
        Add(text, "Participation Trophy", "Teilnahmepokal");
        Add(text, "Lose for the first time.", "Verliere zum ersten Mal.");
        Add(text, "Immortal Army", "Unsterbliche Armee");
        Add(text, "Conquer 100 castles with your Unit Reserve at max capacity.", "Erobere 100 Burgen mit voller Einheitenreserve.");
        Add(text, "I Guess That Was The Wrong Wire", "Das war wohl der falsche Draht");
        Add(text, "Lose 100 monster units to magic explosives.", "Verliere 100 Monstereinheiten durch magische Sprengstoffe.");
        Add(text, "I Think It's Dead Now", "Ich glaube, jetzt ist es tot");
        Add(text, "Clear 1,000 rows.", "Lösche 1.000 Reihen.");
        Add(text, "Tis But A Scratch", "Nur ein Kratzer");
        Add(text, "Conquer a castle with your Unit Reserve at max capacity.", "Erobere eine Burg mit voller Einheitenreserve.");
        Add(text, "Meat Shield Tactics", "Fleischschild-Taktiken");
        Add(text, "Conquer 25 castles with your Unit Reserve at max capacity.", "Erobere 25 Burgen mit voller Einheitenreserve.");
        Add(text, "You Got Your Marching Orders", "Du hast deine Marschbefehle");
        Add(text, "Clear 100 rows.", "Lösche 100 Reihen.");
        Add(text, "Overwhelming Power", "Überwältigende Macht");
        Add(text, "Accumulate 15 buffs in a single run.", "Sammle 15 Buffs in einem einzigen Lauf.");
        Add(text, "Is There Anything Left To Attack?", "Gibt es noch etwas anzugreifen?");
        Add(text, "Clear 10,000 rows.", "Lösche 10.000 Reihen.");
        Add(text, "Shut Up And Takey My Money", "Halt den Mund und nimm mein Geld");
        Add(text, "Buy an upgrade from the shop for the first time.", "Kaufe zum ersten Mal ein Upgrade im Shop.");
        Add(text, "Turning Small Numbers Into Big Numbers", "Aus kleinen Zahlen große machen");
        Add(text, "Get a combo of 10 or higher.", "Erreiche eine Kombo von 10 oder höher.");
        Add(text, "Vewwy Stwong", "Sehw stawk");
        Add(text, "Deal 100 damage or more in a single attack.", "Verursache 100 oder mehr Schaden mit einem einzigen Angriff.");
        Add(text, "I'll Follow You Anywhere", "Ich folge dir überallhin");
        Add(text, "Unlock a Commander for the first time.", "Schalte zum ersten Mal einen Kommandanten frei.");
        Add(text, "Eenie Meenie Miney Mo", "Ene mene muh");
        Add(text, "Unlock a monster unit for the first time.", "Schalte zum ersten Mal eine Monstereinheit frei.");
        Add(text, "Tell Me I'm Pretty", "Sag mir, dass ich hübsch bin");
        Add(text, "Unlock a monster units skin variant for the first time.", "Schalte zum ersten Mal eine Skin-Variante einer Monstereinheit frei.");
        Add(text, "Are We There Yet?", "Sind wir schon da?");
        Add(text, "Conquer 10 castles in a single run.", "Erobere 10 Burgen in einem einzigen Lauf.");
        Add(text, "It's Called Fashion Brenda, Look It Up", "Das nennt man Mode, schlag es nach");
        Add(text, "Unlock ten skin variants.", "Schalte zehn Skin-Varianten frei.");
        Add(text, "New Skin Who Dis?", "Neuer Skin, wer ist das?");
        Add(text, "Unlock five skin variants.", "Schalte fünf Skin-Varianten frei.");
        Add(text, "A Little Special", "Ein bisschen besonders");
        Add(text, "Use a special for the first time.", "Nutze zum ersten Mal ein Spezial.");
        Add(text, "Mama's Special Boy", "Mamas besonderer Junge");
        Add(text, "Use a special 100 times.", "Nutze ein Spezial 100 Mal.");
        Add(text, "Some Are More Special Than Others", "Manche sind besonderer als andere");
        Add(text, "Use every Commanders special 100 times.", "Nutze das Spezial jedes Kommandanten 100 Mal.");
        Add(text, "General Got Me Workin'", "Der General lässt mich schuften");
        Add(text, "Destory 100 stone obstacles.", "Zerstöre 100 Steinhindernisse.");
        Add(text, "Destroy 100 stone obstacles.", "Zerstöre 100 Steinhindernisse.");
        Add(text, "Certified Glue Eater", "Zertifizierter Leimesser");
        Add(text, "Use a special 1000 times.", "Nutze ein Spezial 1000 Mal.");
        Add(text, "We're All Special", "Wir sind alle besonders");
        Add(text, "Use every Commanders special 20 times.", "Nutze das Spezial jedes Kommandanten 20 Mal.");
        Add(text, "That's A Lot Of Rubble", "Das ist eine Menge Schutt");
        Add(text, "Conquer 50 castles.", "Erobere 50 Burgen.");
        Add(text, "King of Rubble", "König des Schutts");
        Add(text, "Conquer 100 castles.", "Erobere 100 Burgen.");
        Add(text, "I. AM. SPEED!", "ICH. BIN. TEMPO!");
        Add(text, "Conquer a castle in 30 seconds or less.", "Erobere eine Burg in 30 Sekunden oder weniger.");
        Add(text, "Can't Stop, Won't Stop", "Kann nicht stoppen, will nicht stoppen");
        Add(text, "Conquer your first castle.", "Erobere deine erste Burg.");
        Add(text, "I Ran Track In Highschool", "Ich war in der Schule im Laufteam");
        Add(text, "Conquer a castle in 45 seconds or less.", "Erobere eine Burg in 45 Sekunden oder weniger.");
        Add(text, "Gotta Go Fast", "Muss schnell gehen");
        Add(text, "Conquer a castle in 60 seconds or less.", "Erobere eine Burg in 60 Sekunden oder weniger.");
    }

    static void AddXpBreakdownText(Dictionary<string, string> text)
    {
        Add(text, "Level {0} Complete", "Level {0} abgeschlossen");
        Add(text, "Base Level XP", "Basis-Level-EP");
        Add(text, "Clear time {0}", "Abschlusszeit {0}");
        Add(text, "Units lost {0}", "Einheiten verloren {0}");
        Add(text, "Largest Combo {0}", "Größte Kombo {0}");
        Add(text, "Obstacles Cleared {0}", "Hindernisse beseitigt {0}");
        Add(text, "Star Difficulty ({0}):", "Sterne-Schwierigkeit ({0}):");
        Add(text, "Total XP Earned {0}", "Gesamte EP erhalten {0}");
    }

    static void AddCommonLabels(Dictionary<string, string> text)
    {
        Add(text, "Run", "Lauf");
        Add(text, "Level", "Level");
        Add(text, "Controls", "Steuerung");
        Add(text, "Gravity", "Schwerkraft");
        Add(text, "Combo", "Kombo");
        Add(text, "Score", "Punkte");
        Add(text, "Reset", "Zurücksetzen");
        Add(text, "Score: {0}", "Punkte: {0}");
        Add(text, "Gravity: {0:0.0}", "Schwerkraft: {0:0.0}");
        Add(text, "Star Difficulty", "Sterne-Schwierigkeit");
        Add(text, "0 Star: Recruit Difficulty", "0 Sterne: Rekruten-Schwierigkeit");
        Add(text, "1 Star: Soldier Difficulty", "1 Stern: Soldaten-Schwierigkeit");
        Add(text, "2 Star: Veteran Difficulty", "2 Sterne: Veteranen-Schwierigkeit");
        Add(text, "3 Star: Lieutenant Difficulty", "3 Sterne: Leutnant-Schwierigkeit");
        Add(text, "4 Star: General Difficulty", "4 Sterne: General-Schwierigkeit");
        Add(text, "5 Star: War God Difficulty", "5 Sterne: Kriegsgott-Schwierigkeit");
        Add(text, "0-Star Difficulty", "0-Sterne-Schwierigkeit");
        Add(text, "1 Star Difficulty", "1-Stern-Schwierigkeit");
        Add(text, "2 Stars Difficulty", "2-Sterne-Schwierigkeit");
        Add(text, "3 Stars Difficulty", "3-Sterne-Schwierigkeit");
        Add(text, "4 Stars Difficulty", "4-Sterne-Schwierigkeit");
        Add(text, "5 Stars Difficulty", "5-Sterne-Schwierigkeit");
        Add(text, "Normal difficulty.", "Normale Schwierigkeit.");
        Add(text, "No gameplay modifiers.", "Keine Spielmodifikatoren.");
        Add(text, "All star difficulties unlocked.", "Alle Sterne-Schwierigkeiten freigeschaltet.");
        Add(text, "0 Stars is always available.", "0 Sterne sind immer verfügbar.");
        Add(text, "Beat the final level on 0 Stars to unlock 1 Star.", "Schließe das letzte Level auf 0 Sternen ab, um 1 Stern freizuschalten.");
        Add(text, "Beat the final level on 1 Star to unlock 2 Stars.", "Schließe das letzte Level auf 1 Stern ab, um 2 Sterne freizuschalten.");
        Add(text, "Beat the final level on 2 Stars to unlock 3 Stars.", "Schließe das letzte Level auf 2 Sternen ab, um 3 Sterne freizuschalten.");
        Add(text, "Beat the final level on 3 Stars to unlock 4 Stars.", "Schließe das letzte Level auf 3 Sternen ab, um 4 Sterne freizuschalten.");
        Add(text, "Beat the final level on 4 Stars to unlock 5 Stars.", "Schließe das letzte Level auf 4 Sternen ab, um 5 Sterne freizuschalten.");
        Add(text, "0 Stars", "0 Sterne");
        Add(text, "1 Star", "1 Stern");
        Add(text, "2 Stars", "2 Sterne");
        Add(text, "3 Stars", "3 Sterne");
        Add(text, "4 Stars", "4 Sterne");
        Add(text, "5 Stars", "5 Sterne");
        Add(text, "Score Gain", "Punktegewinn");
        Add(text, "EXP Gain", "EP-Gewinn");
        Add(text, "Level Modifier", "Levelmodifikator");
        Add(text, "Reserves And Rewards", "Reserven und Belohnungen");
        Add(text, "Reserve Units", "Reserveeinheiten");
        Add(text, "Max Reserve Units", "Max. Reserveeinheiten");
        Add(text, "Reserve Restored On Win", "Reserve bei Sieg wiederhergestellt");
        Add(text, "Round Win Currency", "Gold bei Rundensieg");
        Add(text, "Line Clear Currency Chance", "Goldchance beim Linienlöschen");
        Add(text, "Line Clear Currency Amount", "Goldmenge beim Linienlöschen");
        Add(text, "Monster Combat", "Monsterkampf");
        Add(text, "Monster Damage", "Monsterschaden");
        Add(text, "Monster Special Gain", "Spezialgewinn der Monster");
        Add(text, "Monster Max HP", "Max. TP der Monster");
        Add(text, "Healing Power", "Heilkraft");
        Add(text, "Healing Range Bonus", "Bonus-Heilreichweite");
        Add(text, "Ally Damage Dealt", "Verbündetenschaden verursacht");
        Add(text, "Ally Damage Taken", "Verbündetenschaden erlitten");
        Add(text, "Combo And Passives", "Kombo und Passive");
        Add(text, "Combo Window", "Kombo-Fenster");
        Add(text, "Bonus Combo Chance", "Bonus-Kombo-Chance");
        Add(text, "Stone Buff Drop Chance", "Stein-Buff-Chance");
        Add(text, "Starting Reserve Passive", "Startreserve-Passiv");
        Add(text, "Round Win Reserve Passive", "Rundensieg-Reserve-Passiv");
        Add(text, "Enemy", "Feind");
        Add(text, "Enemy Castle HP", "TP der feindlichen Burg");
        Add(text, "Enemy Damage", "Feindschaden");
        Add(text, "Enemy Attack Interval", "Feindlicher Angriffsintervall");
        Add(text, "Enemy Projectile Speed", "Geschwindigkeit feindlicher Projektile");
        Add(text, "Castle Projectile Damage", "Burgprojektil-Schaden");
        Add(text, "Castle Attack Interval", "Burg-Angriffsintervall");
        Add(text, "Piece And Special", "Figur und Spezial");
        Add(text, "Piece Gravity", "Figur-Schwerkraft");
        Add(text, "Gravity Ramp Rate", "Schwerkraft-Steigerungsrate");
        Add(text, "Special Block Chance", "Spezialblock-Chance");
        Add(text, "Commander Special Gain", "Spezialgewinn des Kommandanten");
        Add(text, "Special Drain", "Spezialentzug");
        Add(text, "Next Preview Disabled", "Nächste Vorschau deaktiviert");
        Add(text, "Landing Hint Disabled", "Landehilfe deaktiviert");
        Add(text, "Special Usage Locked", "Spezialnutzung gesperrt");
        Add(text, "Special Blocks Blocked", "Spezialblöcke blockiert");
        Add(text, "Run Modifier Drops", "Laufmodifikator-Drops");
        Add(text, "Stone Drops Debuffs Only", "Steine geben nur Debuffs");
        Add(text, "Luck", "Glück");
        Add(text, "Misfortune", "Unglück");
        Add(text, "Active Level Modifier", "Aktiver Levelmodifikator");
        Add(text, "Effect", "Effekt");
        Add(text, "Outgoing Damage", "Ausgehender Schaden");
        Add(text, "Incoming Damage", "Eingehender Schaden");
        Add(text, "Overgrowth Target Interval", "Überwucherungsziel-Intervall");
        Add(text, "Initial Target Rows", "Anfängliche Zielreihen");
        Add(text, "Partial Growth Time", "Teilwachstumszeit");
        Add(text, "Full Growth Time", "Vollwachstumszeit");
        Add(text, "Storm Strike Damage", "Sturmschlag-Schaden");
        Add(text, "Storm Floor Tick Damage", "Sturmboden-Tickschaden");
        Add(text, "Storm Floor Duration", "Sturmboden-Dauer");
        Add(text, "Rear Ambush Interval", "Hinterhalt-Intervall");
        Add(text, "Rations Tick Interval", "Rationen-Tickintervall");
        Add(text, "Low Reserve Damage", "Schaden bei niedriger Reserve");
        Add(text, "High Reserve Damage", "Schaden bei hoher Reserve");
        Add(text, "Infection Chance", "Infektionschance");
        Add(text, "Damage Per Tick", "Schaden pro Tick");
        Add(text, "Damage Increase Per Tick", "Schadensanstieg pro Tick");
        Add(text, "Spread Chance", "Ausbreitungschance");
        Add(text, "Special Gauge Gain", "Spezialenergie-Gewinn");
        Add(text, "Death Explosion Damage", "Todesexplosionsschaden");
        Add(text, "Swamp Poison Damage", "Sumpfgiftschaden");
        Add(text, "Swamp Poison Interval", "Sumpfgift-Intervall");
        Add(text, "Manual Rotation", "Manuelle Rotation");
        Add(text, "Auto Rotate Interval", "Autorotationsintervall");
        Add(text, "Manual Horizontal Shift", "Manueller horizontaler Schub");
        Add(text, "Auto Shift Interval", "Autoschub-Intervall");
        Add(text, "Combo Threshold", "Kombo-Schwelle");
        Add(text, "Below Threshold Damage", "Schaden unter Schwelle");
        Add(text, "Shield Combo Threshold", "Schild-Kombo-Schwelle");
        Add(text, "Blocked Damage", "Blockierter Schaden");
        Add(text, "Shield Count", "Schildanzahl");
        Add(text, "Special Pieces", "Spezialfiguren");
        Add(text, "Starting Monster Health", "Startleben der Monster");
        Add(text, "Monster Damage Sharing", "Schadensteilung der Monster");
        Add(text, "Roster", "Trupp");
        Add(text, "Max HP", "Max. TP");
        Add(text, "Starting HP", "Start-TP");
        Add(text, "Attack", "Angriff");
        Add(text, "Special Gain", "Spezialgewinn");
        Add(text, "Heal Power", "Heilkraft");
        Add(text, "Heal Range", "Heilreichweite");
        Add(text, "Heal Speed", "Heiltempo");
        Add(text, "Spawn Weight", "Spawn-Gewichtung");
        Add(text, "Stats", "Werte");
        Add(text, "Monsters", "Monster");
        Add(text, "Shop Buff", "Shop-Buff");
        Add(text, "Passive", "Passiv");
        Add(text, "Run Mod Buff", "Laufmod-Buff");
        Add(text, "Run Mod Debuff", "Laufmod-Debuff");
        Add(text, "Level Mod", "Levelmod");
        Add(text, "Boss Ability", "Bossfähigkeit");
        Add(text, "Defender", "Verteidiger");
        Add(text, "Healer", "Heiler");
        Add(text, "Role", "Rolle");
        Add(text, "HP", "TP");
        Add(text, "Special Rate", "Spezialrate");
        Add(text, "Passive Lv", "Passiv-St.");
        Add(text, "Esc = Pause", "Esc = Pause");
        Add(text, "Q = Rotate Counter Clockwise", "Q = gegen Uhrzeigersinn drehen");
        Add(text, "E = Rotate Clockwise", "E = im Uhrzeigersinn drehen");
        Add(text, "= Shift Left", "= nach links schieben");
        Add(text, "S = Shift Down", "S = nach unten schieben");
        Add(text, "Spacebar = Drop Instantly", "Leertaste = sofort fallen lassen");
        Add(text, "= Character Special (Specaial Gauge 100%)", "= Charakter-Spezial (Spezialenergie 100%)");
        Add(text, "Castle", "Burg");
        Add(text, "Starting Village", "Startdorf");
        Add(text, "Tribe", "Stamm");
        Add(text, "Tribe Chieftain", "Stammeshäuptling");
        Add(text, "Shanty Town", "Armensiedlung");
        Add(text, "Fortified Hamlet", "Befestigter Weiler");
        Add(text, "Industrial City", "Industriestadt");
        Add(text, "Thriving Metropolis", "Blühende Metropole");
        Add(text, "Grand Dukedom", "Großherzogtum");
        Add(text, "Royal Villa", "Königliche Villa");
        Add(text, "Ruk, Tribal Chieftain", "Ruk, Stammeshäuptling");
        Add(text, "Fionne, Village Protector", "Fionne, Beschützerin des Dorfes");
        Add(text, "Sir Ralphie, Captain of the Guard", "Sir Ralphie, Hauptmann der Wache");
        Add(text, "Eris, City Priestess", "Eris, Stadtpriesterin");
        Add(text, "His Holiness Isaeh, Metropolis Pope", "Seine Heiligkeit Isaeh, Papst der Metropole");
        Add(text, "Vivica, Dukedom Arch Mage", "Vivica, Erzmagierin des Herzogtums");
        Add(text, "Emperor Reginald P. Exford IV, Emperor's Palace", "Kaiser Reginald P. Exford IV, Kaiserpalast");
        Add(text, "Esora, Guardian to the Gates of Heaven?", "Esora, Wächterin der Himmelstore?");
    }

    static void AddWarningAndTutorialText(Dictionary<string, string> text)
    {
        Add(text, "Purchases are disabled in the demo. Your earned progress will still carry into the full game.",
            "Käufe sind in der Demo deaktiviert. Dein erspielter Fortschritt wird trotzdem ins vollständige Spiel übernommen.");
        Add(text, "A saved run is waiting. Continue that run or delete the temp save before changing your commander, squad, or shop buffs.",
            "Ein gespeicherter Lauf wartet. Setze ihn fort oder lösche den temporären Spielstand, bevor du Kommandant, Trupp oder Shop-Buffs änderst.");
        Add(text, "Deleting the current temp run will permanently erase that saved run. After deleting it, you will be able to change your commander, monsters, and access the shop again. Continue?",
            "Das Löschen des aktuellen temporären Laufs entfernt diesen Spielstand dauerhaft. Danach kannst du Kommandant und Monster ändern und wieder auf den Shop zugreifen. Fortfahren?");
        Add(text, "Thank you for playing the Tetrabeasts demo! You have cleared the final demo level. If you enjoyed your time with the game, please consider buying the full version.",
            "Danke, dass du die Tetrabeasts-Demo gespielt hast!\n\nDu hast das letzte Demo-Level abgeschlossen. Wenn dir das Spiel gefallen hat, erwäge bitte den Kauf der Vollversion.");
        Add(text, "The Castle Has Fallen", "Die Burg ist gefallen");
        Add(text, "Conquest Failed", "Eroberung gescheitert");
        Add(text, "Endless Survival This final battle cannot be won. The enemy has endless health, and the run continues until a loss condition is met. Survive as long as you can.",
            "Endloses Überleben\n\nDiese letzte Schlacht kann nicht gewonnen werden. Der Feind hat unendliche Gesundheit und der Lauf geht weiter, bis eine Niederlagenbedingung erfüllt ist.\n\nÜberlebe so lange du kannst.");
        Add(text, "Do not show this message again", "Diese Meldung nicht erneut anzeigen");
        Add(text, "Quit without saving? Your current run will be lost and will not be available to continue later.",
            "Ohne Speichern beenden? Dein aktueller Lauf geht verloren und kann später nicht fortgesetzt werden.");
        Add(text, "Restarting will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
            "Ein Neustart zählt diesen Lauf als Niederlage. Der aktuelle temporäre Spielstand wird gelöscht und der Lauf nicht gespeichert. Fortfahren?");
        Add(text, "Returning to the main menu will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
            "Die Rückkehr ins Hauptmenü zählt diesen Lauf als Niederlage. Der aktuelle temporäre Spielstand wird gelöscht und der Lauf nicht gespeichert. Fortfahren?");
        Add(text, "Save this run and quit the game? Continuing later will resume from the start of the current level checkpoint. While a run is saved, you will not be able to change your commander, squad, or shop buffs from the title menu.",
            "Diesen Lauf speichern und das Spiel beenden? Beim späteren Fortsetzen startest du am Anfang des aktuellen Level-Kontrollpunkts. Solange ein Lauf gespeichert ist, kannst du im Titelmenü Kommandant, Trupp oder Shop-Buffs nicht ändern.");
        Add(text, "The run could not be temp-saved, so the game will stay open.",
            "Der Lauf konnte nicht temporär gespeichert werden, daher bleibt das Spiel geöffnet.");
        Add(text, "After completing a level you will be allowed to choose one of three buffs that will empower your units. Press [F] to Continue",
            "Nach Abschluss eines Levels darfst du einen von drei Buffs wählen, die deine Einheiten stärken. Drücke [F], um fortzufahren");
        Add(text, "Most buffs come in multiple rarities that will determine their strength. From weakest to strongest (White -> Green -> Blue -> Purple -> Orange). Press [F] to Continue",
            "Die meisten Buffs haben mehrere Seltenheiten, die ihre Stärke bestimmen. Von schwach nach stark (Weiß -> Grün -> Blau -> Lila -> Orange). Drücke [F], um fortzufahren");
        Add(text, "You will receive a single reroll per level that can be used on your buffs or debuffs. Rerolls can be saved throughout your run, but will be reset at the start of a new run. Press [F] to Continue",
            "Du erhältst pro Level einen Neuwurf, den du für Buffs oder Debuffs einsetzen kannst. Neuwürfe können während des Laufs gespart werden, werden aber zu Beginn eines neuen Laufs zurückgesetzt. Drücke [F], um fortzufahren");
        Add(text, "As you grow stronger your enemies will too. Choose one of three debuffs that will empower your enemies during this run. All buffs and debuffs will stay active and stack throughout the run, but will be reset upon starting a new game. Press [F] to Continue",
            "Wenn du stärker wirst, werden es deine Feinde ebenfalls. Wähle einen von drei Debuffs, die deine Feinde während dieses Laufs stärken. Alle Buffs und Debuffs bleiben aktiv und stapeln sich während des Laufs, werden aber beim Start eines neuen Spiels zurückgesetzt. Drücke [F], um fortzufahren");
        Add(text, "Not all levels are created equal, here you will let lady luck decide what the next battlefield will be like. Pull the lever to reveal it, use rerolls if you have them, then continue into the fight. (Press [F] to Continue)",
            "Nicht alle Level sind gleich. Hier lässt du das Glück entscheiden, wie das nächste Schlachtfeld aussehen wird. Ziehe den Hebel, um es aufzudecken, nutze Neuwürfe, wenn du welche hast, und ziehe dann in den Kampf. (Drücke [F], um fortzufahren)");
        Add(text, "During a run, your monsters are temporary copies. This screen drains the EXP those copies earned so a portion can be preserved for their permanent versions. (Press [F] to Continue)",
            "Während eines Laufs sind deine Monster temporäre Kopien. Dieser Bildschirm entzieht die EP, die diese Kopien verdient haben, damit ein Teil für ihre permanenten Versionen bewahrt werden kann. (Drücke [F], um fortzufahren)");
        Add(text, "Preserved EXP is added to your permanent monsters. Permanent levels make your units stronger at the start of future runs. (Press [F] to Continue)",
            "Bewahrte EP werden deinen permanenten Monstern hinzugefügt. Permanente Level machen deine Einheiten zu Beginn zukünftiger Läufe stärker. (Drücke [F], um fortzufahren)");

        Add(text, "Increase luck, improving favorable random outcomes during runs.",
            "Erhoeht Glueck und verbessert guenstige Zufallsergebnisse waehrend eines Laufs.");
        Add(text, "Reduce the starting gravity speed of falling pieces.",
            "Verringert die anfaengliche Fallgeschwindigkeit der Teile.");
        Add(text, "Reduce how quickly gravity ramps up during a level.",
            "Verringert, wie schnell die Schwerkraft in einem Level ansteigt.");
        Add(text, "Increase the chance to earn gold from cleared rows.",
            "Erhoeht die Chance, beim Raeumen von Reihen Gold zu erhalten.");
        Add(text, "Increase monster attack power.",
            "Erhoeht die Angriffskraft der Monster.");
        Add(text, "Increase monster maximum HP.",
            "Erhoeht die maximalen LP der Monster.");
        Add(text, "Increase monster healing power.",
            "Erhoeht die Heilkraft der Monster.");
        Add(text, "Increase starting unit reserves.",
            "Erhoeht die anfaenglichen Einheitenreserven.");
        Add(text, "You found a Death special block. Drop it in a column to destroy all units matching the first monster below it. (Press [F] to Continue)",
            "Du hast einen Todes-Spezialblock gefunden. Lass ihn in einer Spalte fallen, um alle Einheiten zu zerstoeren, die dem ersten Monster darunter entsprechen. (Druecke [F], um fortzufahren)");
        Add(text, "You found a Bomb special block. Drop it to blast a 3x3 area and damage nearby obstacles. (Press [F] to Continue)",
            "Du hast einen Bomben-Spezialblock gefunden. Lass ihn fallen, um einen 3x3-Bereich zu sprengen und nahe Hindernisse zu beschaedigen. (Druecke [F], um fortzufahren)");
        Add(text, "You found a Bolt special block. Drop it to strike an entire column. (Press [F] to Continue)",
            "Du hast einen Blitz-Spezialblock gefunden. Lass ihn fallen, um eine ganze Spalte zu treffen. (Druecke [F], um fortzufahren)");
        Add(text, "You found an Earthquake special block. Drop it to shake loose unsupported units across the board. (Press [F] to Continue)",
            "Du hast einen Erdbeben-Spezialblock gefunden. Lass ihn fallen, um ungestuetzte Einheiten auf dem Brett loszuruetteln. (Druecke [F], um fortzufahren)");
        Add(text, "You found a Slow Gravity special block. Drop it to slow falling pieces for a short time. (Press [F] to Continue)",
            "Du hast einen Spezialblock fuer langsame Schwerkraft gefunden. Lass ihn fallen, um fallende Teile kurzzeitig zu verlangsamen. (Druecke [F], um fortzufahren)");

        Add(text, "Welcome Overlord, my name is Lilith. I have done my best to rally the few monster I could find to assist in your conquest of the human domain. (Press [F] to Continue)",
            "Willkommen, Overlord. Mein Name ist Lilith. Ich habe mein Bestes getan, die wenigen Monster zu sammeln, die ich finden konnte, um dir bei der Eroberung der Menschenlande zu helfen. (Drücke [F], um fortzufahren)");
        Add(text, "Before going into battle, we will first have to select our commander. Press the commander button in the bottom right to see who is available. (Press [F] to Continue)",
            "Vor der Schlacht müssen wir zuerst unseren Kommandanten wählen. Drücke unten rechts die Kommandanten-Schaltfläche, um zu sehen, wer verfügbar ist. (Drücke [F], um fortzufahren)");
        Add(text, "We only have one Commander that we can use at the moment, but with enough gold we can get a few more. Each commander has their own special ability. The details of each ability can be seen in the Commander preview on the right. (Press [F] to Continue)",
            "Im Moment haben wir nur einen Kommandanten, aber mit genug Gold können wir weitere bekommen. Jeder Kommandant hat eine eigene Spezialfähigkeit. Details siehst du rechts in der Kommandanten-Vorschau. (Drücke [F], um fortzufahren)");
        Add(text, "Since our commander is already selected lets confirm the selection at the bottom and head back to the main lobby.",
            "Da unser Kommandant bereits ausgewählt ist, bestätige die Auswahl unten und kehre zur Hauptlobby zurück.");
        Add(text, "Now we will set up our monster squadron that will go into battle with our Commander. Press the select monster button in the bottom right.",
            "Jetzt stellen wir den Monstertrupp zusammen, der mit unserem Kommandanten in die Schlacht zieht. Drücke unten rechts die Monster-Auswahl.");
        Add(text, "As you can see we only have so many monsters to work with to creat our squadron. A Squadron must have at least two monsters and can have a maximum of 4. (Press [F] to Continue)",
            "Wie du siehst, haben wir nur wenige Monster für unseren Trupp. Ein Trupp braucht mindestens zwei Monster und kann höchstens vier haben. (Drücke [F], um fortzufahren)");
        Add(text, "A war can hardly be fought with such pitiful numbers, but that's where I can be of assistance. (Press [F] to Continue)",
            "Mit so kläglichen Zahlen lässt sich kaum Krieg führen, aber genau da kann ich helfen. (Drücke [F], um fortzufahren)");
        Add(text, "I can make clones of your squadron to be sent into battle. This will allow multiple copies to be made to fill out your ranks. However, this will come with some limitations. (Press [F] to Continue)",
            "Ich kann Klone deines Trupps erschaffen und in die Schlacht schicken. So können mehrere Kopien deine Reihen füllen. Allerdings gibt es einige Einschränkungen. (Drücke [F], um fortzufahren)");
        Add(text, "I will only be able to make so many copies and if we lose too many in battle we will be forced to retreat. Second, the copies will gain their own experience, but I will only be able to safely convert a small fraction of that experience from the copy back to the original monster after a campaign is finished. (Press [F] to Continue)",
            "Ich kann nur eine begrenzte Zahl an Kopien erschaffen, und wenn wir zu viele im Kampf verlieren, müssen wir uns zurückziehen. Außerdem sammeln Kopien eigene Erfahrung, aber nach einer Kampagne kann ich nur einen kleinen Teil davon sicher auf das ursprüngliche Monster übertragen. (Drücke [F], um fortzufahren)");
        Add(text, "You can click on any monster and see a preview of it's current stats and level in the preview section on the right. Click on the arrow button in the preview section to swap between it's stats and it's passive ability description.",
            "Du kannst ein Monster anklicken, um rechts eine Vorschau seiner aktuellen Werte und seines Levels zu sehen. Klicke dort auf den Pfeil, um zwischen Werten und passiver Fähigkeit zu wechseln.");
        Add(text, "More monsters can be unlocked with gold and cosmetic skins can also be purchased once you have gained some extra funds. Now, Prress the confirm button at the bottom of the screen to lock in your team.",
            "Weitere Monster können mit Gold freigeschaltet werden, und kosmetische Skins lassen sich kaufen, sobald du etwas mehr Geld hast. Drücke nun unten auf Bestätigen, um dein Team festzulegen.");
        Add(text, "Now you are ready to start your first campaign. Press the Start button to begin a new run.",
            "Jetzt bist du bereit für deine erste Kampagne. Drücke Start, um einen neuen Lauf zu beginnen.");
        Add(text, "Now you are ready to start your first campaign. Press [F] to Continue",
            "Jetzt bist du bereit fuer deine erste Kampagne. Druecke [F], um fortzufahren");
        Add(text, "Welcome to the battlefield. We will begin by going over a few of the basic controls and battle mechanics. (Press [F] to Continue)",
            "Willkommen auf dem Schlachtfeld. Wir beginnen mit einigen grundlegenden Steuerungen und Kampfmechaniken. (Drücke [F], um fortzufahren)");
        Add(text, "First, try moving your piece to the left by pressing [A].",
            "Versuche zuerst, deine Figur mit [A] nach links zu bewegen.");
        Add(text, "Next, try moving your piece to the right by pressing [D].",
            "Versuche als Nächstes, deine Figur mit [D] nach rechts zu bewegen.");
        Add(text, "Next, try moving your piece down a single row by pressing [S].",
            "Versuche nun, deine Figur mit [S] eine Reihe nach unten zu bewegen.");
        Add(text, "Now, try rotating your piece counter-clockwise by pressing [Q].",
            "Versuche jetzt, deine Figur mit [Q] gegen den Uhrzeigersinn zu drehen.");
        Add(text, "Now, try rotating your piece clockwise by pressing [E].",
            "Versuche jetzt, deine Figur mit [E] im Uhrzeigersinn zu drehen.");
        Add(text, "Look at the bottom of the board and you will see four cells with a bright red tint in the same shape as your current piece, that isn't a coincidence. (Press [F] to Continue)",
            "Sieh dir den unteren Bereich des Spielfelds an. Dort erkennst du vier hellrot markierte Zellen in der Form deiner aktuellen Figur. Das ist kein Zufall. (Drücke [F], um fortzufahren)");
        Add(text, "This is the landing indicator and will help you see exactly where your current piece will lock in place. A piece will lock in place automatically when it come in contact with an obstacle, another locked unit piece, or when it reaches the bottom of the board. (Press [F] to Continue)",
            "Das ist der Landeindikator. Er zeigt dir genau, wo deine aktuelle Figur einrastet. Eine Figur rastet automatisch ein, wenn sie ein Hindernis, eine andere feste Figur oder den unteren Rand des Spielfelds berührt. (Drücke [F], um fortzufahren)");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar].",
            "Als Nächstes sollst du deine aktuelle Figur mit der [Leertaste] hart fallen lassen.");
        Add(text, "Here you can keep track of level information such as any active level modifiers, the curent level number, how long you have been in the level, and the current gravity pulling your pieces down. (Press [F] to Continue)",
            "Hier siehst du Levelinformationen wie aktive Levelmodifikatoren, aktuelle Levelnummer, bisherige Zeit im Level und die Schwerkraft, die deine Figuren nach unten zieht. (Drücke [F], um fortzufahren)");
        Add(text, "Here you can see a preview of the next piece that will be spawned. Knowing what will come next will help you plan out your next step. (Press [F] to Continue)",
            "Hier siehst du eine Vorschau der nächsten Figur. Zu wissen, was als Nächstes kommt, hilft dir beim Planen. (Drücke [F], um fortzufahren)");
        Add(text, "Reserve Units - Loss Condition When any of your units die your current reserve will be reduced. You only have so many soldiers and when your reserve hits 0 you will lose. (Press [F] to Continue)",
            "Reserveeinheiten - Niederlagenbedingung\n\nWenn eine deiner Einheiten stirbt, sinkt deine aktuelle Reserve. Du hast nur eine begrenzte Zahl an Soldaten, und wenn deine Reserve 0 erreicht, verlierst du. (Drücke [F], um fortzufahren)");
        Add(text, "Loss Condition If a piece is locked in place above the top row of the board you will instantly lose, regardless of the number of reserve units left. Be caureful not to build to high. (Press [F] to Continue)",
            "Niederlagenbedingung\n\nWenn eine Figur oberhalb der obersten Reihe einrastet, verlierst du sofort, egal wie viele Reserveeinheiten übrig sind. Baue also nicht zu hoch. (Drücke [F], um fortzufahren)");
        Add(text, "Win Condition Reduce the Enemy Castle's HP to zero. (Press [F] to Continue)",
            "Siegbedingung\n\nReduziere die TP der feindlichen Burg auf null. (Drücke [F], um fortzufahren)");
        Add(text, "If you need a break press [Esc]. This will pause the game and bring up the pause menu.",
            "Wenn du eine Pause brauchst, drücke [Esc]. Das pausiert das Spiel und öffnet das Pausenmenü.");
        Add(text, "Here you can change settings, look through the help menu, check your current modifiers, or end your run early. For now, lets close the pause menu by pressing [Esc] again.",
            "Hier kannst du Einstellungen ändern, das Hilfemenü ansehen, deine aktuellen Modifikatoren prüfen oder den Lauf vorzeitig beenden. Schließe das Pausenmenü fürs Erste wieder mit [Esc].");
        Add(text, "Now lets try filling an entire row on the board next to launch an attack. (Press [F] to Continue)",
            "Versuchen wir nun, eine ganze Reihe auf dem Spielfeld zu füllen, um einen Angriff zu starten. (Drücke [F], um fortzufahren)");
    }

    static void AddCharacterText(Dictionary<string, string> text)
    {
        Add(text, "Charge!", "Angriff!");
        Add(text, "Send all units from the bottom 3 rows to attack immediatley, no need for fully formed lines. Damage based on the number of units cleared.",
            "Schickt alle Einheiten aus den unteren 3 Reihen sofort zum Angriff, ohne vollständige Linien. Der Schaden hängt von der Anzahl entfernter Einheiten ab.");
        Add(text, "Time Shift", "Zeitverschiebung");
        Add(text, "Reduce unit fall speed by 1/3 of its current speed fo 15 seconds.",
            "Verringert die Fallgeschwindigkeit der Einheiten 15 Sekunden lang um ein Drittel der aktuellen Geschwindigkeit.");
        Add(text, "Natures Embrace", "Umarmung der Natur");
        Add(text, "Heal all units on the board back to full health including those that have died.",
            "Heilt alle Einheiten auf dem Spielfeld vollständig, einschließlich der gestorbenen.");
        Add(text, "Grock SMASH!", "Grock ZERTRÜMMERT!");
        Add(text, "Let loose a war cry doubling all units on the boards health and attack damage for 10 seconds.",
            "Entfesselt einen Kriegsschrei, der 10 Sekunden lang Leben und Angriffsschaden aller Einheiten auf dem Spielfeld verdoppelt.");
        Add(text, "Immutable Bulwark", "Unwandelbares Bollwerk");
        Add(text, "All units become immune to damage for 12 seconds.",
            "Alle Einheiten werden 12 Sekunden lang immun gegen Schaden.");
    }

    static void AddHelpTopicText(Dictionary<string, string> text)
    {
        Add(text, "Boss Abilities", "Bossfähigkeiten");
        Add(text, "Controls", "Steuerung");
        Add(text, "Floor Effects", "Bodeneffekte");
        Add(text, "Game Mechanics", "Spielmechaniken");
        Add(text, "Obstacles", "Hindernisse");
        Add(text, "Special Blocks", "Spezialblöcke");
        Add(text, "Traps", "Fallen");
        Add(text, "Other", "Sonstiges");

        Add(text, "Full Board Blast", "Spielfeldweite Explosion");
        Add(text, "The boss will target all monster units set on the board dealing a small amount of damage to each one.",
            "Der Boss zielt auf alle Monstereinheiten auf dem Spielfeld und fügt jeder geringen Schaden zu.");
        Add(text, "Increased Gravity", "Erhöhte Schwerkraft");
        Add(text, "The boss will temporarily increase gravity causing blocks to fall significantly faster for a set period of time.",
            "Der Boss erhöht vorübergehend die Schwerkraft, wodurch Blöcke für eine Weile deutlich schneller fallen.");
        Add(text, "Invulnerable", "Unverwundbar");
        Add(text, "The boss will temporarily become invulnerable. While invulnerable, the boss will take no damage from any sources.",
            "Der Boss wird vorübergehend unverwundbar. Währenddessen erleidet er keinen Schaden aus irgendeiner Quelle.");
        Add(text, "Lightning Strike", "Blitzeinschlag");
        Add(text, "The boss will target 1-3 individual cells with lightning bolts that will deal siginficant damage to any monster unit in that cell. Afterwards that cell will have a lightning floor effect that does continuous damage to any monster unit that occupies the tile.",
            "Der Boss zielt mit Blitzen auf 1 bis 3 einzelne Zellen, die jeder Monstereinheit dort erheblichen Schaden zufügen. Danach erhält die Zelle einen Blitz-Bodeneffekt, der jeder dort stehenden Monstereinheit fortlaufend Schaden zufügt.");
        Add(text, "Magic Explosive", "Magischer Sprengsatz");
        Add(text, "The boss will spawn a single magical explosive as low on the board as possible. This explosive will detonate after 15 seconds and can only be safley removed by clearing the row it occupies.",
            "Der Boss erschafft einen einzelnen magischen Sprengsatz so weit unten wie möglich. Er detoniert nach 15 Sekunden und kann nur sicher entfernt werden, indem seine Reihe gelöscht wird.");
        Add(text, "Magic Shield", "Magischer Schild");
        Add(text, "The boss will spawn multiple magical pylon obstacles on the board. As long as the pylons remain on the board the boss will take 50% reduced damage from all sources.",
            "Der Boss erschafft mehrere magische Pylonen auf dem Spielfeld. Solange sie bleiben, erleidet der Boss 50% weniger Schaden aus allen Quellen.");
        Add(text, "Spawn FE's", "Bodeneffekte erzeugen");
        Add(text, "The boss will spawn multiple floor effects on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. Possible floor effect types are posion, fire, and lightning.",
            "Der Boss erzeugt mehrere Bodeneffekte. Sie können einzeln verstreut oder in Mustern von 2x2, 1x4 oder 4x1 erscheinen. Mögliche Typen sind Gift, Feuer und Blitz.");
        Add(text, "Spawn Obstacles", "Hindernisse erzeugen");
        Add(text, "The boss will spawn multiple obstacles on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns stone obstacles.",
            "Der Boss erzeugt mehrere Hindernisse auf dem Spielfeld. Sie können einzeln verstreut oder in Mustern von 2x2, 1x4 oder 4x1 erscheinen. Diese Fähigkeit erzeugt nur Steinhindernisse.");
        Add(text, "Spawn Traps", "Fallen erzeugen");
        Add(text, "The boss will spawn multiple traps on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns spike traps.",
            "Der Boss erzeugt mehrere Fallen auf dem Spielfeld. Sie können einzeln verstreut oder in Mustern von 2x2, 1x4 oder 4x1 erscheinen. Diese Fähigkeit erzeugt nur Stachelfallen.");
        Add(text, "Row Blast", "Reihenexplosion");
        Add(text, "The boss will target the top three rows the player has set monster units. All monster units in the selected rows will recieve moderate damage from this attack.",
            "Der Boss zielt auf die obersten drei Reihen, in denen der Spieler Monstereinheiten platziert hat. Alle Monstereinheiten in diesen Reihen erleiden moderaten Schaden.");
        Add(text, "Activate Special", "Spezial aktivieren");
        Add(text, "R - Activates the players special ability when the special gauge is charged to 100%",
            "R - Aktiviert die Spezialfähigkeit des Spielers, wenn die Spezialanzeige zu 100% geladen ist.");
        Add(text, "Movement", "Bewegung");
        Add(text, "A - shifts the active piece one column to the left. S - shifts the active piece one row down. D - shifts the active piece one column to the right.",
            "A - verschiebt die aktive Figur eine Spalte nach links.\n\nS - verschiebt die aktive Figur eine Reihe nach unten.\n\nD - verschiebt die aktive Figur eine Spalte nach rechts.");
        Add(text, "Pause", "Pause");
        Add(text, "Escape - Will open the pause menu and pause all gameplay functions. Presseing escape while the pause menu is open will close the pause menu and resume gameplay.",
            "Escape - Öffnet das Pausenmenü und pausiert alle Spielfunktionen. Wenn das Pausenmenü offen ist, schließt Escape es und setzt das Spiel fort.");
        Add(text, "Quick Drop", "Schnellfall");
        Add(text, "Pressing spacebar will quick drop your active piece, setting it in place immediatley.",
            "Die Leertaste lässt deine aktive Figur schnell fallen und rastet sie sofort ein.");
        Add(text, "Rotation", "Rotation");
        Add(text, "Q - Rotate the active piece 90 degrees counter-clockwise. E - Rotate the active piece 90 degrees clockwise.",
            "Q - Dreht die aktive Figur 90 Grad gegen den Uhrzeigersinn.\n\nE - Dreht die aktive Figur 90 Grad im Uhrzeigersinn.");
        Add(text, "Fire", "Feuer");
        Add(text, "Any monster unit set in a cell with the fire floor effect will take constant damage. The damage is realtivley low but occurs often.",
            "Jede Monstereinheit auf einer Zelle mit Feuerbodeneffekt erleidet fortlaufend Schaden. Der Schaden ist relativ gering, tritt aber häufig auf.");
        Add(text, "Lightning", "Blitz");
        Add(text, "Any monster unit set in a cell with the lightning floor effect will take constant damage. The damage is moderate but the floor effect will disappears after a period of time.",
            "Jede Monstereinheit auf einer Zelle mit Blitzbodeneffekt erleidet fortlaufend Schaden. Der Schaden ist moderat, aber der Effekt verschwindet nach einiger Zeit.");
        Add(text, "Poison", "Gift");
        Add(text, "Any monster unit set in a cell with the poison floor effect will take continuous damage. The damage is realtivley low but occurs often.",
            "Jede Monstereinheit auf einer Zelle mit Giftbodeneffekt erleidet fortlaufend Schaden. Der Schaden ist relativ gering, tritt aber häufig auf.");
        Add(text, "Attack Units", "Angriffseinheiten");
        Add(text, "Attack units tend to have a higher attack stat than other unit types. They are best used to deal maximum damage to enemies to end levels more quickly. They cannot heal and tend to have average health stats.",
            "Angriffseinheiten haben meist einen höheren Angriffswert als andere Typen. Sie eignen sich am besten, um Gegner schnell zu besiegen. Sie können nicht heilen und haben meist durchschnittliche Lebenswerte.");
        Add(text, "Currency", "Gold");
        Add(text, "The player can gain currency from completing levels and rarely from clearing rows. Currency can be used to purchase various cosmetics, monster units, player characters, and permanent buffs to improve future runs. Your current currency can be found in the top right of the screen.",
            "Der Spieler erhält Gold durch abgeschlossene Level und selten durch gelöschte Reihen. Gold kauft Kosmetik, Monstereinheiten, Spielercharaktere und permanente Buffs für künftige Läufe. Dein aktuelles Gold steht oben rechts.");
        Add(text, "Enemy Attack", "Feindangriff");
        Add(text, "The enemy castle will send constant attacks at the players monster units in an attempt to cull them. The enemies attacks will increase in power and frequency at higher levels.",
            "Die feindliche Burg greift ständig die Monstereinheiten des Spielers an, um sie zu dezimieren. In höheren Levels werden die Angriffe stärker und häufiger.");
        Add(text, "Healing Units", "Heileinheiten");
        Add(text, "Some units can heal other monster units to varying degrees. They tend to have much lower health and attack stats.",
            "Manche Einheiten können andere Monstereinheiten unterschiedlich stark heilen. Sie haben meist deutlich weniger Leben und Angriff.");
        Add(text, "Landing Hint", "Landehilfe");
        Add(text, "A red tint overlay appears in the location where the current active piece will fall.",
            "Eine rote Markierung erscheint dort, wo die aktuelle aktive Figur landen wird.");
        Add(text, "Loss Condition 1", "Niederlagenbedingung 1");
        Add(text, "If the unit reserve reaches 0, the run will end with your loss! The unit reserve is reduced by one for every monster unit that dies on the board.",
            "Wenn die Einheitenreserve 0 erreicht, endet der Lauf mit deiner Niederlage. Die Reserve sinkt um eins für jede Monstereinheit, die auf dem Spielfeld stirbt.");
        Add(text, "Loss Condition 2", "Niederlagenbedingung 2");
        Add(text, "If a piece is set above the top row of the grid, the run will end with a loss!",
            "Wenn eine Figur oberhalb der obersten Gitterreihe gesetzt wird, endet der Lauf mit einer Niederlage.");
        Add(text, "Monster Units", "Monstereinheiten");
        Add(text, "Choose from multiple units to make up your warband. Each unit has their own individual stats that will make them more suitable for different roles. Some units have balanced stats and others are more specialized.",
            "Wähle mehrere Einheiten für deinen Kriegstrupp. Jede Einheit hat eigene Werte, die sie für verschiedene Rollen geeignet machen. Manche sind ausgewogen, andere spezialisiert.");
        Add(text, "Commander", "Kommandant");
        Add(text, "Each Commander has their own unquie special ability that can be used in battle. New Commanders can be unlocked and set from the main menu.",
            "Jeder Kommandant hat eine einzigartige Spezialfähigkeit für den Kampf. Neue Kommandanten können im Hauptmenü freigeschaltet und gewählt werden.");
        Add(text, "Row Clear", "Reihe löschen");
        Add(text, "Fill each grid cell in a row to launch an attack. Monster Units and Obstacles count as a filled cell. Floor effects and traps do not count as a filled cell. Monster units in the cleared row contribute their attack stats for damage calculation and partially fill the player's special gauge.",
            "Fülle jede Zelle einer Reihe, um einen Angriff auszulösen. Monstereinheiten und Hindernisse zählen als gefüllte Zellen. Bodeneffekte und Fallen nicht. Monster in der gelöschten Reihe tragen ihre Angriffswerte zur Schadensberechnung bei und füllen die Spezialanzeige teilweise.");
        Add(text, "Run Buffs", "Lauf-Buffs");
        Add(text, "After succesfully completing a level you will be given three random buffs to choose from to enhance your current run. All buff modifiers will be reset when the run ends.",
            "Nach erfolgreichem Abschluss eines Levels erhältst du drei zufällige Buffs zur Auswahl, um deinen aktuellen Lauf zu stärken. Alle Buff-Modifikatoren werden am Ende des Laufs zurückgesetzt.");
        Add(text, "Run Debuffs", "Lauf-Debuffs");
        Add(text, "After succesfully completing a level you will be given three random debuffs to choose from to increase the difficulty of your current run. All debuff modifiers will be reset when the run ends.",
            "Nach erfolgreichem Abschluss eines Levels erhältst du drei zufällige Debuffs zur Auswahl, um die Schwierigkeit deines aktuellen Laufs zu erhöhen. Alle Debuff-Modifikatoren werden am Ende des Laufs zurückgesetzt.");
        Add(text, "Clearing rows will earn the player points and add to their overall score. This can be used to measure the success of a run and compete with other players.",
            "Gelöschte Reihen bringen Punkte und erhöhen die Gesamtpunktzahl. Damit lässt sich der Erfolg eines Laufs messen und mit anderen Spielern vergleichen.");
        Add(text, "Shop Buffs", "Shop-Buffs");
        Add(text, "The shop offers different buffs that can be purchased. Each buff is permanent and can be purchased multiple times. Each purchase will increase the price of the buffs next purchase level.",
            "Der Shop bietet verschiedene kaufbare Buffs. Jeder Buff ist permanent und kann mehrfach gekauft werden. Jeder Kauf erhöht den Preis für die nächste Stufe dieses Buffs.");
        Add(text, "Special Gauge", "Spezialanzeige");
        Add(text, "Fills with every row cleared. When it reaches 100% you can use your Commander's unique special ability, resetting the special gauge back 0%.",
            "Füllt sich mit jeder gelöschten Reihe. Bei 100% kannst du die einzigartige Spezialfähigkeit deines Kommandanten nutzen, wodurch die Anzeige auf 0% zurückgesetzt wird.");
        Add(text, "Tank Units", "Tankeinheiten");
        Add(text, "Tank units tend to have significantly more health than other unit types. They are best used to outlast enemy attacks and protect weaker units. They cannot heal and tend to have low attack stats.",
            "Tankeinheiten haben meist deutlich mehr Leben als andere Typen. Sie halten feindliche Angriffe aus und schützen schwächere Einheiten. Sie können nicht heilen und haben meist niedrige Angriffswerte.");
        Add(text, "Unit Death", "Einheitentod");
        Add(text, "When a monster units health drops to zero it dies. Dead units do not contribute their stats to an attack when their row is cleared or help fill up the players Special Gauge.",
            "Wenn die Gesundheit einer Monstereinheit auf null fällt, stirbt sie. Tote Einheiten tragen beim Löschen ihrer Reihe nicht zu Angriffswerten bei und füllen die Spezialanzeige nicht.");
        Add(text, "Unit Reserve", "Einheitenreserve");
        Add(text, "When starting a new run the player will have a set limit of how many units they can afford to lose throughout the run. When a unit dies the reserve bar will be decreased. Suuccesfully completing a level will award the player up to 5 reinforcements for each victory up to the max unit reserve.",
            "Zu Beginn eines neuen Laufs hat der Spieler ein Limit, wie viele Einheiten er verlieren darf. Wenn eine Einheit stirbt, sinkt die Reserveleiste. Erfolgreiche Level gewähren pro Sieg bis zu 5 Verstärkungen, bis die maximale Reserve erreicht ist.");
        Add(text, "Victory Condition", "Siegbedingung");
        Add(text, "Reduce the health of the enemy castle by clearing rows. When the enemy castle reaches 0 health, you win!",
            "Reduziere die Gesundheit der feindlichen Burg durch gelöschte Reihen. Wenn die Burg 0 Gesundheit erreicht, gewinnst du!");
        Add(text, "Explosive", "Sprengsatz");
        Add(text, "The explosive obstacle will explode after a period of time killing all surrounding monster units. Can be safley disposed of by clearing its row. When safley disposed of it add 25 damage to that row clears attack. CAUTION: Using a bomb or lightning special block on the explosive will cause it to detonate.",
            "Das explosive Hindernis explodiert nach einiger Zeit und tötet alle umliegenden Monstereinheiten. Es kann sicher entfernt werden, indem seine Reihe gelöscht wird. Dann fügt es dem Angriff dieser Reihe 25 Schaden hinzu.\n\nVORSICHT: Eine Bombe oder ein Blitz-Spezialblock auf dem Sprengsatz lässt ihn detonieren.");
        Add(text, "Magic Pylon", "Magischer Pylon");
        Add(text, "When magic pylon obstacles are on the board enemies will take 50% reduced damage from all sources. Magic pylon can bee destroyed by clearing the row they occupy or using bomb and lightning special blocks.",
            "Solange magische Pylonen auf dem Spielfeld sind, erleiden Feinde 50% weniger Schaden aus allen Quellen. Magische Pylonen können durch Löschen ihrer Reihe oder durch Bomben- und Blitz-Spezialblöcke zerstört werden.");
        Add(text, "Stone", "Stein");
        Add(text, "Stone obstacles can be spawned at the beggining of a level or by the boss. A lightning special block or clearing a row containing a stone obstacle will deal one damage to it. Stone obstacle need to be damaged 3 times to be removed. Exception: Using a bomb special block will instantly destroy a stone obstacle.",
            "Steinhindernisse können zu Beginn eines Levels oder durch den Boss erscheinen. Ein Blitz-Spezialblock oder das Löschen einer Reihe mit Stein verursacht 1 Schaden daran. Ein Stein braucht 3 Schaden, um entfernt zu werden. Ausnahme: Ein Bomben-Spezialblock zerstört ihn sofort.");
        Add(text, "Bomb", "Bombe");
        Add(text, "The bomb special will detonate immediatley when set. All blocks in the surrounding tiles of its blast will be destroyed!",
            "Das Bomben-Spezial detoniert sofort beim Platzieren. Alle Blöcke in den umliegenden Explosionsfeldern werden zerstört!");
        Add(text, "Death", "Tod");
        Add(text, "The Death special will activate immediatley when set on top of a monster unit. All monster units of the same type will be safley removed from the board with out decreasing your unit reserve.",
            "Das Todes-Spezial aktiviert sich sofort, wenn es auf eine Monstereinheit gesetzt wird. Alle Monstereinheiten desselben Typs werden sicher vom Spielfeld entfernt, ohne deine Reserve zu verringern.");
        Add(text, "Earthquake", "Erdbeben");
        Add(text, "The Earthquake special will activate immediatley when set. All blocks on the board will be dropped if not being supported by another tile beneath them. This effects obstacles as well that may otherwise not be able to be moved by other means.",
            "Das Erdbeben-Spezial aktiviert sich sofort beim Platzieren. Alle Blöcke auf dem Spielfeld fallen, wenn sie nicht von einem Feld darunter gestützt werden. Das betrifft auch Hindernisse, die sonst nicht bewegt werden könnten.");
        Add(text, "Lightning Bolt", "Blitzschlag");
        Add(text, "The Lightning Bolt special will activate immediatley when set. All monster units and traps will be destroyed in that column. Stone obstacles will take partial damage if they are in the affected area.",
            "Der Blitzschlag-Spezialblock aktiviert sich sofort beim Platzieren. Alle Monstereinheiten und Fallen in dieser Spalte werden zerstört. Steinhindernisse in der betroffenen Zone erleiden Teilschaden.");
        Add(text, "Slow Gravity", "Langsame Schwerkraft");
        Add(text, "The Slow Gravity special block will activate immedialtey upon being set. It will significantly reduce the speed at which pieces fall and the how quickly gravity increases over time.",
            "Der Spezialblock Langsame Schwerkraft aktiviert sich sofort beim Platzieren. Er verringert deutlich, wie schnell Figuren fallen und wie schnell die Schwerkraft mit der Zeit zunimmt.");
        Add(text, "Spike Trap", "Stachelfalle");
        Add(text, "Spike traps will deal a high amount of damage to any monster unit that is set on the trapped cell when they are placed. The only way to destroyed spike traps are with the lightning special block.",
            "Stachelfallen verursachen hohen Schaden an jeder Monstereinheit, die auf eine Fallen-Zelle gesetzt wird. Zerstören lassen sie sich nur mit dem Blitz-Spezialblock.");
    }

    static void AddLevelModifierText(Dictionary<string, string> text)
    {
        Add(text, "Spin to Win", "Drehen zum Sieg");
        Add(text, "Active pieces will continuously rotate until set in place. Manual rotation will be locked.",
            "Aktive Figuren rotieren dauerhaft, bis sie platziert werden. Manuelle Rotation ist gesperrt.");
        Add(text, "Timing is Everything", "Timing ist alles");
        Add(text, "Active pieces will continuously shift horizontally back and and forth across the board. Manual shifting will be locked.",
            "Aktive Figuren bewegen sich dauerhaft horizontal über das Spielfeld. Manuelles Verschieben ist gesperrt.");
        Add(text, "Go Big or Go Home", "Ganz oder gar nicht");
        Add(text, "Damage is significantly reduced for all attacks when the combo streak is less than 3.",
            "Der Schaden aller Angriffe ist deutlich verringert, wenn die Kombo-Serie unter 3 liegt.");
        Add(text, "Break Out The Big Guns", "Die schweren Geschütze auffahren");
        Add(text, "The enemy has fortified their position. A combo attack at 4 or higher will be required to remove each enemy shield. Damage dealt to the enemy while shielded is significantly reduced.",
            "Der Feind hat seine Stellung befestigt. Ein Komboangriff von 4 oder höher ist nötig, um jeden feindlichen Schild zu entfernen. Schaden gegen geschützte Feinde ist deutlich verringert.");
        Add(text, "Contagion Outbreak", "Ansteckungsausbruch");
        Add(text, "Disease has begun to spread through the ranks. Close proximitiy has a chance to transfer from afflicted units to healthy units and will spread with certainity on the death of infected units.",
            "Eine Krankheit breitet sich in den Reihen aus. Nähe kann sie von betroffenen auf gesunde Einheiten übertragen, und beim Tod infizierter Einheiten breitet sie sich sicher aus.");
        Add(text, "Double Down", "Alles verdoppeln");
        Add(text, "All damage taken and dealt will be doubled!",
            "Aller erlittene und verursachte Schaden wird verdoppelt!");
        Add(text, "Exploding Corpses", "Explodierende Leichen");
        Add(text, "Units will explode on death dealing damage to all surrounding units. Damage dealt is a percentage based off of the max health of the exploding unit.",
            "Einheiten explodieren beim Tod und schaden allen umliegenden Einheiten. Der Schaden basiert prozentual auf der maximalen Gesundheit der explodierenden Einheit.");
        Add(text, "Tis A Flesh Wound", "Nur eine Fleischwunde");
        Add(text, "All ally units will start with half health.",
            "Alle verbündeten Einheiten starten mit halber Gesundheit.");
        Add(text, "Rations Running Low", "Rationen werden knapp");
        Add(text, "Rations have begun to run out. Ally units have begun to starve trying to share the remaining rations. Units will take continuous damage proportional to the number of current reserve units.",
            "Die Rationen gehen zur Neige. Verbündete Einheiten hungern, während sie die Reste teilen. Einheiten erleiden fortlaufend Schaden proportional zur aktuellen Reserve.");
        Add(text, "Overgrowth", "Überwucherung");
        Add(text, "Overgrowth has taken over the area consuming tiles and monsters. Overgrowth becomes more resilent to destruction once fully grown. Defeat the enemy before your army becomes mulch!",
            "Überwucherung hat das Gebiet erfasst und verschlingt Felder und Monster. Voll ausgewachsen ist sie widerstandsfähiger. Besiege den Feind, bevor deine Armee zu Mulch wird!");
        Add(text, "No Retreat", "Kein Rückzug");
        Add(text, "Enemy ambush will cut off any retreat. Rows will slowly fill with enemy units progressivley limiting space to maneuver.",
            "Ein feindlicher Hinterhalt schneidet jeden Rückzug ab. Reihen füllen sich langsam mit Feinden und schränken den Manöverraum zunehmend ein.");
        Add(text, "Soul Link", "Seelenband");
        Add(text, "All four units in a piece share a single health pool.",
            "Alle vier Einheiten einer Figur teilen sich einen gemeinsamen Gesundheitspool.");
        Add(text, "Back to the Basics", "Zurück zu den Grundlagen");
        Add(text, "Special blocks will not spawn.",
            "Spezialblöcke erscheinen nicht.");
        Add(text, "Commander Special Lock", "Kommandanten-Spezial gesperrt");
        Add(text, "Special ability gauge will be set to zero and locked.",
            "Die Spezialanzeige wird auf null gesetzt und gesperrt.");
        Add(text, "Catastrophic Storm", "Katastrophaler Sturm");
        Add(text, "An unrelenting storm has arrived and will blast the area with devestating lightning strikes.",
            "Ein unerbittlicher Sturm ist aufgezogen und überzieht das Gebiet mit verheerenden Blitzeinschlägen.");
        Add(text, "Miasma Marsh", "Miasma-Moor");
        Add(text, "The battlefield has shifted to the nearby marshes where deadly miasma drifts across the terrain.",
            "Das Schlachtfeld hat sich in nahe Sümpfe verlagert, wo tödliches Miasma über das Gelände zieht.");
    }

    static void AddRunModifierNames(Dictionary<string, string> text)
    {
        Add(text, "All Special Gain Down", "Gesamter Spezialgewinn verringert");
        Add(text, "All Special Gain Up", "Gesamter Spezialgewinn erhöht");
        Add(text, "ATK Down", "ANG verringert");
        Add(text, "ATK Up", "ANG erhöht");
        Add(text, "Currency Drop Up", "Golddrops erhöht");
        Add(text, "Debuffs Only", "Nur Debuffs");
        Add(text, "Enemy ATK Down", "Feind-ANG verringert");
        Add(text, "Enemy ATK SPD Down", "Feind-Angriffstempo verringert");
        Add(text, "Enemy ATK SPD Up", "Feind-Angriffstempo erhöht");
        Add(text, "Enemy ATK Up", "Feind-ANG erhöht");
        Add(text, "Enemy HP Up", "Feind-TP erhöht");
        Add(text, "Gravity Accel SPD Down", "Schwerkraftbeschleunigung verringert");
        Add(text, "Gravity Accel SPD Up", "Schwerkraftbeschleunigung erhöht");
        Add(text, "Gravity Base SPD Down", "Basis-Schwerkrafttempo verringert");
        Add(text, "Gravity SPD Up", "Schwerkrafttempo erhöht");
        Add(text, "Healing Range Up", "Heilreichweite erhöht");
        Add(text, "Healing STR Up", "Heilstärke erhöht");
        Add(text, "HP Down", "TP verringert");
        Add(text, "HP Up", "TP erhöht");
        Add(text, "Luck Up", "Glück erhöht");
        Add(text, "Misfortune Up", "Unglück erhöht");
        Add(text, "No Landing Indicator", "Kein Landeindikator");
        Add(text, "No Next Block Preview", "Keine Nächste-Block-Vorschau");
        Add(text, "No Reinforcements", "Keine Verstärkungen");
        Add(text, "Reinforcements Down", "Verstärkungen verringert");
        Add(text, "Reinforcements Up", "Verstärkungen erhöht");
        Add(text, "Special Block Down", "Spezialblöcke verringert");
        Add(text, "Special Block Up", "Spezialblöcke erhöht");
        Add(text, "Special Gain Stat Down", "Monster-Spezialgewinn verringert");
        Add(text, "Special Gauge Stat Up", "Monster-Spezialgewinn erhöht");
        Add(text, "Stone Buff Drop Down", "Stein-Buff-Drops verringert");
        Add(text, "Stone Buff Drop Up", "Stein-Buff-Drops erhöht");
        Add(text, "Unit Reserve Down", "Einheitenreserve verringert");
        Add(text, "Unit Reserve Up", "Einheitenreserve erhöht");
        Add(text, "Win Currency Down", "Sieg-Gold verringert");
        Add(text, "Win Currency Up", "Sieg-Gold erhöht");
    }

    static void AddRunModifierFixedDescriptions(Dictionary<string, string> text)
    {
        Add(text, "A red tinted outline will no longer be shown where your pieces will land.",
            "Es wird keine rote Kontur mehr angezeigt, wo deine Figuren landen.");
        Add(text, "The next block will no longer be shown.",
            "Der nächste Block wird nicht mehr angezeigt.");
        Add(text, "Reinforcements will no longer arrive after winning a round.",
            "Nach einem Rundensieg treffen keine Verstärkungen mehr ein.");
        Add(text, "Stone obstacles no longer have a chance of dropping buffs and now only drop debuffs. Debuff drop chance is the same as prior buff drop chance.",
            "Steinhindernisse können keine Buffs mehr fallen lassen und geben jetzt nur Debuffs. Die Debuff-Chance entspricht der früheren Buff-Chance.");
        Add(text, "Double the amount of currency gained occasionally when clearing lines.",
            "Verdoppelt gelegentlich das Gold beim Löschen von Linien.");
        Add(text, "Triple the amount of currency gained occasionally when clearing lines.",
            "Verdreifacht gelegentlich das Gold beim Löschen von Linien.");
        Add(text, "Qunituple the amount of currency gained occasionally when clearing lines.",
            "Verfünffacht gelegentlich das Gold beim Löschen von Linien.");
        Add(text, "Increases the healing range of all friendly monsters by 1.",
            "Erhöht die Heilreichweite aller verbündeten Monster um 1.");
        Add(text, "Increases the healing range of all friendly monsters by 2.",
            "Erhöht die Heilreichweite aller verbündeten Monster um 2.");
        Add(text, "Increases the healing range of all friendly monsters by 3.",
            "Erhöht die Heilreichweite aller verbündeten Monster um 3.");
    }

    static void AddStatText(Dictionary<string, string> text)
    {
        Add(text, "Lines Cleared:", "Linien gelöscht:");
        Add(text, "Special Used:", "Spezial genutzt:");
        Add(text, "Obstacles Destroyed:", "Hindernisse zerstört:");
        Add(text, "Highest Combo:", "Höchste Kombo:");
        Add(text, "Highest Single Attack:", "Höchster Einzelangriff:");
        Add(text, "Units Died:", "Einheiten gestorben:");
        Add(text, "Units Healed:", "Einheiten geheilt:");
        Add(text, "Total Damage Dealt:", "Gesamtschaden verursacht:");
        Add(text, "Clear Time:", "Abschlusszeit:");
        Add(text, "Final Score:", "Endpunktzahl:");
        Add(text, "Lines", "Linien");
        Add(text, "Times", "Mal");
        Add(text, "Obstacles", "Hindernisse");
        Add(text, "Damage", "Schaden");
        Add(text, "Units", "Einheiten");
        Add(text, "Health", "Gesundheit");
        Add(text, "Level {0}", "Level {0}");
        Add(text, "{0} of {1} {2} discovered. Total codex unlocked {3}%",
            "{0} von {1} {2} entdeckt. Kodex insgesamt zu {3}% freigeschaltet");
        Add(text, "Buffs", "Buffs");
        Add(text, "Debuffs", "Debuffs");
        Add(text, "Level Modifiers", "Levelmodifikatoren");
    }

    static bool TryTranslateRunModifierDescription(string lookupKey, out string germanText)
    {
        germanText = null;

        for (int i = 0; i < DegreePrefixes.Length; i++)
        {
            string englishPrefix = DegreePrefixes[i].English;
            if (!lookupKey.StartsWith(englishPrefix + " ", StringComparison.OrdinalIgnoreCase))
                continue;

            string remainder = lookupKey.Substring(englishPrefix.Length + 1).Trim();
            if (TryGetRunModifierTemplate(remainder, out string template))
            {
                germanText = string.Format(template, DegreePrefixes[i].German);
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

    static bool TryTranslateLabelValueLines(string englishText, out string germanText)
    {
        germanText = null;

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

        germanText = string.Join("\n", lines);
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

            translatedLine = leading + LinePrefixes[i].German + trimmed.Substring(englishPrefix.Length);
            return true;
        }

        return false;
    }

    static void Add(Dictionary<string, string> text, string english, string german)
    {
        string key = NormalizeLookupKey(english);
        if (!string.IsNullOrEmpty(key))
            text[key] = german;
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

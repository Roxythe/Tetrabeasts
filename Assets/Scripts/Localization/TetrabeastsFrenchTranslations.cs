using System;
using System.Collections.Generic;
using System.Text;

public static class TetrabeastsFrenchTranslations
{
    static readonly Dictionary<string, string> ExactText = BuildExactText();

    static readonly Dictionary<string, string> RunModifierTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["decrease the amount of special gained from all sources."] = "{0} réduit l'énergie spéciale obtenue depuis toutes les sources.",
        ["decreases the amount of special gained from all sources."] = "{0} réduit l'énergie spéciale obtenue depuis toutes les sources.",
        ["increase the amount of special gained from all sources."] = "{0} augmente l'énergie spéciale obtenue depuis toutes les sources.",
        ["increases the amount of special gained from all sources."] = "{0} augmente l'énergie spéciale obtenue depuis toutes les sources.",
        ["decrease the special gained from each monster."] = "{0} réduit l'énergie spéciale obtenue de chaque monstre.",
        ["decreases the special gained from each monster."] = "{0} réduit l'énergie spéciale obtenue de chaque monstre.",
        ["increase the special gained from each monster."] = "{0} augmente l'énergie spéciale obtenue de chaque monstre.",
        ["increases the special gained from each monster."] = "{0} augmente l'énergie spéciale obtenue de chaque monstre.",
        ["decreases the attack value for all monsters in your current roster."] = "{0} réduit l'attaque de tous les monstres de votre escouade actuelle.",
        ["decrease the attack value for all monsters in your current roster."] = "{0} réduit l'attaque de tous les monstres de votre escouade actuelle.",
        ["increases the attack value for all monsters in your current roster."] = "{0} augmente l'attaque de tous les monstres de votre escouade actuelle.",
        ["increase the attack value for all monsters in your current roster."] = "{0} augmente l'attaque de tous les monstres de votre escouade actuelle.",
        ["decrease the damage of enemy projectiles."] = "{0} réduit les dégâts des projectiles ennemis.",
        ["decreases the damage of enemy projectiles."] = "{0} réduit les dégâts des projectiles ennemis.",
        ["incecrease the damage of enemy projectiles."] = "{0} augmente les dégâts des projectiles ennemis.",
        ["incecreases the damage of enemy projectiles."] = "{0} augmente les dégâts des projectiles ennemis.",
        ["increase the damage of enemy projectiles."] = "{0} augmente les dégâts des projectiles ennemis.",
        ["increases the damage of enemy projectiles."] = "{0} augmente les dégâts des projectiles ennemis.",
        ["increases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} augmente le temps entre les attaques ennemies, hors temps de recharge des boss.",
        ["increase the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} augmente le temps entre les attaques ennemies, hors temps de recharge des boss.",
        ["decreases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} réduit le temps entre les attaques ennemies, hors temps de recharge des boss.",
        ["decrease the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} réduit le temps entre les attaques ennemies, hors temps de recharge des boss.",
        ["increase the hit points of all future enemy fortifications."] = "{0} augmente les points de vie de toutes les futures fortifications ennemies.",
        ["increases the hit points of all future enemy fortifications."] = "{0} augmente les points de vie de toutes les futures fortifications ennemies.",
        ["decreases the rate falling speed builds up over time for blocks."] = "{0} réduit la vitesse à laquelle la chute des blocs s'accélère avec le temps.",
        ["decrease the rate falling speed builds up over time for blocks."] = "{0} réduit la vitesse à laquelle la chute des blocs s'accélère avec le temps.",
        ["increases the rate falling speed builds up over time for blocks."] = "{0} augmente la vitesse à laquelle la chute des blocs s'accélère avec le temps.",
        ["increase the rate falling speed builds up over time for blocks."] = "{0} augmente la vitesse à laquelle la chute des blocs s'accélère avec le temps.",
        ["decreases the initial falling speed of blocks."] = "{0} réduit la vitesse de chute initiale des blocs.",
        ["decrease the initial falling speed of blocks."] = "{0} réduit la vitesse de chute initiale des blocs.",
        ["increases the initial falling speed of blocks."] = "{0} augmente la vitesse de chute initiale des blocs.",
        ["increase the initial falling speed of blocks."] = "{0} augmente la vitesse de chute initiale des blocs.",
        ["increase the healing power of all friendly monsters."] = "{0} augmente la puissance de soin de tous les monstres alliés.",
        ["increases the healing power of all friendly monsters."] = "{0} augmente la puissance de soin de tous les monstres alliés.",
        ["decrease friendly monster pieces maximum hit points."] = "{0} réduit les points de vie max des pièces de monstres alliés.",
        ["decreases friendly monster pieces maximum hit points."] = "{0} réduit les points de vie max des pièces de monstres alliés.",
        ["increase friendly monster pieces maximum hit points."] = "{0} augmente les points de vie max des pièces de monstres alliés.",
        ["increases friendly monster pieces maximum hit points."] = "{0} augmente les points de vie max des pièces de monstres alliés.",
        ["increase luck raising the chance of getting higher rarity buffs."] = "{0} augmente la chance et la probabilité d'obtenir des bonus plus rares.",
        ["increases luck raising the chance of getting higher rarity buffs."] = "{0} augmente la chance et la probabilité d'obtenir des bonus plus rares.",
        ["increases the likelihood of finding higher rarity debuffs."] = "{0} augmente la probabilité de trouver des malus plus rares.",
        ["increase the likelihood of finding higher rarity debuffs."] = "{0} augmente la probabilité de trouver des malus plus rares.",
        ["decrease the number of reinforcement units added after winning a round."] = "{0} réduit le nombre de renforts ajoutés après une victoire.",
        ["decreases the number of reinforcement units added after winning a round."] = "{0} réduit le nombre de renforts ajoutés après une victoire.",
        ["increase the number of reinforcement units added after winning a round."] = "{0} augmente le nombre de renforts ajoutés après une victoire.",
        ["increases the number of reinforcement units added after winning a round."] = "{0} augmente le nombre de renforts ajoutés après une victoire.",
        ["decreases the chance of special blocks appearing."] = "{0} réduit les chances d'apparition des blocs spéciaux.",
        ["decrease the chance of special blocks appearing."] = "{0} réduit les chances d'apparition des blocs spéciaux.",
        ["incecreases the chance of special blocks appearing."] = "{0} augmente les chances d'apparition des blocs spéciaux.",
        ["incecrease the chance of special blocks appearing."] = "{0} augmente les chances d'apparition des blocs spéciaux.",
        ["increases the chance of special blocks appearing."] = "{0} augmente les chances d'apparition des blocs spéciaux.",
        ["increase the chance of special blocks appearing."] = "{0} augmente les chances d'apparition des blocs spéciaux.",
        ["decrease the chance a buff will drop from a stone obstacle."] = "{0} réduit la chance qu'un obstacle de pierre lâche un bonus.",
        ["decreases the chance a buff will drop from a stone obstacle."] = "{0} réduit la chance qu'un obstacle de pierre lâche un bonus.",
        ["increase the chance a buff will drop from a stone obstacle."] = "{0} augmente la chance qu'un obstacle de pierre lâche un bonus.",
        ["increases the chance a buff will drop from a stone obstacle."] = "{0} augmente la chance qu'un obstacle de pierre lâche un bonus.",
        ["decrease the maximum limit of the unit reserve."] = "{0} réduit la limite maximale de la réserve d'unités.",
        ["decreases the maximum limit of the unit reserve."] = "{0} réduit la limite maximale de la réserve d'unités.",
        ["increase the maximum limit of the unit reserve."] = "{0} augmente la limite maximale de la réserve d'unités.",
        ["increases the maximum limit of the unit reserve."] = "{0} augmente la limite maximale de la réserve d'unités.",
        ["reduces the amount of currency the player gains after winning a round."] = "{0} réduit la quantité d'or gagnée après une victoire.",
        ["reduce the amount of currency the player gains after winning a round."] = "{0} réduit la quantité d'or gagnée après une victoire.",
        ["increases the amount of currency the player gains after winning a round."] = "{0} augmente la quantité d'or gagnée après une victoire.",
        ["increase the amount of currency the player gains after winning a round."] = "{0} augmente la quantité d'or gagnée après une victoire.",
        ["increase the chance currency will be earned when clearing lines."] = "{0} augmente la chance de gagner de l'or en complétant des lignes.",
        ["increases the chance currency will be earned when clearing lines."] = "{0} augmente la chance de gagner de l'or en complétant des lignes."
    };

    static readonly (string English, string French)[] DegreePrefixes =
    {
        ("Slightly", "Légèrement"),
        ("Modestly", "Modestement"),
        ("Moderatley", "Modérément"),
        ("Moderately", "Modérément"),
        ("Significantly", "Nettement"),
        ("Massivley", "Fortement"),
        ("Massively", "Fortement")
    };

    static readonly (string English, string French)[] LinePrefixes =
    {
        ("Special Gauge Gain", "Gain de jauge spéciale"),
        ("Enemy Damage", "Dégâts ennemis"),
        ("Enemy HP", "PV ennemis"),
        ("Score Gain", "Gain de score"),
        ("EXP Gain", "Gain d'EXP"),
        ("Misfortune", "Infortune"),
        ("Gravity", "Gravité"),
        ("Score", "Points"),
        ("Level", "Niveau"),
        ("Reset", "Réinitialisation")
    };

    public static bool TryGetText(string englishText, out string frenchText)
    {
        frenchText = null;

        if (string.IsNullOrWhiteSpace(englishText))
            return false;

        string lookupKey = NormalizeLookupKey(englishText);
        if (ExactText.TryGetValue(lookupKey, out frenchText))
            return true;

        if (TryTranslateRunModifierDescription(lookupKey, out frenchText))
            return true;

        if (TryTranslateLabelValueLines(englishText, out frenchText))
            return true;

        return false;
    }

    static Dictionary<string, string> BuildExactText()
    {
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Add(text, "OK", "OK");
        Add(text, "Cancel", "Annuler");
        Add(text, "Continue", "Continuer");
        Add(text, "Confirm", "Confirmer");
        Add(text, "Close", "Fermer");
        Add(text, "Start", "Commencer");
        Add(text, "PAUSED", "PAUSE");
        Add(text, "Resume", "Reprendre");
        Add(text, "Main Menu", "Menu principal");
        Add(text, "Restart", "Recommencer");
        Add(text, "Save & Quit", "Sauver et quitter");
        Add(text, "Quit", "Quitter");
        Add(text, "New Game", "Nouvelle partie");
        Add(text, "Shop", "Boutique");
        Add(text, "Codex", "Grimoire");
        Add(text, "Help", "Aide");
        Add(text, "HighScore", "Meilleur score");
        Add(text, "Select Monsters", "Choisir les monstres");
        Add(text, "Select Commander", "Choisir le commandant");
        Add(text, "Tip: Special blocks activate as soon as they are placed.", "Astuce : Les blocs spéciaux s'activent dès qu'ils sont placés.");
        Add(text, "Tip: Rerolls can be saved and used on future reward screens during the same run.", "Astuce : Les relances peuvent être conservées et utilisées sur de futurs écrans de récompense pendant le même run.");
        Add(text, "Tip: Temporary monster copies earn EXP during a run, and some of it becomes permanent after the run ends.", "Astuce : Les copies temporaires de monstres gagnent de l'EXP pendant un run, et une partie devient permanente à la fin du run.");
        Add(text, "Tip: Full rows launch attacks at the enemy castle.", "Astuce : Les lignes complètes lancent des attaques contre le château ennemi.");
        Add(text, "Tip: Keep an eye on your unit reserve. If it reaches 0, the run is over.", "Astuce : Surveillez votre réserve d'unités. Si elle atteint 0, le run est terminé.");
        Add(text, "Tip: Level modifiers stack with your run buffs and debuffs.", "Astuce : Les modificateurs de niveau se cumulent avec vos bonus et malus de run.");
        Add(text, "Combat Log", "Journal de combat");
        Add(text, "{0} takes {1} damage.", "{0} subit {1} d\u00e9g\u00e2ts.");
        Add(text, "{0} heals {1}.", "{0} se soigne de {1}.");
        Add(text, "{0} dies.", "{0} meurt.");
        Add(text, "{0} uses {1}.", "{0} utilise {1}.");
        Add(text, "{0} casts {1}.", "{0} lance {1}.");
        Add(text, "{0} took {1}{2} damage{3}.", "{0} subit {1}{2} d\u00e9g\u00e2ts{3}.");
        Add(text, "{0} restored {1} health for {2}.", "{0} rend {1} PV \u00e0 {2}.");
        Add(text, "{0} dealt {1} damage to {2}.{3}", "{0} inflige {1} d\u00e9g\u00e2ts \u00e0 {2}.{3}");
        Add(text, " from {0}", " de {0}");
        Add(text, "(shielded)", "(prot\u00e9g\u00e9)");
        Add(text, "poison", "poison");
        Add(text, "fire", "feu");
        Add(text, "lightning", "foudre");
        Add(text, "contagion", "contagion");
        Add(text, "starvation", "famine");
        Add(text, "burst", "explosion");
        Add(text, "floor effect", "effet de sol");
        Add(text, "storm", "temp\u00eate");
        Add(text, "infection", "infection");
        Add(text, "low rations", "rations basses");
        Add(text, "death burst", "explosion mortelle");
        Add(text, "spikes", "pics");
        Add(text, "Enemy Archer", "archer ennemi");
        Add(text, "rear ambush", "embuscade arri\u00e8re");
        Add(text, "Castle", "Ch\u00e2teau");
        Add(text, "Skybreaker Edict", "\u00c9dit brise-ciel");
        Add(text, "Heaven's Judgement", "Jugement du ciel");
        Add(text, "Stormcaller's Verdict", "Verdict de l'invocateur d'orage");
        Add(text, "Hex of the Warped Ground", "Mal\u00e9fice du sol distordu");
        Add(text, "Aegis of the Unbroken Crown", "\u00c9gide de la couronne intacte");
        Add(text, "Temporal Distortion", "Distorsion temporelle");
        Add(text, "Ward of the Arcane Pylons", "Protection des pyl\u00f4nes arcanes");
        Add(text, "Rune of Ruin", "Rune de ruine");
        Add(text, "Summon Earthen Rampart", "Invoquer un rempart de terre");
        Add(text, "Raise Iron Thorns", "Dresser des \u00e9pines de fer");
        Add(text, "Sow Venomous Miasma", "R\u00e9pandre un miasme venimeux");
        Add(text, "Kindle Infernal Sigils", "Embraser des sceaux infernaux");
        Add(text, "Call Stormbound Sigils", "Appeler des sceaux d'orage");
        Add(text, "Skip Trailer", "Passer la bande-annonce");
        Add(text, "BGM Genre", "Genre de musique");
        Add(text, "EDM", "EDM");
        Add(text, "Metal", "Musique métal");
        Add(text, "Random", "Aléatoire");
        Add(text, "Language", "Langue");
        Add(text, "Settings", "Paramètres");
        Add(text, "Master Volume", "Volume général");
        Add(text, "Music Volume", "Volume de la musique");
        Add(text, "SFX Volume", "Volume des effets");
        Add(text, "Cursor Size", "Taille du curseur");
        Add(text, "Choose Language", "Choisir la langue");
        Add(text, "Pick the language Tetrabeasts should use. You can change this later in Settings.", "Choisissez la langue utilisée par Tetrabeasts. Vous pourrez la changer plus tard dans les paramètres.");
        Add(text, "Press Any Key", "Appuyez sur une touche");
        Add(text, "Modifiers", "Modificateurs");
        Add(text, "Active Run Modifiers", "Modificateurs de run actifs");
        Add(text, "Back", "Retour");
        Add(text, "None", "Aucun");
        Add(text, "Yes", "Oui");
        Add(text, "No", "Non");
        Add(text, "Locked", "Verrouillé");
        Add(text, "Blocked", "Bloqué");
        Add(text, "Active", "Actif");
        Add(text, "(Missing)", "(Manquant)");
        Add(text, "???", "???");
        Add(text, "Leaderboard", "Classement");
        Add(text, "LEADERBOARDS", "CLASSEMENTS");
        Add(text, "Global", "Global");
        Add(text, "Friends", "Amis");
        Add(text, "Current", "Actuel");
        Add(text, "Rank", "Rang");
        Add(text, "Player", "Joueur");
        Add(text, "Refresh", "Actualiser");
        Add(text, "Loading...", "Chargement...");
        Add(text, "Refreshing leaderboards...", "Actualisation des classements...");
        Add(text, "Steam leaderboard ready.", "Classement Steam prêt.");
        Add(text, "Leaderboard refresh failed.", "Échec de l'actualisation du classement.");
        Add(text, "No leaderboard data.", "Aucune donnée de classement.");
        Add(text, "No global scores yet.", "Aucun score global pour l'instant.");
        Add(text, "No friend scores yet.", "Aucun score d'ami pour l'instant.");
        Add(text, "You are not ranked yet.", "Vous n'êtes pas encore classé.");
        Add(text, "Achievements", "Succès");
        Add(text, "Submit Name", "Envoyer le nom");
        Add(text, "Help Menu", "Menu d'aide");
        Add(text, "Modifier not yet discovered.", "Modificateur pas encore découvert.");
        Add(text, "No level Modifier", "Aucun modificateur de niveau");
        Add(text, "Level modifier: {0}.", "Modificateur de niveau : {0}.");
        Add(text, "Secret Achievements", "Succès secrets");
        Add(text, "1 secret achievement remaining", "1 succès secret restant");
        Add(text, "{0} secret achievements remaining", "{0} succès secrets restants");
        Add(text, "No GameController found.", "Aucun GameController trouvé.");
        Add(text, "No monsters in roster.", "Aucun monstre dans l'escouade.");
        Add(text, "Reinforcements", "Renforts");
        Add(text, "Active Squad", "Escouade active");
        Add(text, "Choose A Buff", "Choisir un bonus");
        Add(text, "Choose A Debuff", "Choisir un malus");
        Add(text, "Next Piece", "Pièce suivante");
        Add(text, "Reroll", "Relancer");
        Add(text, "EXP Gained", "EXP gagnée");
        Add(text, "Permanent EXP Gained", "EXP permanente gagnée");
        Add(text, "EXP Preserved", "EXP conservée");
        Add(text, "New Difficulty Unlocked!", "Nouvelle difficulté débloquée !");
        Add(text, "Special Ability: {0}", "Capacité spéciale : {0}");
        Add(text, "{0} is broken.", "{0} est brisé.");
        Add(text, "{0}: {1} shield(s) remain.", "{0} : {1} bouclier(s) restant(s).");
        Add(text, "Starting a new game will erase the saved run. Continue?", "Commencer une nouvelle partie effacera le run sauvegardé. Continuer ?");
        Add(text, "Current Level: {0}", "Niveau actuel : {0}");
        Add(text, "Current Level: 0", "Niveau actuel : 0");
        Add(text, " x{0}", " x{0}");
        Add(text, "Refund", "Rembourser");
        Add(text, "Unlock", "Débloquer");
        Add(text, " Unlock", " Débloquer");
        Add(text, "Selected:", "Sélectionné :");
        Add(text, "Selected: ", "Sélectionné : ");
        Add(text, "{0}  Lv.{1}", "{0}  Nv.{1}");
        Add(text, "Luck Up", "Chance +");
        Add(text, "Gravity Down", "Gravité -");
        Add(text, "Velocity Down", "Vitesse -");
        Add(text, "Gold Up", "Or +");
        Add(text, "Attack Up", "Attaque +");
        Add(text, "HP Up", "PV +");
        Add(text, "Unit Lives Up", "Vies d'unité +");
        Add(text, "Role: {0}", "Rôle : {0}");
        Add(text, "Level: {0}  ({1:0.#}/{2})", "Niveau : {0}  ({1:0.#}/{2})");
        Add(text, "Max HP: {0:0.#}  (+{1}) = {2:0.#}", "PV max : {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Attack: {0:0.#}  (+{1}) = {2:0.#}", "Attaque : {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Special Gain: {0:0.#}", "Gain spécial : {0:0.#}");
        Add(text, "Heal: {0:0.#}  (+{1}) = {2:0.#}", "Soin : {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Heal Range: {0:0.#}", "Portée de soin : {0:0.#}");
        Add(text, "Heal Speed: {0:0.#}s", "Vitesse de soin : {0:0.#} s");
        Add(text, "Heal: -", "Soin : -");
        Add(text, "Base Stats + Shop Buff = Total Stats", "Stats de base + bonus de boutique = stats totales");
        Add(text, "Defense", "Défense");
        Add(text, "Beefy boy that deals very little damage.", "Un solide gaillard qui inflige très peu de dégâts.");
        Add(text, "Healer with a wide range, but weak spells.", "Soigneur à grande portée, mais aux sorts faibles.");
        Add(text, "A well rounded attacker unit with decent damage, but enough health to survive weaker attacks.", "Une unité d'attaque équilibrée avec des dégâts corrects et assez de vie pour survivre aux attaques faibles.");
        Add(text, "A specialized attacker unit with high base damage and lower health.", "Une unité d'attaque spécialisée avec de gros dégâts de base, mais moins de vie.");
        Add(text, "Can take a hit and keep going, but won't deal much damage.", "Peut encaisser des coups et continuer, mais n'inflige pas beaucoup de dégâts.");
        Add(text, "Healer with a short range, but powerful healing spells.", "Soigneur à courte portée, mais aux sorts de soin puissants.");
        Add(text, "Combo Extension", "Extension de combo");
        Add(text, "Chain Surge", "Déferlement en chaîne");
        Add(text, "Stone Scrounger", "Fouilleur de pierre");
        Add(text, "Reserve Stockpile", "Stock de réserve");
        Add(text, "Reserve Recovery", "Récupération de réserve");
        Add(text, "Bulwark Aura", "Aura de rempart");
        Add(text, "Increase combo timer duration by {0}.", "Augmente la durée du chronomètre de combo de {0}.");
        Add(text, "Each row clear has a {0} chance to increase combo count one additional time.", "Chaque ligne complétée a {0} de chances d'augmenter le compteur de combo une fois de plus.");
        Add(text, "Increase chance of buff drop from stone obstacle destruction by {0}.", "Augmente de {0} la chance d'obtenir un bonus en détruisant un obstacle de pierre.");
        Add(text, "Increase the number of starting reserve units by {0}.", "Augmente le nombre d'unités de réserve de départ de {0}.");
        Add(text, "Increase the number of reserve units restored on round win by {0}.", "Augmente de {0} le nombre d'unités de réserve restaurées après une victoire.");
        Add(text, "Decrease damage taken and damage done for all ally monster units by {0}.", "Réduit de {0} les dégâts subis et infligés par toutes les unités monstrueuses alliées.");
        Add(text, "Passive - {0}", "Passif - {0}");
        Add(text, "Passive - {0}:", "Passif - {0} :");
        Add(text, "Next upgrade at Lv.{0}:", "Prochaine amélioration au niv.{0} :");
        Add(text, "Passive is fully upgraded.", "Le passif est entièrement amélioré.");
        Add(text, "1 second", "1 seconde");
        Add(text, "{0} seconds", "{0} secondes");
        Add(text, "A = Shift Left", "A = Décaler à gauche");
        Add(text, "D = Shift Right", "D = Décaler à droite");
        Add(text, "D = Shft Right", "D = Décaler à droite");
        Add(text, "R = Character Special", "R = Spécial du personnage");
        Add(text, "= Character Special", "= Spécial du personnage");
        Add(text, "R = Character Special (Special Guage 100%)", "R = Spécial du personnage (jauge spéciale 100 %)");
        Add(text, "R = Character Special (Special Gauge 100%)", "R = Spécial du personnage (jauge spéciale 100 %)");
        Add(text, "R = Character Special (Specaial Gauge 100%)", "R = Spécial du personnage (jauge spéciale 100 %)");
        Add(text, "= Character Special (Special Guage 100%)", "= Spécial du personnage (jauge spéciale 100 %)");
        Add(text, "= Character Special (Special Gauge 100%)", "= Spécial du personnage (jauge spéciale 100 %)");
        Add(text, "Boss", "Boss");
        Add(text, "Boss Ability", "Capacité de boss");
        Add(text, "Boss Abilities", "Capacités de boss");
        Add(text, "Gold Won This Round:", "Or gagné ce round :");
        Add(text, "Gold Won This Round", "Or gagné ce round");
        Add(text, "Rerolls", "Relances");
        Add(text, "Rerolls: {0}", "Relances : {0}");
        Add(text, "Rerolls: 0", "Relances : 0");
        Add(text, "Rerolls ({0})", "Relances ({0})");
        Add(text, "Modifier", "Modificateur");
        Add(text, "x{0}", "x{0}");
        Add(text, "Level Up", "Niveau supérieur");
        Add(text, "Level Up!", "Niveau supérieur !");
        Add(text, "LEVEL UP!", "NIVEAU SUPÉRIEUR !");
        Add(text, "LEVEL UP! x{0}", "NIVEAU SUPÉRIEUR ! x{0}");
        Add(text, "Level {0} -> Level {1}", "Niveau {0} -> Niveau {1}");
        Add(text, "+{0} Exp", "+{0} EXP");
        Add(text, "{0} permanent EXP ({1}% of {2} transferable EXP)", "{0} EXP permanente ({1} % des {2} EXP transférables)");
        Add(text, "Converted from {0} run EXP at {1}%", "Converti depuis {0} EXP de run à {1} %");
        Add(text, "Passive+", "Passif+");
        Add(text, "+ 5 HP", "+ 5 PV");
        Add(text, "+ 1 Attack", "+ 1 Attaque");
        Add(text, "+ 1 Special", "+ 1 Spécial");
        Add(text, "+ 5 Heal", "+ 5 Soin");
        Add(text, "+ 1 Range", "+ 1 Portée");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.", "Ensuite, je veux que vous fassiez tomber instantanément votre pièce actuelle avec la [Barre d'espace]. Cela fera immédiatement tomber la pièce pour la placer vite, la verrouiller et en faire apparaître une nouvelle.");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.", "Ensuite, je veux que vous fassiez tomber instantanément votre pièce actuelle avec la [Barre d'espace]. Cela fera immédiatement tomber la pièce pour la placer vite, la verrouiller et en faire apparaître une nouvelle.");
        Add(text, "This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.", "Cela fera immédiatement tomber votre pièce pour la placer vite, la verrouiller et en faire apparaître une nouvelle.");
        Add(text, "This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.", "Cela fera immédiatement tomber votre pièce pour la placer vite, la verrouiller et en faire apparaître une nouvelle.");
        Add(text, "I Want Every Able Body", "Je veux tous les combattants disponibles");
        Add(text, "Unlock all monster units.", "Débloquez toutes les unités monstrueuses.");
        Add(text, "0 Star Victory", "Victoire 0 étoile");
        Add(text, "Beat the final boss.", "Battez le boss final.");
        Add(text, "One Star Victory", "Victoire 1 étoile");
        Add(text, "Beat the final boss on 1-Star difficulty.", "Battez le boss final en difficulté 1 étoile.");
        Add(text, "Pay To Win", "Payer pour gagner");
        Add(text, "Upgrade any shop buff to level 5.", "Améliorez un bonus de boutique au niveau 5.");
        Add(text, "Gangs All Here", "Toute la bande est là");
        Add(text, "Unlock all Commanders.", "Débloquez tous les commandants.");
        Add(text, "Two Star Victory", "Victoire 2 étoiles");
        Add(text, "Beat the final boss on 2-Star difficulty.", "Battez le boss final en difficulté 2 étoiles.");
        Add(text, "Three Star Victory", "Victoire 3 étoiles");
        Add(text, "Beat the final boss on 3-Star difficulty.", "Battez le boss final en difficulté 3 étoiles.");
        Add(text, "Four Star Victory", "Victoire 4 étoiles");
        Add(text, "Beat the final boss on 4-Star difficulty.", "Battez le boss final en difficulté 4 étoiles.");
        Add(text, "Five Star Victory", "Victoire 5 étoiles");
        Add(text, "Beat the final boss on 5-Star difficulty.", "Battez le boss final en difficulté 5 étoiles.");
        Add(text, "This is Fine", "Tout va bien");
        Add(text, "Take 1,000 burn damage from fire floor effects.", "Subissez 1 000 dégâts de brûlure dus aux effets de sol de feu.");
        Add(text, "That Escalated Quickly", "Ça a vite dégénéré");
        Add(text, "Remove 1,000 units using the Death Special Block.", "Retirez 1 000 unités avec le bloc spécial Mort.");
        Add(text, "First Time Raider", "Premier pillage");
        Add(text, "Take 1,000 damage from traps.", "Subissez 1 000 dégâts de pièges.");
        Add(text, "I think I Stepped in Something", "Je crois avoir marché dans quelque chose");
        Add(text, "Take 1,000 toxic damage from posioned floor effects.", "Subissez 1 000 dégâts toxiques dus aux effets de sol empoisonné.");
        Add(text, "Take 1,000 toxic damage from poisoned floor effects.", "Subissez 1 000 dégâts toxiques dus aux effets de sol empoisonné.");
        Add(text, "Shake It Until You Break It", "Secouez jusqu'à ce que ça casse");
        Add(text, "Clear 250 rows by using the earthquake special block.", "Complétez 250 lignes avec le bloc spécial Séisme.");
        Add(text, "A Little Jiggle Goes A Long Way", "Une petite secousse peut tout changer");
        Add(text, "Clear 25 rows by using the earthquake special block.", "Complétez 25 lignes avec le bloc spécial Séisme.");
        Add(text, "Girthquake", "Méga-séisme");
        Add(text, "Clear 1,000 rows by using the earthquake special block.", "Complétez 1 000 lignes avec le bloc spécial Séisme.");
        Add(text, "Get in Loser, We're Going Shopping", "Monte, on va faire les boutiques");
        Add(text, "Accumulate 1,000 gold.", "Accumulez 1 000 pièces d'or.");
        Add(text, "Is I Rich Now?", "Je suis riche maintenant ?");
        Add(text, "Accumulate 100 gold.", "Accumulez 100 pièces d'or.");
        Add(text, "This Lasted Longer Than Some Collectible Fads", "Ça a duré plus longtemps que certaines modes de collection");
        Add(text, "Take more than 5 minutes to conquer a castle.", "Mettez plus de 5 minutes à conquérir un château.");
        Add(text, "Anything You Can Do, I Can Do Slower", "Tout ce que tu fais, je peux le faire plus lentement");
        Add(text, "Take more than 3 minutes to conquer a castle.", "Mettez plus de 3 minutes à conquérir un château.");
        Add(text, "Sloth Lord", "Seigneur de la lenteur");
        Add(text, "Take more than 4 minutes to conquer a castle.", "Mettez plus de 4 minutes à conquérir un château.");
        Add(text, "My Fingers Hurt", "Mes doigts me font mal");
        Add(text, "Survive with gravity at 10 for 60 seconds.", "Survivez 60 secondes avec la gravité à 10.");
        Add(text, "Nevermind...", "Laissez tomber...");
        Add(text, "Unlock your first first temporary debuff.", "Débloquez votre premier malus temporaire.");
        Add(text, "A Record That Would Make Lions Blush", "Un record à faire rougir les lions");
        Add(text, "Lose 50 Times.", "Perdez 50 fois.");
        Add(text, "Thrive Under Pressure", "Prospérer sous pression");
        Add(text, "Survive with gravity at 10 for 30 seconds.", "Survivez 30 secondes avec la gravité à 10.");
        Add(text, "Is I Strong Now?", "Je suis fort maintenant ?");
        Add(text, "Unlock your first first temporary buff.", "Débloquez votre premier bonus temporaire.");
        Add(text, "I Say This Not As An Insult, But As A Statement Of Fact", "Je ne dis pas ça comme une insulte, mais comme un fait");
        Add(text, "Lose 100 Times.", "Perdez 100 fois.");
        Add(text, "I Think We Need A Bigger Vault", "Je crois qu'il nous faut un plus grand coffre");
        Add(text, "Accumulate 10,000 gold.", "Accumulez 10 000 pièces d'or.");
        Add(text, "GG EZ", "GG facile");
        Add(text, "Beat the final level with every Commander.", "Terminez le dernier niveau avec chaque commandant.");
        Add(text, "Participation Trophy", "Trophée de participation");
        Add(text, "Lose for the first time.", "Perdez pour la première fois.");
        Add(text, "Immortal Army", "Armée immortelle");
        Add(text, "Conquer 100 castles with your Unit Reserve at max capacity.", "Conquérez 100 châteaux avec votre réserve d'unités au maximum.");
        Add(text, "I Guess That Was The Wrong Wire", "Je crois que c'était le mauvais fil");
        Add(text, "Lose 100 monster units to magic explosives.", "Perdez 100 unités monstrueuses à cause d'explosifs magiques.");
        Add(text, "I Think It's Dead Now", "Je crois que c'est mort maintenant");
        Add(text, "Clear 1,000 rows.", "Complétez 1 000 lignes.");
        Add(text, "Tis But A Scratch", "Ce n'est qu'une égratignure");
        Add(text, "Conquer a castle with your Unit Reserve at max capacity.", "Conquérez un château avec votre réserve d'unités au maximum.");
        Add(text, "Meat Shield Tactics", "Tactiques de bouclier de chair");
        Add(text, "Conquer 25 castles with your Unit Reserve at max capacity.", "Conquérez 25 châteaux avec votre réserve d'unités au maximum.");
        Add(text, "You Got Your Marching Orders", "Vous avez vos ordres de marche");
        Add(text, "Clear 100 rows.", "Complétez 100 lignes.");
        Add(text, "Overwhelming Power", "Puissance écrasante");
        Add(text, "Accumulate 15 buffs in a single run.", "Accumulez 15 bonus en un seul run.");
        Add(text, "Is There Anything Left To Attack?", "Reste-t-il quelque chose à attaquer ?");
        Add(text, "Clear 10,000 rows.", "Complétez 10 000 lignes.");
        Add(text, "Shut Up And Takey My Money", "Tais-toi et prends mon argent");
        Add(text, "Buy an upgrade from the shop for the first time.", "Achetez une amélioration à la boutique pour la première fois.");
        Add(text, "Turning Small Numbers Into Big Numbers", "Transformer les petits nombres en grands nombres");
        Add(text, "Get a combo of 10 or higher.", "Obtenez un combo de 10 ou plus.");
        Add(text, "Vewwy Stwong", "Twès fowt");
        Add(text, "Deal 100 damage or more in a single attack.", "Infligez 100 dégâts ou plus en une seule attaque.");
        Add(text, "I'll Follow You Anywhere", "Je te suivrai partout");
        Add(text, "Unlock a Commander for the first time.", "Débloquez un commandant pour la première fois.");
        Add(text, "Eenie Meenie Miney Mo", "Am stram gram");
        Add(text, "Unlock a monster unit for the first time.", "Débloquez une unité monstrueuse pour la première fois.");
        Add(text, "Tell Me I'm Pretty", "Dis-moi que je suis beau");
        Add(text, "Unlock a monster units skin variant for the first time.", "Débloquez une variante d'apparence d'une unité monstrueuse pour la première fois.");
        Add(text, "Are We There Yet?", "On est bientôt arrivés ?");
        Add(text, "Conquer 10 castles in a single run.", "Conquérez 10 châteaux en un seul run.");
        Add(text, "It's Called Fashion Brenda, Look It Up", "Ça s'appelle la mode, Brenda, renseigne-toi");
        Add(text, "Unlock ten skin variants.", "Débloquez dix variantes d'apparence.");
        Add(text, "New Skin Who Dis?", "Nouvelle apparence, qui est-ce ?");
        Add(text, "Unlock five skin variants.", "Débloquez cinq variantes d'apparence.");
        Add(text, "A Little Special", "Un peu spécial");
        Add(text, "Use a special for the first time.", "Utilisez une capacité spéciale pour la première fois.");
        Add(text, "Mama's Special Boy", "Le petit spécial de maman");
        Add(text, "Use a special 100 times.", "Utilisez une capacité spéciale 100 fois.");
        Add(text, "Some Are More Special Than Others", "Certains sont plus spéciaux que d'autres");
        Add(text, "Use every Commanders special 100 times.", "Utilisez la capacité spéciale de chaque commandant 100 fois.");
        Add(text, "General Got Me Workin'", "Le général me fait travailler");
        Add(text, "Destory 100 stone obstacles.", "Détruisez 100 obstacles de pierre.");
        Add(text, "Destroy 100 stone obstacles.", "Détruisez 100 obstacles de pierre.");
        Add(text, "Certified Glue Eater", "Mangeur de colle certifié");
        Add(text, "Use a special 1000 times.", "Utilisez une capacité spéciale 1 000 fois.");
        Add(text, "We're All Special", "Nous sommes tous spéciaux");
        Add(text, "Use every Commanders special 20 times.", "Utilisez la capacité spéciale de chaque commandant 20 fois.");
        Add(text, "That's A Lot Of Rubble", "Ça fait beaucoup de gravats");
        Add(text, "Conquer 50 castles.", "Conquérez 50 châteaux.");
        Add(text, "King of Rubble", "Roi des gravats");
        Add(text, "Conquer 100 castles.", "Conquérez 100 châteaux.");
        Add(text, "I. AM. SPEED!", "JE. SUIS. LA VITESSE !");
        Add(text, "Conquer a castle in 30 seconds or less.", "Conquérez un château en 30 secondes ou moins.");
        Add(text, "Can't Stop, Won't Stop", "Impossible de s'arrêter");
        Add(text, "Conquer your first castle.", "Conquérez votre premier château.");
        Add(text, "I Ran Track In Highschool", "Je faisais de l'athlé au lycée");
        Add(text, "Conquer a castle in 45 seconds or less.", "Conquérez un château en 45 secondes ou moins.");
        Add(text, "Gotta Go Fast", "Il faut aller vite");
        Add(text, "Conquer a castle in 60 seconds or less.", "Conquérez un château en 60 secondes ou moins.");
        Add(text, "Level {0} Complete", "Niveau {0} terminé");
        Add(text, "Base Level XP", "XP de base du niveau");
        Add(text, "Clear time {0}", "Temps de réussite {0}");
        Add(text, "Units lost {0}", "Unités perdues {0}");
        Add(text, "Largest Combo {0}", "Plus grand combo {0}");
        Add(text, "Obstacles Cleared {0}", "Obstacles détruits {0}");
        Add(text, "Star Difficulty ({0}):", "Difficulté étoilée ({0}) :");
        Add(text, "Total XP Earned {0}", "XP totale gagnée {0}");
        Add(text, "Run", "Run");
        Add(text, "Level", "Niveau");
        Add(text, "Controls", "Commandes");
        Add(text, "Gravity", "Gravité");
        Add(text, "Combo", "Combo");
        Add(text, "Score", "Points");
        Add(text, "Reset", "Réinitialiser");
        Add(text, "Score: {0}", "Points : {0}");
        Add(text, "Gravity: {0:0.0}", "Gravité : {0:0.0}");
        Add(text, "Star Difficulty", "Difficulté étoilée");
        Add(text, "0 Star: Recruit Difficulty", "0 étoile : difficulté Recrue");
        Add(text, "1 Star: Soldier Difficulty", "1 étoile : difficulté Soldat");
        Add(text, "2 Star: Veteran Difficulty", "2 étoiles : difficulté Vétéran");
        Add(text, "3 Star: Lieutenant Difficulty", "3 étoiles : difficulté Lieutenant");
        Add(text, "4 Star: General Difficulty", "4 étoiles : difficulté Général");
        Add(text, "5 Star: War God Difficulty", "5 étoiles : difficulté Dieu de guerre");
        Add(text, "0-Star Difficulty", "Difficulté 0 étoile");
        Add(text, "1 Star Difficulty", "Difficulté 1 étoile");
        Add(text, "2 Stars Difficulty", "Difficulté 2 étoiles");
        Add(text, "3 Stars Difficulty", "Difficulté 3 étoiles");
        Add(text, "4 Stars Difficulty", "Difficulté 4 étoiles");
        Add(text, "5 Stars Difficulty", "Difficulté 5 étoiles");
        Add(text, "Normal difficulty.", "Difficulté normale.");
        Add(text, "No gameplay modifiers.", "Aucun modificateur de gameplay.");
        Add(text, "All star difficulties unlocked.", "Toutes les difficultés étoilées sont débloquées.");
        Add(text, "0 Stars is always available.", "0 étoile est toujours disponible.");
        Add(text, "Beat the final level on 0 Stars to unlock 1 Star.", "Terminez le dernier niveau en 0 étoile pour débloquer 1 étoile.");
        Add(text, "Beat the final level on 1 Star to unlock 2 Stars.", "Terminez le dernier niveau en 1 étoile pour débloquer 2 étoiles.");
        Add(text, "Beat the final level on 2 Stars to unlock 3 Stars.", "Terminez le dernier niveau en 2 étoiles pour débloquer 3 étoiles.");
        Add(text, "Beat the final level on 3 Stars to unlock 4 Stars.", "Terminez le dernier niveau en 3 étoiles pour débloquer 4 étoiles.");
        Add(text, "Beat the final level on 4 Stars to unlock 5 Stars.", "Terminez le dernier niveau en 4 étoiles pour débloquer 5 étoiles.");
        Add(text, "0 Stars", "0 étoile");
        Add(text, "1 Star", "1 étoile");
        Add(text, "2 Stars", "2 étoiles");
        Add(text, "3 Stars", "3 étoiles");
        Add(text, "4 Stars", "4 étoiles");
        Add(text, "5 Stars", "5 étoiles");
        Add(text, "Score Gain", "Gain de score");
        Add(text, "EXP Gain", "Gain d'EXP");
        Add(text, "Level Modifier", "Modificateur de niveau");
        Add(text, "Reserves And Rewards", "Réserves et récompenses");
        Add(text, "Reserve Units", "Unités de réserve");
        Add(text, "Max Reserve Units", "Unités de réserve max");
        Add(text, "Reserve Restored On Win", "Réserve restaurée à la victoire");
        Add(text, "Round Win Currency", "Or de victoire");
        Add(text, "Line Clear Currency Chance", "Chance d'or par ligne");
        Add(text, "Line Clear Currency Amount", "Quantité d'or par ligne");
        Add(text, "Monster Combat", "Combat des monstres");
        Add(text, "Monster Damage", "Dégâts des monstres");
        Add(text, "Monster Special Gain", "Gain spécial des monstres");
        Add(text, "Monster Max HP", "PV max des monstres");
        Add(text, "Healing Power", "Puissance de soin");
        Add(text, "Healing Range Bonus", "Bonus de portée de soin");
        Add(text, "Ally Damage Dealt", "Dégâts alliés infligés");
        Add(text, "Ally Damage Taken", "Dégâts alliés subis");
        Add(text, "Combo And Passives", "Combo et passifs");
        Add(text, "Combo Window", "Fenêtre de combo");
        Add(text, "Bonus Combo Chance", "Chance de combo bonus");
        Add(text, "Stone Buff Drop Chance", "Chance de bonus des pierres");
        Add(text, "Starting Reserve Passive", "Passif de réserve initiale");
        Add(text, "Round Win Reserve Passive", "Passif de réserve à la victoire");
        Add(text, "Enemy", "Ennemi");
        Add(text, "Enemy Castle HP", "PV du château ennemi");
        Add(text, "Enemy Damage", "Dégâts ennemis");
        Add(text, "Enemy Attack Interval", "Intervalle d'attaque ennemi");
        Add(text, "Enemy Projectile Speed", "Vitesse des projectiles ennemis");
        Add(text, "Castle Projectile Damage", "Dégâts des projectiles du château");
        Add(text, "Castle Attack Interval", "Intervalle d'attaque du château");
        Add(text, "Piece And Special", "Pièce et spécial");
        Add(text, "Piece Gravity", "Gravité des pièces");
        Add(text, "Gravity Ramp Rate", "Accélération de gravité");
        Add(text, "Special Block Chance", "Chance de bloc spécial");
        Add(text, "Commander Special Gain", "Gain spécial du commandant");
        Add(text, "Special Drain", "Drain spécial");
        Add(text, "Next Preview Disabled", "Aperçu suivant désactivé");
        Add(text, "Landing Hint Disabled", "Indicateur d'atterrissage désactivé");
        Add(text, "Special Usage Locked", "Utilisation spéciale verrouillée");
        Add(text, "Special Blocks Blocked", "Blocs spéciaux bloqués");
        Add(text, "Run Modifier Drops", "Butins de modificateurs de run");
        Add(text, "Stone Drops Debuffs Only", "Les pierres lâchent seulement des malus");
        Add(text, "Luck", "Chance");
        Add(text, "Misfortune", "Infortune");
        Add(text, "Active Level Modifier", "Modificateur de niveau actif");
        Add(text, "Effect", "Effet");
        Add(text, "Outgoing Damage", "Dégâts sortants");
        Add(text, "Incoming Damage", "Dégâts entrants");
        Add(text, "Overgrowth Target Interval", "Intervalle de cible de prolifération");
        Add(text, "Initial Target Rows", "Lignes ciblées initiales");
        Add(text, "Partial Growth Time", "Temps de croissance partielle");
        Add(text, "Full Growth Time", "Temps de croissance totale");
        Add(text, "Storm Strike Damage", "Dégâts de frappe d'orage");
        Add(text, "Storm Floor Tick Damage", "Dégâts périodiques du sol d'orage");
        Add(text, "Storm Floor Duration", "Durée du sol d'orage");
        Add(text, "Rear Ambush Interval", "Intervalle d'embuscade arrière");
        Add(text, "Rations Tick Interval", "Intervalle de rationnement");
        Add(text, "Low Reserve Damage", "Dégâts de réserve basse");
        Add(text, "High Reserve Damage", "Dégâts de réserve haute");
        Add(text, "Infection Chance", "Chance d'infection");
        Add(text, "Damage Per Tick", "Dégâts par tick");
        Add(text, "Damage Increase Per Tick", "Augmentation des dégâts par tick");
        Add(text, "Spread Chance", "Chance de propagation");
        Add(text, "Special Gauge Gain", "Gain de jauge spéciale");
        Add(text, "Death Explosion Damage", "Dégâts d'explosion mortelle");
        Add(text, "Swamp Poison Damage", "Dégâts de poison du marais");
        Add(text, "Swamp Poison Interval", "Intervalle du poison du marais");
        Add(text, "Manual Rotation", "Rotation manuelle");
        Add(text, "Auto Rotate Interval", "Intervalle de rotation auto");
        Add(text, "Manual Horizontal Shift", "Déplacement horizontal manuel");
        Add(text, "Auto Shift Interval", "Intervalle de déplacement auto");
        Add(text, "Combo Threshold", "Seuil de combo");
        Add(text, "Below Threshold Damage", "Dégâts sous le seuil");
        Add(text, "Shield Combo Threshold", "Seuil de combo du bouclier");
        Add(text, "Blocked Damage", "Dégâts bloqués");
        Add(text, "Shield Count", "Nombre de boucliers");
        Add(text, "Special Pieces", "Pièces spéciales");
        Add(text, "Starting Monster Health", "Santé de départ des monstres");
        Add(text, "Monster Damage Sharing", "Partage des dégâts des monstres");
        Add(text, "Roster", "Escouade");
        Add(text, "Max HP", "PV max");
        Add(text, "Starting HP", "PV de départ");
        Add(text, "Attack", "Attaque");
        Add(text, "Special Gain", "Gain spécial");
        Add(text, "Heal Power", "Puissance de soin");
        Add(text, "Heal Range", "Portée de soin");
        Add(text, "Heal Speed", "Vitesse de soin");
        Add(text, "Spawn Weight", "Poids d'apparition");
        Add(text, "Stats", "Stats");
        Add(text, "Monsters", "Monstres");
        Add(text, "Shop Buff", "Bonus de boutique");
        Add(text, "Passive", "Passif");
        Add(text, "Run Mod Buff", "Bonus de run");
        Add(text, "Run Mod Debuff", "Malus de run");
        Add(text, "Level Mod", "Mod. de niveau");
        Add(text, "Boss Ability", "Capacité de boss");
        Add(text, "Defender", "Défenseur");
        Add(text, "Healer", "Soigneur");
        Add(text, "Role", "Rôle");
        Add(text, "HP", "PV");
        Add(text, "Special Rate", "Taux spécial");
        Add(text, "Passive Lv", "Nv. passif");
        Add(text, "Esc = Pause", "Échap = Pause");
        Add(text, "Q = Rotate Counter Clockwise", "Q = Rotation antihoraire");
        Add(text, "E = Rotate Clockwise", "E = Rotation horaire");
        Add(text, "= Shift Left", "= Décaler à gauche");
        Add(text, "S = Shift Down", "S = Descendre");
        Add(text, "Spacebar = Drop Instantly", "Espace = Chute instantanée");
        Add(text, "= Character Special (Specaial Gauge 100%)", "= Spécial du personnage (jauge spéciale 100 %)");
        Add(text, "Castle", "Château");
        Add(text, "Starting Village", "Village de départ");
        Add(text, "Tribe", "Tribu");
        Add(text, "Tribe Chieftain", "Chef de tribu");
        Add(text, "Shanty Town", "Bidonville");
        Add(text, "Fortified Hamlet", "Hameau fortifié");
        Add(text, "Industrial City", "Cité industrielle");
        Add(text, "Thriving Metropolis", "Métropole prospère");
        Add(text, "Grand Dukedom", "Grand-duché");
        Add(text, "Royal Villa", "Villa royale");
        Add(text, "Ruk, Tribal Chieftain", "Ruk, chef de tribu");
        Add(text, "Fionne, Village Protector", "Fionne, protectrice du village");
        Add(text, "Sir Ralphie, Captain of the Guard", "Sir Ralphie, capitaine de la garde");
        Add(text, "Eris, City Priestess", "Eris, prêtresse de la cité");
        Add(text, "His Holiness Isaeh, Metropolis Pope", "Sa Sainteté Isaeh, pape de la métropole");
        Add(text, "Vivica, Dukedom Arch Mage", "Vivica, archimage du duché");
        Add(text, "Emperor Reginald P. Exford IV, Emperor's Palace", "Empereur Reginald P. Exford IV, palais impérial");
        Add(text, "Esora, Guardian to the Gates of Heaven?", "Esora, gardienne des portes du paradis ?");
        Add(text, "Purchases are disabled in the demo. Your earned progress will still carry into the full game.", "Les achats sont désactivés dans la démo. Votre progression acquise sera tout de même conservée dans le jeu complet.");
        Add(text, "A saved run is waiting. Continue that run or delete the temp save before changing your commander, squad, or shop buffs.", "Un run sauvegardé est en attente. Continuez ce run ou supprimez la sauvegarde temporaire avant de changer de commandant, d'escouade ou de bonus de boutique.");
        Add(text, "Deleting the current temp run will permanently erase that saved run. After deleting it, you will be able to change your commander, monsters, and access the shop again. Continue?", "Supprimer le run temporaire actuel effacera définitivement cette sauvegarde. Après suppression, vous pourrez à nouveau changer de commandant, de monstres et accéder à la boutique. Continuer ?");
        Add(text, "Thank you for playing the Tetrabeasts demo! You have cleared the final demo level. If you enjoyed your time with the game, please consider buying the full version.", "Merci d'avoir joué à la démo de Tetrabeasts ! Vous avez terminé le dernier niveau de la démo. Si le jeu vous a plu, pensez à acheter la version complète.");
        Add(text, "The Castle Has Fallen", "Le château est tombé");
        Add(text, "Conquest Failed", "Conquête échouée");
        Add(text, "Endless Survival This final battle cannot be won. The enemy has endless health, and the run continues until a loss condition is met. Survive as long as you can.", "Survie infinie Ce combat final ne peut pas être gagné. L'ennemi a une santé infinie et le run continue jusqu'à ce qu'une condition de défaite soit remplie. Survivez aussi longtemps que possible.");
        Add(text, "Do not show this message again", "Ne plus afficher ce message");
        Add(text, "Increase luck, improving favorable random outcomes during runs.", "Augmente la chance et ameliore les resultats aleatoires favorables pendant les runs.");
        Add(text, "Reduce the starting gravity speed of falling pieces.", "Reduit la vitesse de gravite initiale des pieces qui tombent.");
        Add(text, "Reduce how quickly gravity ramps up during a level.", "Reduit la vitesse a laquelle la gravite augmente pendant un niveau.");
        Add(text, "Increase the chance to earn gold from cleared rows.", "Augmente les chances de gagner de l'or en nettoyant des lignes.");
        Add(text, "Increase monster attack power.", "Augmente la puissance d'attaque des monstres.");
        Add(text, "Increase monster maximum HP.", "Augmente les PV maximum des monstres.");
        Add(text, "Increase monster healing power.", "Augmente la puissance de soin des monstres.");
        Add(text, "Increase starting unit reserves.", "Augmente les reserves d'unites de depart.");
        Add(text, "Now you are ready to start your first campaign. Press [F] to Continue", "Vous etes maintenant pret a lancer votre premiere campagne. Appuyez sur [F] pour continuer");
        Add(text, "You found a Death special block. Drop it in a column to destroy all units matching the first monster below it. (Press [F] to Continue)", "Vous avez trouve un bloc special Mort. Lachez-le dans une colonne pour detruire toutes les unites correspondant au premier monstre en dessous. (Appuyez sur [F] pour continuer)");
        Add(text, "You found a Bomb special block. Drop it to blast a 3x3 area and damage nearby obstacles. (Press [F] to Continue)", "Vous avez trouve un bloc special Bombe. Lachez-le pour exploser une zone de 3x3 et endommager les obstacles proches. (Appuyez sur [F] pour continuer)");
        Add(text, "You found a Bolt special block. Drop it to strike an entire column. (Press [F] to Continue)", "Vous avez trouve un bloc special Eclair. Lachez-le pour frapper toute une colonne. (Appuyez sur [F] pour continuer)");
        Add(text, "You found an Earthquake special block. Drop it to shake loose unsupported units across the board. (Press [F] to Continue)", "Vous avez trouve un bloc special Seisme. Lachez-le pour faire tomber les unites sans support sur le plateau. (Appuyez sur [F] pour continuer)");
        Add(text, "You found a Slow Gravity special block. Drop it to slow falling pieces for a short time. (Press [F] to Continue)", "Vous avez trouve un bloc special Gravite lente. Lachez-le pour ralentir brievement les pieces qui tombent. (Appuyez sur [F] pour continuer)");
        Add(text, "Quit without saving? Your current run will be lost and will not be available to continue later.", "Quitter sans sauvegarder ? Votre run actuel sera perdu et ne pourra pas être repris plus tard.");
        Add(text, "Restarting will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?", "Redémarrer comptera ce run comme une défaite. La sauvegarde temporaire actuelle sera effacée et ce run ne sera pas sauvegardé. Continuer ?");
        Add(text, "Returning to the main menu will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?", "Retourner au menu principal comptera ce run comme une défaite. La sauvegarde temporaire actuelle sera effacée et ce run ne sera pas sauvegardé. Continuer ?");
        Add(text, "Save this run and quit the game? Continuing later will resume from the start of the current level checkpoint. While a run is saved, you will not be able to change your commander, squad, or shop buffs from the title menu.", "Sauvegarder ce run et quitter le jeu ? En reprenant plus tard, vous repartirez du début du point de contrôle du niveau actuel. Tant qu'un run est sauvegardé, vous ne pourrez pas changer de commandant, d'escouade ou de bonus de boutique depuis le menu titre.");
        Add(text, "The run could not be temp-saved, so the game will stay open.", "Le run n'a pas pu être sauvegardé temporairement, le jeu restera donc ouvert.");
        Add(text, "After completing a level you will be allowed to choose one of three buffs that will empower your units. Press [F] to Continue", "Après avoir terminé un niveau, vous pourrez choisir l'un des trois bonus qui renforceront vos unités. Appuyez sur [F] pour continuer");
        Add(text, "Most buffs come in multiple rarities that will determine their strength. From weakest to strongest (White -> Green -> Blue -> Purple -> Orange). Press [F] to Continue", "La plupart des bonus existent en plusieurs raretés qui déterminent leur puissance. Du plus faible au plus fort (Blanc -> Vert -> Bleu -> Violet -> Orange). Appuyez sur [F] pour continuer");
        Add(text, "You will receive a single reroll per level that can be used on your buffs or debuffs. Rerolls can be saved throughout your run, but will be reset at the start of a new run. Press [F] to Continue", "Vous recevrez une relance par niveau, utilisable sur vos bonus ou malus. Les relances peuvent être conservées pendant votre run, mais seront réinitialisées au début d'un nouveau run. Appuyez sur [F] pour continuer");
        Add(text, "As you grow stronger your enemies will too. Choose one of three debuffs that will empower your enemies during this run. All buffs and debuffs will stay active and stack throughout the run, but will be reset upon starting a new game. Press [F] to Continue", "À mesure que vous devenez plus fort, vos ennemis aussi. Choisissez l'un des trois malus qui renforceront vos ennemis pendant ce run. Tous les bonus et malus restent actifs et se cumulent pendant le run, mais seront réinitialisés au démarrage d'une nouvelle partie. Appuyez sur [F] pour continuer");
        Add(text, "Not all levels are created equal, here you will let lady luck decide what the next battlefield will be like. Pull the lever to reveal it, use rerolls if you have them, then continue into the fight. (Press [F] to Continue)", "Tous les niveaux ne se valent pas. Ici, vous laisserez la chance décider à quoi ressemblera le prochain champ de bataille. Tirez le levier pour le révéler, utilisez des relances si vous en avez, puis poursuivez le combat. (Appuyez sur [F] pour continuer)");
        Add(text, "During a run, your monsters are temporary copies. This screen drains the EXP those copies earned so a portion can be preserved for their permanent versions. (Press [F] to Continue)", "Pendant un run, vos monstres sont des copies temporaires. Cet écran draine l'EXP gagnée par ces copies afin qu'une partie puisse être conservée pour leurs versions permanentes. (Appuyez sur [F] pour continuer)");
        Add(text, "Preserved EXP is added to your permanent monsters. Permanent levels make your units stronger at the start of future runs. (Press [F] to Continue)", "L'EXP conservée est ajoutée à vos monstres permanents. Les niveaux permanents rendent vos unités plus fortes au début des futurs runs. (Appuyez sur [F] pour continuer)");
        Add(text, "Welcome Overlord, my name is Lilith. I have done my best to rally the few monster I could find to assist in your conquest of the human domain. (Press [F] to Continue)", "Bienvenue, Suzerain. Je m'appelle Lilith. J'ai fait de mon mieux pour rallier les quelques monstres que j'ai trouvés afin de vous aider à conquérir le domaine humain. (Appuyez sur [F] pour continuer)");
        Add(text, "Before going into battle, we will first have to select our commander. Press the commander button in the bottom right to see who is available. (Press [F] to Continue)", "Avant d'aller au combat, nous devons d'abord choisir notre commandant. Appuyez sur le bouton de commandant en bas à droite pour voir qui est disponible. (Appuyez sur [F] pour continuer)");
        Add(text, "We only have one Commander that we can use at the moment, but with enough gold we can get a few more. Each commander has their own special ability. The details of each ability can be seen in the Commander preview on the right. (Press [F] to Continue)", "Nous n'avons qu'un seul commandant utilisable pour l'instant, mais avec assez d'or nous pourrons en obtenir d'autres. Chaque commandant possède sa propre capacité spéciale. Les détails sont visibles dans l'aperçu du commandant à droite. (Appuyez sur [F] pour continuer)");
        Add(text, "Since our commander is already selected lets confirm the selection at the bottom and head back to the main lobby.", "Puisque notre commandant est déjà sélectionné, confirmons la sélection en bas puis retournons au hall principal.");
        Add(text, "Now we will set up our monster squadron that will go into battle with our Commander. Press the select monster button in the bottom right.", "Nous allons maintenant préparer l'escouade de monstres qui ira au combat avec notre commandant. Appuyez sur le bouton de sélection des monstres en bas à droite.");
        Add(text, "As you can see we only have so many monsters to work with to creat our squadron. A Squadron must have at least two monsters and can have a maximum of 4. (Press [F] to Continue)", "Comme vous pouvez le voir, nous avons peu de monstres pour créer notre escouade. Une escouade doit avoir au moins deux monstres et peut en contenir au maximum 4. (Appuyez sur [F] pour continuer)");
        Add(text, "A war can hardly be fought with such pitiful numbers, but that's where I can be of assistance. (Press [F] to Continue)", "On ne mène guère une guerre avec des effectifs aussi pitoyables, mais c'est là que je peux aider. (Appuyez sur [F] pour continuer)");
        Add(text, "I can make clones of your squadron to be sent into battle. This will allow multiple copies to be made to fill out your ranks. However, this will come with some limitations. (Press [F] to Continue)", "Je peux créer des clones de votre escouade à envoyer au combat. Cela permet de produire plusieurs copies pour remplir vos rangs. Cependant, cela comporte certaines limites. (Appuyez sur [F] pour continuer)");
        Add(text, "I will only be able to make so many copies and if we lose too many in battle we will be forced to retreat. Second, the copies will gain their own experience, but I will only be able to safely convert a small fraction of that experience from the copy back to the original monster after a campaign is finished. (Press [F] to Continue)", "Je ne pourrai créer qu'un certain nombre de copies, et si nous en perdons trop au combat, nous serons forcés de battre en retraite. Ensuite, les copies gagneront leur propre expérience, mais je ne pourrai convertir en toute sécurité qu'une petite partie de cette expérience vers le monstre original à la fin d'une campagne. (Appuyez sur [F] pour continuer)");
        Add(text, "You can click on any monster and see a preview of it's current stats and level in the preview section on the right. Click on the arrow button in the preview section to swap between it's stats and it's passive ability description.", "Vous pouvez cliquer sur n'importe quel monstre pour voir un aperçu de ses stats et de son niveau dans la section de droite. Cliquez sur la flèche de l'aperçu pour alterner entre ses stats et la description de son passif.");
        Add(text, "More monsters can be unlocked with gold and cosmetic skins can also be purchased once you have gained some extra funds. Now, Prress the confirm button at the bottom of the screen to lock in your team.", "D'autres monstres peuvent être débloqués avec de l'or, et des apparences cosmétiques pourront aussi être achetées lorsque vous aurez des fonds supplémentaires. Maintenant, appuyez sur le bouton de confirmation en bas de l'écran pour valider votre équipe.");
        Add(text, "Now you are ready to start your first campaign. Press the Start button to begin a new run.", "Vous êtes maintenant prêt à lancer votre première campagne. Appuyez sur Commencer pour démarrer un nouveau run.");
        Add(text, "Welcome to the battlefield. We will begin by going over a few of the basic controls and battle mechanics. (Press [F] to Continue)", "Bienvenue sur le champ de bataille. Nous allons commencer par revoir quelques commandes et mécaniques de combat de base. (Appuyez sur [F] pour continuer)");
        Add(text, "First, try moving your piece to the left by pressing [A].", "D'abord, essayez de déplacer votre pièce vers la gauche avec [A].");
        Add(text, "Next, try moving your piece to the right by pressing [D].", "Ensuite, essayez de déplacer votre pièce vers la droite avec [D].");
        Add(text, "Next, try moving your piece down a single row by pressing [S].", "Ensuite, essayez de descendre votre pièce d'une ligne avec [S].");
        Add(text, "Now, try rotating your piece counter-clockwise by pressing [Q].", "Maintenant, essayez de faire pivoter votre pièce dans le sens antihoraire avec [Q].");
        Add(text, "Now, try rotating your piece clockwise by pressing [E].", "Maintenant, essayez de faire pivoter votre pièce dans le sens horaire avec [E].");
        Add(text, "Look at the bottom of the board and you will see four cells with a bright red tint in the same shape as your current piece, that isn't a coincidence. (Press [F] to Continue)", "Regardez le bas du plateau : vous verrez quatre cases teintées de rouge vif dans la même forme que votre pièce actuelle. Ce n'est pas une coïncidence. (Appuyez sur [F] pour continuer)");
        Add(text, "This is the landing indicator and will help you see exactly where your current piece will lock in place. A piece will lock in place automatically when it come in contact with an obstacle, another locked unit piece, or when it reaches the bottom of the board. (Press [F] to Continue)", "C'est l'indicateur d'atterrissage : il vous aide à voir exactement où votre pièce se verrouillera. Une pièce se verrouille automatiquement lorsqu'elle touche un obstacle, une autre pièce d'unité verrouillée ou le bas du plateau. (Appuyez sur [F] pour continuer)");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar].", "Ensuite, faites tomber instantanément votre pièce actuelle avec la [Barre d'espace].");
        Add(text, "Here you can keep track of level information such as any active level modifiers, the curent level number, how long you have been in the level, and the current gravity pulling your pieces down. (Press [F] to Continue)", "Ici, vous pouvez suivre les informations du niveau : modificateurs actifs, numéro du niveau, temps passé dans le niveau et gravité actuelle qui attire vos pièces vers le bas. (Appuyez sur [F] pour continuer)");
        Add(text, "Here you can see a preview of the next piece that will be spawned. Knowing what will come next will help you plan out your next step. (Press [F] to Continue)", "Ici, vous voyez un aperçu de la prochaine pièce. Savoir ce qui arrive vous aidera à planifier votre prochain coup. (Appuyez sur [F] pour continuer)");
        Add(text, "Reserve Units - Loss Condition When any of your units die your current reserve will be reduced. You only have so many soldiers and when your reserve hits 0 you will lose. (Press [F] to Continue)", "Unités de réserve - Condition de défaite Quand une de vos unités meurt, votre réserve actuelle diminue. Vous n'avez qu'un nombre limité de soldats, et si la réserve atteint 0, vous perdez. (Appuyez sur [F] pour continuer)");
        Add(text, "Loss Condition If a piece is locked in place above the top row of the board you will instantly lose, regardless of the number of reserve units left. Be caureful not to build to high. (Press [F] to Continue)", "Condition de défaite Si une pièce se verrouille au-dessus de la rangée supérieure du plateau, vous perdez instantanément, quel que soit le nombre d'unités en réserve. Faites attention à ne pas construire trop haut. (Appuyez sur [F] pour continuer)");
        Add(text, "Win Condition Reduce the Enemy Castle's HP to zero. (Press [F] to Continue)", "Condition de victoire Réduisez les PV du château ennemi à zéro. (Appuyez sur [F] pour continuer)");
        Add(text, "If you need a break press [Esc]. This will pause the game and bring up the pause menu.", "Si vous avez besoin d'une pause, appuyez sur [Échap]. Cela mettra le jeu en pause et ouvrira le menu de pause.");
        Add(text, "Here you can change settings, look through the help menu, check your current modifiers, or end your run early. For now, lets close the pause menu by pressing [Esc] again.", "Ici, vous pouvez modifier les paramètres, consulter l'aide, vérifier vos modificateurs actuels ou terminer votre run plus tôt. Pour l'instant, fermons le menu de pause en appuyant à nouveau sur [Échap].");
        Add(text, "Now lets try filling an entire row on the board next to launch an attack. (Press [F] to Continue)", "Essayons maintenant de remplir une ligne entière du plateau pour lancer une attaque. (Appuyez sur [F] pour continuer)");
        Add(text, "Charge!", "Chargez !");
        Add(text, "Send all units from the bottom 3 rows to attack immediatley, no need for fully formed lines. Damage based on the number of units cleared.", "Envoie immédiatement toutes les unités des 3 rangées du bas à l'attaque, sans lignes complètes. Les dégâts dépendent du nombre d'unités retirées.");
        Add(text, "Time Shift", "Décalage temporel");
        Add(text, "Reduce unit fall speed by 1/3 of its current speed fo 15 seconds.", "Réduit la vitesse de chute des unités d'un tiers pendant 15 secondes.");
        Add(text, "Natures Embrace", "Étreinte de la nature");
        Add(text, "Heal all units on the board back to full health including those that have died.", "Rend tous leurs PV aux unités sur le plateau, y compris celles qui sont mortes.");
        Add(text, "Grock SMASH!", "Grock ÉCRASE !");
        Add(text, "Let loose a war cry doubling all units on the boards health and attack damage for 10 seconds.", "Pousse un cri de guerre qui double les PV et les dégâts d'attaque de toutes les unités du plateau pendant 10 secondes.");
        Add(text, "Immutable Bulwark", "Rempart immuable");
        Add(text, "All units become immune to damage for 12 seconds.", "Toutes les unités deviennent immunisées aux dégâts pendant 12 secondes.");
        Add(text, "Boss Abilities", "Capacités de boss");
        Add(text, "Controls", "Commandes");
        Add(text, "Floor Effects", "Effets de sol");
        Add(text, "Game Mechanics", "Mécaniques de jeu");
        Add(text, "Obstacles", "Objets bloquants");
        Add(text, "Special Blocks", "Blocs spéciaux");
        Add(text, "Traps", "Pièges");
        Add(text, "Other", "Autre");
        Add(text, "Full Board Blast", "Explosion du plateau entier");
        Add(text, "The boss will target all monster units set on the board dealing a small amount of damage to each one.", "Le boss cible toutes les unités monstrueuses placées sur le plateau et inflige de faibles dégâts à chacune.");
        Add(text, "Increased Gravity", "Gravité accrue");
        Add(text, "The boss will temporarily increase gravity causing blocks to fall significantly faster for a set period of time.", "Le boss augmente temporairement la gravité, ce qui fait tomber les blocs bien plus vite pendant un certain temps.");
        Add(text, "Invulnerable", "Invulnérable");
        Add(text, "The boss will temporarily become invulnerable. While invulnerable, the boss will take no damage from any sources.", "Le boss devient temporairement invulnérable. Tant qu'il l'est, il ne subit aucun dégât, quelle qu'en soit la source.");
        Add(text, "Lightning Strike", "Frappe de foudre");
        Add(text, "The boss will target 1-3 individual cells with lightning bolts that will deal siginficant damage to any monster unit in that cell. Afterwards that cell will have a lightning floor effect that does continuous damage to any monster unit that occupies the tile.", "Le boss cible 1 à 3 cases avec des éclairs qui infligent d'importants dégâts à toute unité monstrueuse présente. Ensuite, ces cases reçoivent un effet de sol de foudre qui inflige des dégâts continus aux unités qui les occupent.");
        Add(text, "Magic Explosive", "Explosif magique");
        Add(text, "The boss will spawn a single magical explosive as low on the board as possible. This explosive will detonate after 15 seconds and can only be safley removed by clearing the row it occupies.", "Le boss fait apparaître un explosif magique aussi bas que possible sur le plateau. Il explose après 15 secondes et ne peut être retiré sans danger qu'en complétant la ligne qu'il occupe.");
        Add(text, "Magic Shield", "Bouclier magique");
        Add(text, "The boss will spawn multiple magical pylon obstacles on the board. As long as the pylons remain on the board the boss will take 50% reduced damage from all sources.", "Le boss fait apparaître plusieurs pylônes magiques sur le plateau. Tant qu'ils restent en place, le boss subit 50 % de dégâts en moins de toutes les sources.");
        Add(text, "Spawn FE's", "Générer des effets de sol");
        Add(text, "The boss will spawn multiple floor effects on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. Possible floor effect types are posion, fire, and lightning.", "Le boss génère plusieurs effets de sol sur le plateau. Ils peuvent être dispersés individuellement ou apparaître en motifs 2x2, 1x4 ou 4x1. Les types possibles sont poison, feu et foudre.");
        Add(text, "Spawn Obstacles", "Générer des obstacles");
        Add(text, "The boss will spawn multiple obstacles on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns stone obstacles.", "Le boss génère plusieurs obstacles sur le plateau. Ils peuvent être dispersés individuellement ou apparaître en motifs 2x2, 1x4 ou 4x1. Cette capacité ne crée que des obstacles de pierre.");
        Add(text, "Spawn Traps", "Générer des pièges");
        Add(text, "The boss will spawn multiple traps on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns spike traps.", "Le boss génère plusieurs pièges sur le plateau. Ils peuvent être dispersés individuellement ou apparaître en motifs 2x2, 1x4 ou 4x1. Cette capacité ne crée que des pièges à pointes.");
        Add(text, "Row Blast", "Explosion de rangées");
        Add(text, "The boss will target the top three rows the player has set monster units. All monster units in the selected rows will recieve moderate damage from this attack.", "Le boss cible les trois rangées les plus hautes où le joueur a placé des unités monstrueuses. Toutes les unités des rangées sélectionnées subissent des dégâts modérés.");
        Add(text, "Activate Special", "Activer le spécial");
        Add(text, "R - Activates the players special ability when the special gauge is charged to 100%", "R - Active la capacité spéciale du joueur quand la jauge spéciale est chargée à 100 %");
        Add(text, "Movement", "Déplacement");
        Add(text, "A - shifts the active piece one column to the left. S - shifts the active piece one row down. D - shifts the active piece one column to the right.", "A - déplace la pièce active d'une colonne vers la gauche. S - la descend d'une ligne. D - la déplace d'une colonne vers la droite.");
        Add(text, "Pause", "Pause");
        Add(text, "Escape - Will open the pause menu and pause all gameplay functions. Presseing escape while the pause menu is open will close the pause menu and resume gameplay.", "Échap - ouvre le menu de pause et met toutes les fonctions de jeu en pause. Appuyer sur Échap lorsque le menu est ouvert le ferme et reprend la partie.");
        Add(text, "Quick Drop", "Chute rapide");
        Add(text, "Pressing spacebar will quick drop your active piece, setting it in place immediatley.", "Appuyer sur Espace fait tomber rapidement la pièce active et la place immédiatement.");
        Add(text, "Rotation", "Rotation");
        Add(text, "Q - Rotate the active piece 90 degrees counter-clockwise. E - Rotate the active piece 90 degrees clockwise.", "Q - fait pivoter la pièce active de 90 degrés dans le sens antihoraire. E - la fait pivoter de 90 degrés dans le sens horaire.");
        Add(text, "Fire", "Feu");
        Add(text, "Any monster unit set in a cell with the fire floor effect will take constant damage. The damage is realtivley low but occurs often.", "Toute unité monstrueuse placée sur une case avec un effet de sol de feu subit des dégâts constants. Ils sont relativement faibles, mais fréquents.");
        Add(text, "Lightning", "Foudre");
        Add(text, "Any monster unit set in a cell with the lightning floor effect will take constant damage. The damage is moderate but the floor effect will disappears after a period of time.", "Toute unité monstrueuse placée sur une case avec un effet de sol de foudre subit des dégâts constants. Les dégâts sont modérés, mais l'effet disparaît après un certain temps.");
        Add(text, "Poison", "Poison");
        Add(text, "Any monster unit set in a cell with the poison floor effect will take continuous damage. The damage is realtivley low but occurs often.", "Toute unité monstrueuse placée sur une case avec un effet de sol de poison subit des dégâts continus. Ils sont relativement faibles, mais fréquents.");
        Add(text, "Attack Units", "Unités d'attaque");
        Add(text, "Attack units tend to have a higher attack stat than other unit types. They are best used to deal maximum damage to enemies to end levels more quickly. They cannot heal and tend to have average health stats.", "Les unités d'attaque ont souvent une attaque plus élevée que les autres types. Elles servent à infliger un maximum de dégâts et finir les niveaux plus vite. Elles ne peuvent pas soigner et ont généralement une santé moyenne.");
        Add(text, "Currency", "Monnaie");
        Add(text, "The player can gain currency from completing levels and rarely from clearing rows. Currency can be used to purchase various cosmetics, monster units, player characters, and permanent buffs to improve future runs. Your current currency can be found in the top right of the screen.", "Le joueur peut gagner de la monnaie en terminant des niveaux et, plus rarement, en complétant des lignes. Elle sert à acheter des cosmétiques, des unités monstrueuses, des personnages et des bonus permanents pour améliorer les prochains runs. Votre monnaie actuelle se trouve en haut à droite de l'écran.");
        Add(text, "Enemy Attack", "Attaque ennemie");
        Add(text, "The enemy castle will send constant attacks at the players monster units in an attempt to cull them. The enemies attacks will increase in power and frequency at higher levels.", "Le château ennemi attaque constamment les unités monstrueuses du joueur pour les éliminer. Les attaques ennemies gagnent en puissance et en fréquence aux niveaux supérieurs.");
        Add(text, "Healing Units", "Unités de soin");
        Add(text, "Some units can heal other monster units to varying degrees. They tend to have much lower health and attack stats.", "Certaines unités peuvent soigner d'autres monstres à différents degrés. Elles ont généralement bien moins de santé et d'attaque.");
        Add(text, "Landing Hint", "Indicateur d'atterrissage");
        Add(text, "A red tint overlay appears in the location where the current active piece will fall.", "Une superposition rouge apparaît à l'endroit où la pièce active tombera.");
        Add(text, "Loss Condition 1", "Condition de défaite 1");
        Add(text, "If the unit reserve reaches 0, the run will end with your loss! The unit reserve is reduced by one for every monster unit that dies on the board.", "Si la réserve d'unités atteint 0, le run se termine par une défaite ! La réserve diminue d'une unité pour chaque monstre qui meurt sur le plateau.");
        Add(text, "Loss Condition 2", "Condition de défaite 2");
        Add(text, "If a piece is set above the top row of the grid, the run will end with a loss!", "Si une pièce est placée au-dessus de la rangée supérieure de la grille, le run se termine par une défaite !");
        Add(text, "Monster Units", "Unités monstrueuses");
        Add(text, "Choose from multiple units to make up your warband. Each unit has their own individual stats that will make them more suitable for different roles. Some units have balanced stats and others are more specialized.", "Choisissez parmi plusieurs unités pour composer votre bande de guerre. Chaque unité a ses propres stats, qui la rendent plus adaptée à certains rôles. Certaines sont équilibrées, d'autres plus spécialisées.");
        Add(text, "Commander", "Commandant");
        Add(text, "Each Commander has their own unquie special ability that can be used in battle. New Commanders can be unlocked and set from the main menu.", "Chaque commandant possède une capacité spéciale unique utilisable au combat. De nouveaux commandants peuvent être débloqués et choisis depuis le menu principal.");
        Add(text, "Row Clear", "Ligne complétée");
        Add(text, "Fill each grid cell in a row to launch an attack. Monster Units and Obstacles count as a filled cell. Floor effects and traps do not count as a filled cell. Monster units in the cleared row contribute their attack stats for damage calculation and partially fill the player's special gauge.", "Remplissez chaque case d'une rangée pour lancer une attaque. Les unités monstrueuses et les obstacles comptent comme des cases remplies. Les effets de sol et les pièges ne comptent pas. Les unités de la rangée complétée ajoutent leur attaque au calcul des dégâts et remplissent partiellement la jauge spéciale du joueur.");
        Add(text, "Run Buffs", "Bonus de run");
        Add(text, "After succesfully completing a level you will be given three random buffs to choose from to enhance your current run. All buff modifiers will be reset when the run ends.", "Après avoir terminé un niveau, vous recevrez trois bonus aléatoires parmi lesquels choisir pour renforcer votre run actuel. Tous les bonus seront réinitialisés à la fin du run.");
        Add(text, "Run Debuffs", "Malus de run");
        Add(text, "After succesfully completing a level you will be given three random debuffs to choose from to increase the difficulty of your current run. All debuff modifiers will be reset when the run ends.", "Après avoir terminé un niveau, vous recevrez trois malus aléatoires parmi lesquels choisir pour augmenter la difficulté du run actuel. Tous les malus seront réinitialisés à la fin du run.");
        Add(text, "Clearing rows will earn the player points and add to their overall score. This can be used to measure the success of a run and compete with other players.", "Compléter des lignes rapporte des points et augmente le score global. Cela sert à mesurer la réussite d'un run et à rivaliser avec les autres joueurs.");
        Add(text, "Shop Buffs", "Bonus de boutique");
        Add(text, "The shop offers different buffs that can be purchased. Each buff is permanent and can be purchased multiple times. Each purchase will increase the price of the buffs next purchase level.", "La boutique propose différents bonus à acheter. Chaque bonus est permanent et peut être acheté plusieurs fois. Chaque achat augmente le prix du prochain niveau de ce bonus.");
        Add(text, "Special Gauge", "Jauge spéciale");
        Add(text, "Fills with every row cleared. When it reaches 100% you can use your Commander's unique special ability, resetting the special gauge back 0%.", "Se remplit à chaque ligne complétée. À 100 %, vous pouvez utiliser la capacité spéciale unique de votre commandant, ce qui ramène la jauge à 0 %.");
        Add(text, "Tank Units", "Unités tank");
        Add(text, "Tank units tend to have significantly more health than other unit types. They are best used to outlast enemy attacks and protect weaker units. They cannot heal and tend to have low attack stats.", "Les unités tank ont généralement bien plus de santé que les autres types. Elles servent à encaisser les attaques ennemies et à protéger les unités plus faibles. Elles ne peuvent pas soigner et ont souvent une attaque faible.");
        Add(text, "Unit Death", "Mort d'unité");
        Add(text, "When a monster units health drops to zero it dies. Dead units do not contribute their stats to an attack when their row is cleared or help fill up the players Special Gauge.", "Quand la santé d'une unité monstrueuse tombe à zéro, elle meurt. Les unités mortes ne contribuent pas à l'attaque quand leur rangée est complétée et ne remplissent pas la jauge spéciale du joueur.");
        Add(text, "Unit Reserve", "Réserve d'unités");
        Add(text, "When starting a new run the player will have a set limit of how many units they can afford to lose throughout the run. When a unit dies the reserve bar will be decreased. Suuccesfully completing a level will award the player up to 5 reinforcements for each victory up to the max unit reserve.", "Au début d'un nouveau run, le joueur a une limite d'unités qu'il peut perdre. Quand une unité meurt, la barre de réserve diminue. Terminer un niveau avec succès octroie jusqu'à 5 renforts par victoire, jusqu'à la réserve maximale.");
        Add(text, "Victory Condition", "Condition de victoire");
        Add(text, "Reduce the health of the enemy castle by clearing rows. When the enemy castle reaches 0 health, you win!", "Réduisez la santé du château ennemi en complétant des lignes. Quand le château ennemi atteint 0 santé, vous gagnez !");
        Add(text, "Explosive", "Explosif");
        Add(text, "The explosive obstacle will explode after a period of time killing all surrounding monster units. Can be safley disposed of by clearing its row. When safley disposed of it add 25 damage to that row clears attack. CAUTION: Using a bomb or lightning special block on the explosive will cause it to detonate.", "L'obstacle explosif explose après un certain temps et tue toutes les unités monstrueuses alentour. Il peut être éliminé sans danger en complétant sa rangée. S'il est éliminé ainsi, il ajoute 25 dégâts à l'attaque de cette ligne. ATTENTION : utiliser une bombe ou un bloc spécial foudre sur l'explosif le fera détoner.");
        Add(text, "Magic Pylon", "Pylône magique");
        Add(text, "When magic pylon obstacles are on the board enemies will take 50% reduced damage from all sources. Magic pylon can bee destroyed by clearing the row they occupy or using bomb and lightning special blocks.", "Quand des pylônes magiques sont sur le plateau, les ennemis subissent 50 % de dégâts en moins de toutes les sources. Les pylônes peuvent être détruits en complétant leur rangée ou avec des blocs spéciaux bombe et foudre.");
        Add(text, "Stone", "Pierre");
        Add(text, "Stone obstacles can be spawned at the beggining of a level or by the boss. A lightning special block or clearing a row containing a stone obstacle will deal one damage to it. Stone obstacle need to be damaged 3 times to be removed. Exception: Using a bomb special block will instantly destroy a stone obstacle.", "Les obstacles de pierre peuvent apparaître au début d'un niveau ou via le boss. Un bloc spécial foudre ou une ligne complétée contenant un obstacle de pierre lui inflige 1 dégât. Un obstacle de pierre doit subir 3 dégâts pour être retiré. Exception : un bloc spécial bombe le détruit instantanément.");
        Add(text, "Bomb", "Bombe");
        Add(text, "The bomb special will detonate immediatley when set. All blocks in the surrounding tiles of its blast will be destroyed!", "Le spécial Bombe explose immédiatement lorsqu'il est placé. Tous les blocs dans les cases autour de l'explosion sont détruits !");
        Add(text, "Death", "Mort");
        Add(text, "The Death special will activate immediatley when set on top of a monster unit. All monster units of the same type will be safley removed from the board with out decreasing your unit reserve.", "Le spécial Mort s'active immédiatement lorsqu'il est placé sur une unité monstrueuse. Toutes les unités du même type sont retirées du plateau sans danger et sans réduire votre réserve.");
        Add(text, "Earthquake", "Séisme");
        Add(text, "The Earthquake special will activate immediatley when set. All blocks on the board will be dropped if not being supported by another tile beneath them. This effects obstacles as well that may otherwise not be able to be moved by other means.", "Le spécial Séisme s'active immédiatement lorsqu'il est placé. Tous les blocs du plateau tombent s'ils ne sont pas soutenus par une case en dessous. Cela affecte aussi les obstacles qui ne pourraient pas être déplacés autrement.");
        Add(text, "Lightning Bolt", "Éclair");
        Add(text, "The Lightning Bolt special will activate immediatley when set. All monster units and traps will be destroyed in that column. Stone obstacles will take partial damage if they are in the affected area.", "Le spécial Éclair s'active immédiatement lorsqu'il est placé. Toutes les unités monstrueuses et les pièges de cette colonne sont détruits. Les obstacles de pierre subissent des dégâts partiels s'ils se trouvent dans la zone.");
        Add(text, "Slow Gravity", "Gravité ralentie");
        Add(text, "The Slow Gravity special block will activate immedialtey upon being set. It will significantly reduce the speed at which pieces fall and the how quickly gravity increases over time.", "Le bloc spécial Gravité ralentie s'active immédiatement lorsqu'il est placé. Il réduit fortement la vitesse de chute des pièces et la vitesse à laquelle la gravité augmente avec le temps.");
        Add(text, "Spike Trap", "Piège à pointes");
        Add(text, "Spike traps will deal a high amount of damage to any monster unit that is set on the trapped cell when they are placed. The only way to destroyed spike traps are with the lightning special block.", "Les pièges à pointes infligent de gros dégâts à toute unité monstrueuse placée sur leur case. Le seul moyen de les détruire est le bloc spécial foudre.");
        Add(text, "Spin to Win", "Tourner pour gagner");
        Add(text, "Active pieces will continuously rotate until set in place. Manual rotation will be locked.", "Les pièces actives tournent continuellement jusqu'à leur placement. La rotation manuelle est verrouillée.");
        Add(text, "Timing is Everything", "Le timing fait tout");
        Add(text, "Active pieces will continuously shift horizontally back and and forth across the board. Manual shifting will be locked.", "Les pièces actives se déplacent continuellement horizontalement d'avant en arrière sur le plateau. Le déplacement manuel est verrouillé.");
        Add(text, "Go Big or Go Home", "Tout ou rien");
        Add(text, "Damage is significantly reduced for all attacks when the combo streak is less than 3.", "Les dégâts de toutes les attaques sont fortement réduits lorsque la série de combo est inférieure à 3.");
        Add(text, "Break Out The Big Guns", "Sortez l'artillerie lourde");
        Add(text, "The enemy has fortified their position. A combo attack at 4 or higher will be required to remove each enemy shield. Damage dealt to the enemy while shielded is significantly reduced.", "L'ennemi a fortifié sa position. Une attaque de combo de 4 ou plus est nécessaire pour retirer chaque bouclier ennemi. Les dégâts infligés à l'ennemi protégé sont fortement réduits.");
        Add(text, "Contagion Outbreak", "Épidémie de contagion");
        Add(text, "Disease has begun to spread through the ranks. Close proximitiy has a chance to transfer from afflicted units to healthy units and will spread with certainity on the death of infected units.", "La maladie commence à se propager dans les rangs. La proximité peut la transmettre des unités atteintes aux unités saines, et elle se propage à coup sûr à la mort des unités infectées.");
        Add(text, "Double Down", "Quitte ou double");
        Add(text, "All damage taken and dealt will be doubled!", "Tous les dégâts subis et infligés sont doublés !");
        Add(text, "Exploding Corpses", "Cadavres explosifs");
        Add(text, "Units will explode on death dealing damage to all surrounding units. Damage dealt is a percentage based off of the max health of the exploding unit.", "Les unités explosent à leur mort et infligent des dégâts aux unités alentour. Les dégâts correspondent à un pourcentage des PV max de l'unité qui explose.");
        Add(text, "Tis A Flesh Wound", "Ce n'est qu'une blessure superficielle");
        Add(text, "All ally units will start with half health.", "Toutes les unités alliées commencent avec la moitié de leur santé.");
        Add(text, "Rations Running Low", "Rations presque épuisées");
        Add(text, "Rations have begun to run out. Ally units have begun to starve trying to share the remaining rations. Units will take continuous damage proportional to the number of current reserve units.", "Les rations commencent à manquer. Les unités alliées souffrent de la faim en essayant de partager ce qui reste. Elles subissent des dégâts continus proportionnels au nombre d'unités de réserve actuelles.");
        Add(text, "Overgrowth", "Prolifération");
        Add(text, "Overgrowth has taken over the area consuming tiles and monsters. Overgrowth becomes more resilent to destruction once fully grown. Defeat the enemy before your army becomes mulch!", "La prolifération envahit la zone en consommant cases et monstres. Une fois mature, elle devient plus résistante à la destruction. Battez l'ennemi avant que votre armée ne devienne du compost !");
        Add(text, "No Retreat", "Aucune retraite");
        Add(text, "Enemy ambush will cut off any retreat. Rows will slowly fill with enemy units progressivley limiting space to maneuver.", "Une embuscade ennemie coupe toute retraite. Les rangées se remplissent lentement d'unités ennemies et limitent progressivement l'espace de manœuvre.");
        Add(text, "Soul Link", "Lien d'âme");
        Add(text, "All four units in a piece share a single health pool.", "Les quatre unités d'une pièce partagent une seule réserve de santé.");
        Add(text, "Back to the Basics", "Retour aux bases");
        Add(text, "Special blocks will not spawn.", "Les blocs spéciaux n'apparaîtront pas.");
        Add(text, "Commander Special Lock", "Spécial du commandant verrouillé");
        Add(text, "Special ability gauge will be set to zero and locked.", "La jauge de capacité spéciale est remise à zéro et verrouillée.");
        Add(text, "Catastrophic Storm", "Tempête catastrophique");
        Add(text, "An unrelenting storm has arrived and will blast the area with devestating lightning strikes.", "Une tempête implacable est arrivée et frappe la zone d'éclairs dévastateurs.");
        Add(text, "Miasma Marsh", "Marais de miasmes");
        Add(text, "The battlefield has shifted to the nearby marshes where deadly miasma drifts across the terrain.", "Le champ de bataille s'est déplacé vers les marais voisins, où des miasmes mortels flottent sur le terrain.");
        Add(text, "All Special Gain Down", "Gain spécial global réduit");
        Add(text, "All Special Gain Up", "Gain spécial global augmenté");
        Add(text, "ATK Down", "ATQ réduite");
        Add(text, "ATK Up", "ATQ augmentée");
        Add(text, "Currency Drop Up", "Butin d'or augmenté");
        Add(text, "Debuffs Only", "Malus seulement");
        Add(text, "Enemy ATK Down", "ATQ ennemie réduite");
        Add(text, "Enemy ATK SPD Down", "Vitesse d'attaque ennemie réduite");
        Add(text, "Enemy ATK SPD Up", "Vitesse d'attaque ennemie augmentée");
        Add(text, "Enemy ATK Up", "ATQ ennemie augmentée");
        Add(text, "Enemy HP Up", "PV ennemis augmentés");
        Add(text, "Gravity Accel SPD Down", "Accélération de gravité réduite");
        Add(text, "Gravity Accel SPD Up", "Accélération de gravité augmentée");
        Add(text, "Gravity Base SPD Down", "Vitesse de gravité de base réduite");
        Add(text, "Gravity SPD Up", "Vitesse de gravité augmentée");
        Add(text, "Healing Range Up", "Portée de soin augmentée");
        Add(text, "Healing STR Up", "Puissance de soin augmentée");
        Add(text, "HP Down", "PV réduits");
        Add(text, "HP Up", "PV augmentés");
        Add(text, "Luck Up", "Chance augmentée");
        Add(text, "Misfortune Up", "Infortune augmentée");
        Add(text, "No Landing Indicator", "Pas d'indicateur d'atterrissage");
        Add(text, "No Next Block Preview", "Pas d'aperçu du prochain bloc");
        Add(text, "No Reinforcements", "Pas de renforts");
        Add(text, "Reinforcements Down", "Renforts réduits");
        Add(text, "Reinforcements Up", "Renforts augmentés");
        Add(text, "Special Block Down", "Blocs spéciaux réduits");
        Add(text, "Special Block Up", "Blocs spéciaux augmentés");
        Add(text, "Special Gain Stat Down", "Stat de gain spécial réduite");
        Add(text, "Special Gauge Stat Up", "Stat de jauge spéciale augmentée");
        Add(text, "Stone Buff Drop Down", "Bonus des pierres réduits");
        Add(text, "Stone Buff Drop Up", "Bonus des pierres augmentés");
        Add(text, "Unit Reserve Down", "Réserve d'unités réduite");
        Add(text, "Unit Reserve Up", "Réserve d'unités augmentée");
        Add(text, "Win Currency Down", "Or de victoire réduit");
        Add(text, "Win Currency Up", "Or de victoire augmenté");
        Add(text, "A red tinted outline will no longer be shown where your pieces will land.", "Le contour rouge indiquant où vos pièces atterriront n'est plus affiché.");
        Add(text, "The next block will no longer be shown.", "Le prochain bloc n'est plus affiché.");
        Add(text, "Reinforcements will no longer arrive after winning a round.", "Les renforts n'arrivent plus après une victoire.");
        Add(text, "Stone obstacles no longer have a chance of dropping buffs and now only drop debuffs. Debuff drop chance is the same as prior buff drop chance.", "Les obstacles de pierre ne peuvent plus lâcher de bonus et ne lâchent désormais que des malus. La chance de malus est la même que l'ancienne chance de bonus.");
        Add(text, "Double the amount of currency gained occasionally when clearing lines.", "Double parfois la quantité d'or gagnée en complétant des lignes.");
        Add(text, "Triple the amount of currency gained occasionally when clearing lines.", "Triple parfois la quantité d'or gagnée en complétant des lignes.");
        Add(text, "Qunituple the amount of currency gained occasionally when clearing lines.", "Quintuple parfois la quantité d'or gagnée en complétant des lignes.");
        Add(text, "Increases the healing range of all friendly monsters by 1.", "Augmente de 1 la portée de soin de tous les monstres alliés.");
        Add(text, "Increases the healing range of all friendly monsters by 2.", "Augmente de 2 la portée de soin de tous les monstres alliés.");
        Add(text, "Increases the healing range of all friendly monsters by 3.", "Augmente de 3 la portée de soin de tous les monstres alliés.");
        Add(text, "Lines Cleared:", "Lignes complétées :");
        Add(text, "Special Used:", "Spéciaux utilisés :");
        Add(text, "Obstacles Destroyed:", "Obstacles détruits :");
        Add(text, "Highest Combo:", "Combo le plus élevé :");
        Add(text, "Highest Single Attack:", "Meilleure attaque unique :");
        Add(text, "Units Died:", "Unités mortes :");
        Add(text, "Units Healed:", "Unités soignées :");
        Add(text, "Total Damage Dealt:", "Dégâts totaux infligés :");
        Add(text, "Clear Time:", "Temps de réussite :");
        Add(text, "Final Score:", "Score final :");
        Add(text, "Lines", "Lignes");
        Add(text, "Times", "Fois");
        Add(text, "Obstacles", "Objets bloquants");
        Add(text, "Damage", "Dégâts");
        Add(text, "Units", "Unités");
        Add(text, "Health", "Santé");
        Add(text, "Level {0}", "Niveau {0}");
        Add(text, "{0} of {1} {2} discovered ({3}%)", "{0} sur {1} {2} découverts ({3} %)");
        Add(text, "Buffs", "Bonus");
        Add(text, "Debuffs", "Malus");
        Add(text, "Level Modifiers", "Modificateurs de niveau");

        return text;
    }

    static bool TryTranslateRunModifierDescription(string lookupKey, out string frenchText)
    {
        frenchText = null;

        for (int i = 0; i < DegreePrefixes.Length; i++)
        {
            string englishPrefix = DegreePrefixes[i].English;
            if (!lookupKey.StartsWith(englishPrefix + " ", StringComparison.OrdinalIgnoreCase))
                continue;

            string remainder = lookupKey.Substring(englishPrefix.Length + 1).Trim();
            if (TryGetRunModifierTemplate(remainder, out string template))
            {
                frenchText = string.Format(template, DegreePrefixes[i].French);
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

        if (!string.Equals(normalized, remainder, StringComparison.OrdinalIgnoreCase) &&
            RunModifierTemplates.TryGetValue(normalized, out template))
            return true;

        return !normalized.EndsWith(".", StringComparison.Ordinal) &&
            RunModifierTemplates.TryGetValue(normalized + ".", out template);
    }

    static bool TryTranslateLabelValueLines(string englishText, out string frenchText)
    {
        frenchText = null;

        string[] lines = englishText.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0)
            return false;

        bool changed = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (TryTranslateColonLine(line, out string colonLine))
            {
                lines[i] = colonLine;
                changed = true;
                continue;
            }

            if (TryTranslateLinePrefix(line, out string prefixLine))
            {
                lines[i] = prefixLine;
                changed = true;
            }
        }

        if (!changed)
            return false;

        frenchText = string.Join("\n", lines);
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

        translatedLine = $"{translatedLabel} : {value}";
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

            translatedLine = leading + LinePrefixes[i].French + trimmed.Substring(englishPrefix.Length);
            return true;
        }

        return false;
    }

    static void Add(Dictionary<string, string> text, string english, string french)
    {
        string key = NormalizeLookupKey(english);
        if (!string.IsNullOrEmpty(key))
            text[key] = french;
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

using System;
using System.Collections.Generic;
using System.Text;

public static class TetrabeastsSpanishTranslations
{
    static readonly Dictionary<string, string> ExactText = BuildExactText();

    static readonly Dictionary<string, string> RunModifierTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["decrease the amount of special gained from all sources."] = "Reduce {0} la cantidad de especial obtenida de todas las fuentes.",
        ["decreases the amount of special gained from all sources."] = "Reduce {0} la cantidad de especial obtenida de todas las fuentes.",
        ["increase the amount of special gained from all sources."] = "Aumenta {0} la cantidad de especial obtenida de todas las fuentes.",
        ["increases the amount of special gained from all sources."] = "Aumenta {0} la cantidad de especial obtenida de todas las fuentes.",
        ["decrease the special gained from each monster."] = "Reduce {0} el especial obtenido de cada monstruo.",
        ["decreases the special gained from each monster."] = "Reduce {0} el especial obtenido de cada monstruo.",
        ["increase the special gained from each monster."] = "Aumenta {0} el especial obtenido de cada monstruo.",
        ["increases the special gained from each monster."] = "Aumenta {0} el especial obtenido de cada monstruo.",
        ["decreases the attack value for all monsters in your current roster."] = "Reduce {0} el ataque de todos los monstruos de tu escuadrón actual.",
        ["decrease the attack value for all monsters in your current roster."] = "Reduce {0} el ataque de todos los monstruos de tu escuadrón actual.",
        ["increases the attack value for all monsters in your current roster."] = "Aumenta {0} el ataque de todos los monstruos de tu escuadrón actual.",
        ["increase the attack value for all monsters in your current roster."] = "Aumenta {0} el ataque de todos los monstruos de tu escuadrón actual.",
        ["decrease the damage of enemy projectiles."] = "Reduce {0} el daño de los proyectiles enemigos.",
        ["decreases the damage of enemy projectiles."] = "Reduce {0} el daño de los proyectiles enemigos.",
        ["incecrease the damage of enemy projectiles."] = "Aumenta {0} el daño de los proyectiles enemigos.",
        ["incecreases the damage of enemy projectiles."] = "Aumenta {0} el daño de los proyectiles enemigos.",
        ["increase the damage of enemy projectiles."] = "Aumenta {0} el daño de los proyectiles enemigos.",
        ["increases the damage of enemy projectiles."] = "Aumenta {0} el daño de los proyectiles enemigos.",
        ["increases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "Aumenta {0} el tiempo entre ataques enemigos (no incluye los tiempos de recarga de jefes).",
        ["increase the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "Aumenta {0} el tiempo entre ataques enemigos (no incluye los tiempos de recarga de jefes).",
        ["decreases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "Reduce {0} el tiempo entre ataques enemigos (no incluye los tiempos de recarga de jefes).",
        ["decrease the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "Reduce {0} el tiempo entre ataques enemigos (no incluye los tiempos de recarga de jefes).",
        ["increase the hit points of all future enemy fortifications."] = "Aumenta {0} los puntos de vida de todas las fortificaciones enemigas futuras.",
        ["increases the hit points of all future enemy fortifications."] = "Aumenta {0} los puntos de vida de todas las fortificaciones enemigas futuras.",
        ["decreases the rate falling speed builds up over time for blocks."] = "Reduce {0} la velocidad a la que aumenta la caída de los bloques con el tiempo.",
        ["decrease the rate falling speed builds up over time for blocks."] = "Reduce {0} la velocidad a la que aumenta la caída de los bloques con el tiempo.",
        ["increases the rate falling speed builds up over time for blocks."] = "Aumenta {0} la velocidad a la que aumenta la caída de los bloques con el tiempo.",
        ["increase the rate falling speed builds up over time for blocks."] = "Aumenta {0} la velocidad a la que aumenta la caída de los bloques con el tiempo.",
        ["decreases the initial falling speed of blocks."] = "Reduce {0} la velocidad inicial de caída de los bloques.",
        ["decrease the initial falling speed of blocks."] = "Reduce {0} la velocidad inicial de caída de los bloques.",
        ["increases the initial falling speed of blocks."] = "Aumenta {0} la velocidad inicial de caída de los bloques.",
        ["increase the initial falling speed of blocks."] = "Aumenta {0} la velocidad inicial de caída de los bloques.",
        ["increase the healing power of all friendly monsters."] = "Aumenta {0} el poder de curación de todos los monstruos aliados.",
        ["increases the healing power of all friendly monsters."] = "Aumenta {0} el poder de curación de todos los monstruos aliados.",
        ["decrease friendly monster pieces maximum hit points."] = "Reduce {0} los puntos de vida máximos de las piezas de monstruos aliados.",
        ["decreases friendly monster pieces maximum hit points."] = "Reduce {0} los puntos de vida máximos de las piezas de monstruos aliados.",
        ["increase friendly monster pieces maximum hit points."] = "Aumenta {0} los puntos de vida máximos de las piezas de monstruos aliados.",
        ["increases friendly monster pieces maximum hit points."] = "Aumenta {0} los puntos de vida máximos de las piezas de monstruos aliados.",
        ["increase luck raising the chance of getting higher rarity buffs."] = "Aumenta {0} la suerte, elevando la probabilidad de obtener mejoras de mayor rareza.",
        ["increases luck raising the chance of getting higher rarity buffs."] = "Aumenta {0} la suerte, elevando la probabilidad de obtener mejoras de mayor rareza.",
        ["increases the likelihood of finding higher rarity debuffs."] = "Aumenta {0} la probabilidad de encontrar desventajas de mayor rareza.",
        ["increase the likelihood of finding higher rarity debuffs."] = "Aumenta {0} la probabilidad de encontrar desventajas de mayor rareza.",
        ["decrease the number of reinforcement units added after winning a round."] = "Reduce {0} la cantidad de refuerzos añadidos después de ganar una ronda.",
        ["decreases the number of reinforcement units added after winning a round."] = "Reduce {0} la cantidad de refuerzos añadidos después de ganar una ronda.",
        ["increase the number of reinforcement units added after winning a round."] = "Aumenta {0} la cantidad de refuerzos añadidos después de ganar una ronda.",
        ["increases the number of reinforcement units added after winning a round."] = "Aumenta {0} la cantidad de refuerzos añadidos después de ganar una ronda.",
        ["decreases the chance of special blocks appearing."] = "Reduce {0} la probabilidad de que aparezcan bloques especiales.",
        ["decrease the chance of special blocks appearing."] = "Reduce {0} la probabilidad de que aparezcan bloques especiales.",
        ["incecreases the chance of special blocks appearing."] = "Aumenta {0} la probabilidad de que aparezcan bloques especiales.",
        ["incecrease the chance of special blocks appearing."] = "Aumenta {0} la probabilidad de que aparezcan bloques especiales.",
        ["increases the chance of special blocks appearing."] = "Aumenta {0} la probabilidad de que aparezcan bloques especiales.",
        ["increase the chance of special blocks appearing."] = "Aumenta {0} la probabilidad de que aparezcan bloques especiales.",
        ["decrease the chance a buff will drop from a stone obstacle."] = "Reduce {0} la probabilidad de que un obstáculo de piedra suelte una mejora.",
        ["decreases the chance a buff will drop from a stone obstacle."] = "Reduce {0} la probabilidad de que un obstáculo de piedra suelte una mejora.",
        ["increase the chance a buff will drop from a stone obstacle."] = "Aumenta {0} la probabilidad de que un obstáculo de piedra suelte una mejora.",
        ["increases the chance a buff will drop from a stone obstacle."] = "Aumenta {0} la probabilidad de que un obstáculo de piedra suelte una mejora.",
        ["decrease the maximum limit of the unit reserve."] = "Reduce {0} el límite máximo de la reserva de unidades.",
        ["decreases the maximum limit of the unit reserve."] = "Reduce {0} el límite máximo de la reserva de unidades.",
        ["increase the maximum limit of the unit reserve."] = "Aumenta {0} el límite máximo de la reserva de unidades.",
        ["increases the maximum limit of the unit reserve."] = "Aumenta {0} el límite máximo de la reserva de unidades.",
        ["reduces the amount of currency the player gains after winning a round."] = "Reduce {0} la cantidad de oro que gana el jugador al ganar una ronda.",
        ["reduce the amount of currency the player gains after winning a round."] = "Reduce {0} la cantidad de oro que gana el jugador al ganar una ronda.",
        ["increases the amount of currency the player gains after winning a round."] = "Aumenta {0} la cantidad de oro que gana el jugador al ganar una ronda.",
        ["increase the amount of currency the player gains after winning a round."] = "Aumenta {0} la cantidad de oro que gana el jugador al ganar una ronda.",
        ["increase the chance currency will be earned when clearing lines."] = "Aumenta {0} la probabilidad de obtener oro al limpiar líneas.",
        ["increases the chance currency will be earned when clearing lines."] = "Aumenta {0} la probabilidad de obtener oro al limpiar líneas."
    };

    static readonly (string English, string Spanish)[] DegreePrefixes =
    {
        ("Slightly", "ligeramente"),
        ("Modestly", "un poco"),
        ("Moderatley", "moderadamente"),
        ("Moderately", "moderadamente"),
        ("Significantly", "significativamente"),
        ("Massivley", "enormemente"),
        ("Massively", "enormemente")
    };

    static readonly (string English, string Spanish)[] LinePrefixes =
    {
        ("Special Gauge Gain", "Ganancia del medidor especial"),
        ("Enemy Damage", "Da\u00f1o enemigo"),
        ("Enemy HP", "PV enemigo"),
        ("Score Gain", "Ganancia de puntuaci\u00f3n"),
        ("EXP Gain", "Ganancia de EXP"),
        ("Misfortune", "Infortunio"),
        ("Gravity", "Gravedad"),
        ("Score", "Puntuaci\u00f3n"),
        ("Level", "Nivel"),
        ("Reset", "Reinicio")
    };

    public static bool TryGetText(string englishText, out string spanishText)
    {
        spanishText = null;

        if (string.IsNullOrWhiteSpace(englishText))
            return false;

        string lookupKey = NormalizeLookupKey(englishText);
        if (ExactText.TryGetValue(lookupKey, out spanishText))
            return true;

        if (TryTranslateRunModifierDescription(lookupKey, out spanishText))
            return true;

        if (TryTranslateLabelValueLines(englishText, out spanishText))
            return true;

        return false;
    }

    static Dictionary<string, string> BuildExactText()
    {
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Add(text, "OK", "OK");
        Add(text, "Cancel", "Cancelar");
        Add(text, "Continue", "Continuar");
        Add(text, "Confirm", "Confirmar");
        Add(text, "Close", "Cerrar");
        Add(text, "Start", "Comenzar");
        Add(text, "PAUSED", "PAUSA");
        Add(text, "Resume", "Reanudar");
        Add(text, "Main Menu", "Men\u00fa principal");
        Add(text, "Restart", "Reiniciar");
        Add(text, "Save & Quit", "Guardar y salir");
        Add(text, "Quit", "Salir");
        Add(text, "New Game", "Nueva partida");
        Add(text, "Shop", "Tienda");
        Add(text, "Codex", "C\u00f3dice");
        Add(text, "Help", "Ayuda");
        Add(text, "HighScore", "Puntuaci\u00f3n m\u00e1xima");
        Add(text, "Select Monsters", "Elegir monstruos");
        Add(text, "Select Commander", "Seleccionar comandante");
        Add(text, "Tip: Special blocks activate as soon as they are placed.", "Consejo: Los bloques especiales se activan en cuanto se colocan.");
        Add(text, "Tip: Rerolls can be saved and used on future reward screens during the same run.", "Consejo: Las repeticiones se pueden guardar y usar en futuras pantallas de recompensa durante la misma partida.");
        Add(text, "Tip: Temporary monster copies earn EXP during a run, and some of it becomes permanent after the run ends.", "Consejo: Las copias temporales de monstruos ganan EXP durante una partida, y parte de ella se vuelve permanente cuando termina.");
        Add(text, "Tip: Full rows launch attacks at the enemy castle.", "Consejo: Las filas completas lanzan ataques contra el castillo enemigo.");
        Add(text, "Tip: Keep an eye on your unit reserve. If it reaches 0, the run is over.", "Consejo: Vigila tu reserva de unidades. Si llega a 0, la partida termina.");
        Add(text, "Tip: Level modifiers stack with your run buffs and debuffs.", "Consejo: Los modificadores de nivel se acumulan con tus mejoras y desventajas de la partida.");
        Add(text, "Combat Log", "Registro de combate");
        Add(text, "{0} takes {1} damage.", "{0} recibe {1} de da\u00f1o.");
        Add(text, "{0} heals {1}.", "{0} se cura {1}.");
        Add(text, "{0} dies.", "{0} muere.");
        Add(text, "{0} uses {1}.", "{0} usa {1}.");
        Add(text, "{0} casts {1}.", "{0} lanza {1}.");
        Add(text, "{0} took {1}{2} damage{3}.", "{0} recibi\u00f3 {1}{2} de da\u00f1o{3}.");
        Add(text, "{0} restored {1} health for {2}.", "{0} restaur\u00f3 {1} de salud a {2}.");
        Add(text, "{0} dealt {1} damage to {2}.{3}", "{0} hizo {1} de da\u00f1o a {2}.{3}");
        Add(text, " from {0}", " de {0}");
        Add(text, "(shielded)", "(protegido)");
        Add(text, "poison", "veneno");
        Add(text, "fire", "fuego");
        Add(text, "lightning", "rayo");
        Add(text, "contagion", "contagio");
        Add(text, "starvation", "hambre");
        Add(text, "burst", "explosi\u00f3n");
        Add(text, "floor effect", "efecto de suelo");
        Add(text, "storm", "tormenta");
        Add(text, "infection", "infecci\u00f3n");
        Add(text, "low rations", "raciones bajas");
        Add(text, "death burst", "explosi\u00f3n mortal");
        Add(text, "spikes", "pinchos");
        Add(text, "Enemy Archer", "Arquero enemigo");
        Add(text, "rear ambush", "emboscada trasera");
        Add(text, "Castle", "Castillo");
        Add(text, "Skybreaker Edict", "Edicto quebracielos");
        Add(text, "Heaven's Judgement", "Juicio del cielo");
        Add(text, "Stormcaller's Verdict", "Veredicto del invocador de tormentas");
        Add(text, "Hex of the Warped Ground", "Maleficio del suelo deformado");
        Add(text, "Aegis of the Unbroken Crown", "\u00c9gida de la corona intacta");
        Add(text, "Temporal Distortion", "Distorsi\u00f3n temporal");
        Add(text, "Ward of the Arcane Pylons", "Resguardo de los pilones arcanos");
        Add(text, "Rune of Ruin", "Runa de ruina");
        Add(text, "Summon Earthen Rampart", "Invocar muralla de tierra");
        Add(text, "Raise Iron Thorns", "Alzar espinas de hierro");
        Add(text, "Sow Venomous Miasma", "Sembrar miasma venenoso");
        Add(text, "Kindle Infernal Sigils", "Encender sigilos infernales");
        Add(text, "Call Stormbound Sigils", "Invocar sigilos de tormenta");
        Add(text, "Skip Trailer", "Saltar tr\u00e1iler");
        Add(text, "BGM Genre", "G\u00e9nero BGM");
        Add(text, "EDM", "EDM");
        Add(text, "Metal", "Música metal");
        Add(text, "Random", "Aleatorio");
        Add(text, "Modifiers", "Modificadores");
        Add(text, "Active Run Modifiers", "Modificadores de partida activos");
        Add(text, "Back", "Atrás");
        Add(text, "None", "Ninguno");
        Add(text, "Yes", "Sí");
        Add(text, "No", "No");
        Add(text, "Locked", "Bloqueado");
        Add(text, "Blocked", "Bloqueado");
        Add(text, "Active", "Activo");
        Add(text, "(Missing)", "(Falta)");
        Add(text, "???", "???");
        Add(text, "Leaderboard", "Clasificaci\u00f3n");
        Add(text, "LEADERBOARDS", "CLASIFICACIONES");
        Add(text, "Global", "Global");
        Add(text, "Friends", "Amigos");
        Add(text, "Current", "Actual");
        Add(text, "Rank", "Rango");
        Add(text, "Player", "Jugador");
        Add(text, "Refresh", "Actualizar");
        Add(text, "Loading...", "Cargando...");
        Add(text, "Refreshing leaderboards...", "Actualizando clasificaciones...");
        Add(text, "Steam leaderboard ready.", "Clasificaci\u00f3n de Steam lista.");
        Add(text, "Leaderboard refresh failed.", "No se pudo actualizar la clasificaci\u00f3n.");
        Add(text, "No leaderboard data.", "No hay datos de clasificaci\u00f3n.");
        Add(text, "No global scores yet.", "A\u00fan no hay puntuaciones globales.");
        Add(text, "No friend scores yet.", "A\u00fan no hay puntuaciones de amigos.");
        Add(text, "You are not ranked yet.", "A\u00fan no tienes rango.");
        Add(text, "Help Menu", "Menú de ayuda");
        Add(text, "Modifier not yet discovered.", "Modificador aún no descubierto.");
        Add(text, "No level Modifier", "Sin modificador de nivel");
        Add(text, "Level modifier: {0}.", "Modificador de nivel: {0}.");
        Add(text, "Secret Achievements", "Logros secretos");
        Add(text, "1 secret achievement remaining", "Queda 1 logro secreto");
        Add(text, "{0} secret achievements remaining", "Quedan {0} logros secretos");
        Add(text, "No GameController found.", "No se encontró GameController.");
        Add(text, "No monsters in roster.", "No hay monstruos en el escuadrón.");
        Add(text, "Reinforcements", "Refuerzos");
        Add(text, "Special Ability: {0}", "Habilidad especial: {0}");
        Add(text, "{0} is broken.", "{0} se ha roto.");
        Add(text, "{0}: {1} shield(s) remain.", "{0}: quedan {1} escudo(s).");

        AddCommonLabels(text);
        AddXpBreakdownText(text);
        AddCharacterText(text);
        AddWarningAndTutorialText(text);
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
        Add(text, "Starting a new game will erase the saved run. Continue?", "Iniciar una nueva partida borrará la partida guardada. ¿Continuar?");
        Add(text, "Current Level: {0}", "Nivel actual: {0}");
        Add(text, "Current Level: 0", "Nivel actual: 0");
        Add(text, " x{0}", " x{0}");
        Add(text, "Refund", "Reembolsar");
        Add(text, "Unlock", "Desbloquear");
        Add(text, " Unlock", " Desbloquear");
        Add(text, "Selected:", "Seleccionados:");
        Add(text, "Selected: ", "Seleccionados: ");
        Add(text, "{0}  Lv.{1}", "{0}  Nv.{1}");
        Add(text, "Luck Up", "Suerte +");
        Add(text, "Gravity Down", "Gravedad -");
        Add(text, "Velocity Down", "Velocidad -");
        Add(text, "Gold Up", "Oro +");
        Add(text, "Attack Up", "Ataque +");
        Add(text, "HP Up", "PV +");
        Add(text, "Unit Lives Up", "Vidas de unidad +");
        Add(text, "Role: {0}", "Rol: {0}");
        Add(text, "Level: {0}  ({1:0.#}/{2})", "Nivel: {0}  ({1:0.#}/{2})");
        Add(text, "Max HP: {0:0.#}  (+{1}) = {2:0.#}", "PV máx.: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Attack: {0:0.#}  (+{1}) = {2:0.#}", "Ataque: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Special Gain: {0:0.#}", "Ganancia especial: {0:0.#}");
        Add(text, "Heal: {0:0.#}  (+{1}) = {2:0.#}", "Curación: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Heal Range: {0:0.#}", "Alcance de curación: {0:0.#}");
        Add(text, "Heal Speed: {0:0.#}s", "Velocidad de curación: {0:0.#} s");
        Add(text, "Heal: -", "Curación: -");
        Add(text, "Base Stats + Shop Buff = Total Stats", "Estadísticas base + mejora de tienda = estadísticas totales");
        Add(text, "Defense", "Defensa");
        Add(text, "Beefy boy that deals very little damage.", "Un tanque robusto que inflige muy poco daño.");
        Add(text, "Healer with a wide range, but weak spells.", "Sanadora de gran alcance, pero con hechizos débiles.");
        Add(text, "A well rounded attacker unit with decent damage, but enough health to survive weaker attacks.", "Unidad atacante equilibrada con daño decente y suficiente salud para resistir ataques débiles.");
        Add(text, "A specialized attacker unit with high base damage and lower health.", "Unidad atacante especializada con mucho daño base y poca salud.");
        Add(text, "Can take a hit and keep going, but won't deal much damage.", "Puede aguantar golpes y seguir adelante, pero no inflige mucho daño.");
        Add(text, "Healer with a short range, but powerful healing spells.", "Sanadora de corto alcance, pero con hechizos de curación potentes.");
        Add(text, "Combo Extension", "Extensión de combo");
        Add(text, "Chain Surge", "Impulso de cadena");
        Add(text, "Stone Scrounger", "Buscador de piedra");
        Add(text, "Reserve Stockpile", "Reserva acumulada");
        Add(text, "Reserve Recovery", "Recuperación de reserva");
        Add(text, "Bulwark Aura", "Aura de baluarte");
        Add(text, "Increase combo timer duration by {0}.", "Aumenta la duración del temporizador de combo en {0}.");
        Add(text, "Each row clear has a {0} chance to increase combo count one additional time.", "Cada fila eliminada tiene un {0} de probabilidad de aumentar el combo una vez adicional.");
        Add(text, "Increase chance of buff drop from stone obstacle destruction by {0}.", "Aumenta en {0} la probabilidad de obtener mejoras al destruir obstáculos de piedra.");
        Add(text, "Increase the number of starting reserve units by {0}.", "Aumenta en {0} las unidades de reserva iniciales.");
        Add(text, "Increase the number of reserve units restored on round win by {0}.", "Aumenta en {0} las unidades de reserva restauradas al ganar una ronda.");
        Add(text, "Decrease damage taken and damage done for all ally monster units by {0}.", "Reduce en {0} el daño recibido e infligido por todas las unidades monstruo aliadas.");
        Add(text, "Passive - {0}", "Pasiva - {0}");
        Add(text, "Passive - {0}:", "Pasiva - {0}:");
        Add(text, "Next upgrade at Lv.{0}:", "Próxima mejora en Nv.{0}:");
        Add(text, "Passive is fully upgraded.", "La pasiva está al nivel máximo.");
        Add(text, "1 second", "1 segundo");
        Add(text, "{0} seconds", "{0} segundos");
        Add(text, "A = Shift Left", "A = Mover izq.");
        Add(text, "D = Shift Right", "D = Mover der.");
        Add(text, "D = Shft Right", "D = Mover der.");
        Add(text, "R = Character Special", "R = Especial del personaje");
        Add(text, "= Character Special", "= Especial del personaje");
        Add(text, "R = Character Special (Special Guage 100%)", "R = Especial del personaje (medidor especial 100%)");
        Add(text, "R = Character Special (Special Gauge 100%)", "R = Especial del personaje (medidor especial 100%)");
        Add(text, "R = Character Special (Specaial Gauge 100%)", "R = Especial del personaje (medidor especial 100%)");
        Add(text, "= Character Special (Special Guage 100%)", "= Especial del personaje (medidor especial 100%)");
        Add(text, "= Character Special (Special Gauge 100%)", "= Especial del personaje (medidor especial 100%)");

        Add(text, "Gold Won This Round:", "Oro ganado esta ronda:");
        Add(text, "Gold Won This Round", "Oro ganado esta ronda");
        Add(text, "Rerolls", "Repeticiones");
        Add(text, "Rerolls: {0}", "Repeticiones: {0}");
        Add(text, "Rerolls: 0", "Repeticiones: 0");
        Add(text, "Rerolls ({0})", "Repeticiones ({0})");
        Add(text, "Modifier", "Modificador");
        Add(text, "x{0}", "x{0}");

        Add(text, "Level Up", "Sube de nivel");
        Add(text, "Level Up!", "¡Sube de nivel!");
        Add(text, "LEVEL UP!", "¡SUBE DE NIVEL!");
        Add(text, "LEVEL UP! x{0}", "¡SUBE DE NIVEL! x{0}");
        Add(text, "Level {0} -> Level {1}", "Nivel {0} -> Nivel {1}");
        Add(text, "+{0} Exp", "+{0} EXP");
        Add(text, "{0} permanent EXP ({1}% of {2} transferable EXP)", "{0} EXP permanente ({1}% de {2} EXP transferible)");
        Add(text, "Converted from {0} run EXP at {1}%", "Convertida desde {0} EXP de partida al {1}%");

        Add(text, "Passive+", "Pasiva+");
        Add(text, "+ 5 HP", "+ 5 PV");
        Add(text, "+ 1 Attack", "+ 1 Ataque");
        Add(text, "+ 1 Special", "+ 1 Especial");
        Add(text, "+ 5 Heal", "+ 5 Curación");
        Add(text, "+ 1 Range", "+ 1 Alcance");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Ahora quiero que hagas una caída rápida de tu pieza actual pulsando la [barra espaciadora]. Esto soltará la pieza al instante para que puedas colocarla rápido, fijarla y generar una nueva.");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Ahora quiero que hagas una caída rápida de tu pieza actual pulsando la [barra espaciadora]. Esto soltará la pieza al instante para que puedas colocarla rápido, fijarla y generar una nueva.");
        Add(text, "This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Esto soltará la pieza al instante para que puedas colocarla rápido, fijarla y generar una nueva.");
        Add(text, "This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Esto soltará la pieza al instante para que puedas colocarla rápido, fijarla y generar una nueva.");
        AddAchievementText(text);
    }

    static void AddAchievementText(Dictionary<string, string> text)
    {
        Add(text, "I Want Every Able Body", "Quiero a todos los disponibles");
        Add(text, "Unlock all monster units.", "Desbloquea todas las unidades monstruo.");
        Add(text, "0 Star Victory", "Victoria de 0 estrellas");
        Add(text, "Beat the final boss.", "Derrota al jefe final.");
        Add(text, "One Star Victory", "Victoria de una estrella");
        Add(text, "Beat the final boss on 1-Star difficulty.", "Derrota al jefe final en dificultad de 1 estrella.");
        Add(text, "Pay To Win", "Pagar para ganar");
        Add(text, "Upgrade any shop buff to level 5.", "Mejora cualquier mejora de tienda al nivel 5.");
        Add(text, "Gangs All Here", "La banda está completa");
        Add(text, "Unlock all Commanders.", "Desbloquea todos los comandantes.");
        Add(text, "Two Star Victory", "Victoria de dos estrellas");
        Add(text, "Beat the final boss on 2-Star difficulty.", "Derrota al jefe final en dificultad de 2 estrellas.");
        Add(text, "Three Star Victory", "Victoria de tres estrellas");
        Add(text, "Beat the final boss on 3-Star difficulty.", "Derrota al jefe final en dificultad de 3 estrellas.");
        Add(text, "Four Star Victory", "Victoria de cuatro estrellas");
        Add(text, "Beat the final boss on 4-Star difficulty.", "Derrota al jefe final en dificultad de 4 estrellas.");
        Add(text, "Five Star Victory", "Victoria de cinco estrellas");
        Add(text, "Beat the final boss on 5-Star difficulty.", "Derrota al jefe final en dificultad de 5 estrellas.");
        Add(text, "This is Fine", "Esto está bien");
        Add(text, "Take 1,000 burn damage from fire floor effects.", "Recibe 1,000 de daño por quemadura de efectos de suelo de fuego.");
        Add(text, "That Escalated Quickly", "Eso escaló rápido");
        Add(text, "Remove 1,000 units using the Death Special Block.", "Elimina 1,000 unidades usando el bloque especial de muerte.");
        Add(text, "First Time Raider", "Asaltante primerizo");
        Add(text, "Take 1,000 damage from traps.", "Recibe 1,000 de daño de trampas.");
        Add(text, "I think I Stepped in Something", "Creo que pisé algo");
        Add(text, "Take 1,000 toxic damage from posioned floor effects.", "Recibe 1,000 de daño tóxico de efectos de suelo envenenado.");
        Add(text, "Take 1,000 toxic damage from poisoned floor effects.", "Recibe 1,000 de daño tóxico de efectos de suelo envenenado.");
        Add(text, "Shake It Until You Break It", "Agítalo hasta romperlo");
        Add(text, "Clear 250 rows by using the earthquake special block.", "Elimina 250 filas usando el bloque especial de terremoto.");
        Add(text, "A Little Jiggle Goes A Long Way", "Un pequeño temblor llega lejos");
        Add(text, "Clear 25 rows by using the earthquake special block.", "Elimina 25 filas usando el bloque especial de terremoto.");
        Add(text, "Girthquake", "Megaterremoto");
        Add(text, "Clear 1,000 rows by using the earthquake special block.", "Elimina 1,000 filas usando el bloque especial de terremoto.");
        Add(text, "Get in Loser, We're Going Shopping", "Sube, vamos de compras");
        Add(text, "Accumulate 1,000 gold.", "Acumula 1,000 de oro.");
        Add(text, "Is I Rich Now?", "¿Ya soy rico?");
        Add(text, "Accumulate 100 gold.", "Acumula 100 de oro.");
        Add(text, "This Lasted Longer Than Some Collectible Fads", "Esto duró más que algunas modas coleccionables");
        Add(text, "Take more than 5 minutes to conquer a castle.", "Tarda más de 5 minutos en conquistar un castillo.");
        Add(text, "Anything You Can Do, I Can Do Slower", "Todo lo que haces, yo lo hago más lento");
        Add(text, "Take more than 3 minutes to conquer a castle.", "Tarda más de 3 minutos en conquistar un castillo.");
        Add(text, "Sloth Lord", "Señor de la pereza");
        Add(text, "Take more than 4 minutes to conquer a castle.", "Tarda más de 4 minutos en conquistar un castillo.");
        Add(text, "My Fingers Hurt", "Me duelen los dedos");
        Add(text, "Survive with gravity at 10 for 60 seconds.", "Sobrevive con gravedad 10 durante 60 segundos.");
        Add(text, "Nevermind...", "Mejor no...");
        Add(text, "Unlock your first first temporary debuff.", "Desbloquea tu primera desventaja temporal.");
        Add(text, "A Record That Would Make Lions Blush", "Un récord para avergonzar a cualquiera");
        Add(text, "Lose 50 Times.", "Pierde 50 veces.");
        Add(text, "Thrive Under Pressure", "Prospera bajo presión");
        Add(text, "Survive with gravity at 10 for 30 seconds.", "Sobrevive con gravedad 10 durante 30 segundos.");
        Add(text, "Is I Strong Now?", "¿Ya soy fuerte?");
        Add(text, "Unlock your first first temporary buff.", "Desbloquea tu primera mejora temporal.");
        Add(text, "I Say This Not As An Insult, But As A Statement Of Fact", "No lo digo como insulto, sino como un hecho");
        Add(text, "Lose 100 Times.", "Pierde 100 veces.");
        Add(text, "I Think We Need A Bigger Vault", "Creo que necesitamos una bóveda más grande");
        Add(text, "Accumulate 10,000 gold.", "Acumula 10,000 de oro.");
        Add(text, "GG EZ", "GG EZ");
        Add(text, "Beat the final level with every Commander.", "Supera el nivel final con cada comandante.");
        Add(text, "Participation Trophy", "Trofeo de participación");
        Add(text, "Lose for the first time.", "Pierde por primera vez.");
        Add(text, "Immortal Army", "Ejército inmortal");
        Add(text, "Conquer 100 castles with your Unit Reserve at max capacity.", "Conquista 100 castillos con la reserva de unidades al máximo.");
        Add(text, "I Guess That Was The Wrong Wire", "Supongo que ese era el cable equivocado");
        Add(text, "Lose 100 monster units to magic explosives.", "Pierde 100 unidades monstruo por explosivos mágicos.");
        Add(text, "I Think It's Dead Now", "Creo que ya está muerto");
        Add(text, "Clear 1,000 rows.", "Elimina 1,000 filas.");
        Add(text, "Tis But A Scratch", "Solo es un rasguño");
        Add(text, "Conquer a castle with your Unit Reserve at max capacity.", "Conquista un castillo con la reserva de unidades al máximo.");
        Add(text, "Meat Shield Tactics", "Tácticas de escudo de carne");
        Add(text, "Conquer 25 castles with your Unit Reserve at max capacity.", "Conquista 25 castillos con la reserva de unidades al máximo.");
        Add(text, "You Got Your Marching Orders", "Ya tienes tus órdenes de marcha");
        Add(text, "Clear 100 rows.", "Elimina 100 filas.");
        Add(text, "Overwhelming Power", "Poder abrumador");
        Add(text, "Accumulate 15 buffs in a single run.", "Acumula 15 mejoras en una sola partida.");
        Add(text, "Is There Anything Left To Attack?", "¿Queda algo que atacar?");
        Add(text, "Clear 10,000 rows.", "Elimina 10,000 filas.");
        Add(text, "Shut Up And Takey My Money", "Cállate y toma mi dinero");
        Add(text, "Buy an upgrade from the shop for the first time.", "Compra una mejora en la tienda por primera vez.");
        Add(text, "Turning Small Numbers Into Big Numbers", "Convirtiendo números pequeños en grandes");
        Add(text, "Get a combo of 10 or higher.", "Consigue un combo de 10 o más.");
        Add(text, "Vewwy Stwong", "Muuuy fuerte");
        Add(text, "Deal 100 damage or more in a single attack.", "Inflige 100 de daño o más en un solo ataque.");
        Add(text, "I'll Follow You Anywhere", "Te seguiré a cualquier parte");
        Add(text, "Unlock a Commander for the first time.", "Desbloquea un comandante por primera vez.");
        Add(text, "Eenie Meenie Miney Mo", "Pito pito colorito");
        Add(text, "Unlock a monster unit for the first time.", "Desbloquea una unidad monstruo por primera vez.");
        Add(text, "Tell Me I'm Pretty", "Dime que soy bonito");
        Add(text, "Unlock a monster units skin variant for the first time.", "Desbloquea una variante de aspecto de una unidad monstruo por primera vez.");
        Add(text, "Are We There Yet?", "¿Ya llegamos?");
        Add(text, "Conquer 10 castles in a single run.", "Conquista 10 castillos en una sola partida.");
        Add(text, "It's Called Fashion Brenda, Look It Up", "Se llama moda, búscalo");
        Add(text, "Unlock ten skin variants.", "Desbloquea diez variantes de aspecto.");
        Add(text, "New Skin Who Dis?", "Nuevo aspecto, ¿quién es?");
        Add(text, "Unlock five skin variants.", "Desbloquea cinco variantes de aspecto.");
        Add(text, "A Little Special", "Un poco especial");
        Add(text, "Use a special for the first time.", "Usa una habilidad especial por primera vez.");
        Add(text, "Mama's Special Boy", "El niño especial de mamá");
        Add(text, "Use a special 100 times.", "Usa una habilidad especial 100 veces.");
        Add(text, "Some Are More Special Than Others", "Algunos son más especiales que otros");
        Add(text, "Use every Commanders special 100 times.", "Usa la habilidad especial de cada comandante 100 veces.");
        Add(text, "General Got Me Workin'", "El general me puso a trabajar");
        Add(text, "Destory 100 stone obstacles.", "Destruye 100 obstáculos de piedra.");
        Add(text, "Destroy 100 stone obstacles.", "Destruye 100 obstáculos de piedra.");
        Add(text, "Certified Glue Eater", "Comedor de pegamento certificado");
        Add(text, "Use a special 1000 times.", "Usa una habilidad especial 1000 veces.");
        Add(text, "We're All Special", "Todos somos especiales");
        Add(text, "Use every Commanders special 20 times.", "Usa la habilidad especial de cada comandante 20 veces.");
        Add(text, "That's A Lot Of Rubble", "Eso es mucho escombro");
        Add(text, "Conquer 50 castles.", "Conquista 50 castillos.");
        Add(text, "King of Rubble", "Rey del escombro");
        Add(text, "Conquer 100 castles.", "Conquista 100 castillos.");
        Add(text, "I. AM. SPEED!", "¡SOY VELOCIDAD!");
        Add(text, "Conquer a castle in 30 seconds or less.", "Conquista un castillo en 30 segundos o menos.");
        Add(text, "Can't Stop, Won't Stop", "No puedo parar, no voy a parar");
        Add(text, "Conquer your first castle.", "Conquista tu primer castillo.");
        Add(text, "I Ran Track In Highschool", "Corría atletismo en la escuela");
        Add(text, "Conquer a castle in 45 seconds or less.", "Conquista un castillo en 45 segundos o menos.");
        Add(text, "Gotta Go Fast", "Hay que ir rápido");
        Add(text, "Conquer a castle in 60 seconds or less.", "Conquista un castillo en 60 segundos o menos.");
    }

    static void AddXpBreakdownText(Dictionary<string, string> text)
    {
        Add(text, "Level {0} Complete", "Nivel {0} completado");
        Add(text, "Base Level XP", "EXP base del nivel");
        Add(text, "Clear time {0}", "Tiempo de limpieza {0}");
        Add(text, "Units lost {0}", "Unidades perdidas {0}");
        Add(text, "Largest Combo {0}", "Mayor combo {0}");
        Add(text, "Obstacles Cleared {0}", "Obst\u00e1culos eliminados {0}");
        Add(text, "Star Difficulty ({0}):", "Dificultad de estrellas ({0}):");
        Add(text, "Total XP Earned {0}", "EXP total obtenida {0}");
    }

    static void AddCommonLabels(Dictionary<string, string> text)
    {
        Add(text, "Run", "Partida");
        Add(text, "Level", "Nivel");
        Add(text, "Controls", "Controles");
        Add(text, "Gravity", "Gravedad");
        Add(text, "Combo", "Combo");
        Add(text, "Score", "Puntuaci\u00f3n");
        Add(text, "Reset", "Reinicio");
        Add(text, "Score: {0}", "Puntuaci\u00f3n: {0}");
        Add(text, "Gravity: {0:0.0}", "Gravedad: {0:0.0}");
        Add(text, "Star Difficulty", "Dificultad de estrellas");
        Add(text, "0 Star: Recruit Difficulty", "0 estrellas: dificultad recluta");
        Add(text, "1 Star: Soldier Difficulty", "1 estrella: dificultad soldado");
        Add(text, "2 Star: Veteran Difficulty", "2 estrellas: dificultad veterano");
        Add(text, "3 Star: Lieutenant Difficulty", "3 estrellas: dificultad teniente");
        Add(text, "4 Star: General Difficulty", "4 estrellas: dificultad general");
        Add(text, "5 Star: War God Difficulty", "5 estrellas: dificultad dios de guerra");
        Add(text, "0-Star Difficulty", "Dificultad 0 estrellas");
        Add(text, "1 Star Difficulty", "Dificultad 1 estrella");
        Add(text, "2 Stars Difficulty", "Dificultad 2 estrellas");
        Add(text, "3 Stars Difficulty", "Dificultad 3 estrellas");
        Add(text, "4 Stars Difficulty", "Dificultad 4 estrellas");
        Add(text, "5 Stars Difficulty", "Dificultad 5 estrellas");
        Add(text, "Normal difficulty.", "Dificultad normal.");
        Add(text, "No gameplay modifiers.", "Sin modificadores de juego.");
        Add(text, "All star difficulties unlocked.", "Todas las dificultades de estrellas est\u00e1n desbloqueadas.");
        Add(text, "0 Stars is always available.", "0 estrellas siempre est\u00e1 disponible.");
        Add(text, "Beat the final level on 0 Stars to unlock 1 Star.", "Supera el nivel final en 0 estrellas para desbloquear 1 estrella.");
        Add(text, "Beat the final level on 1 Star to unlock 2 Stars.", "Supera el nivel final en 1 estrella para desbloquear 2 estrellas.");
        Add(text, "Beat the final level on 2 Stars to unlock 3 Stars.", "Supera el nivel final en 2 estrellas para desbloquear 3 estrellas.");
        Add(text, "Beat the final level on 3 Stars to unlock 4 Stars.", "Supera el nivel final en 3 estrellas para desbloquear 4 estrellas.");
        Add(text, "Beat the final level on 4 Stars to unlock 5 Stars.", "Supera el nivel final en 4 estrellas para desbloquear 5 estrellas.");
        Add(text, "0 Stars", "0 estrellas");
        Add(text, "1 Star", "1 estrella");
        Add(text, "2 Stars", "2 estrellas");
        Add(text, "3 Stars", "3 estrellas");
        Add(text, "4 Stars", "4 estrellas");
        Add(text, "5 Stars", "5 estrellas");
        Add(text, "Score Gain", "Ganancia de puntuación");
        Add(text, "EXP Gain", "Ganancia de EXP");
        Add(text, "Level Modifier", "Modificador de nivel");
        Add(text, "Reserves And Rewards", "Reservas y recompensas");
        Add(text, "Reserve Units", "Reserva de unidades");
        Add(text, "Max Reserve Units", "Máx. reserva de unidades");
        Add(text, "Reserve Restored On Win", "Reserva restaurada al ganar");
        Add(text, "Round Win Currency", "Oro por ronda ganada");
        Add(text, "Line Clear Currency Chance", "Probabilidad de oro al limpiar líneas");
        Add(text, "Line Clear Currency Amount", "Cantidad de oro al limpiar líneas");
        Add(text, "Monster Combat", "Combate de monstruos");
        Add(text, "Monster Damage", "Daño de monstruos");
        Add(text, "Monster Special Gain", "Ganancia especial de monstruos");
        Add(text, "Monster Max HP", "PV máximos de monstruos");
        Add(text, "Healing Power", "Poder de curación");
        Add(text, "Healing Range Bonus", "Bonificación de alcance de curación");
        Add(text, "Ally Damage Dealt", "Daño aliado infligido");
        Add(text, "Ally Damage Taken", "Daño aliado recibido");
        Add(text, "Combo And Passives", "Combo y pasivas");
        Add(text, "Combo Window", "Ventana de combo");
        Add(text, "Bonus Combo Chance", "Probabilidad de combo extra");
        Add(text, "Stone Buff Drop Chance", "Probabilidad de mejora en piedra");
        Add(text, "Starting Reserve Passive", "Pasiva de reserva inicial");
        Add(text, "Round Win Reserve Passive", "Pasiva de reserva al ganar ronda");
        Add(text, "Enemy", "Enemigo");
        Add(text, "Enemy Castle HP", "PV del castillo enemigo");
        Add(text, "Enemy Damage", "Daño enemigo");
        Add(text, "Enemy Attack Interval", "Intervalo de ataque enemigo");
        Add(text, "Enemy Projectile Speed", "Velocidad de proyectiles enemigos");
        Add(text, "Castle Projectile Damage", "Daño de proyectil del castillo");
        Add(text, "Castle Attack Interval", "Intervalo de ataque del castillo");
        Add(text, "Piece And Special", "Pieza y especial");
        Add(text, "Piece Gravity", "Gravedad de pieza");
        Add(text, "Gravity Ramp Rate", "Ritmo de aumento de gravedad");
        Add(text, "Special Block Chance", "Probabilidad de bloque especial");
        Add(text, "Commander Special Gain", "Ganancia especial del comandante");
        Add(text, "Special Drain", "Drenaje especial");
        Add(text, "Next Preview Disabled", "Vista previa siguiente desactivada");
        Add(text, "Landing Hint Disabled", "Indicador de aterrizaje desactivado");
        Add(text, "Special Usage Locked", "Uso de especial bloqueado");
        Add(text, "Special Blocks Blocked", "Bloques especiales bloqueados");
        Add(text, "Run Modifier Drops", "Aparición de modificadores de partida");
        Add(text, "Stone Drops Debuffs Only", "Las piedras solo sueltan desventajas");
        Add(text, "Luck", "Suerte");
        Add(text, "Misfortune", "Infortunio");
        Add(text, "Active Level Modifier", "Modificador de nivel activo");
        Add(text, "Effect", "Efecto");
        Add(text, "Outgoing Damage", "Daño saliente");
        Add(text, "Incoming Damage", "Daño recibido");
        Add(text, "Overgrowth Target Interval", "Intervalo objetivo de sobrecrecimiento");
        Add(text, "Initial Target Rows", "Filas objetivo iniciales");
        Add(text, "Partial Growth Time", "Tiempo de crecimiento parcial");
        Add(text, "Full Growth Time", "Tiempo de crecimiento completo");
        Add(text, "Storm Strike Damage", "Daño de impacto de tormenta");
        Add(text, "Storm Floor Tick Damage", "Daño periódico de suelo tormentoso");
        Add(text, "Storm Floor Duration", "Duración del suelo tormentoso");
        Add(text, "Rear Ambush Interval", "Intervalo de emboscada trasera");
        Add(text, "Rations Tick Interval", "Intervalo de raciones");
        Add(text, "Low Reserve Damage", "Daño con reserva baja");
        Add(text, "High Reserve Damage", "Daño con reserva alta");
        Add(text, "Infection Chance", "Probabilidad de infección");
        Add(text, "Damage Per Tick", "Daño por intervalo");
        Add(text, "Damage Increase Per Tick", "Aumento de daño por intervalo");
        Add(text, "Spread Chance", "Probabilidad de propagación");
        Add(text, "Special Gauge Gain", "Ganancia de medidor especial");
        Add(text, "Death Explosion Damage", "Daño de explosión por muerte");
        Add(text, "Swamp Poison Damage", "Daño de veneno del pantano");
        Add(text, "Swamp Poison Interval", "Intervalo de veneno del pantano");
        Add(text, "Manual Rotation", "Rotación manual");
        Add(text, "Auto Rotate Interval", "Intervalo de autorrotación");
        Add(text, "Manual Horizontal Shift", "Desplazamiento horizontal manual");
        Add(text, "Auto Shift Interval", "Intervalo de autodesplazamiento");
        Add(text, "Combo Threshold", "Umbral de combo");
        Add(text, "Below Threshold Damage", "Daño bajo el umbral");
        Add(text, "Shield Combo Threshold", "Umbral de combo del escudo");
        Add(text, "Blocked Damage", "Daño bloqueado");
        Add(text, "Shield Count", "Cantidad de escudos");
        Add(text, "Special Pieces", "Piezas especiales");
        Add(text, "Starting Monster Health", "Salud inicial de monstruos");
        Add(text, "Monster Damage Sharing", "Daño compartido entre monstruos");
        Add(text, "Roster", "Escuadrón");
        Add(text, "Max HP", "PV máximos");
        Add(text, "Starting HP", "PV iniciales");
        Add(text, "Attack", "Ataque");
        Add(text, "Special Gain", "Ganancia especial");
        Add(text, "Heal Power", "Poder de curación");
        Add(text, "Heal Range", "Alcance de curación");
        Add(text, "Heal Speed", "Velocidad de curación");
        Add(text, "Spawn Weight", "Peso de aparición");
        Add(text, "Stats", "Estadísticas");
        Add(text, "Monsters", "Monstruos");
        Add(text, "Shop Buff", "Mejora de tienda");
        Add(text, "Passive", "Pasiva");
        Add(text, "Run Mod Buff", "Mejora de partida");
        Add(text, "Run Mod Debuff", "Desventaja de partida");
        Add(text, "Level Mod", "Mod. de nivel");
        Add(text, "Boss Ability", "Habilidad de jefe");
        Add(text, "Defender", "Defensor");
        Add(text, "Healer", "Sanador");
        Add(text, "Role", "Rol");
        Add(text, "HP", "PV");
        Add(text, "Special Rate", "Tasa especial");
        Add(text, "Passive Lv", "Nivel de pasiva");
        Add(text, "Esc = Pause", "Esc = Pausa");
        Add(text, "Q = Rotate Counter Clockwise", "Q = Girar antihorario");
        Add(text, "E = Rotate Clockwise", "E = Girar horario");
        Add(text, "= Shift Left", "= Mover izq.");
        Add(text, "D = Shft Right", "D = Mover der.");
        Add(text, "S = Shift Down", "S = Bajar");
        Add(text, "Spacebar = Drop Instantly", "Espacio = Soltar al instante");
        Add(text, "= Character Special (Specaial Gauge 100%)", "= Especial del personaje (medidor al 100%)");
        Add(text, "Castle", "Castillo");
        Add(text, "Starting Village", "Aldea inicial");
        Add(text, "Tribe", "Tribu");
        Add(text, "Tribe Chieftain", "Cacique de la tribu");
        Add(text, "Shanty Town", "Barrio humilde");
        Add(text, "Fortified Hamlet", "Aldea fortificada");
        Add(text, "Industrial City", "Ciudad industrial");
        Add(text, "Thriving Metropolis", "Metr\u00f3polis pr\u00f3spera");
        Add(text, "Grand Dukedom", "Gran ducado");
        Add(text, "Royal Villa", "Villa real");
        Add(text, "Ruk, Tribal Chieftain", "Ruk, cacique tribal");
        Add(text, "Fionne, Village Protector", "Fionne, protectora de la aldea");
        Add(text, "Sir Ralphie, Captain of the Guard", "Sir Ralphie, capit\u00e1n de la guardia");
        Add(text, "Eris, City Priestess", "Eris, sacerdotisa de la ciudad");
        Add(text, "His Holiness Isaeh, Metropolis Pope", "Su Santidad Isaeh, papa de la metr\u00f3polis");
        Add(text, "Vivica, Dukedom Arch Mage", "Vivica, archimaga del ducado");
        Add(text, "Emperor Reginald P. Exford IV, Emperor's Palace", "Emperador Reginald P. Exford IV, Palacio imperial");
        Add(text, "Esora, Guardian to the Gates of Heaven?", "Esora, guardiana de las puertas del cielo?");
    }

    static void AddWarningAndTutorialText(Dictionary<string, string> text)
    {
        Add(text, "Purchases are disabled in the demo. Your earned progress will still carry into the full game.",
            "Las compras están desactivadas en la demo. El progreso que ganes se conservará en el juego completo.");
        Add(text, "A saved run is waiting. Continue that run or delete the temp save before changing your commander, squad, or shop buffs.",
            "Hay una partida guardada pendiente. Continúa esa partida o borra el guardado temporal antes de cambiar tu comandante, escuadrón o mejoras de tienda.");
        Add(text, "Deleting the current temp run will permanently erase that saved run. After deleting it, you will be able to change your commander, monsters, and access the shop again. Continue?",
            "Borrar la partida temporal actual eliminará permanentemente esa partida guardada. Después podrás cambiar tu comandante, monstruos y acceder a la tienda de nuevo. ¿Continuar?");
        Add(text, "Thank you for playing the Tetrabeasts demo! You have cleared the final demo level. If you enjoyed your time with the game, please consider buying the full version.",
            "¡Gracias por jugar la demo de Tetrabeasts!\n\nHas superado el último nivel de la demo. Si disfrutaste el juego, considera comprar la versión completa.");
        Add(text, "The Castle Has Fallen", "El castillo ha caído");
        Add(text, "Conquest Failed", "Conquista fallida");
        Add(text, "Endless Survival This final battle cannot be won. The enemy has endless health, and the run continues until a loss condition is met. Survive as long as you can.",
            "Supervivencia sin fin\n\nEsta batalla final no se puede ganar. El enemigo tiene salud infinita y la partida continúa hasta que se cumpla una condición de derrota.\n\nSobrevive tanto como puedas.");
        Add(text, "Do not show this message again", "No volver a mostrar este mensaje");
        Add(text, "Quit without saving? Your current run will be lost and will not be available to continue later.",
            "¿Salir sin guardar? Tu partida actual se perderá y no podrás continuarla más tarde.");
        Add(text, "Restarting will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
            "Reiniciar contará esta partida como una derrota. El guardado temporal actual se borrará y la partida no se guardará. ¿Continuar?");
        Add(text, "Returning to the main menu will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
            "Volver al menú principal contará esta partida como una derrota. El guardado temporal actual se borrará y la partida no se guardará. ¿Continuar?");
        Add(text, "Save this run and quit the game? Continuing later will resume from the start of the current level checkpoint. While a run is saved, you will not be able to change your commander, squad, or shop buffs from the title menu.",
            "¿Guardar esta partida y salir del juego? Al continuar más tarde, volverás al inicio del punto de control del nivel actual. Mientras haya una partida guardada, no podrás cambiar tu comandante, escuadrón ni mejoras de tienda desde el menú principal.");
        Add(text, "The run could not be temp-saved, so the game will stay open.",
            "No se pudo guardar temporalmente la partida, así que el juego permanecerá abierto.");
        Add(text, "After completing a level you will be allowed to choose one of three buffs that will empower your units. Press [F] to Continue",
            "Al completar un nivel, podrás elegir una de tres mejoras que potenciarán tus unidades. Pulsa [F] para continuar");
        Add(text, "Most buffs come in multiple rarities that will determine their strength. From weakest to strongest (White -> Green -> Blue -> Purple -> Orange). Press [F] to Continue",
            "La mayoría de las mejoras tienen varias rarezas que determinan su fuerza. De más débil a más fuerte (Blanco -> Verde -> Azul -> Morado -> Naranja). Pulsa [F] para continuar");
        Add(text, "You will receive a single reroll per level that can be used on your buffs or debuffs. Rerolls can be saved throughout your run, but will be reset at the start of a new run. Press [F] to Continue",
            "Recibirás una repetición por nivel que puedes usar en tus mejoras o desventajas. Las repeticiones se pueden guardar durante la partida, pero se reiniciarán al empezar una nueva partida. Pulsa [F] para continuar");
        Add(text, "As you grow stronger your enemies will too. Choose one of three debuffs that will empower your enemies during this run. All buffs and debuffs will stay active and stack throughout the run, but will be reset upon starting a new game. Press [F] to Continue",
            "A medida que te vuelves más fuerte, tus enemigos también. Elige una de tres desventajas que potenciarán a tus enemigos durante esta partida. Todas las mejoras y desventajas permanecerán activas y se acumularán durante la partida, pero se reiniciarán al empezar una nueva partida. Pulsa [F] para continuar");
        Add(text, "Not all levels are created equal, here you will let lady luck decide what the next battlefield will be like. Pull the lever to reveal it, use rerolls if you have them, then continue into the fight. (Press [F] to Continue)",
            "No todos los niveles son iguales. Aquí dejarás que la suerte decida cómo será el próximo campo de batalla. Tira de la palanca para revelarlo, usa repeticiones si tienes, y luego continúa hacia la batalla. (Pulsa [F] para continuar)");
        Add(text, "During a run, your monsters are temporary copies. This screen drains the EXP those copies earned so a portion can be preserved for their permanent versions. (Press [F] to Continue)",
            "Durante una partida, tus monstruos son copias temporales. Esta pantalla drena la EXP que esas copias ganaron para que una parte pueda conservarse en sus versiones permanentes. (Pulsa [F] para continuar)");
        Add(text, "Preserved EXP is added to your permanent monsters. Permanent levels make your units stronger at the start of future runs. (Press [F] to Continue)",
            "La EXP conservada se añade a tus monstruos permanentes. Los niveles permanentes hacen que tus unidades sean más fuertes al inicio de futuras partidas. (Pulsa [F] para continuar)");

        Add(text, "Increase luck, improving favorable random outcomes during runs.",
            "Aumenta la suerte y mejora los resultados aleatorios favorables durante las partidas.");
        Add(text, "Reduce the starting gravity speed of falling pieces.",
            "Reduce la velocidad inicial de gravedad de las piezas que caen.");
        Add(text, "Reduce how quickly gravity ramps up during a level.",
            "Reduce la rapidez con la que la gravedad aumenta durante un nivel.");
        Add(text, "Increase the chance to earn gold from cleared rows.",
            "Aumenta la probabilidad de ganar oro al limpiar filas.");
        Add(text, "Increase monster attack power.",
            "Aumenta el poder de ataque de los monstruos.");
        Add(text, "Increase monster maximum HP.",
            "Aumenta los PV maximos de los monstruos.");
        Add(text, "Increase monster healing power.",
            "Aumenta el poder de curacion de los monstruos.");
        Add(text, "Increase starting unit reserves.",
            "Aumenta las reservas iniciales de unidades.");
        Add(text, "You found a Death special block. Drop it in a column to destroy all units matching the first monster below it. (Press [F] to Continue)",
            "Has encontrado un bloque especial de Muerte. Sueltalo en una columna para destruir todas las unidades que coincidan con el primer monstruo debajo. (Pulsa [F] para continuar)");
        Add(text, "You found a Bomb special block. Drop it to blast a 3x3 area and damage nearby obstacles. (Press [F] to Continue)",
            "Has encontrado un bloque especial de Bomba. Sueltalo para explotar un area de 3x3 y danar obstaculos cercanos. (Pulsa [F] para continuar)");
        Add(text, "You found a Bolt special block. Drop it to strike an entire column. (Press [F] to Continue)",
            "Has encontrado un bloque especial de Rayo. Sueltalo para golpear una columna completa. (Pulsa [F] para continuar)");
        Add(text, "You found an Earthquake special block. Drop it to shake loose unsupported units across the board. (Press [F] to Continue)",
            "Has encontrado un bloque especial de Terremoto. Sueltalo para hacer caer unidades sin soporte por todo el tablero. (Pulsa [F] para continuar)");
        Add(text, "You found a Slow Gravity special block. Drop it to slow falling pieces for a short time. (Press [F] to Continue)",
            "Has encontrado un bloque especial de Gravedad lenta. Sueltalo para ralentizar las piezas que caen durante un breve tiempo. (Pulsa [F] para continuar)");

        Add(text, "Welcome Overlord, my name is Lilith. I have done my best to rally the few monster I could find to assist in your conquest of the human domain. (Press [F] to Continue)",
            "Bienvenido, Overlord. Mi nombre es Lilith. He hecho lo posible por reunir los pocos monstruos que pude encontrar para ayudarte en tu conquista del dominio humano. (Pulsa [F] para continuar)");
        Add(text, "Before going into battle, we will first have to select our commander. Press the commander button in the bottom right to see who is available. (Press [F] to Continue)",
            "Antes de entrar en batalla, primero debemos elegir a nuestro comandante. Pulsa el botón de comandante en la esquina inferior derecha para ver quién está disponible. (Pulsa [F] para continuar)");
        Add(text, "We only have one Commander that we can use at the moment, but with enough gold we can get a few more. Each commander has their own special ability. The details of each ability can be seen in the Commander preview on the right. (Press [F] to Continue)",
            "Por ahora solo tenemos un comandante disponible, pero con suficiente oro podremos conseguir más. Cada comandante tiene su propia habilidad especial. Los detalles de cada habilidad aparecen en la vista previa del comandante a la derecha. (Pulsa [F] para continuar)");
        Add(text, "Since our commander is already selected lets confirm the selection at the bottom and head back to the main lobby.",
            "Como nuestro comandante ya está seleccionado, confirma la selección abajo y vuelve al vestíbulo principal.");
        Add(text, "Now we will set up our monster squadron that will go into battle with our Commander. Press the select monster button in the bottom right.",
            "Ahora prepararemos el escuadrón de monstruos que irá a la batalla con nuestro comandante. Pulsa el botón de seleccionar monstruos en la esquina inferior derecha.");
        Add(text, "As you can see we only have so many monsters to work with to creat our squadron. A Squadron must have at least two monsters and can have a maximum of 4. (Press [F] to Continue)",
            "Como puedes ver, tenemos pocos monstruos para formar el escuadrón. Un escuadrón debe tener al menos dos monstruos y puede tener un máximo de cuatro. (Pulsa [F] para continuar)");
        Add(text, "A war can hardly be fought with such pitiful numbers, but that's where I can be of assistance. (Press [F] to Continue)",
            "Difícilmente se gana una guerra con números tan pobres, pero ahí es donde puedo ayudarte. (Pulsa [F] para continuar)");
        Add(text, "I can make clones of your squadron to be sent into battle. This will allow multiple copies to be made to fill out your ranks. However, this will come with some limitations. (Press [F] to Continue)",
            "Puedo crear clones de tu escuadrón para enviarlos a la batalla. Eso permitirá crear múltiples copias para completar tus filas. Sin embargo, tendrá algunas limitaciones. (Pulsa [F] para continuar)");
        Add(text, "I will only be able to make so many copies and if we lose too many in battle we will be forced to retreat. Second, the copies will gain their own experience, but I will only be able to safely convert a small fraction of that experience from the copy back to the original monster after a campaign is finished. (Press [F] to Continue)",
            "Solo podré crear cierta cantidad de copias y, si perdemos demasiadas en batalla, tendremos que retirarnos. Además, las copias ganarán su propia experiencia, pero al terminar una campaña solo podré convertir de forma segura una pequeña parte de esa experiencia al monstruo original. (Pulsa [F] para continuar)");
        Add(text, "You can click on any monster and see a preview of it's current stats and level in the preview section on the right. Click on the arrow button in the preview section to swap between it's stats and it's passive ability description.",
            "Puedes hacer clic en cualquier monstruo para ver una vista previa de sus estadísticas y nivel actuales en la sección de la derecha. Usa el botón de flecha para alternar entre sus estadísticas y la descripción de su habilidad pasiva.");
        Add(text, "More monsters can be unlocked with gold and cosmetic skins can also be purchased once you have gained some extra funds. Now, Prress the confirm button at the bottom of the screen to lock in your team.",
            "Puedes desbloquear más monstruos con oro y comprar aspectos cosméticos cuando tengas fondos extra. Ahora pulsa el botón de confirmar en la parte inferior para fijar tu equipo.");
        Add(text, "Now you are ready to start your first campaign. Press the Start button to begin a new run.",
            "Ya estás listo para iniciar tu primera campaña. Pulsa el botón de comenzar para iniciar una nueva partida.");
        Add(text, "Now you are ready to start your first campaign. Press [F] to Continue",
            "Ya estas listo para iniciar tu primera campana. Pulsa [F] para continuar");
        Add(text, "Welcome to the battlefield. We will begin by going over a few of the basic controls and battle mechanics. (Press [F] to Continue)",
            "Bienvenido al campo de batalla. Empezaremos repasando algunos controles básicos y mecánicas de combate. (Pulsa [F] para continuar)");
        Add(text, "First, try moving your piece to the left by pressing [A].",
            "Primero, intenta mover tu pieza a la izquierda pulsando [A].");
        Add(text, "Next, try moving your piece to the right by pressing [D].",
            "Ahora intenta mover tu pieza a la derecha pulsando [D].");
        Add(text, "Next, try moving your piece down a single row by pressing [S].",
            "Ahora intenta mover tu pieza una fila hacia abajo pulsando [S].");
        Add(text, "Now, try rotating your piece counter-clockwise by pressing [Q].",
            "Ahora intenta rotar tu pieza en sentido antihorario pulsando [Q].");
        Add(text, "Now, try rotating your piece clockwise by pressing [E].",
            "Ahora intenta rotar tu pieza en sentido horario pulsando [E].");
        Add(text, "Look at the bottom of the board and you will see four cells with a bright red tint in the same shape as your current piece, that isn't a coincidence. (Press [F] to Continue)",
            "Mira la parte inferior del tablero: verás cuatro celdas con un tinte rojo brillante con la misma forma que tu pieza actual. No es coincidencia. (Pulsa [F] para continuar)");
        Add(text, "This is the landing indicator and will help you see exactly where your current piece will lock in place. A piece will lock in place automatically when it come in contact with an obstacle, another locked unit piece, or when it reaches the bottom of the board. (Press [F] to Continue)",
            "Ese es el indicador de aterrizaje y te ayuda a ver exactamente dónde se fijará tu pieza actual. Una pieza se fija automáticamente al tocar un obstáculo, otra pieza ya fijada o el fondo del tablero. (Pulsa [F] para continuar)");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar].",
            "Ahora quiero que hagas una caída rápida de tu pieza actual pulsando la [barra espaciadora].");
        Add(text, "Here you can keep track of level information such as any active level modifiers, the curent level number, how long you have been in the level, and the current gravity pulling your pieces down. (Press [F] to Continue)",
            "Aquí puedes consultar información del nivel, como modificadores activos, número de nivel actual, tiempo dentro del nivel y la gravedad que empuja tus piezas hacia abajo. (Pulsa [F] para continuar)");
        Add(text, "Here you can see a preview of the next piece that will be spawned. Knowing what will come next will help you plan out your next step. (Press [F] to Continue)",
            "Aquí puedes ver una vista previa de la siguiente pieza que aparecerá. Saber qué viene después te ayudará a planear tu siguiente movimiento. (Pulsa [F] para continuar)");
        Add(text, "Reserve Units - Loss Condition When any of your units die your current reserve will be reduced. You only have so many soldiers and when your reserve hits 0 you will lose. (Press [F] to Continue)",
            "Reserva de unidades - Condición de derrota\n\nCuando una de tus unidades muere, tu reserva actual se reduce. Solo tienes cierta cantidad de soldados y, si tu reserva llega a 0, perderás. (Pulsa [F] para continuar)");
        Add(text, "Loss Condition If a piece is locked in place above the top row of the board you will instantly lose, regardless of the number of reserve units left. Be caureful not to build to high. (Press [F] to Continue)",
            "Condición de derrota\n\nSi una pieza queda fijada por encima de la fila superior del tablero, perderás al instante sin importar cuántas unidades de reserva queden. Ten cuidado de no construir demasiado alto. (Pulsa [F] para continuar)");
        Add(text, "Win Condition Reduce the Enemy Castle's HP to zero. (Press [F] to Continue)",
            "Condición de victoria\n\nReduce los PV del castillo enemigo a cero. (Pulsa [F] para continuar)");
        Add(text, "If you need a break press [Esc]. This will pause the game and bring up the pause menu.",
            "Si necesitas un descanso, pulsa [Esc]. Esto pausará el juego y abrirá el menú de pausa.");
        Add(text, "Here you can change settings, look through the help menu, check your current modifiers, or end your run early. For now, lets close the pause menu by pressing [Esc] again.",
            "Aquí puedes cambiar ajustes, revisar el menú de ayuda, consultar tus modificadores actuales o terminar la partida antes de tiempo. Por ahora, cierra el menú de pausa pulsando [Esc] otra vez.");
        Add(text, "Now lets try filling an entire row on the board next to launch an attack. (Press [F] to Continue)",
            "Ahora intentemos llenar una fila completa del tablero para lanzar un ataque. (Pulsa [F] para continuar)");
    }

    static void AddCharacterText(Dictionary<string, string> text)
    {
        Add(text, "Charge!", "¡Carga!");
        Add(text, "Send all units from the bottom 3 rows to attack immediatley, no need for fully formed lines. Damage based on the number of units cleared.",
            "Envía a atacar inmediatamente a todas las unidades de las 3 filas inferiores, sin necesidad de formar líneas completas. El daño depende de la cantidad de unidades limpiadas.");
        Add(text, "Time Shift", "Cambio temporal");
        Add(text, "Reduce unit fall speed by 1/3 of its current speed fo 15 seconds.",
            "Reduce la velocidad de caída de las unidades en un tercio de su velocidad actual durante 15 segundos.");
        Add(text, "Natures Embrace", "Abrazo de la naturaleza");
        Add(text, "Heal all units on the board back to full health including those that have died.",
            "Cura por completo a todas las unidades del tablero, incluidas las que han muerto.");
        Add(text, "Grock SMASH!", "¡Grock APLASTA!");
        Add(text, "Let loose a war cry doubling all units on the boards health and attack damage for 10 seconds.",
            "Desata un grito de guerra que duplica la salud y el daño de ataque de todas las unidades del tablero durante 10 segundos.");
        Add(text, "Immutable Bulwark", "Baluarte inmutable");
        Add(text, "All units become immune to damage for 12 seconds.",
            "Todas las unidades se vuelven inmunes al daño durante 12 segundos.");
    }

    static void AddHelpTopicText(Dictionary<string, string> text)
    {
        Add(text, "Boss Abilities", "Habilidades de jefe");
        Add(text, "Controls", "Controles");
        Add(text, "Floor Effects", "Efectos de suelo");
        Add(text, "Game Mechanics", "Mecánicas de juego");
        Add(text, "Obstacles", "Obstáculos");
        Add(text, "Special Blocks", "Bloques especiales");
        Add(text, "Traps", "Trampas");
        Add(text, "Other", "Otros");

        Add(text, "Full Board Blast", "Explosión de tablero completo");
        Add(text, "The boss will target all monster units set on the board dealing a small amount of damage to each one.",
            "El jefe apuntará a todas las unidades monstruo colocadas en el tablero e infligirá una pequeña cantidad de daño a cada una.");
        Add(text, "Increased Gravity", "Gravedad aumentada");
        Add(text, "The boss will temporarily increase gravity causing blocks to fall significantly faster for a set period of time.",
            "El jefe aumentará temporalmente la gravedad, haciendo que los bloques caigan mucho más rápido durante un tiempo determinado.");
        Add(text, "Invulnerable", "Invulnerable");
        Add(text, "The boss will temporarily become invulnerable. While invulnerable, the boss will take no damage from any sources.",
            "El jefe se volverá invulnerable temporalmente. Mientras sea invulnerable, no recibirá daño de ninguna fuente.");
        Add(text, "Lightning Strike", "Impacto de relámpago");
        Add(text, "The boss will target 1-3 individual cells with lightning bolts that will deal siginficant damage to any monster unit in that cell. Afterwards that cell will have a lightning floor effect that does continuous damage to any monster unit that occupies the tile.",
            "El jefe apuntará a entre 1 y 3 celdas individuales con relámpagos que infligen daño significativo a cualquier unidad monstruo en esa celda. Después, la celda tendrá un efecto de suelo eléctrico que causa daño continuo a cualquier unidad monstruo que la ocupe.");
        Add(text, "Magic Explosive", "Explosivo mágico");
        Add(text, "The boss will spawn a single magical explosive as low on the board as possible. This explosive will detonate after 15 seconds and can only be safley removed by clearing the row it occupies.",
            "El jefe hará aparecer un explosivo mágico tan abajo en el tablero como sea posible. Detonará después de 15 segundos y solo puede retirarse de forma segura limpiando la fila que ocupa.");
        Add(text, "Magic Shield", "Escudo mágico");
        Add(text, "The boss will spawn multiple magical pylon obstacles on the board. As long as the pylons remain on the board the boss will take 50% reduced damage from all sources.",
            "El jefe hará aparecer varios pilones mágicos en el tablero. Mientras permanezcan en el tablero, el jefe recibirá un 50% menos de daño de todas las fuentes.");
        Add(text, "Spawn FE's", "Generar efectos de suelo");
        Add(text, "The boss will spawn multiple floor effects on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. Possible floor effect types are posion, fire, and lightning.",
            "El jefe hará aparecer múltiples efectos de suelo. Pueden aparecer dispersos de forma individual o en patrones de 2x2, 1x4 o 4x1. Los tipos posibles son veneno, fuego y relámpago.");
        Add(text, "Spawn Obstacles", "Generar obstáculos");
        Add(text, "The boss will spawn multiple obstacles on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns stone obstacles.",
            "El jefe hará aparecer múltiples obstáculos en el tablero. Pueden aparecer dispersos de forma individual o en patrones de 2x2, 1x4 o 4x1. Esta habilidad solo genera obstáculos de piedra.");
        Add(text, "Spawn Traps", "Generar trampas");
        Add(text, "The boss will spawn multiple traps on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns spike traps.",
            "El jefe hará aparecer múltiples trampas en el tablero. Pueden aparecer dispersas de forma individual o en patrones de 2x2, 1x4 o 4x1. Esta habilidad solo genera trampas de pinchos.");
        Add(text, "Row Blast", "Explosión de filas");
        Add(text, "The boss will target the top three rows the player has set monster units. All monster units in the selected rows will recieve moderate damage from this attack.",
            "El jefe apuntará a las tres filas superiores donde el jugador haya colocado unidades monstruo. Todas las unidades monstruo en las filas seleccionadas recibirán daño moderado.");
        Add(text, "Activate Special", "Activar especial");
        Add(text, "R - Activates the players special ability when the special gauge is charged to 100%",
            "R - Activa la habilidad especial del jugador cuando el medidor especial está cargado al 100%.");
        Add(text, "Movement", "Movimiento");
        Add(text, "A - shifts the active piece one column to the left. S - shifts the active piece one row down. D - shifts the active piece one column to the right.",
            "A - desplaza la pieza activa una columna a la izquierda.\n\nS - desplaza la pieza activa una fila hacia abajo.\n\nD - desplaza la pieza activa una columna a la derecha.");
        Add(text, "Pause", "Pausa");
        Add(text, "Escape - Will open the pause menu and pause all gameplay functions. Presseing escape while the pause menu is open will close the pause menu and resume gameplay.",
            "Escape - Abre el menú de pausa y pausa todas las funciones de juego. Pulsar Escape mientras el menú de pausa está abierto cerrará el menú y reanudará la partida.");
        Add(text, "Quick Drop", "Caída rápida");
        Add(text, "Pressing spacebar will quick drop your active piece, setting it in place immediatley.",
            "Pulsar la barra espaciadora hará caer rápidamente tu pieza activa y la fijará en su lugar de inmediato.");
        Add(text, "Rotation", "Rotación");
        Add(text, "Q - Rotate the active piece 90 degrees counter-clockwise. E - Rotate the active piece 90 degrees clockwise.",
            "Q - Rota la pieza activa 90 grados en sentido antihorario.\n\nE - Rota la pieza activa 90 grados en sentido horario.");
        Add(text, "Fire", "Fuego");
        Add(text, "Any monster unit set in a cell with the fire floor effect will take constant damage. The damage is realtivley low but occurs often.",
            "Cualquier unidad monstruo colocada en una celda con efecto de fuego recibirá daño constante. El daño es relativamente bajo, pero ocurre a menudo.");
        Add(text, "Lightning", "Relámpago");
        Add(text, "Any monster unit set in a cell with the lightning floor effect will take constant damage. The damage is moderate but the floor effect will disappears after a period of time.",
            "Cualquier unidad monstruo colocada en una celda con efecto de relámpago recibirá daño constante. El daño es moderado, pero el efecto desaparecerá después de un tiempo.");
        Add(text, "Poison", "Veneno");
        Add(text, "Any monster unit set in a cell with the poison floor effect will take continuous damage. The damage is realtivley low but occurs often.",
            "Cualquier unidad monstruo colocada en una celda con efecto de veneno recibirá daño continuo. El daño es relativamente bajo, pero ocurre a menudo.");
        Add(text, "Attack Units", "Unidades de ataque");
        Add(text, "Attack units tend to have a higher attack stat than other unit types. They are best used to deal maximum damage to enemies to end levels more quickly. They cannot heal and tend to have average health stats.",
            "Las unidades de ataque suelen tener más ataque que otros tipos de unidad. Son ideales para infligir el máximo daño a los enemigos y terminar niveles más rápido. No pueden curar y suelen tener salud media.");
        Add(text, "Currency", "Oro");
        Add(text, "The player can gain currency from completing levels and rarely from clearing rows. Currency can be used to purchase various cosmetics, monster units, player characters, and permanent buffs to improve future runs. Your current currency can be found in the top right of the screen.",
            "El jugador puede ganar oro al completar niveles y, rara vez, al limpiar filas. El oro sirve para comprar cosméticos, unidades monstruo, comandantes y mejoras permanentes para futuras partidas. Tu oro actual aparece en la esquina superior derecha de la pantalla.");
        Add(text, "Enemy Attack", "Ataque enemigo");
        Add(text, "The enemy castle will send constant attacks at the players monster units in an attempt to cull them. The enemies attacks will increase in power and frequency at higher levels.",
            "El castillo enemigo lanzará ataques constantes contra las unidades monstruo del jugador para reducir sus filas. Los ataques enemigos aumentan en poder y frecuencia en niveles más altos.");
        Add(text, "Healing Units", "Unidades sanadoras");
        Add(text, "Some units can heal other monster units to varying degrees. They tend to have much lower health and attack stats.",
            "Algunas unidades pueden curar a otras unidades monstruo en distintas cantidades. Suelen tener salud y ataque mucho más bajos.");
        Add(text, "Landing Hint", "Indicador de aterrizaje");
        Add(text, "A red tint overlay appears in the location where the current active piece will fall.",
            "Aparece una superposición roja en el lugar donde caerá la pieza activa actual.");
        Add(text, "Loss Condition 1", "Condición de derrota 1");
        Add(text, "If the unit reserve reaches 0, the run will end with your loss! The unit reserve is reduced by one for every monster unit that dies on the board.",
            "Si la reserva de unidades llega a 0, la partida terminará en derrota. La reserva se reduce en uno por cada unidad monstruo que muere en el tablero.");
        Add(text, "Loss Condition 2", "Condición de derrota 2");
        Add(text, "If a piece is set above the top row of the grid, the run will end with a loss!",
            "Si una pieza se fija por encima de la fila superior de la cuadrícula, la partida terminará en derrota.");
        Add(text, "Monster Units", "Unidades monstruo");
        Add(text, "Choose from multiple units to make up your warband. Each unit has their own individual stats that will make them more suitable for different roles. Some units have balanced stats and others are more specialized.",
            "Elige entre varias unidades para formar tu banda de guerra. Cada unidad tiene estadísticas propias que la hacen más adecuada para distintos roles. Algunas tienen estadísticas equilibradas y otras son más especializadas.");
        Add(text, "Commander", "Comandante");
        Add(text, "Each Commander has their own unquie special ability that can be used in battle. New Commanders can be unlocked and set from the main menu.",
            "Cada comandante tiene una habilidad especial única que puede usarse en batalla. Puedes desbloquear y elegir nuevos comandantes desde el menú principal.");
        Add(text, "Row Clear", "Limpieza de fila");
        Add(text, "Fill each grid cell in a row to launch an attack. Monster Units and Obstacles count as a filled cell. Floor effects and traps do not count as a filled cell. Monster units in the cleared row contribute their attack stats for damage calculation and partially fill the player's special gauge.",
            "Llena cada celda de una fila para lanzar un ataque. Las unidades monstruo y los obstáculos cuentan como celdas llenas. Los efectos de suelo y las trampas no cuentan. Las unidades monstruo de la fila limpiada aportan su ataque al cálculo de daño y llenan parcialmente el medidor especial del jugador.");
        Add(text, "Run Buffs", "Mejoras de partida");
        Add(text, "After succesfully completing a level you will be given three random buffs to choose from to enhance your current run. All buff modifiers will be reset when the run ends.",
            "Después de completar un nivel, recibirás tres mejoras aleatorias para potenciar tu partida actual. Todas las mejoras se reiniciarán cuando termine la partida.");
        Add(text, "Run Debuffs", "Desventajas de partida");
        Add(text, "After succesfully completing a level you will be given three random debuffs to choose from to increase the difficulty of your current run. All debuff modifiers will be reset when the run ends.",
            "Después de completar un nivel, recibirás tres desventajas aleatorias para aumentar la dificultad de tu partida actual. Todas las desventajas se reiniciarán cuando termine la partida.");
        Add(text, "Score", "Puntuación");
        Add(text, "Clearing rows will earn the player points and add to their overall score. This can be used to measure the success of a run and compete with other players.",
            "Limpiar filas otorga puntos al jugador y aumenta su puntuación total. Esto sirve para medir el éxito de una partida y competir con otros jugadores.");
        Add(text, "Shop Buffs", "Mejoras de tienda");
        Add(text, "The shop offers different buffs that can be purchased. Each buff is permanent and can be purchased multiple times. Each purchase will increase the price of the buffs next purchase level.",
            "La tienda ofrece distintas mejoras que se pueden comprar. Cada mejora es permanente y puede comprarse varias veces. Cada compra aumentará el precio del siguiente nivel de esa mejora.");
        Add(text, "Special Gauge", "Medidor especial");
        Add(text, "Fills with every row cleared. When it reaches 100% you can use your Commander's unique special ability, resetting the special gauge back 0%.",
            "Se llena con cada fila limpiada. Al llegar al 100%, puedes usar la habilidad especial única de tu comandante, lo que reinicia el medidor especial a 0%.");
        Add(text, "Tank Units", "Unidades tanque");
        Add(text, "Tank units tend to have significantly more health than other unit types. They are best used to outlast enemy attacks and protect weaker units. They cannot heal and tend to have low attack stats.",
            "Las unidades tanque suelen tener mucha más salud que otros tipos de unidad. Son ideales para resistir ataques enemigos y proteger unidades más débiles. No pueden curar y suelen tener bajo ataque.");
        Add(text, "Unit Death", "Muerte de unidad");
        Add(text, "When a monster units health drops to zero it dies. Dead units do not contribute their stats to an attack when their row is cleared or help fill up the players Special Gauge.",
            "Cuando la salud de una unidad monstruo llega a cero, muere. Las unidades muertas no aportan estadísticas al ataque cuando se limpia su fila ni ayudan a llenar el medidor especial del jugador.");
        Add(text, "Unit Reserve", "Reserva de unidades");
        Add(text, "When starting a new run the player will have a set limit of how many units they can afford to lose throughout the run. When a unit dies the reserve bar will be decreased. Suuccesfully completing a level will award the player up to 5 reinforcements for each victory up to the max unit reserve.",
            "Al iniciar una nueva partida, el jugador tendrá un límite de unidades que puede permitirse perder durante la partida. Cuando una unidad muere, la barra de reserva disminuye. Completar un nivel con éxito otorga hasta 5 refuerzos por victoria, hasta el máximo de reserva de unidades.");
        Add(text, "Victory Condition", "Condición de victoria");
        Add(text, "Reduce the health of the enemy castle by clearing rows. When the enemy castle reaches 0 health, you win!",
            "Reduce la salud del castillo enemigo limpiando filas. Cuando el castillo enemigo llegue a 0 de salud, ganas.");
        Add(text, "Explosive", "Explosivo");
        Add(text, "The explosive obstacle will explode after a period of time killing all surrounding monster units. Can be safley disposed of by clearing its row. When safley disposed of it add 25 damage to that row clears attack. CAUTION: Using a bomb or lightning special block on the explosive will cause it to detonate.",
            "El obstáculo explosivo explotará después de un tiempo y matará a todas las unidades monstruo cercanas. Puede retirarse de forma segura limpiando su fila. Si se retira de forma segura, añade 25 de daño al ataque de esa fila.\n\nPRECAUCIÓN: usar un bloque especial de bomba o relámpago sobre el explosivo hará que detone.");
        Add(text, "Magic Pylon", "Pilón mágico");
        Add(text, "When magic pylon obstacles are on the board enemies will take 50% reduced damage from all sources. Magic pylon can bee destroyed by clearing the row they occupy or using bomb and lightning special blocks.",
            "Cuando hay pilones mágicos en el tablero, los enemigos reciben un 50% menos de daño de todas las fuentes. Los pilones mágicos pueden destruirse limpiando la fila que ocupan o usando bloques especiales de bomba y relámpago.");
        Add(text, "Stone", "Piedra");
        Add(text, "Stone obstacles can be spawned at the beggining of a level or by the boss. A lightning special block or clearing a row containing a stone obstacle will deal one damage to it. Stone obstacle need to be damaged 3 times to be removed. Exception: Using a bomb special block will instantly destroy a stone obstacle.",
            "Los obstáculos de piedra pueden aparecer al inicio de un nivel o ser invocados por el jefe. Un bloque especial de relámpago o limpiar una fila con una piedra le inflige un punto de daño. Una piedra necesita recibir 3 daños para ser eliminada.\nExcepción: un bloque especial de bomba destruye instantáneamente una piedra.");
        Add(text, "Bomb", "Bomba");
        Add(text, "The bomb special will detonate immediatley when set. All blocks in the surrounding tiles of its blast will be destroyed!",
            "El especial de bomba detonará inmediatamente al colocarse. Todos los bloques en las casillas alrededor de la explosión serán destruidos.");
        Add(text, "Death", "Muerte");
        Add(text, "The Death special will activate immediatley when set on top of a monster unit. All monster units of the same type will be safley removed from the board with out decreasing your unit reserve.",
            "El especial de Muerte se activará inmediatamente al colocarse sobre una unidad monstruo. Todas las unidades monstruo del mismo tipo serán retiradas del tablero de forma segura sin reducir tu reserva de unidades.");
        Add(text, "Earthquake", "Terremoto");
        Add(text, "The Earthquake special will activate immediatley when set. All blocks on the board will be dropped if not being supported by another tile beneath them. This effects obstacles as well that may otherwise not be able to be moved by other means.",
            "El especial de Terremoto se activará inmediatamente al colocarse. Todos los bloques del tablero caerán si no están sostenidos por otra casilla debajo. Esto también afecta a obstáculos que quizá no podrían moverse por otros medios.");
        Add(text, "Lightning Bolt", "Rayo");
        Add(text, "The Lightning Bolt special will activate immediatley when set. All monster units and traps will be destroyed in that column. Stone obstacles will take partial damage if they are in the affected area.",
            "El especial de Rayo se activará inmediatamente al colocarse. Todas las unidades monstruo y trampas de esa columna serán destruidas. Los obstáculos de piedra recibirán daño parcial si están en el área afectada.");
        Add(text, "Slow Gravity", "Gravedad lenta");
        Add(text, "The Slow Gravity special block will activate immedialtey upon being set. It will significantly reduce the speed at which pieces fall and the how quickly gravity increases over time.",
            "El bloque especial de Gravedad lenta se activará inmediatamente al colocarse. Reducirá significativamente la velocidad a la que caen las piezas y la rapidez con la que aumenta la gravedad con el tiempo.");
        Add(text, "Spike Trap", "Trampa de pinchos");
        Add(text, "Spike traps will deal a high amount of damage to any monster unit that is set on the trapped cell when they are placed. The only way to destroyed spike traps are with the lightning special block.",
            "Las trampas de pinchos infligen mucho daño a cualquier unidad monstruo que se coloque en la celda atrapada. La única forma de destruirlas es con el bloque especial de relámpago.");
    }

    static void AddLevelModifierText(Dictionary<string, string> text)
    {
        Add(text, "Spin to Win", "Girar para ganar");
        Add(text, "Active pieces will continuously rotate until set in place. Manual rotation will be locked.",
            "Las piezas activas rotarán continuamente hasta fijarse en su lugar. La rotación manual estará bloqueada.");
        Add(text, "Timing is Everything", "El tiempo lo es todo");
        Add(text, "Active pieces will continuously shift horizontally back and and forth across the board. Manual shifting will be locked.",
            "Las piezas activas se desplazarán horizontalmente de un lado a otro del tablero. El desplazamiento manual estará bloqueado.");
        Add(text, "Go Big or Go Home", "A lo grande o nada");
        Add(text, "Damage is significantly reduced for all attacks when the combo streak is less than 3.",
            "El daño de todos los ataques se reduce significativamente cuando la racha de combo es inferior a 3.");
        Add(text, "Break Out The Big Guns", "Saca la artillería pesada");
        Add(text, "The enemy has fortified their position. A combo attack at 4 or higher will be required to remove each enemy shield. Damage dealt to the enemy while shielded is significantly reduced.",
            "El enemigo ha fortificado su posición. Se requiere un ataque de combo 4 o superior para eliminar cada escudo enemigo. El daño infligido al enemigo mientras está protegido se reduce significativamente.");
        Add(text, "Contagion Outbreak", "Brote de contagio");
        Add(text, "Disease has begun to spread through the ranks. Close proximitiy has a chance to transfer from afflicted units to healthy units and will spread with certainity on the death of infected units.",
            "La enfermedad ha empezado a propagarse entre las filas. La cercanía puede transferirla de unidades afectadas a unidades sanas y se propagará con certeza al morir unidades infectadas.");
        Add(text, "Double Down", "Doble apuesta");
        Add(text, "All damage taken and dealt will be doubled!",
            "Todo el daño recibido e infligido se duplicará.");
        Add(text, "Exploding Corpses", "Cadáveres explosivos");
        Add(text, "Units will explode on death dealing damage to all surrounding units. Damage dealt is a percentage based off of the max health of the exploding unit.",
            "Las unidades explotarán al morir e infligirán daño a todas las unidades cercanas. El daño es un porcentaje de la salud máxima de la unidad que explota.");
        Add(text, "Tis A Flesh Wound", "Solo es una herida superficial");
        Add(text, "All ally units will start with half health.",
            "Todas las unidades aliadas empezarán con la mitad de salud.");
        Add(text, "Rations Running Low", "Las raciones escasean");
        Add(text, "Rations have begun to run out. Ally units have begun to starve trying to share the remaining rations. Units will take continuous damage proportional to the number of current reserve units.",
            "Las raciones empiezan a agotarse. Las unidades aliadas pasan hambre intentando compartir lo que queda. Las unidades recibirán daño continuo proporcional al número actual de unidades en reserva.");
        Add(text, "Overgrowth", "Sobrecrecimiento");
        Add(text, "Overgrowth has taken over the area consuming tiles and monsters. Overgrowth becomes more resilent to destruction once fully grown. Defeat the enemy before your army becomes mulch!",
            "El sobrecrecimiento ha invadido el área, consumiendo casillas y monstruos. Se vuelve más resistente a la destrucción cuando madura por completo. Derrota al enemigo antes de que tu ejército termine como abono.");
        Add(text, "No Retreat", "Sin retirada");
        Add(text, "Enemy ambush will cut off any retreat. Rows will slowly fill with enemy units progressivley limiting space to maneuver.",
            "Una emboscada enemiga cortará toda retirada. Las filas se llenarán lentamente con unidades enemigas, limitando progresivamente el espacio para maniobrar.");
        Add(text, "Soul Link", "Vínculo de almas");
        Add(text, "All four units in a piece share a single health pool.",
            "Las cuatro unidades de una pieza comparten una sola reserva de salud.");
        Add(text, "Back to the Basics", "Vuelta a lo básico");
        Add(text, "Special blocks will not spawn.",
            "No aparecerán bloques especiales.");
        Add(text, "Commander Special Lock", "Bloqueo del especial del comandante");
        Add(text, "Special ability gauge will be set to zero and locked.",
            "El medidor de habilidad especial se pondrá en cero y quedará bloqueado.");
        Add(text, "Catastrophic Storm", "Tormenta catastrófica");
        Add(text, "An unrelenting storm has arrived and will blast the area with devestating lightning strikes.",
            "Ha llegado una tormenta implacable que azotará el área con relámpagos devastadores.");
        Add(text, "Miasma Marsh", "Marisma de miasma");
        Add(text, "The battlefield has shifted to the nearby marshes where deadly miasma drifts across the terrain.",
            "El campo de batalla se ha desplazado a las marismas cercanas, donde un miasma mortal se extiende por el terreno.");
    }

    static void AddRunModifierNames(Dictionary<string, string> text)
    {
        Add(text, "All Special Gain Down", "Menos ganancia especial total");
        Add(text, "All Special Gain Up", "Más ganancia especial total");
        Add(text, "ATK Down", "Ataque reducido");
        Add(text, "ATK Up", "Ataque aumentado");
        Add(text, "Currency Drop Up", "Más aparición de oro");
        Add(text, "Debuffs Only", "Solo desventajas");
        Add(text, "Enemy ATK Down", "Ataque enemigo reducido");
        Add(text, "Enemy ATK SPD Down", "Velocidad de ataque enemiga reducida");
        Add(text, "Enemy ATK SPD Up", "Velocidad de ataque enemiga aumentada");
        Add(text, "Enemy ATK Up", "Ataque enemigo aumentado");
        Add(text, "Enemy HP Up", "PV enemigos aumentados");
        Add(text, "Gravity Accel SPD Down", "Aceleración de gravedad reducida");
        Add(text, "Gravity Accel SPD Up", "Aceleración de gravedad aumentada");
        Add(text, "Gravity Base SPD Down", "Velocidad base de gravedad reducida");
        Add(text, "Gravity SPD Up", "Velocidad de gravedad aumentada");
        Add(text, "Healing Range Up", "Alcance de curación aumentado");
        Add(text, "Healing STR Up", "Poder de curación aumentado");
        Add(text, "HP Down", "PV reducidos");
        Add(text, "HP Up", "PV aumentados");
        Add(text, "Luck Up", "Suerte aumentada");
        Add(text, "Misfortune Up", "Infortunio aumentado");
        Add(text, "No Landing Indicator", "Sin indicador de aterrizaje");
        Add(text, "No Next Block Preview", "Sin vista previa de bloque");
        Add(text, "No Reinforcements", "Sin refuerzos");
        Add(text, "Reinforcements Down", "Refuerzos reducidos");
        Add(text, "Reinforcements Up", "Refuerzos aumentados");
        Add(text, "Special Block Down", "Bloques especiales reducidos");
        Add(text, "Special Block Up", "Bloques especiales aumentados");
        Add(text, "Special Gain Stat Down", "Ganancia especial por monstruo reducida");
        Add(text, "Special Gauge Stat Up", "Ganancia especial por monstruo aumentada");
        Add(text, "Stone Buff Drop Down", "Menos mejoras de piedra");
        Add(text, "Stone Buff Drop Up", "Más mejoras de piedra");
        Add(text, "Unit Reserve Down", "Reserva de unidades reducida");
        Add(text, "Unit Reserve Up", "Reserva de unidades aumentada");
        Add(text, "Win Currency Down", "Oro por victoria reducido");
        Add(text, "Win Currency Up", "Oro por victoria aumentado");
    }

    static void AddRunModifierFixedDescriptions(Dictionary<string, string> text)
    {
        Add(text, "A red tinted outline will no longer be shown where your pieces will land.",
            "Ya no se mostrará una silueta roja donde aterrizarán tus piezas.");
        Add(text, "The next block will no longer be shown.",
            "Ya no se mostrará el siguiente bloque.");
        Add(text, "Reinforcements will no longer arrive after winning a round.",
            "Ya no llegarán refuerzos después de ganar una ronda.");
        Add(text, "Stone obstacles no longer have a chance of dropping buffs and now only drop debuffs. Debuff drop chance is the same as prior buff drop chance.",
            "Los obstáculos de piedra ya no pueden soltar mejoras y ahora solo sueltan desventajas. La probabilidad de soltar desventajas es igual a la probabilidad previa de soltar mejoras.");
        Add(text, "Double the amount of currency gained occasionally when clearing lines.",
            "Duplica la cantidad de oro ganada ocasionalmente al limpiar líneas.");
        Add(text, "Triple the amount of currency gained occasionally when clearing lines.",
            "Triplica la cantidad de oro ganada ocasionalmente al limpiar líneas.");
        Add(text, "Qunituple the amount of currency gained occasionally when clearing lines.",
            "Quintuplica la cantidad de oro ganada ocasionalmente al limpiar líneas.");
        Add(text, "Increases the healing range of all friendly monsters by 1.",
            "Aumenta en 1 el alcance de curación de todos los monstruos aliados.");
        Add(text, "Increases the healing range of all friendly monsters by 2.",
            "Aumenta en 2 el alcance de curación de todos los monstruos aliados.");
        Add(text, "Increases the healing range of all friendly monsters by 3.",
            "Aumenta en 3 el alcance de curación de todos los monstruos aliados.");
    }

    static void AddStatText(Dictionary<string, string> text)
    {
        Add(text, "Lines Cleared:", "Líneas limpiadas:");
        Add(text, "Special Used:", "Especial usado:");
        Add(text, "Obstacles Destroyed:", "Obstáculos destruidos:");
        Add(text, "Highest Combo:", "Combo más alto:");
        Add(text, "Highest Single Attack:", "Ataque individual más alto:");
        Add(text, "Units Died:", "Unidades muertas:");
        Add(text, "Units Healed:", "Unidades curadas:");
        Add(text, "Total Damage Dealt:", "Daño total infligido:");
        Add(text, "Clear Time:", "Tiempo de limpieza:");
        Add(text, "Final Score:", "Puntuación final:");
        Add(text, "Lines", "Líneas");
        Add(text, "Times", "Veces");
        Add(text, "Obstacles", "Obstáculos");
        Add(text, "Damage", "Daño");
        Add(text, "Units", "Unidades");
        Add(text, "Health", "Salud");
        Add(text, "Level {0}", "Nivel {0}");
        Add(text, "{0} of {1} {2} discovered. Total codex unlocked {3}%",
            "{0} de {1} {2} descubiertos. Códice total desbloqueado: {3}%");
        Add(text, "Buffs", "Mejoras");
        Add(text, "Debuffs", "Desventajas");
        Add(text, "Level Modifiers", "Modificadores de nivel");
    }

    static bool TryTranslateRunModifierDescription(string lookupKey, out string spanishText)
    {
        spanishText = null;

        for (int i = 0; i < DegreePrefixes.Length; i++)
        {
            string englishPrefix = DegreePrefixes[i].English;
            if (!lookupKey.StartsWith(englishPrefix + " ", StringComparison.OrdinalIgnoreCase))
                continue;

            string remainder = lookupKey.Substring(englishPrefix.Length + 1).Trim();
            if (TryGetRunModifierTemplate(remainder, out string template))
            {
                spanishText = string.Format(template, DegreePrefixes[i].Spanish);
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

    static bool TryTranslateLabelValueLines(string englishText, out string spanishText)
    {
        spanishText = null;

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

        spanishText = string.Join("\n", lines);
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

            translatedLine = leading + LinePrefixes[i].Spanish + trimmed.Substring(englishPrefix.Length);
            return true;
        }

        return false;
    }

    static void Add(Dictionary<string, string> text, string english, string spanish)
    {
        string key = NormalizeLookupKey(english);
        if (!string.IsNullOrEmpty(key))
            text[key] = spanish;
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

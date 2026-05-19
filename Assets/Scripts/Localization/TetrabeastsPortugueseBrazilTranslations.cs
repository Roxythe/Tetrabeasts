using System;
using System.Collections.Generic;
using System.Text;

public static class TetrabeastsPortugueseBrazilTranslations
{
    static readonly Dictionary<string, string> ExactText = BuildExactText();

    static readonly Dictionary<string, string> RunModifierTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["decrease the amount of special gained from all sources."] = "{0} reduz a quantidade de energia especial obtida de todas as fontes.",
        ["decreases the amount of special gained from all sources."] = "{0} reduz a quantidade de energia especial obtida de todas as fontes.",
        ["increase the amount of special gained from all sources."] = "{0} aumenta a quantidade de energia especial obtida de todas as fontes.",
        ["increases the amount of special gained from all sources."] = "{0} aumenta a quantidade de energia especial obtida de todas as fontes.",
        ["decrease the special gained from each monster."] = "{0} reduz a energia especial obtida de cada monstro.",
        ["decreases the special gained from each monster."] = "{0} reduz a energia especial obtida de cada monstro.",
        ["increase the special gained from each monster."] = "{0} aumenta a energia especial obtida de cada monstro.",
        ["increases the special gained from each monster."] = "{0} aumenta a energia especial obtida de cada monstro.",
        ["decreases the attack value for all monsters in your current roster."] = "{0} reduz o ataque de todos os monstros no seu esquadrão atual.",
        ["decrease the attack value for all monsters in your current roster."] = "{0} reduz o ataque de todos os monstros no seu esquadrão atual.",
        ["increases the attack value for all monsters in your current roster."] = "{0} aumenta o ataque de todos os monstros no seu esquadrão atual.",
        ["increase the attack value for all monsters in your current roster."] = "{0} aumenta o ataque de todos os monstros no seu esquadrão atual.",
        ["decrease the damage of enemy projectiles."] = "{0} reduz o dano dos projéteis inimigos.",
        ["decreases the damage of enemy projectiles."] = "{0} reduz o dano dos projéteis inimigos.",
        ["incecrease the damage of enemy projectiles."] = "{0} aumenta o dano dos projéteis inimigos.",
        ["incecreases the damage of enemy projectiles."] = "{0} aumenta o dano dos projéteis inimigos.",
        ["increase the damage of enemy projectiles."] = "{0} aumenta o dano dos projéteis inimigos.",
        ["increases the damage of enemy projectiles."] = "{0} aumenta o dano dos projéteis inimigos.",
        ["increases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} aumenta o tempo entre ataques inimigos, sem contar recargas de habilidades de chefe.",
        ["increase the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} aumenta o tempo entre ataques inimigos, sem contar recargas de habilidades de chefe.",
        ["decreases the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} reduz o tempo entre ataques inimigos, sem contar recargas de habilidades de chefe.",
        ["decrease the amount of time between enemy attacks (boss ability cooldowns excluded)."] = "{0} reduz o tempo entre ataques inimigos, sem contar recargas de habilidades de chefe.",
        ["increase the hit points of all future enemy fortifications."] = "{0} aumenta os pontos de vida de todas as futuras fortificações inimigas.",
        ["increases the hit points of all future enemy fortifications."] = "{0} aumenta os pontos de vida de todas as futuras fortificações inimigas.",
        ["decreases the rate falling speed builds up over time for blocks."] = "{0} reduz a taxa em que a velocidade de queda aumenta com o tempo.",
        ["decrease the rate falling speed builds up over time for blocks."] = "{0} reduz a taxa em que a velocidade de queda aumenta com o tempo.",
        ["increases the rate falling speed builds up over time for blocks."] = "{0} aumenta a taxa em que a velocidade de queda aumenta com o tempo.",
        ["increase the rate falling speed builds up over time for blocks."] = "{0} aumenta a taxa em que a velocidade de queda aumenta com o tempo.",
        ["decreases the initial falling speed of blocks."] = "{0} reduz a velocidade inicial de queda dos blocos.",
        ["decrease the initial falling speed of blocks."] = "{0} reduz a velocidade inicial de queda dos blocos.",
        ["increases the initial falling speed of blocks."] = "{0} aumenta a velocidade inicial de queda dos blocos.",
        ["increase the initial falling speed of blocks."] = "{0} aumenta a velocidade inicial de queda dos blocos.",
        ["increase the healing power of all friendly monsters."] = "{0} aumenta o poder de cura de todos os monstros aliados.",
        ["increases the healing power of all friendly monsters."] = "{0} aumenta o poder de cura de todos os monstros aliados.",
        ["decrease friendly monster pieces maximum hit points."] = "{0} reduz os pontos de vida máximos das peças de monstros aliados.",
        ["decreases friendly monster pieces maximum hit points."] = "{0} reduz os pontos de vida máximos das peças de monstros aliados.",
        ["increase friendly monster pieces maximum hit points."] = "{0} aumenta os pontos de vida máximos das peças de monstros aliados.",
        ["increases friendly monster pieces maximum hit points."] = "{0} aumenta os pontos de vida máximos das peças de monstros aliados.",
        ["increase luck raising the chance of getting higher rarity buffs."] = "{0} aumenta a sorte, elevando a chance de obter melhorias de raridade maior.",
        ["increases luck raising the chance of getting higher rarity buffs."] = "{0} aumenta a sorte, elevando a chance de obter melhorias de raridade maior.",
        ["increases the likelihood of finding higher rarity debuffs."] = "{0} aumenta a chance de encontrar penalidades de raridade maior.",
        ["increase the likelihood of finding higher rarity debuffs."] = "{0} aumenta a chance de encontrar penalidades de raridade maior.",
        ["decrease the number of reinforcement units added after winning a round."] = "{0} reduz a quantidade de reforços adicionados após vencer uma rodada.",
        ["decreases the number of reinforcement units added after winning a round."] = "{0} reduz a quantidade de reforços adicionados após vencer uma rodada.",
        ["increase the number of reinforcement units added after winning a round."] = "{0} aumenta a quantidade de reforços adicionados após vencer uma rodada.",
        ["increases the number of reinforcement units added after winning a round."] = "{0} aumenta a quantidade de reforços adicionados após vencer uma rodada.",
        ["decreases the chance of special blocks appearing."] = "{0} reduz a chance de blocos especiais aparecerem.",
        ["decrease the chance of special blocks appearing."] = "{0} reduz a chance de blocos especiais aparecerem.",
        ["incecreases the chance of special blocks appearing."] = "{0} aumenta a chance de blocos especiais aparecerem.",
        ["incecrease the chance of special blocks appearing."] = "{0} aumenta a chance de blocos especiais aparecerem.",
        ["increases the chance of special blocks appearing."] = "{0} aumenta a chance de blocos especiais aparecerem.",
        ["increase the chance of special blocks appearing."] = "{0} aumenta a chance de blocos especiais aparecerem.",
        ["decrease the chance a buff will drop from a stone obstacle."] = "{0} reduz a chance de uma melhoria cair de um obstáculo de pedra.",
        ["decreases the chance a buff will drop from a stone obstacle."] = "{0} reduz a chance de uma melhoria cair de um obstáculo de pedra.",
        ["increase the chance a buff will drop from a stone obstacle."] = "{0} aumenta a chance de uma melhoria cair de um obstáculo de pedra.",
        ["increases the chance a buff will drop from a stone obstacle."] = "{0} aumenta a chance de uma melhoria cair de um obstáculo de pedra.",
        ["decrease the maximum limit of the unit reserve."] = "{0} reduz o limite máximo da reserva de unidades.",
        ["decreases the maximum limit of the unit reserve."] = "{0} reduz o limite máximo da reserva de unidades.",
        ["increase the maximum limit of the unit reserve."] = "{0} aumenta o limite máximo da reserva de unidades.",
        ["increases the maximum limit of the unit reserve."] = "{0} aumenta o limite máximo da reserva de unidades.",
        ["reduces the amount of currency the player gains after winning a round."] = "{0} reduz a quantidade de ouro que o jogador ganha após vencer uma rodada.",
        ["reduce the amount of currency the player gains after winning a round."] = "{0} reduz a quantidade de ouro que o jogador ganha após vencer uma rodada.",
        ["increases the amount of currency the player gains after winning a round."] = "{0} aumenta a quantidade de ouro que o jogador ganha após vencer uma rodada.",
        ["increase the amount of currency the player gains after winning a round."] = "{0} aumenta a quantidade de ouro que o jogador ganha após vencer uma rodada.",
        ["increase the chance currency will be earned when clearing lines."] = "{0} aumenta a chance de ganhar ouro ao limpar linhas.",
        ["increases the chance currency will be earned when clearing lines."] = "{0} aumenta a chance de ganhar ouro ao limpar linhas."
    };

    static readonly (string English, string Portuguese)[] DegreePrefixes =
    {
        ("Slightly", "Levemente"),
        ("Modestly", "Moderadamente"),
        ("Moderatley", "Moderadamente"),
        ("Moderately", "Moderadamente"),
        ("Significantly", "Significativamente"),
        ("Massivley", "Muito"),
        ("Massively", "Muito")
    };

    static readonly (string English, string Portuguese)[] LinePrefixes =
    {
        ("Special Gauge Gain", "Ganho de energia especial"),
        ("Enemy Damage", "Dano inimigo"),
        ("Enemy HP", "PV inimigo"),
        ("Score Gain", "Ganho de pontuação"),
        ("EXP Gain", "Ganho de EXP"),
        ("Misfortune", "Infortúnio"),
        ("Gravity", "Gravidade"),
        ("Score", "Pontuação"),
        ("Level", "Nível"),
        ("Reset", "Reinício")
    };

    public static bool TryGetText(string englishText, out string portugueseText)
    {
        portugueseText = null;

        if (string.IsNullOrWhiteSpace(englishText))
            return false;

        string lookupKey = NormalizeLookupKey(englishText);
        if (ExactText.TryGetValue(lookupKey, out portugueseText))
            return true;

        if (TryTranslateRunModifierDescription(lookupKey, out portugueseText))
            return true;

        if (TryTranslateLabelValueLines(englishText, out portugueseText))
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
        Add(text, "Cancel", "Cancelar");
        Add(text, "Continue", "Continuar");
        Add(text, "Confirm", "Confirmar");
        Add(text, "Close", "Fechar");
        Add(text, "Start", "Iniciar");
        Add(text, "PAUSED", "PAUSADO");
        Add(text, "Resume", "Continuar");
        Add(text, "Main Menu", "Menu principal");
        Add(text, "Restart", "Reiniciar");
        Add(text, "Save & Quit", "Salvar e sair");
        Add(text, "Quit", "Sair");
        Add(text, "New Game", "Novo jogo");
        Add(text, "Shop", "Loja");
        Add(text, "Codex", "Códice");
        Add(text, "Help", "Ajuda");
        Add(text, "HighScore", "Recorde");
        Add(text, "Select Monsters", "Selecionar monstros");
        Add(text, "Select Commander", "Selecionar comandante");
        Add(text, "Combat Log", "Registro de combate");
        Add(text, "Skip Trailer", "Pular trailer");
        Add(text, "BGM Genre", "Gênero da música");
        Add(text, "EDM", "EDM");
        Add(text, "Metal", "Metal");
        Add(text, "Random", "Aleatório");
        Add(text, "Language", "Idioma");
        Add(text, "Settings", "Configurações");
        Add(text, "Master Volume", "Volume geral");
        Add(text, "Music Volume", "Volume da música");
        Add(text, "SFX Volume", "Volume dos efeitos");
        Add(text, "Cursor Size", "Tamanho do cursor");
        Add(text, "Choose Language", "Escolher idioma");
        Add(text, "Pick the language Tetrabeasts should use. You can change this later in Settings.", "Escolha o idioma que Tetrabeasts deve usar. Você pode mudar isso depois em Configurações.");
        Add(text, "Press Any Key", "Pressione qualquer tecla");
        Add(text, "Modifiers", "Modificadores");
        Add(text, "Active Run Modifiers", "Modificadores ativos da partida");
        Add(text, "Back", "Voltar");
        Add(text, "None", "Nenhum");
        Add(text, "Yes", "Sim");
        Add(text, "No", "Não");
        Add(text, "Locked", "Bloqueado");
        Add(text, "Blocked", "Bloqueado");
        Add(text, "Active", "Ativo");
        Add(text, "(Missing)", "(Ausente)");
        Add(text, "???", "???");
        Add(text, "Leaderboard", "Ranking");
        Add(text, "LEADERBOARDS", "RANKINGS");
        Add(text, "Global", "Global");
        Add(text, "Friends", "Amigos");
        Add(text, "Current", "Atual");
        Add(text, "Rank", "Posição");
        Add(text, "Player", "Jogador");
        Add(text, "Refresh", "Atualizar");
        Add(text, "Loading...", "Carregando...");
        Add(text, "Refreshing leaderboards...", "Atualizando rankings...");
        Add(text, "Steam leaderboard ready.", "Ranking da Steam pronto.");
        Add(text, "Leaderboard refresh failed.", "Falha ao atualizar o ranking.");
        Add(text, "No leaderboard data.", "Sem dados de ranking.");
        Add(text, "No global scores yet.", "Ainda não há pontuações globais.");
        Add(text, "No friend scores yet.", "Ainda não há pontuações de amigos.");
        Add(text, "You are not ranked yet.", "Você ainda não está ranqueado.");
        Add(text, "Achievements", "Conquistas");
        Add(text, "Submit Name", "Enviar nome");
        Add(text, "Help Menu", "Menu de ajuda");
        Add(text, "Modifier not yet discovered.", "Modificador ainda não descoberto.");
        Add(text, "No level Modifier", "Sem modificador de nível");
        Add(text, "Level modifier: {0}.", "Modificador de nível: {0}.");
        Add(text, "Secret Achievements", "Conquistas secretas");
        Add(text, "1 secret achievement remaining", "Resta 1 conquista secreta");
        Add(text, "{0} secret achievements remaining", "Restam {0} conquistas secretas");
        Add(text, "No GameController found.", "GameController não encontrado.");
        Add(text, "No monsters in roster.", "Não há monstros no esquadrão.");
        Add(text, "Reinforcements", "Reforços");
        Add(text, "Active Squad", "Esquadrão ativo");
        Add(text, "Choose A Buff", "Escolha uma melhoria");
        Add(text, "Choose A Debuff", "Escolha uma penalidade");
        Add(text, "Next Piece", "Próxima peça");
        Add(text, "Reroll", "Rerrolar");
        Add(text, "EXP Gained", "EXP ganha");
        Add(text, "Permanent EXP Gained", "EXP permanente ganha");
        Add(text, "EXP Preserved", "EXP preservada");
        Add(text, "New Difficulty Unlocked!", "Nova dificuldade desbloqueada!");
        Add(text, "Special Ability: {0}", "Habilidade especial: {0}");
        Add(text, "{0} is broken.", "{0} quebrou.");
        Add(text, "{0}: {1} shield(s) remain.", "{0}: restam {1} escudo(s).");
    }

    static void AddRecentFixupText(Dictionary<string, string> text)
    {
        Add(text, "Starting a new game will erase the saved run. Continue?", "Começar um novo jogo apagará a partida salva. Continuar?");
        Add(text, "Current Level: {0}", "Nível atual: {0}");
        Add(text, "Current Level: 0", "Nível atual: 0");
        Add(text, " x{0}", " x{0}");
        Add(text, "Refund", "Reembolsar");
        Add(text, "Unlock", "Desbloquear");
        Add(text, " Unlock", " Desbloquear");
        Add(text, "Selected:", "Selecionados:");
        Add(text, "Selected: ", "Selecionados: ");
        Add(text, "{0}  Lv.{1}", "{0}  Nv.{1}");
        Add(text, "Luck Up", "Sorte +");
        Add(text, "Gravity Down", "Gravidade -");
        Add(text, "Velocity Down", "Velocidade -");
        Add(text, "Gold Up", "Ouro +");
        Add(text, "Attack Up", "Ataque +");
        Add(text, "HP Up", "PV +");
        Add(text, "Unit Lives Up", "Vidas da unidade +");
        Add(text, "Role: {0}", "Função: {0}");
        Add(text, "Level: {0}  ({1:0.#}/{2})", "Nível: {0}  ({1:0.#}/{2})");
        Add(text, "Max HP: {0:0.#}  (+{1}) = {2:0.#}", "PV máx.: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Attack: {0:0.#}  (+{1}) = {2:0.#}", "Ataque: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Special Gain: {0:0.#}", "Ganho especial: {0:0.#}");
        Add(text, "Heal: {0:0.#}  (+{1}) = {2:0.#}", "Cura: {0:0.#}  (+{1}) = {2:0.#}");
        Add(text, "Heal Range: {0:0.#}", "Alcance de cura: {0:0.#}");
        Add(text, "Heal Speed: {0:0.#}s", "Velocidade de cura: {0:0.#} s");
        Add(text, "Heal: -", "Cura: -");
        Add(text, "Base Stats + Shop Buff = Total Stats", "Atributos base + melhoria da loja = atributos totais");
        Add(text, "Defense", "Defesa");
        Add(text, "Beefy boy that deals very little damage.", "Um grandalhão resistente que causa muito pouco dano.");
        Add(text, "Healer with a wide range, but weak spells.", "Curandeira de longo alcance, mas com feitiços fracos.");
        Add(text, "A well rounded attacker unit with decent damage, but enough health to survive weaker attacks.", "Unidade atacante equilibrada, com bom dano e vida suficiente para sobreviver a ataques mais fracos.");
        Add(text, "A specialized attacker unit with high base damage and lower health.", "Unidade atacante especializada, com alto dano base e pouca vida.");
        Add(text, "Can take a hit and keep going, but won't deal much damage.", "Aguenta pancada e continua lutando, mas não causa muito dano.");
        Add(text, "Healer with a short range, but powerful healing spells.", "Curandeira de curto alcance, mas com feitiços de cura poderosos.");
        Add(text, "Combo Extension", "Extensão de combo");
        Add(text, "Chain Surge", "Surto em cadeia");
        Add(text, "Stone Scrounger", "Catador de pedras");
        Add(text, "Reserve Stockpile", "Estoque de reserva");
        Add(text, "Reserve Recovery", "Recuperação de reserva");
        Add(text, "Bulwark Aura", "Aura de baluarte");
        Add(text, "Increase combo timer duration by {0}.", "Aumenta a duração do temporizador de combo em {0}.");
        Add(text, "Each row clear has a {0} chance to increase combo count one additional time.", "Cada linha limpa tem {0} de chance de aumentar o combo uma vez adicional.");
        Add(text, "Increase chance of buff drop from stone obstacle destruction by {0}.", "Aumenta em {0} a chance de uma melhoria cair ao destruir obstáculos de pedra.");
        Add(text, "Increase the number of starting reserve units by {0}.", "Aumenta em {0} o número de unidades de reserva iniciais.");
        Add(text, "Increase the number of reserve units restored on round win by {0}.", "Aumenta em {0} o número de unidades de reserva restauradas ao vencer uma rodada.");
        Add(text, "Decrease damage taken and damage done for all ally monster units by {0}.", "Reduz em {0} o dano recebido e causado por todas as unidades de monstros aliadas.");
        Add(text, "Passive - {0}", "Passiva - {0}");
        Add(text, "Passive - {0}:", "Passiva - {0}:");
        Add(text, "Next upgrade at Lv.{0}:", "Próxima melhoria no Nv.{0}:");
        Add(text, "Passive is fully upgraded.", "A passiva está no nível máximo.");
        Add(text, "1 second", "1 segundo");
        Add(text, "{0} seconds", "{0} segundos");
        Add(text, "A = Shift Left", "A = mover para a esquerda");
        Add(text, "D = Shift Right", "D = mover para a direita");
        Add(text, "D = Shft Right", "D = mover para a direita");
        Add(text, "R = Character Special", "R = especial do personagem");
        Add(text, "= Character Special", "= especial do personagem");
        Add(text, "R = Character Special (Special Guage 100%)", "R = especial do personagem (energia especial 100%)");
        Add(text, "R = Character Special (Special Gauge 100%)", "R = especial do personagem (energia especial 100%)");
        Add(text, "R = Character Special (Specaial Gauge 100%)", "R = especial do personagem (energia especial 100%)");
        Add(text, "= Character Special (Special Guage 100%)", "= especial do personagem (energia especial 100%)");
        Add(text, "= Character Special (Special Gauge 100%)", "= especial do personagem (energia especial 100%)");
        Add(text, "Boss", "Chefe");
        Add(text, "Boss Ability", "Habilidade do chefe");
        Add(text, "Boss Abilities", "Habilidades do chefe");

        Add(text, "Gold Won This Round:", "Ouro ganho nesta rodada:");
        Add(text, "Gold Won This Round", "Ouro ganho nesta rodada");
        Add(text, "Rerolls", "Rerrolagens");
        Add(text, "Rerolls: {0}", "Rerrolagens: {0}");
        Add(text, "Rerolls: 0", "Rerrolagens: 0");
        Add(text, "Modifier", "Modificador");
        Add(text, "x{0}", "x{0}");

        Add(text, "Level Up", "Subiu de nível");
        Add(text, "Level Up!", "Subiu de nível!");
        Add(text, "LEVEL UP!", "SUBIU DE NÍVEL!");
        Add(text, "LEVEL UP! x{0}", "SUBIU DE NÍVEL! x{0}");
        Add(text, "Level {0} -> Level {1}", "Nível {0} -> Nível {1}");
        Add(text, "+{0} Exp", "+{0} EXP");
        Add(text, "{0} permanent EXP ({1}% of {2} transferable EXP)", "{0} EXP permanente ({1}% de {2} EXP transferível)");
        Add(text, "Converted from {0} run EXP at {1}%", "Convertida de {0} EXP da partida a {1}%");

        Add(text, "Passive+", "Passiva+");
        Add(text, "+ 5 HP", "+ 5 PV");
        Add(text, "+ 1 Attack", "+ 1 Ataque");
        Add(text, "+ 1 Special", "+ 1 Especial");
        Add(text, "+ 5 Heal", "+ 5 Cura");
        Add(text, "+ 1 Range", "+ 1 Alcance");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Agora, faça uma queda instantânea da peça atual pressionando [Barra de Espaço]. Isso soltará a peça imediatamente para que você possa posicioná-la rápido, travá-la no lugar e gerar uma nova.");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar]. This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Agora, faça uma queda instantânea da peça atual pressionando [Barra de Espaço]. Isso soltará a peça imediatamente para que você possa posicioná-la rápido, travá-la no lugar e gerar uma nova.");
        Add(text, "This will immediatley drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Isso soltará a peça imediatamente para que você possa posicioná-la rápido, travá-la no lugar e gerar uma nova.");
        Add(text, "This will immediately drop your piece so that you can quickly place your current piece locking it in place and spawning a new one.",
            "Isso soltará a peça imediatamente para que você possa posicioná-la rápido, travá-la no lugar e gerar uma nova.");
        AddAchievementText(text);
    }

    static void AddAchievementText(Dictionary<string, string> text)
    {
        Add(text, "I Want Every Able Body", "Quero todos disponíveis");
        Add(text, "Unlock all monster units.", "Desbloqueie todas as unidades de monstros.");
        Add(text, "0 Star Victory", "Vitória de 0 estrela");
        Add(text, "Beat the final boss.", "Derrote o chefe final.");
        Add(text, "One Star Victory", "Vitória de uma estrela");
        Add(text, "Beat the final boss on 1-Star difficulty.", "Derrote o chefe final na dificuldade de 1 estrela.");
        Add(text, "Pay To Win", "Pagar para vencer");
        Add(text, "Upgrade any shop buff to level 5.", "Melhore qualquer melhoria da loja até o nível 5.");
        Add(text, "Gangs All Here", "A turma toda chegou");
        Add(text, "Unlock all Commanders.", "Desbloqueie todos os comandantes.");
        Add(text, "Two Star Victory", "Vitória de duas estrelas");
        Add(text, "Beat the final boss on 2-Star difficulty.", "Derrote o chefe final na dificuldade de 2 estrelas.");
        Add(text, "Three Star Victory", "Vitória de três estrelas");
        Add(text, "Beat the final boss on 3-Star difficulty.", "Derrote o chefe final na dificuldade de 3 estrelas.");
        Add(text, "Four Star Victory", "Vitória de quatro estrelas");
        Add(text, "Beat the final boss on 4-Star difficulty.", "Derrote o chefe final na dificuldade de 4 estrelas.");
        Add(text, "Five Star Victory", "Vitória de cinco estrelas");
        Add(text, "Beat the final boss on 5-Star difficulty.", "Derrote o chefe final na dificuldade de 5 estrelas.");
        Add(text, "This is Fine", "Está tudo bem");
        Add(text, "Take 1,000 burn damage from fire floor effects.", "Receba 1.000 de dano de queimadura por efeitos de piso de fogo.");
        Add(text, "That Escalated Quickly", "Isso escalou rápido");
        Add(text, "Remove 1,000 units using the Death Special Block.", "Remova 1.000 unidades usando o bloco especial de morte.");
        Add(text, "First Time Raider", "Saqueador de primeira viagem");
        Add(text, "Take 1,000 damage from traps.", "Receba 1.000 de dano de armadilhas.");
        Add(text, "I think I Stepped in Something", "Acho que pisei em alguma coisa");
        Add(text, "Take 1,000 toxic damage from posioned floor effects.", "Receba 1.000 de dano tóxico por efeitos de piso envenenado.");
        Add(text, "Take 1,000 toxic damage from poisoned floor effects.", "Receba 1.000 de dano tóxico por efeitos de piso envenenado.");
        Add(text, "Shake It Until You Break It", "Sacuda até quebrar");
        Add(text, "Clear 250 rows by using the earthquake special block.", "Limpe 250 linhas usando o bloco especial de terremoto.");
        Add(text, "A Little Jiggle Goes A Long Way", "Uma tremidinha ajuda muito");
        Add(text, "Clear 25 rows by using the earthquake special block.", "Limpe 25 linhas usando o bloco especial de terremoto.");
        Add(text, "Girthquake", "Megaterremoto");
        Add(text, "Clear 1,000 rows by using the earthquake special block.", "Limpe 1.000 linhas usando o bloco especial de terremoto.");
        Add(text, "Get in Loser, We're Going Shopping", "Entra aí, vamos às compras");
        Add(text, "Accumulate 1,000 gold.", "Acumule 1.000 de ouro.");
        Add(text, "Is I Rich Now?", "Agora eu sou rico?");
        Add(text, "Accumulate 100 gold.", "Acumule 100 de ouro.");
        Add(text, "This Lasted Longer Than Some Collectible Fads", "Isso durou mais que algumas modas colecionáveis");
        Add(text, "Take more than 5 minutes to conquer a castle.", "Leve mais de 5 minutos para conquistar um castelo.");
        Add(text, "Anything You Can Do, I Can Do Slower", "Tudo que você faz, eu faço mais devagar");
        Add(text, "Take more than 3 minutes to conquer a castle.", "Leve mais de 3 minutos para conquistar um castelo.");
        Add(text, "Sloth Lord", "Senhor da preguiça");
        Add(text, "Take more than 4 minutes to conquer a castle.", "Leve mais de 4 minutos para conquistar um castelo.");
        Add(text, "My Fingers Hurt", "Meus dedos doem");
        Add(text, "Survive with gravity at 10 for 60 seconds.", "Sobreviva com gravidade 10 por 60 segundos.");
        Add(text, "Nevermind...", "Deixa pra lá...");
        Add(text, "Unlock your first first temporary debuff.", "Desbloqueie sua primeira penalidade temporária.");
        Add(text, "A Record That Would Make Lions Blush", "Um recorde de dar vergonha");
        Add(text, "Lose 50 Times.", "Perca 50 vezes.");
        Add(text, "Thrive Under Pressure", "Prosperar sob pressão");
        Add(text, "Survive with gravity at 10 for 30 seconds.", "Sobreviva com gravidade 10 por 30 segundos.");
        Add(text, "Is I Strong Now?", "Agora eu sou forte?");
        Add(text, "Unlock your first first temporary buff.", "Desbloqueie sua primeira melhoria temporária.");
        Add(text, "I Say This Not As An Insult, But As A Statement Of Fact", "Não digo isso como insulto, mas como fato");
        Add(text, "Lose 100 Times.", "Perca 100 vezes.");
        Add(text, "I Think We Need A Bigger Vault", "Acho que precisamos de um cofre maior");
        Add(text, "Accumulate 10,000 gold.", "Acumule 10.000 de ouro.");
        Add(text, "GG EZ", "GG EZ");
        Add(text, "Beat the final level with every Commander.", "Conclua o nível final com cada comandante.");
        Add(text, "Participation Trophy", "Troféu de participação");
        Add(text, "Lose for the first time.", "Perca pela primeira vez.");
        Add(text, "Immortal Army", "Exército imortal");
        Add(text, "Conquer 100 castles with your Unit Reserve at max capacity.", "Conquiste 100 castelos com a reserva de unidades no máximo.");
        Add(text, "I Guess That Was The Wrong Wire", "Acho que esse era o fio errado");
        Add(text, "Lose 100 monster units to magic explosives.", "Perca 100 unidades de monstros para explosivos mágicos.");
        Add(text, "I Think It's Dead Now", "Acho que agora morreu");
        Add(text, "Clear 1,000 rows.", "Limpe 1.000 linhas.");
        Add(text, "Tis But A Scratch", "É só um arranhão");
        Add(text, "Conquer a castle with your Unit Reserve at max capacity.", "Conquiste um castelo com a reserva de unidades no máximo.");
        Add(text, "Meat Shield Tactics", "Táticas de escudo de carne");
        Add(text, "Conquer 25 castles with your Unit Reserve at max capacity.", "Conquiste 25 castelos com a reserva de unidades no máximo.");
        Add(text, "You Got Your Marching Orders", "Você recebeu suas ordens de marcha");
        Add(text, "Clear 100 rows.", "Limpe 100 linhas.");
        Add(text, "Overwhelming Power", "Poder esmagador");
        Add(text, "Accumulate 15 buffs in a single run.", "Acumule 15 melhorias em uma única partida.");
        Add(text, "Is There Anything Left To Attack?", "Ainda resta algo para atacar?");
        Add(text, "Clear 10,000 rows.", "Limpe 10.000 linhas.");
        Add(text, "Shut Up And Takey My Money", "Cala a boca e pega meu dinheiro");
        Add(text, "Buy an upgrade from the shop for the first time.", "Compre uma melhoria na loja pela primeira vez.");
        Add(text, "Turning Small Numbers Into Big Numbers", "Transformando números pequenos em grandes");
        Add(text, "Get a combo of 10 or higher.", "Consiga um combo de 10 ou mais.");
        Add(text, "Vewwy Stwong", "Muito fowte");
        Add(text, "Deal 100 damage or more in a single attack.", "Cause 100 de dano ou mais em um único ataque.");
        Add(text, "I'll Follow You Anywhere", "Eu sigo você a qualquer lugar");
        Add(text, "Unlock a Commander for the first time.", "Desbloqueie um comandante pela primeira vez.");
        Add(text, "Eenie Meenie Miney Mo", "Uni duni tê");
        Add(text, "Unlock a monster unit for the first time.", "Desbloqueie uma unidade de monstro pela primeira vez.");
        Add(text, "Tell Me I'm Pretty", "Diga que eu sou bonito");
        Add(text, "Unlock a monster units skin variant for the first time.", "Desbloqueie uma variante de visual de uma unidade de monstro pela primeira vez.");
        Add(text, "Are We There Yet?", "Já chegamos?");
        Add(text, "Conquer 10 castles in a single run.", "Conquiste 10 castelos em uma única partida.");
        Add(text, "It's Called Fashion Brenda, Look It Up", "Isso se chama moda, vai pesquisar");
        Add(text, "Unlock ten skin variants.", "Desbloqueie dez variantes de visual.");
        Add(text, "New Skin Who Dis?", "Visual novo, quem é?");
        Add(text, "Unlock five skin variants.", "Desbloqueie cinco variantes de visual.");
        Add(text, "A Little Special", "Um pouco especial");
        Add(text, "Use a special for the first time.", "Use um especial pela primeira vez.");
        Add(text, "Mama's Special Boy", "O especial da mamãe");
        Add(text, "Use a special 100 times.", "Use um especial 100 vezes.");
        Add(text, "Some Are More Special Than Others", "Alguns são mais especiais que outros");
        Add(text, "Use every Commanders special 100 times.", "Use o especial de cada comandante 100 vezes.");
        Add(text, "General Got Me Workin'", "O general me botou pra trabalhar");
        Add(text, "Destory 100 stone obstacles.", "Destrua 100 obstáculos de pedra.");
        Add(text, "Destroy 100 stone obstacles.", "Destrua 100 obstáculos de pedra.");
        Add(text, "Certified Glue Eater", "Comedor de cola certificado");
        Add(text, "Use a special 1000 times.", "Use um especial 1000 vezes.");
        Add(text, "We're All Special", "Todos somos especiais");
        Add(text, "Use every Commanders special 20 times.", "Use o especial de cada comandante 20 vezes.");
        Add(text, "That's A Lot Of Rubble", "Isso é muito entulho");
        Add(text, "Conquer 50 castles.", "Conquiste 50 castelos.");
        Add(text, "King of Rubble", "Rei do entulho");
        Add(text, "Conquer 100 castles.", "Conquiste 100 castelos.");
        Add(text, "I. AM. SPEED!", "EU. SOU. VELOCIDADE!");
        Add(text, "Conquer a castle in 30 seconds or less.", "Conquiste um castelo em 30 segundos ou menos.");
        Add(text, "Can't Stop, Won't Stop", "Não posso parar, não vou parar");
        Add(text, "Conquer your first castle.", "Conquiste seu primeiro castelo.");
        Add(text, "I Ran Track In Highschool", "Eu corria na escola");
        Add(text, "Conquer a castle in 45 seconds or less.", "Conquiste um castelo em 45 segundos ou menos.");
        Add(text, "Gotta Go Fast", "Tem que ir rápido");
        Add(text, "Conquer a castle in 60 seconds or less.", "Conquiste um castelo em 60 segundos ou menos.");
    }

    static void AddXpBreakdownText(Dictionary<string, string> text)
    {
        Add(text, "Level {0} Complete", "Nível {0} concluído");
        Add(text, "Base Level XP", "EXP base do nível");
        Add(text, "Clear time {0}", "Tempo de conclusão {0}");
        Add(text, "Units lost {0}", "Unidades perdidas {0}");
        Add(text, "Largest Combo {0}", "Maior combo {0}");
        Add(text, "Obstacles Cleared {0}", "Obstáculos removidos {0}");
        Add(text, "Star Difficulty ({0}):", "Dificuldade de estrelas ({0}):");
        Add(text, "Total XP Earned {0}", "EXP total ganha {0}");
    }

    static void AddCommonLabels(Dictionary<string, string> text)
    {
        Add(text, "Run", "Partida");
        Add(text, "Level", "Nível");
        Add(text, "Controls", "Controles");
        Add(text, "Gravity", "Gravidade");
        Add(text, "Combo", "Combo");
        Add(text, "Score", "Pontuação");
        Add(text, "Reset", "Reinício");
        Add(text, "Score: {0}", "Pontuação: {0}");
        Add(text, "Gravity: {0:0.0}", "Gravidade: {0:0.0}");
        Add(text, "Star Difficulty", "Dificuldade de estrelas");
        Add(text, "0 Star: Recruit Difficulty", "0 estrela: dificuldade recruta");
        Add(text, "1 Star: Soldier Difficulty", "1 estrela: dificuldade soldado");
        Add(text, "2 Star: Veteran Difficulty", "2 estrelas: dificuldade veterano");
        Add(text, "3 Star: Lieutenant Difficulty", "3 estrelas: dificuldade tenente");
        Add(text, "4 Star: General Difficulty", "4 estrelas: dificuldade general");
        Add(text, "5 Star: War God Difficulty", "5 estrelas: dificuldade deus da guerra");
        Add(text, "0-Star Difficulty", "Dificuldade 0 estrela");
        Add(text, "1 Star Difficulty", "Dificuldade 1 estrela");
        Add(text, "2 Stars Difficulty", "Dificuldade 2 estrelas");
        Add(text, "3 Stars Difficulty", "Dificuldade 3 estrelas");
        Add(text, "4 Stars Difficulty", "Dificuldade 4 estrelas");
        Add(text, "5 Stars Difficulty", "Dificuldade 5 estrelas");
        Add(text, "Normal difficulty.", "Dificuldade normal.");
        Add(text, "No gameplay modifiers.", "Sem modificadores de jogo.");
        Add(text, "All star difficulties unlocked.", "Todas as dificuldades de estrelas foram desbloqueadas.");
        Add(text, "0 Stars is always available.", "0 estrelas está sempre disponível.");
        Add(text, "Beat the final level on 0 Stars to unlock 1 Star.", "Conclua o nível final em 0 estrelas para desbloquear 1 estrela.");
        Add(text, "Beat the final level on 1 Star to unlock 2 Stars.", "Conclua o nível final em 1 estrela para desbloquear 2 estrelas.");
        Add(text, "Beat the final level on 2 Stars to unlock 3 Stars.", "Conclua o nível final em 2 estrelas para desbloquear 3 estrelas.");
        Add(text, "Beat the final level on 3 Stars to unlock 4 Stars.", "Conclua o nível final em 3 estrelas para desbloquear 4 estrelas.");
        Add(text, "Beat the final level on 4 Stars to unlock 5 Stars.", "Conclua o nível final em 4 estrelas para desbloquear 5 estrelas.");
        Add(text, "0 Stars", "0 estrelas");
        Add(text, "1 Star", "1 estrela");
        Add(text, "2 Stars", "2 estrelas");
        Add(text, "3 Stars", "3 estrelas");
        Add(text, "4 Stars", "4 estrelas");
        Add(text, "5 Stars", "5 estrelas");
        Add(text, "Score Gain", "Ganho de pontuação");
        Add(text, "EXP Gain", "Ganho de EXP");
        Add(text, "Level Modifier", "Modificador de nível");
        Add(text, "Reserves And Rewards", "Reservas e recompensas");
        Add(text, "Reserve Units", "Unidades de reserva");
        Add(text, "Max Reserve Units", "Máx. de unidades de reserva");
        Add(text, "Reserve Restored On Win", "Reserva restaurada ao vencer");
        Add(text, "Round Win Currency", "Ouro por vitória na rodada");
        Add(text, "Line Clear Currency Chance", "Chance de ouro ao limpar linhas");
        Add(text, "Line Clear Currency Amount", "Quantidade de ouro ao limpar linhas");
        Add(text, "Monster Combat", "Combate de monstros");
        Add(text, "Monster Damage", "Dano dos monstros");
        Add(text, "Monster Special Gain", "Ganho especial dos monstros");
        Add(text, "Monster Max HP", "PV máximo dos monstros");
        Add(text, "Healing Power", "Poder de cura");
        Add(text, "Healing Range Bonus", "Bônus de alcance de cura");
        Add(text, "Ally Damage Dealt", "Dano aliado causado");
        Add(text, "Ally Damage Taken", "Dano aliado recebido");
        Add(text, "Combo And Passives", "Combo e passivas");
        Add(text, "Combo Window", "Janela de combo");
        Add(text, "Bonus Combo Chance", "Chance de combo bônus");
        Add(text, "Stone Buff Drop Chance", "Chance de melhoria de pedra");
        Add(text, "Starting Reserve Passive", "Passiva de reserva inicial");
        Add(text, "Round Win Reserve Passive", "Passiva de reserva ao vencer");
        Add(text, "Enemy", "Inimigo");
        Add(text, "Enemy Castle HP", "PV do castelo inimigo");
        Add(text, "Enemy Damage", "Dano inimigo");
        Add(text, "Enemy Attack Interval", "Intervalo de ataque inimigo");
        Add(text, "Enemy Projectile Speed", "Velocidade dos projéteis inimigos");
        Add(text, "Castle Projectile Damage", "Dano do projétil do castelo");
        Add(text, "Castle Attack Interval", "Intervalo de ataque do castelo");
        Add(text, "Piece And Special", "Peça e especial");
        Add(text, "Piece Gravity", "Gravidade da peça");
        Add(text, "Gravity Ramp Rate", "Taxa de aumento da gravidade");
        Add(text, "Special Block Chance", "Chance de bloco especial");
        Add(text, "Commander Special Gain", "Ganho especial do comandante");
        Add(text, "Special Drain", "Dreno especial");
        Add(text, "Next Preview Disabled", "Prévia da próxima peça desativada");
        Add(text, "Landing Hint Disabled", "Indicador de aterrissagem desativado");
        Add(text, "Special Usage Locked", "Uso de especial bloqueado");
        Add(text, "Special Blocks Blocked", "Blocos especiais bloqueados");
        Add(text, "Run Modifier Drops", "Quedas de modificadores da partida");
        Add(text, "Stone Drops Debuffs Only", "Pedras soltam apenas penalidades");
        Add(text, "Luck", "Sorte");
        Add(text, "Misfortune", "Infortúnio");
        Add(text, "Active Level Modifier", "Modificador de nível ativo");
        Add(text, "Effect", "Efeito");
        Add(text, "Outgoing Damage", "Dano causado");
        Add(text, "Incoming Damage", "Dano recebido");
        Add(text, "Overgrowth Target Interval", "Intervalo de alvo do crescimento");
        Add(text, "Initial Target Rows", "Linhas alvo iniciais");
        Add(text, "Partial Growth Time", "Tempo de crescimento parcial");
        Add(text, "Full Growth Time", "Tempo de crescimento total");
        Add(text, "Storm Strike Damage", "Dano do raio da tempestade");
        Add(text, "Storm Floor Tick Damage", "Dano periódico do piso de tempestade");
        Add(text, "Storm Floor Duration", "Duração do piso de tempestade");
        Add(text, "Rear Ambush Interval", "Intervalo de emboscada traseira");
        Add(text, "Rations Tick Interval", "Intervalo de rações");
        Add(text, "Low Reserve Damage", "Dano com reserva baixa");
        Add(text, "High Reserve Damage", "Dano com reserva alta");
        Add(text, "Infection Chance", "Chance de infecção");
        Add(text, "Damage Per Tick", "Dano por tique");
        Add(text, "Damage Increase Per Tick", "Aumento de dano por tique");
        Add(text, "Spread Chance", "Chance de propagação");
        Add(text, "Special Gauge Gain", "Ganho de energia especial");
        Add(text, "Death Explosion Damage", "Dano de explosão por morte");
        Add(text, "Swamp Poison Damage", "Dano do veneno do pântano");
        Add(text, "Swamp Poison Interval", "Intervalo do veneno do pântano");
        Add(text, "Manual Rotation", "Rotação manual");
        Add(text, "Auto Rotate Interval", "Intervalo de rotação automática");
        Add(text, "Manual Horizontal Shift", "Deslocamento horizontal manual");
        Add(text, "Auto Shift Interval", "Intervalo de deslocamento automático");
        Add(text, "Combo Threshold", "Limite de combo");
        Add(text, "Below Threshold Damage", "Dano abaixo do limite");
        Add(text, "Shield Combo Threshold", "Limite de combo do escudo");
        Add(text, "Blocked Damage", "Dano bloqueado");
        Add(text, "Shield Count", "Quantidade de escudos");
        Add(text, "Special Pieces", "Peças especiais");
        Add(text, "Starting Monster Health", "Vida inicial dos monstros");
        Add(text, "Monster Damage Sharing", "Compartilhamento de dano dos monstros");
        Add(text, "Roster", "Esquadrão");
        Add(text, "Max HP", "PV máximo");
        Add(text, "Starting HP", "PV inicial");
        Add(text, "Attack", "Ataque");
        Add(text, "Special Gain", "Ganho especial");
        Add(text, "Heal Power", "Poder de cura");
        Add(text, "Heal Range", "Alcance de cura");
        Add(text, "Heal Speed", "Velocidade de cura");
        Add(text, "Spawn Weight", "Peso de surgimento");
        Add(text, "Stats", "Atributos");
        Add(text, "Monsters", "Monstros");
        Add(text, "Shop Buff", "Melhoria da loja");
        Add(text, "Passive", "Passiva");
        Add(text, "Run Mod Buff", "Melhoria da partida");
        Add(text, "Run Mod Debuff", "Penalidade da partida");
        Add(text, "Level Mod", "Mod. de nível");
        Add(text, "Boss Ability", "Habilidade do chefe");
        Add(text, "Defender", "Defensor");
        Add(text, "Healer", "Curandeiro");
        Add(text, "Role", "Função");
        Add(text, "HP", "PV");
        Add(text, "Special Rate", "Taxa especial");
        Add(text, "Passive Lv", "Nv. da passiva");
        Add(text, "Esc = Pause", "Esc = pausar");
        Add(text, "Q = Rotate Counter Clockwise", "Q = girar anti-horário");
        Add(text, "E = Rotate Clockwise", "E = girar horário");
        Add(text, "= Shift Left", "= mover para a esquerda");
        Add(text, "S = Shift Down", "S = mover para baixo");
        Add(text, "Spacebar = Drop Instantly", "Barra de espaço = queda instantânea");
        Add(text, "= Character Special (Specaial Gauge 100%)", "= especial do personagem (energia especial 100%)");
        Add(text, "Castle", "Castelo");
        Add(text, "Starting Village", "Vila inicial");
        Add(text, "Tribe", "Tribo");
        Add(text, "Tribe Chieftain", "Chefe tribal");
        Add(text, "Shanty Town", "Favela");
        Add(text, "Fortified Hamlet", "Povoado fortificado");
        Add(text, "Industrial City", "Cidade industrial");
        Add(text, "Thriving Metropolis", "Metrópole próspera");
        Add(text, "Grand Dukedom", "Grande ducado");
        Add(text, "Royal Villa", "Vila real");
        Add(text, "Ruk, Tribal Chieftain", "Ruk, chefe tribal");
        Add(text, "Fionne, Village Protector", "Fionne, protetora da vila");
        Add(text, "Sir Ralphie, Captain of the Guard", "Sir Ralphie, capitão da guarda");
        Add(text, "Eris, City Priestess", "Eris, sacerdotisa da cidade");
        Add(text, "His Holiness Isaeh, Metropolis Pope", "Sua Santidade Isaeh, papa da metrópole");
        Add(text, "Vivica, Dukedom Arch Mage", "Vivica, arquimaga do ducado");
        Add(text, "Emperor Reginald P. Exford IV, Emperor's Palace", "Imperador Reginald P. Exford IV, palácio imperial");
        Add(text, "Esora, Guardian to the Gates of Heaven?", "Esora, guardiã dos portões do céu?");
    }

    static void AddWarningAndTutorialText(Dictionary<string, string> text)
    {
        Add(text, "Purchases are disabled in the demo. Your earned progress will still carry into the full game.",
            "As compras estão desativadas na demo. O progresso conquistado ainda será levado para o jogo completo.");
        Add(text, "A saved run is waiting. Continue that run or delete the temp save before changing your commander, squad, or shop buffs.",
            "Há uma partida salva aguardando. Continue essa partida ou exclua o salvamento temporário antes de trocar comandante, esquadrão ou melhorias da loja.");
        Add(text, "Deleting the current temp run will permanently erase that saved run. After deleting it, you will be able to change your commander, monsters, and access the shop again. Continue?",
            "Excluir a partida temporária atual apagará permanentemente essa partida salva. Depois disso, você poderá trocar comandante, monstros e acessar a loja novamente. Continuar?");
        Add(text, "Thank you for playing the Tetrabeasts demo! You have cleared the final demo level. If you enjoyed your time with the game, please consider buying the full version.",
            "Obrigado por jogar a demo de Tetrabeasts!\n\nVocê concluiu o último nível da demo. Se gostou do jogo, considere comprar a versão completa.");
        Add(text, "The Castle Has Fallen", "O castelo caiu");
        Add(text, "Conquest Failed", "Conquista fracassou");
        Add(text, "Endless Survival This final battle cannot be won. The enemy has endless health, and the run continues until a loss condition is met. Survive as long as you can.",
            "Sobrevivência infinita\n\nEsta batalha final não pode ser vencida. O inimigo tem vida infinita, e a partida continua até uma condição de derrota ser cumprida.\n\nSobreviva pelo máximo de tempo que puder.");
        Add(text, "Do not show this message again", "Não mostrar esta mensagem novamente");
        Add(text, "Quit without saving? Your current run will be lost and will not be available to continue later.",
            "Sair sem salvar? Sua partida atual será perdida e não poderá ser continuada depois.");
        Add(text, "Restarting will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
            "Reiniciar contará esta partida como derrota. O salvamento temporário atual será apagado, e a partida não será salva. Continuar?");
        Add(text, "Returning to the main menu will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
            "Voltar ao menu principal contará esta partida como derrota. O salvamento temporário atual será apagado, e a partida não será salva. Continuar?");
        Add(text, "Save this run and quit the game? Continuing later will resume from the start of the current level checkpoint. While a run is saved, you will not be able to change your commander, squad, or shop buffs from the title menu.",
            "Salvar esta partida e sair do jogo? Ao continuar depois, você voltará ao início do ponto de controle do nível atual. Enquanto houver uma partida salva, você não poderá trocar comandante, esquadrão ou melhorias da loja no menu inicial.");
        Add(text, "The run could not be temp-saved, so the game will stay open.",
            "A partida não pôde ser salva temporariamente, então o jogo permanecerá aberto.");

        Add(text, "Welcome Overlord, my name is Lilith. I have done my best to rally the few monster I could find to assist in your conquest of the human domain. (Press [F] to Continue)",
            "Bem-vindo, Suserano. Meu nome é Lilith. Fiz o possível para reunir os poucos monstros que encontrei para ajudar na sua conquista do domínio humano. (Pressione [F] para continuar)");
        Add(text, "Before going into battle, we will first have to select our commander. Press the commander button in the bottom right to see who is available. (Press [F] to Continue)",
            "Antes de entrar em batalha, precisamos selecionar nosso comandante. Pressione o botão de comandante no canto inferior direito para ver quem está disponível. (Pressione [F] para continuar)");
        Add(text, "We only have one Commander that we can use at the moment, but with enough gold we can get a few more. Each commander has their own special ability. The details of each ability can be seen in the Commander preview on the right. (Press [F] to Continue)",
            "No momento temos apenas um comandante disponível, mas com ouro suficiente poderemos obter outros. Cada comandante tem sua própria habilidade especial. Os detalhes aparecem na prévia do comandante à direita. (Pressione [F] para continuar)");
        Add(text, "Since our commander is already selected lets confirm the selection at the bottom and head back to the main lobby.",
            "Como nosso comandante já está selecionado, confirme a escolha na parte inferior e volte ao lobby principal.");
        Add(text, "Now we will set up our monster squadron that will go into battle with our Commander. Press the select monster button in the bottom right.",
            "Agora vamos montar o esquadrão de monstros que irá para a batalha com nosso comandante. Pressione o botão de selecionar monstros no canto inferior direito.");
        Add(text, "As you can see we only have so many monsters to work with to creat our squadron. A Squadron must have at least two monsters and can have a maximum of 4. (Press [F] to Continue)",
            "Como você pode ver, temos poucos monstros para formar o esquadrão. Um esquadrão precisa ter pelo menos dois monstros e no máximo quatro. (Pressione [F] para continuar)");
        Add(text, "A war can hardly be fought with such pitiful numbers, but that's where I can be of assistance. (Press [F] to Continue)",
            "É difícil lutar uma guerra com números tão lamentáveis, mas é aí que posso ajudar. (Pressione [F] para continuar)");
        Add(text, "I can make clones of your squadron to be sent into battle. This will allow multiple copies to be made to fill out your ranks. However, this will come with some limitations. (Press [F] to Continue)",
            "Posso criar clones do seu esquadrão para enviá-los à batalha. Isso permite criar várias cópias para preencher suas fileiras. Porém, há algumas limitações. (Pressione [F] para continuar)");
        Add(text, "I will only be able to make so many copies and if we lose too many in battle we will be forced to retreat. Second, the copies will gain their own experience, but I will only be able to safely convert a small fraction of that experience from the copy back to the original monster after a campaign is finished. (Press [F] to Continue)",
            "Só poderei criar uma quantidade limitada de cópias e, se perdermos muitas em batalha, seremos forçados a recuar. Além disso, as cópias ganharão experiência própria, mas só poderei converter com segurança uma pequena parte dessa experiência da cópia para o monstro original após o fim da campanha. (Pressione [F] para continuar)");
        Add(text, "You can click on any monster and see a preview of it's current stats and level in the preview section on the right. Click on the arrow button in the preview section to swap between it's stats and it's passive ability description.",
            "Você pode clicar em qualquer monstro para ver uma prévia dos atributos e nível atuais na seção à direita. Clique no botão de seta nessa seção para alternar entre atributos e descrição da habilidade passiva.");
        Add(text, "More monsters can be unlocked with gold and cosmetic skins can also be purchased once you have gained some extra funds. Now, Prress the confirm button at the bottom of the screen to lock in your team.",
            "Mais monstros podem ser desbloqueados com ouro, e visuais cosméticos também podem ser comprados quando você tiver recursos extras. Agora pressione o botão de confirmar na parte inferior da tela para fixar sua equipe.");
        Add(text, "Now you are ready to start your first campaign. Press the Start button to begin a new run.",
            "Agora você está pronto para começar sua primeira campanha. Pressione o botão Iniciar para começar uma nova partida.");
        Add(text, "Welcome to the battlefield. We will begin by going over a few of the basic controls and battle mechanics. (Press [F] to Continue)",
            "Bem-vindo ao campo de batalha. Vamos começar revisando alguns controles básicos e mecânicas de combate. (Pressione [F] para continuar)");
        Add(text, "First, try moving your piece to the left by pressing [A].",
            "Primeiro, tente mover sua peça para a esquerda pressionando [A].");
        Add(text, "Next, try moving your piece to the right by pressing [D].",
            "Agora tente mover sua peça para a direita pressionando [D].");
        Add(text, "Next, try moving your piece down a single row by pressing [S].",
            "Agora tente mover sua peça uma linha para baixo pressionando [S].");
        Add(text, "Now, try rotating your piece counter-clockwise by pressing [Q].",
            "Agora tente girar sua peça no sentido anti-horário pressionando [Q].");
        Add(text, "Now, try rotating your piece clockwise by pressing [E].",
            "Agora tente girar sua peça no sentido horário pressionando [E].");
        Add(text, "Look at the bottom of the board and you will see four cells with a bright red tint in the same shape as your current piece, that isn't a coincidence. (Press [F] to Continue)",
            "Olhe para a parte inferior do tabuleiro e verá quatro células com um tom vermelho brilhante no mesmo formato da sua peça atual. Isso não é coincidência. (Pressione [F] para continuar)");
        Add(text, "This is the landing indicator and will help you see exactly where your current piece will lock in place. A piece will lock in place automatically when it come in contact with an obstacle, another locked unit piece, or when it reaches the bottom of the board. (Press [F] to Continue)",
            "Este é o indicador de aterrissagem e ajuda a ver exatamente onde sua peça atual será fixada. Uma peça trava automaticamente ao tocar um obstáculo, outra peça já travada ou o fundo do tabuleiro. (Pressione [F] para continuar)");
        Add(text, "Next, I want you to hard drop your current piece by pressing the [Space Bar].",
            "Agora faça uma queda instantânea da peça atual pressionando [Barra de Espaço].");
        Add(text, "Here you can keep track of level information such as any active level modifiers, the curent level number, how long you have been in the level, and the current gravity pulling your pieces down. (Press [F] to Continue)",
            "Aqui você acompanha informações do nível, como modificadores ativos, número do nível atual, tempo no nível e a gravidade atual puxando suas peças para baixo. (Pressione [F] para continuar)");
        Add(text, "Here you can see a preview of the next piece that will be spawned. Knowing what will come next will help you plan out your next step. (Press [F] to Continue)",
            "Aqui você vê uma prévia da próxima peça que será gerada. Saber o que vem a seguir ajuda a planejar o próximo passo. (Pressione [F] para continuar)");
        Add(text, "Reserve Units - Loss Condition When any of your units die your current reserve will be reduced. You only have so many soldiers and when your reserve hits 0 you will lose. (Press [F] to Continue)",
            "Unidades de reserva - condição de derrota\n\nQuando qualquer uma das suas unidades morre, sua reserva atual diminui. Você tem soldados limitados e, quando a reserva chegar a 0, perderá. (Pressione [F] para continuar)");
        Add(text, "Loss Condition If a piece is locked in place above the top row of the board you will instantly lose, regardless of the number of reserve units left. Be caureful not to build to high. (Press [F] to Continue)",
            "Condição de derrota\n\nSe uma peça for travada acima da linha superior do tabuleiro, você perderá instantaneamente, independentemente da reserva restante. Cuidado para não construir alto demais. (Pressione [F] para continuar)");
        Add(text, "Win Condition Reduce the Enemy Castle's HP to zero. (Press [F] to Continue)",
            "Condição de vitória\n\nReduza os PV do castelo inimigo a zero. (Pressione [F] para continuar)");
        Add(text, "If you need a break press [Esc]. This will pause the game and bring up the pause menu.",
            "Se precisar de uma pausa, pressione [Esc]. Isso pausará o jogo e abrirá o menu de pausa.");
        Add(text, "Here you can change settings, look through the help menu, check your current modifiers, or end your run early. For now, lets close the pause menu by pressing [Esc] again.",
            "Aqui você pode mudar configurações, consultar o menu de ajuda, verificar os modificadores atuais ou encerrar a partida antes. Por enquanto, feche o menu de pausa pressionando [Esc] novamente.");
        Add(text, "Now lets try filling an entire row on the board next to launch an attack. (Press [F] to Continue)",
            "Agora vamos tentar preencher uma linha inteira do tabuleiro para lançar um ataque. (Pressione [F] para continuar)");
    }

    static void AddCharacterText(Dictionary<string, string> text)
    {
        Add(text, "Charge!", "Avançar!");
        Add(text, "Send all units from the bottom 3 rows to attack immediatley, no need for fully formed lines. Damage based on the number of units cleared.",
            "Envia imediatamente todas as unidades das 3 linhas inferiores para atacar, sem precisar de linhas completas. O dano depende do número de unidades removidas.");
        Add(text, "Time Shift", "Dobra temporal");
        Add(text, "Reduce unit fall speed by 1/3 of its current speed fo 15 seconds.",
            "Reduz a velocidade de queda das unidades em 1/3 da velocidade atual por 15 segundos.");
        Add(text, "Natures Embrace", "Abraço da natureza");
        Add(text, "Heal all units on the board back to full health including those that have died.",
            "Restaura a vida de todas as unidades no tabuleiro, incluindo as que morreram.");
        Add(text, "Grock SMASH!", "Grock ESMAGA!");
        Add(text, "Let loose a war cry doubling all units on the boards health and attack damage for 10 seconds.",
            "Solta um grito de guerra que dobra a vida e o dano de ataque de todas as unidades no tabuleiro por 10 segundos.");
        Add(text, "Immutable Bulwark", "Baluarte imutável");
        Add(text, "All units become immune to damage for 12 seconds.",
            "Todas as unidades ficam imunes a dano por 12 segundos.");
    }

    static void AddHelpTopicText(Dictionary<string, string> text)
    {
        Add(text, "Boss Abilities", "Habilidades de chefe");
        Add(text, "Controls", "Controles");
        Add(text, "Floor Effects", "Efeitos de piso");
        Add(text, "Game Mechanics", "Mecânicas de jogo");
        Add(text, "Obstacles", "Obstáculos");
        Add(text, "Special Blocks", "Blocos especiais");
        Add(text, "Traps", "Armadilhas");
        Add(text, "Other", "Outros");

        Add(text, "Full Board Blast", "Explosão do tabuleiro inteiro");
        Add(text, "The boss will target all monster units set on the board dealing a small amount of damage to each one.",
            "O chefe mira todas as unidades de monstros colocadas no tabuleiro, causando uma pequena quantidade de dano a cada uma.");
        Add(text, "Increased Gravity", "Gravidade aumentada");
        Add(text, "The boss will temporarily increase gravity causing blocks to fall significantly faster for a set period of time.",
            "O chefe aumenta temporariamente a gravidade, fazendo os blocos caírem muito mais rápido por um período.");
        Add(text, "Invulnerable", "Invulnerável");
        Add(text, "The boss will temporarily become invulnerable. While invulnerable, the boss will take no damage from any sources.",
            "O chefe fica temporariamente invulnerável. Enquanto estiver invulnerável, não recebe dano de nenhuma fonte.");
        Add(text, "Lightning Strike", "Raio");
        Add(text, "The boss will target 1-3 individual cells with lightning bolts that will deal siginficant damage to any monster unit in that cell. Afterwards that cell will have a lightning floor effect that does continuous damage to any monster unit that occupies the tile.",
            "O chefe mira de 1 a 3 células individuais com raios que causam dano significativo a qualquer monstro nessa célula. Depois, a célula terá um efeito de piso elétrico que causa dano contínuo a qualquer monstro que ocupar o espaço.");
        Add(text, "Magic Explosive", "Explosivo mágico");
        Add(text, "The boss will spawn a single magical explosive as low on the board as possible. This explosive will detonate after 15 seconds and can only be safley removed by clearing the row it occupies.",
            "O chefe cria um explosivo mágico o mais baixo possível no tabuleiro. Ele detona após 15 segundos e só pode ser removido com segurança limpando a linha que ocupa.");
        Add(text, "Magic Shield", "Escudo mágico");
        Add(text, "The boss will spawn multiple magical pylon obstacles on the board. As long as the pylons remain on the board the boss will take 50% reduced damage from all sources.",
            "O chefe cria vários pilares mágicos no tabuleiro. Enquanto eles permanecerem, o chefe recebe 50% menos dano de todas as fontes.");
        Add(text, "Spawn FE's", "Gerar efeitos de piso");
        Add(text, "The boss will spawn multiple floor effects on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. Possible floor effect types are posion, fire, and lightning.",
            "O chefe cria vários efeitos de piso no tabuleiro. Eles podem aparecer espalhados individualmente ou em padrões 2x2, 1x4 ou 4x1. Os tipos possíveis são veneno, fogo e raio.");
        Add(text, "Spawn Obstacles", "Gerar obstáculos");
        Add(text, "The boss will spawn multiple obstacles on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns stone obstacles.",
            "O chefe cria vários obstáculos no tabuleiro. Eles podem aparecer espalhados individualmente ou em padrões 2x2, 1x4 ou 4x1. Esta habilidade só cria obstáculos de pedra.");
        Add(text, "Spawn Traps", "Gerar armadilhas");
        Add(text, "The boss will spawn multiple traps on the board. They can be spawned individually scattered across the board or in patterns of 2x2, 1x4, or 4x1. This ability only spawns spike traps.",
            "O chefe cria várias armadilhas no tabuleiro. Elas podem aparecer espalhadas individualmente ou em padrões 2x2, 1x4 ou 4x1. Esta habilidade só cria armadilhas de espinhos.");
        Add(text, "Row Blast", "Explosão de linhas");
        Add(text, "The boss will target the top three rows the player has set monster units. All monster units in the selected rows will recieve moderate damage from this attack.",
            "O chefe mira as três linhas superiores em que o jogador colocou unidades de monstros. Todos os monstros nas linhas selecionadas recebem dano moderado.");
        Add(text, "Activate Special", "Ativar especial");
        Add(text, "R - Activates the players special ability when the special gauge is charged to 100%",
            "R - ativa a habilidade especial do jogador quando a energia especial chega a 100%.");
        Add(text, "Movement", "Movimento");
        Add(text, "A - shifts the active piece one column to the left. S - shifts the active piece one row down. D - shifts the active piece one column to the right.",
            "A - move a peça ativa uma coluna para a esquerda.\n\nS - move a peça ativa uma linha para baixo.\n\nD - move a peça ativa uma coluna para a direita.");
        Add(text, "Pause", "Pausa");
        Add(text, "Escape - Will open the pause menu and pause all gameplay functions. Presseing escape while the pause menu is open will close the pause menu and resume gameplay.",
            "Escape - abre o menu de pausa e pausa todas as funções de jogo. Pressionar Escape com o menu aberto fecha o menu e retoma o jogo.");
        Add(text, "Quick Drop", "Queda rápida");
        Add(text, "Pressing spacebar will quick drop your active piece, setting it in place immediatley.",
            "Pressionar a barra de espaço faz a peça ativa cair rapidamente e ser fixada imediatamente.");
        Add(text, "Rotation", "Rotação");
        Add(text, "Q - Rotate the active piece 90 degrees counter-clockwise. E - Rotate the active piece 90 degrees clockwise.",
            "Q - gira a peça ativa 90 graus no sentido anti-horário.\n\nE - gira a peça ativa 90 graus no sentido horário.");
        Add(text, "Fire", "Fogo");
        Add(text, "Any monster unit set in a cell with the fire floor effect will take constant damage. The damage is realtivley low but occurs often.",
            "Qualquer monstro colocado em uma célula com efeito de piso de fogo recebe dano constante. O dano é relativamente baixo, mas ocorre com frequência.");
        Add(text, "Lightning", "Raio");
        Add(text, "Any monster unit set in a cell with the lightning floor effect will take constant damage. The damage is moderate but the floor effect will disappears after a period of time.",
            "Qualquer monstro colocado em uma célula com efeito de piso elétrico recebe dano constante. O dano é moderado, mas o efeito desaparece após um tempo.");
        Add(text, "Poison", "Veneno");
        Add(text, "Any monster unit set in a cell with the poison floor effect will take continuous damage. The damage is realtivley low but occurs often.",
            "Qualquer monstro colocado em uma célula com efeito de piso venenoso recebe dano contínuo. O dano é relativamente baixo, mas ocorre com frequência.");
        Add(text, "Attack Units", "Unidades de ataque");
        Add(text, "Attack units tend to have a higher attack stat than other unit types. They are best used to deal maximum damage to enemies to end levels more quickly. They cannot heal and tend to have average health stats.",
            "Unidades de ataque costumam ter ataque maior que outros tipos. São melhores para causar dano máximo aos inimigos e terminar níveis mais rápido. Elas não curam e geralmente têm vida média.");
        Add(text, "Currency", "Ouro");
        Add(text, "The player can gain currency from completing levels and rarely from clearing rows. Currency can be used to purchase various cosmetics, monster units, player characters, and permanent buffs to improve future runs. Your current currency can be found in the top right of the screen.",
            "O jogador ganha ouro ao concluir níveis e, raramente, ao limpar linhas. O ouro compra cosméticos, unidades de monstros, personagens e melhorias permanentes para futuras partidas. O ouro atual aparece no canto superior direito da tela.");
        Add(text, "Enemy Attack", "Ataque inimigo");
        Add(text, "The enemy castle will send constant attacks at the players monster units in an attempt to cull them. The enemies attacks will increase in power and frequency at higher levels.",
            "O castelo inimigo envia ataques constantes contra os monstros do jogador para reduzir suas forças. Os ataques inimigos ficam mais fortes e frequentes em níveis mais altos.");
        Add(text, "Healing Units", "Unidades curandeiras");
        Add(text, "Some units can heal other monster units to varying degrees. They tend to have much lower health and attack stats.",
            "Algumas unidades podem curar outros monstros em diferentes quantidades. Elas tendem a ter vida e ataque bem menores.");
        Add(text, "Landing Hint", "Indicador de aterrissagem");
        Add(text, "A red tint overlay appears in the location where the current active piece will fall.",
            "Uma sobreposição vermelha aparece no local onde a peça ativa atual cairá.");
        Add(text, "Loss Condition 1", "Condição de derrota 1");
        Add(text, "If the unit reserve reaches 0, the run will end with your loss! The unit reserve is reduced by one for every monster unit that dies on the board.",
            "Se a reserva de unidades chegar a 0, a partida terminará em derrota. A reserva é reduzida em um para cada monstro que morre no tabuleiro.");
        Add(text, "Loss Condition 2", "Condição de derrota 2");
        Add(text, "If a piece is set above the top row of the grid, the run will end with a loss!",
            "Se uma peça for fixada acima da linha superior da grade, a partida terminará em derrota.");
        Add(text, "Monster Units", "Unidades de monstros");
        Add(text, "Choose from multiple units to make up your warband. Each unit has their own individual stats that will make them more suitable for different roles. Some units have balanced stats and others are more specialized.",
            "Escolha várias unidades para formar seu bando de guerra. Cada unidade tem atributos próprios que a tornam mais adequada a diferentes funções. Algumas são equilibradas e outras mais especializadas.");
        Add(text, "Commander", "Comandante");
        Add(text, "Each Commander has their own unquie special ability that can be used in battle. New Commanders can be unlocked and set from the main menu.",
            "Cada comandante tem uma habilidade especial única que pode ser usada em batalha. Novos comandantes podem ser desbloqueados e definidos no menu principal.");
        Add(text, "Row Clear", "Limpeza de linha");
        Add(text, "Fill each grid cell in a row to launch an attack. Monster Units and Obstacles count as a filled cell. Floor effects and traps do not count as a filled cell. Monster units in the cleared row contribute their attack stats for damage calculation and partially fill the player's special gauge.",
            "Preencha todas as células de uma linha para lançar um ataque. Unidades de monstros e obstáculos contam como células preenchidas. Efeitos de piso e armadilhas não contam. Os monstros na linha limpa contribuem com seus ataques no cálculo de dano e preenchem parcialmente a energia especial do jogador.");
        Add(text, "Run Buffs", "Melhorias da partida");
        Add(text, "After succesfully completing a level you will be given three random buffs to choose from to enhance your current run. All buff modifiers will be reset when the run ends.",
            "Após concluir um nível com sucesso, você receberá três melhorias aleatórias para escolher e fortalecer a partida atual. Todos os modificadores de melhoria serão reiniciados quando a partida acabar.");
        Add(text, "Run Debuffs", "Penalidades da partida");
        Add(text, "After succesfully completing a level you will be given three random debuffs to choose from to increase the difficulty of your current run. All debuff modifiers will be reset when the run ends.",
            "Após concluir um nível com sucesso, você receberá três penalidades aleatórias para escolher e aumentar a dificuldade da partida atual. Todos os modificadores de penalidade serão reiniciados quando a partida acabar.");
        Add(text, "Clearing rows will earn the player points and add to their overall score. This can be used to measure the success of a run and compete with other players.",
            "Limpar linhas concede pontos ao jogador e aumenta a pontuação total. Isso pode medir o sucesso de uma partida e competir com outros jogadores.");
        Add(text, "Shop Buffs", "Melhorias da loja");
        Add(text, "The shop offers different buffs that can be purchased. Each buff is permanent and can be purchased multiple times. Each purchase will increase the price of the buffs next purchase level.",
            "A loja oferece diferentes melhorias que podem ser compradas. Cada melhoria é permanente e pode ser comprada várias vezes. Cada compra aumenta o preço do próximo nível dela.");
        Add(text, "Special Gauge", "Energia especial");
        Add(text, "Fills with every row cleared. When it reaches 100% you can use your Commander's unique special ability, resetting the special gauge back 0%.",
            "Preenche a cada linha limpa. Ao chegar a 100%, você pode usar a habilidade especial única do comandante, reiniciando a energia especial para 0%.");
        Add(text, "Tank Units", "Unidades tanque");
        Add(text, "Tank units tend to have significantly more health than other unit types. They are best used to outlast enemy attacks and protect weaker units. They cannot heal and tend to have low attack stats.",
            "Unidades tanque tendem a ter muito mais vida que outros tipos. São melhores para resistir a ataques inimigos e proteger unidades mais fracas. Não curam e costumam ter baixo ataque.");
        Add(text, "Unit Death", "Morte de unidade");
        Add(text, "When a monster units health drops to zero it dies. Dead units do not contribute their stats to an attack when their row is cleared or help fill up the players Special Gauge.",
            "Quando a vida de um monstro chega a zero, ele morre. Unidades mortas não contribuem com atributos para um ataque quando sua linha é limpa nem ajudam a preencher a energia especial do jogador.");
        Add(text, "Unit Reserve", "Reserva de unidades");
        Add(text, "When starting a new run the player will have a set limit of how many units they can afford to lose throughout the run. When a unit dies the reserve bar will be decreased. Suuccesfully completing a level will award the player up to 5 reinforcements for each victory up to the max unit reserve.",
            "Ao iniciar uma nova partida, o jogador tem um limite de quantas unidades pode perder ao longo dela. Quando uma unidade morre, a barra de reserva diminui. Concluir um nível com sucesso concede até 5 reforços por vitória, até o máximo da reserva de unidades.");
        Add(text, "Victory Condition", "Condição de vitória");
        Add(text, "Reduce the health of the enemy castle by clearing rows. When the enemy castle reaches 0 health, you win!",
            "Reduza a vida do castelo inimigo limpando linhas. Quando a vida do castelo chegar a 0, você vence!");
        Add(text, "Explosive", "Explosivo");
        Add(text, "The explosive obstacle will explode after a period of time killing all surrounding monster units. Can be safley disposed of by clearing its row. When safley disposed of it add 25 damage to that row clears attack. CAUTION: Using a bomb or lightning special block on the explosive will cause it to detonate.",
            "O obstáculo explosivo explode após um tempo, matando todos os monstros ao redor. Pode ser removido com segurança limpando sua linha. Ao ser removido com segurança, adiciona 25 de dano ao ataque dessa linha.\n\nCUIDADO: usar uma bomba ou bloco especial de raio no explosivo fará com que ele detone.");
        Add(text, "Magic Pylon", "Pilar mágico");
        Add(text, "When magic pylon obstacles are on the board enemies will take 50% reduced damage from all sources. Magic pylon can bee destroyed by clearing the row they occupy or using bomb and lightning special blocks.",
            "Quando pilares mágicos estão no tabuleiro, os inimigos recebem 50% menos dano de todas as fontes. Um pilar mágico pode ser destruído limpando a linha que ocupa ou usando blocos especiais de bomba e raio.");
        Add(text, "Stone", "Pedra");
        Add(text, "Stone obstacles can be spawned at the beggining of a level or by the boss. A lightning special block or clearing a row containing a stone obstacle will deal one damage to it. Stone obstacle need to be damaged 3 times to be removed. Exception: Using a bomb special block will instantly destroy a stone obstacle.",
            "Obstáculos de pedra podem surgir no início de um nível ou ser criados pelo chefe. Um bloco especial de raio ou limpar uma linha contendo pedra causa 1 de dano a ela. A pedra precisa receber 3 danos para ser removida. Exceção: um bloco especial de bomba destrói a pedra instantaneamente.");
        Add(text, "Bomb", "Bomba");
        Add(text, "The bomb special will detonate immediatley when set. All blocks in the surrounding tiles of its blast will be destroyed!",
            "O especial de bomba detonará imediatamente ao ser colocado. Todos os blocos nas células ao redor da explosão serão destruídos!");
        Add(text, "Death", "Morte");
        Add(text, "The Death special will activate immediatley when set on top of a monster unit. All monster units of the same type will be safley removed from the board with out decreasing your unit reserve.",
            "O especial de morte ativa imediatamente ao ser colocado sobre uma unidade de monstro. Todos os monstros do mesmo tipo serão removidos do tabuleiro com segurança sem diminuir sua reserva de unidades.");
        Add(text, "Earthquake", "Terremoto");
        Add(text, "The Earthquake special will activate immediatley when set. All blocks on the board will be dropped if not being supported by another tile beneath them. This effects obstacles as well that may otherwise not be able to be moved by other means.",
            "O especial de terremoto ativa imediatamente ao ser colocado. Todos os blocos no tabuleiro cairão se não estiverem apoiados por outra peça abaixo. Isso também afeta obstáculos que talvez não pudessem ser movidos de outra forma.");
        Add(text, "Lightning Bolt", "Raio");
        Add(text, "The Lightning Bolt special will activate immediatley when set. All monster units and traps will be destroyed in that column. Stone obstacles will take partial damage if they are in the affected area.",
            "O especial de raio ativa imediatamente ao ser colocado. Todas as unidades de monstros e armadilhas nessa coluna serão destruídas. Obstáculos de pedra receberão dano parcial se estiverem na área afetada.");
        Add(text, "Slow Gravity", "Gravidade lenta");
        Add(text, "The Slow Gravity special block will activate immedialtey upon being set. It will significantly reduce the speed at which pieces fall and the how quickly gravity increases over time.",
            "O bloco especial de gravidade lenta ativa imediatamente ao ser colocado. Ele reduz significativamente a velocidade de queda das peças e a rapidez com que a gravidade aumenta com o tempo.");
        Add(text, "Spike Trap", "Armadilha de espinhos");
        Add(text, "Spike traps will deal a high amount of damage to any monster unit that is set on the trapped cell when they are placed. The only way to destroyed spike traps are with the lightning special block.",
            "Armadilhas de espinhos causam muito dano a qualquer monstro colocado na célula armadilhada. A única forma de destruir armadilhas de espinhos é com o bloco especial de raio.");
    }

    static void AddLevelModifierText(Dictionary<string, string> text)
    {
        Add(text, "Spin to Win", "Gire para vencer");
        Add(text, "Active pieces will continuously rotate until set in place. Manual rotation will be locked.",
            "As peças ativas girarão continuamente até serem fixadas. A rotação manual ficará bloqueada.");
        Add(text, "Timing is Everything", "O tempo é tudo");
        Add(text, "Active pieces will continuously shift horizontally back and and forth across the board. Manual shifting will be locked.",
            "As peças ativas se moverão horizontalmente de um lado para o outro do tabuleiro. O deslocamento manual ficará bloqueado.");
        Add(text, "Go Big or Go Home", "Ou vai grande ou vai embora");
        Add(text, "Damage is significantly reduced for all attacks when the combo streak is less than 3.",
            "O dano de todos os ataques é significativamente reduzido quando a sequência de combo é menor que 3.");
        Add(text, "Break Out The Big Guns", "Traga a artilharia pesada");
        Add(text, "The enemy has fortified their position. A combo attack at 4 or higher will be required to remove each enemy shield. Damage dealt to the enemy while shielded is significantly reduced.",
            "O inimigo fortificou a posição. Será necessário um ataque de combo 4 ou maior para remover cada escudo inimigo. O dano causado ao inimigo enquanto estiver protegido é significativamente reduzido.");
        Add(text, "Contagion Outbreak", "Surto de contágio");
        Add(text, "Disease has begun to spread through the ranks. Close proximitiy has a chance to transfer from afflicted units to healthy units and will spread with certainity on the death of infected units.",
            "A doença começou a se espalhar pelas fileiras. A proximidade pode transferi-la de unidades afetadas para saudáveis, e ela se espalha com certeza quando unidades infectadas morrem.");
        Add(text, "Double Down", "Dobrar a aposta");
        Add(text, "All damage taken and dealt will be doubled!",
            "Todo dano recebido e causado será dobrado!");
        Add(text, "Exploding Corpses", "Corpos explosivos");
        Add(text, "Units will explode on death dealing damage to all surrounding units. Damage dealt is a percentage based off of the max health of the exploding unit.",
            "As unidades explodem ao morrer, causando dano a todas as unidades ao redor. O dano é baseado em uma porcentagem da vida máxima da unidade que explodiu.");
        Add(text, "Tis A Flesh Wound", "É só um ferimento");
        Add(text, "All ally units will start with half health.",
            "Todas as unidades aliadas começam com metade da vida.");
        Add(text, "Rations Running Low", "Rações acabando");
        Add(text, "Rations have begun to run out. Ally units have begun to starve trying to share the remaining rations. Units will take continuous damage proportional to the number of current reserve units.",
            "As rações começaram a acabar. As unidades aliadas passam fome tentando dividir o que resta. Elas recebem dano contínuo proporcional ao número atual de unidades de reserva.");
        Add(text, "Overgrowth", "Supercrescimento");
        Add(text, "Overgrowth has taken over the area consuming tiles and monsters. Overgrowth becomes more resilent to destruction once fully grown. Defeat the enemy before your army becomes mulch!",
            "O supercrescimento tomou a área, consumindo células e monstros. Quando amadurece totalmente, fica mais resistente à destruição. Derrote o inimigo antes que seu exército vire adubo!");
        Add(text, "No Retreat", "Sem retirada");
        Add(text, "Enemy ambush will cut off any retreat. Rows will slowly fill with enemy units progressivley limiting space to maneuver.",
            "Uma emboscada inimiga cortará qualquer retirada. As linhas se preencherão lentamente com unidades inimigas, limitando progressivamente o espaço de manobra.");
        Add(text, "Soul Link", "Vínculo de almas");
        Add(text, "All four units in a piece share a single health pool.",
            "As quatro unidades de uma peça compartilham uma única reserva de vida.");
        Add(text, "Back to the Basics", "De volta ao básico");
        Add(text, "Special blocks will not spawn.",
            "Blocos especiais não aparecerão.");
        Add(text, "Commander Special Lock", "Bloqueio do especial do comandante");
        Add(text, "Special ability gauge will be set to zero and locked.",
            "A energia especial será definida como zero e bloqueada.");
        Add(text, "Catastrophic Storm", "Tempestade catastrófica");
        Add(text, "An unrelenting storm has arrived and will blast the area with devestating lightning strikes.",
            "Uma tempestade implacável chegou e bombardeará a área com raios devastadores.");
        Add(text, "Miasma Marsh", "Pântano de miasma");
        Add(text, "The battlefield has shifted to the nearby marshes where deadly miasma drifts across the terrain.",
            "O campo de batalha mudou para os pântanos próximos, onde miasma mortal se espalha pelo terreno.");
    }

    static void AddRunModifierNames(Dictionary<string, string> text)
    {
        Add(text, "All Special Gain Down", "Ganho especial total reduzido");
        Add(text, "All Special Gain Up", "Ganho especial total aumentado");
        Add(text, "ATK Down", "Ataque reduzido");
        Add(text, "ATK Up", "Ataque aumentado");
        Add(text, "Currency Drop Up", "Queda de ouro aumentada");
        Add(text, "Debuffs Only", "Apenas penalidades");
        Add(text, "Enemy ATK Down", "Ataque inimigo reduzido");
        Add(text, "Enemy ATK SPD Down", "Velocidade de ataque inimiga reduzida");
        Add(text, "Enemy ATK SPD Up", "Velocidade de ataque inimiga aumentada");
        Add(text, "Enemy ATK Up", "Ataque inimigo aumentado");
        Add(text, "Enemy HP Up", "PV inimigo aumentado");
        Add(text, "Gravity Accel SPD Down", "Aceleração da gravidade reduzida");
        Add(text, "Gravity Accel SPD Up", "Aceleração da gravidade aumentada");
        Add(text, "Gravity Base SPD Down", "Velocidade base da gravidade reduzida");
        Add(text, "Gravity SPD Up", "Velocidade da gravidade aumentada");
        Add(text, "Healing Range Up", "Alcance de cura aumentado");
        Add(text, "Healing STR Up", "Força de cura aumentada");
        Add(text, "HP Down", "PV reduzido");
        Add(text, "HP Up", "PV aumentado");
        Add(text, "Luck Up", "Sorte aumentada");
        Add(text, "Misfortune Up", "Infortúnio aumentado");
        Add(text, "No Landing Indicator", "Sem indicador de aterrissagem");
        Add(text, "No Next Block Preview", "Sem prévia do próximo bloco");
        Add(text, "No Reinforcements", "Sem reforços");
        Add(text, "Reinforcements Down", "Reforços reduzidos");
        Add(text, "Reinforcements Up", "Reforços aumentados");
        Add(text, "Special Block Down", "Blocos especiais reduzidos");
        Add(text, "Special Block Up", "Blocos especiais aumentados");
        Add(text, "Special Gain Stat Down", "Ganho especial dos monstros reduzido");
        Add(text, "Special Gauge Stat Up", "Ganho especial dos monstros aumentado");
        Add(text, "Stone Buff Drop Down", "Melhorias de pedra reduzidas");
        Add(text, "Stone Buff Drop Up", "Melhorias de pedra aumentadas");
        Add(text, "Unit Reserve Down", "Reserva de unidades reduzida");
        Add(text, "Unit Reserve Up", "Reserva de unidades aumentada");
        Add(text, "Win Currency Down", "Ouro por vitória reduzido");
        Add(text, "Win Currency Up", "Ouro por vitória aumentado");
    }

    static void AddRunModifierFixedDescriptions(Dictionary<string, string> text)
    {
        Add(text, "A red tinted outline will no longer be shown where your pieces will land.",
            "O contorno vermelho não será mais exibido onde suas peças irão cair.");
        Add(text, "The next block will no longer be shown.",
            "O próximo bloco não será mais exibido.");
        Add(text, "Reinforcements will no longer arrive after winning a round.",
            "Reforços não chegarão mais após vencer uma rodada.");
        Add(text, "Stone obstacles no longer have a chance of dropping buffs and now only drop debuffs. Debuff drop chance is the same as prior buff drop chance.",
            "Obstáculos de pedra não têm mais chance de soltar melhorias e agora soltam apenas penalidades. A chance de penalidade é igual à antiga chance de melhoria.");
        Add(text, "Double the amount of currency gained occasionally when clearing lines.",
            "Dobra ocasionalmente o ouro ganho ao limpar linhas.");
        Add(text, "Triple the amount of currency gained occasionally when clearing lines.",
            "Triplica ocasionalmente o ouro ganho ao limpar linhas.");
        Add(text, "Qunituple the amount of currency gained occasionally when clearing lines.",
            "Quintuplica ocasionalmente o ouro ganho ao limpar linhas.");
        Add(text, "Increases the healing range of all friendly monsters by 1.",
            "Aumenta em 1 o alcance de cura de todos os monstros aliados.");
        Add(text, "Increases the healing range of all friendly monsters by 2.",
            "Aumenta em 2 o alcance de cura de todos os monstros aliados.");
        Add(text, "Increases the healing range of all friendly monsters by 3.",
            "Aumenta em 3 o alcance de cura de todos os monstros aliados.");
    }

    static void AddStatText(Dictionary<string, string> text)
    {
        Add(text, "Lines Cleared:", "Linhas limpas:");
        Add(text, "Special Used:", "Especial usado:");
        Add(text, "Obstacles Destroyed:", "Obstáculos destruídos:");
        Add(text, "Highest Combo:", "Maior combo:");
        Add(text, "Highest Single Attack:", "Maior ataque único:");
        Add(text, "Units Died:", "Unidades mortas:");
        Add(text, "Units Healed:", "Unidades curadas:");
        Add(text, "Total Damage Dealt:", "Dano total causado:");
        Add(text, "Clear Time:", "Tempo de conclusão:");
        Add(text, "Final Score:", "Pontuação final:");
        Add(text, "Lines", "Linhas");
        Add(text, "Times", "Vezes");
        Add(text, "Obstacles", "Obstáculos");
        Add(text, "Damage", "Dano");
        Add(text, "Units", "Unidades");
        Add(text, "Health", "Vida");
        Add(text, "Level {0}", "Nível {0}");
        Add(text, "{0} of {1} {2} discovered. Total codex unlocked {3}%",
            "{0} de {1} {2} descobertos. Códice total desbloqueado: {3}%");
        Add(text, "Buffs", "Melhorias");
        Add(text, "Debuffs", "Penalidades");
        Add(text, "Level Modifiers", "Modificadores de nível");
    }

    static bool TryTranslateRunModifierDescription(string lookupKey, out string portugueseText)
    {
        portugueseText = null;

        for (int i = 0; i < DegreePrefixes.Length; i++)
        {
            string englishPrefix = DegreePrefixes[i].English;
            if (!lookupKey.StartsWith(englishPrefix + " ", StringComparison.OrdinalIgnoreCase))
                continue;

            string remainder = lookupKey.Substring(englishPrefix.Length + 1).Trim();
            if (TryGetRunModifierTemplate(remainder, out string template))
            {
                portugueseText = string.Format(template, DegreePrefixes[i].Portuguese);
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

    static bool TryTranslateLabelValueLines(string englishText, out string portugueseText)
    {
        portugueseText = null;

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

        portugueseText = string.Join("\n", lines);
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

            translatedLine = leading + LinePrefixes[i].Portuguese + trimmed.Substring(englishPrefix.Length);
            return true;
        }

        return false;
    }

    static void Add(Dictionary<string, string> text, string english, string portuguese)
    {
        string key = NormalizeLookupKey(english);
        if (!string.IsNullOrEmpty(key))
            text[key] = portuguese;
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

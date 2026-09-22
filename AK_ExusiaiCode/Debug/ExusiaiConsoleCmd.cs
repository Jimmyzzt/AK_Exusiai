using System.Text;
using System.Text.Json;
using AK_Exusiai.Cards;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Diagnostics.DevConsole;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Debug;

/// <summary>
/// Extensible developer-console entry point for Exusiai utilities and combat test fixtures.
/// </summary>
public sealed class ExusiaiConsoleCmd : AbstractConsoleCmd
{
    private const int TestEnergy = 100;
    private const int MaxSafeReplayCount = 100;
    private static IReadOnlyDictionary<string, string>? _englishRelicTitles;

    private static readonly string[] ScenarioNames =
    [
        "ammo-partial",
        "ammo-snapshot",
        "ammo-multipliers",
        "overload",
        "explosive",
        "angel",
        "angel-x-empathy",
        "interference",
        "new-cards",
        "v1.1-core",
        "v1.1-multiplayer",
    ];

    private static IReadOnlyList<CardModel> DeliveryTestCards =>
    [
        ModelDb.Card<ViolentDelivery>(),
        ModelDb.Card<FreeDelivery>(),
        ModelDb.Card<PenguinExpress>(),
        ModelDb.Card<PenguinStandard>(),
        ModelDb.Card<PenguinInternational>(),
        ModelDb.Card<PenguinFreight>(),
        ModelDb.Card<GuaranteedSuccess>(),
        ModelDb.Card<Expedite>(),
        ModelDb.Card<LogisticsOutsourcing>(),
        ModelDb.Card<CargoInMotion>(),
    ];

    private static IReadOnlyList<CardModel> TransitTestCards =>
    [
        ModelDb.Card<ArmedEscort>(),
        ModelDb.Card<TheLordsProtection>(),
        ModelDb.Card<MoveOut>(),
        ModelDb.Card<PenguinTransitHub>(),
        ModelDb.Card<Package>(),
        ModelDb.Card<MayTheLordBeWithUs>(),
    ];

    private static readonly string[] Subcommands =
    [
        "help",
        "save",
        "fx",
        "scenario",
        "state",
        "logic",
        "hand",
        "replay",
        "ammo",
        "angel",
        "interference",
        "relics",
        "transit",
        "delivery",
    ];

    public override string CmdName => "exusiai";

    public override string Args => "<save|fx|scenario|state|logic|hand|replay|ammo|angel|interference|relics|transit|delivery> [args]";

    public override string Description =>
        "Runs AK_Exusiai utilities and combat test fixtures.";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            return Help();

        string subcommand = args[0].ToLowerInvariant();
        string[] subArgs = args.Skip(1).ToArray();
        return subcommand switch
        {
            "save" => ExusiaiSaveConsoleCmd.Process(subArgs),
            "fx" => ExusiaiEffectConsoleCmd.Process(issuingPlayer, subArgs),
            "scenario" or "test" => SetupScenario(issuingPlayer, subArgs),
            "state" => ShowState(issuingPlayer, subArgs),
            "logic" or "logistics" => SetupLogic(issuingPlayer, subArgs),
            "hand" => ShowHand(issuingPlayer, subArgs),
            "replay" => AddReplay(issuingPlayer, subArgs),
            "ammo" => SetAmmo(issuingPlayer, subArgs),
            "angel" => AddAngel(issuingPlayer, subArgs),
            "interference" => AddInterference(issuingPlayer, subArgs),
            "relics" or "relic" => ShowRelics(issuingPlayer, subArgs),
            "transit" => AddTransitRelic(issuingPlayer, subArgs),
            "delivery" => AddRelicDelivery(issuingPlayer, subArgs),
            _ => new CmdResult(
                success: false,
                $"Unknown Exusiai test command '{args[0]}'.\n{UsageSummary()}"),
        };
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            return CompleteArgument(
                Subcommands,
                [],
                args.FirstOrDefault() ?? string.Empty);
        }

        string subcommand = args[0].ToLowerInvariant();
        if (subcommand == "save")
        {
            if (args.Length == 2)
            {
                return CompleteArgument(
                    ["status", "import-vanilla"],
                    [args[0]],
                    args[1]);
            }

            if (args.Length == 3 && args[1].Equals("import-vanilla", StringComparison.OrdinalIgnoreCase))
            {
                return CompleteArgument(
                    ["confirm"],
                    [args[0], args[1]],
                    args[2]);
            }
        }

        if (subcommand == "fx" && args.Length == 2)
            return CompleteArgument(["on", "off", "status"], [args[0]], args[1]);
        if (subcommand is "scenario" or "test")
        {
            if (args.Length == 2)
            {
                return CompleteArgument(
                    ["list", .. ScenarioNames],
                    [args[0]],
                    args[1]);
            }

            if (args.Length == 3 && !args[1].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                return CompleteArgument(
                    ["base", "upgraded"],
                    [args[0], args[1]],
                    args[2]);
            }
        }

        if (subcommand is "logic" or "logistics" && args.Length == 2)
        {
            return CompleteArgument(
                ["delivery", "transit", "base", "upgraded"],
                [args[0]],
                args[1]);
        }

        if (subcommand is "logic" or "logistics" && args.Length == 3)
        {
            string[] candidates = args[1].Equals("base", StringComparison.OrdinalIgnoreCase) ||
                                  args[1].Equals("upgraded", StringComparison.OrdinalIgnoreCase)
                ? ["delivery", "transit"]
                : ["base", "upgraded"];
            return CompleteArgument(
                candidates,
                [args[0], args[1]],
                args[2]);
        }

        if (subcommand == "replay")
        {
            if (args.Length == 2)
            {
                return CompleteArgument(
                    GetHandIndices(player),
                    [args[0]],
                    args[1]);
            }

            if (args.Length == 3)
            {
                return CompleteArgument(
                    ["1", "2", "5", "10"],
                    [args[0], args[1]],
                    args[2]);
            }
        }

        if (subcommand == "ammo" && args.Length == 2)
        {
            return CompleteArgument(
                ["0", "5", "10", "16", "22", "30"],
                [args[0]],
                args[1]);
        }

        if (subcommand == "angel" && args.Length == 2)
        {
            return CompleteArgument(
                GetHandIndices(player),
                [args[0]],
                args[1]);
        }

        if (subcommand == "interference")
        {
            if (args.Length == 2)
            {
                return CompleteArgument(
                    GetEnemyIndices(player),
                    [args[0]],
                    args[1]);
            }

            if (args.Length == 3)
            {
                return CompleteArgument(
                    ["1", "5", "6", "10"],
                    [args[0], args[1]],
                    args[2]);
            }
        }

        if (subcommand is "relics" or "relic" && args.Length == 2)
        {
            return CompleteArgument(
                ["owned", "transit"],
                [args[0]],
                args[1]);
        }

        if (subcommand == "transit")
        {
            if (args.Length == 2)
            {
                return CompleteTransitRelicArgument(player, args[0], args[1]);
            }

            if (args.Length == 3)
            {
                return CompleteArgument(
                    ["1", "2", "3", "5"],
                    [args[0], args[1]],
                    args[2]);
            }
        }

        if (subcommand == "delivery")
        {
            if (args.Length == 2)
            {
                return CompleteArgument(
                    GetOwnedRelicIndices(player),
                    [args[0]],
                    args[1]);
            }

            if (args.Length == 3)
            {
                return CompleteArgument(
                    ["1", "2", "3", "5"],
                    [args[0], args[1]],
                    args[2]);
            }
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName,
        };
    }

    private static CmdResult Help() => new(
        success: true,
        "[gold]Exusiai commands[/gold]\n" +
        "  exusiai save status - Compare Vanilla and Modded progress for the current profile slot.\n" +
        "  exusiai save import-vanilla confirm - Back up and replace this slot's Modded progress with its Vanilla progress. Main menu only.\n" +
        "  exusiai fx <on|off|status> - Control the single-player card effect preview bridge.\n" +
        "  exusiai scenario <name> [base|upgraded] - Build an isolated V1 combat fixture; use 'scenario list' for names.\n" +
        "  exusiai state - Show Ammo, relevant player powers, and indexed enemy Interference/Silence.\n" +
        "  exusiai logic [delivery|transit] [base|upgraded] - Replace the hand with new relic-logistics test cards and set energy to 100.\n" +
        "  exusiai hand - List the current hand with zero-based indices and test-relevant state.\n" +
        "  exusiai replay <hand-index> <amount> - Add Replay to one card (maximum total: 100).\n" +
        "  exusiai ammo <amount> - Set ammunition (0-30).\n" +
        "  exusiai angel <hand-index> - Give or refresh Angel on one card.\n" +
        "  exusiai interference <enemy-index> <amount> - Add Interference and resolve crossed Silence thresholds.\n" +
        "  exusiai relics [owned|transit] - List current relic indices or the deterministic Transit relic pool.\n" +
        "  exusiai transit <relic-id|name|pool-index> [amount=1] - Obtain or refresh a specific Transit relic; press Tab to search by localized or English name.\n" +
        "  exusiai delivery <relic-index> <amount> - Give Delivery to a current relic."
    );

    private static string UsageSummary() =>
        "Use 'exusiai help' for the available test commands.";

    private static CmdResult SetupScenario(Player? player, string[] args)
    {
        if (args.Length == 1 && args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            return ScenarioList();
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;
        if (args.Length is < 1 or > 2)
            return ScenarioUsage();

        bool upgraded = false;
        if (args.Length == 2)
        {
            if (args[1].Equals("upgraded", StringComparison.OrdinalIgnoreCase))
                upgraded = true;
            else if (!args[1].Equals("base", StringComparison.OrdinalIgnoreCase))
                return ScenarioUsage();
        }

        ScenarioDefinition? scenario = CreateScenario(args[0].ToLowerInvariant());
        if (scenario == null)
            return ScenarioUsage($"Unknown scenario '{args[0]}'.");
        if (scenario.RequiresEnemy && combatPlayer.Creature.CombatState!.HittableEnemies.Count == 0)
        {
            return new CmdResult(
                success: false,
                "This scenario needs at least one living enemy.");
        }
        if (scenario.Hand.Count > CardPile.MaxCardsInHand)
        {
            return new CmdResult(
                success: false,
                $"Scenario '{scenario.Name}' contains {scenario.Hand.Count} hand cards, exceeding the hand limit of {CardPile.MaxCardsInHand}.");
        }

        Task task = SetupScenarioAsync(combatPlayer, scenario, upgraded);
        return new CmdResult(
            task,
            success: true,
            $"Building isolated scenario '{scenario.Name}' ({(upgraded ? "upgraded" : "base")}).\n[gold]Check[/gold] {scenario.Checks}\nRun 'exusiai state' and 'exusiai hand' to inspect it.");
    }

    private static CmdResult ScenarioList() => new(
        success: true,
        "[gold]Exusiai V1 scenarios[/gold]\n" +
        "  ammo-partial - 5 Ammo against 8-hit, 4-hit, and AOE attacks; rerun before each card.\n" +
        "  ammo-snapshot - 16 Ammo with Sweeping the Skies; verify 5x8 damage and 8 Ammo left.\n" +
        "  ammo-multipliers - Double-Shot Kit, Paganini, ancient multiplier, and spend-all attacks.\n" +
        "  overload - 30 Ammo, Overload cards, and exactly five discard-pile attacks for Shootoholic.\n" +
        "  explosive - Three Explosive Ammo copies plus single-hit, multi-hit, and AOE attacks.\n" +
        "  angel - Confession/Empathy, Angel refresh, natural Angels, and curse-only exhaustion.\n" +
        "  angel-x-empathy - X-cost Angel payment and Empathy's no-replay behavior.\n" +
        "  interference - First enemy starts at I10/S0; add 6 to verify two crossed Silence thresholds.\n" +
        "  new-cards - The six cards introduced by the V1 card-list conversion.\n" +
        "  v1.1-core - V1.1 single-player powers, Covenant queueing, and temporary Soar.\n" +
        "  v1.1-multiplayer - V1.1 multiplayer cards and Compassion transfer behavior.\n" +
        "Use: exusiai scenario <name> [base|upgraded]. Scenario setup replaces all combat-pile copies, never the run deck."
    );

    private static CmdResult ScenarioUsage(string? prefix = null) => new(
        success: false,
        (prefix == null ? string.Empty : prefix + "\n") +
        "Usage: exusiai scenario <ammo-partial|ammo-snapshot|ammo-multipliers|overload|explosive|angel|angel-x-empathy|interference|new-cards|v1.1-core|v1.1-multiplayer> [base|upgraded]\n" +
        "Use 'exusiai scenario list' for expected checks.");

    private static ScenarioDefinition? CreateScenario(string name) => name switch
    {
        "ammo-partial" => new ScenarioDefinition(
            name,
            5,
            [
                ModelDb.Card<SweepingTheSkies>(),
                ModelDb.Card<ShootingMode>(),
                ModelDb.Card<WelcomeToLaterano>(),
            ],
            [],
            [],
            0,
            ClearInitialSilence: false,
            "With no other modifiers, the 8-hit attack enhances only its first five executed hits and spends exactly 5 Ammo. Rerun before testing each card; one AOE hit spends only one Ammo."),
        "ammo-snapshot" => new ScenarioDefinition(
            name,
            16,
            [
                ModelDb.Card<SweepingTheSkies>(),
                ModelDb.Card<ChargingMode>(),
            ],
            [],
            [],
            0,
            ClearInitialSilence: false,
            "Base Sweeping the Skies should deal (2+3)x8=40 total before target modifiers, spend 8 Ammo, and keep the +3 snapshot for all eight hits."),
        "ammo-multipliers" => new ScenarioDefinition(
            name,
            10,
            [
                ModelDb.Card<DoubleShotKit>(),
                ModelDb.Card<PaganiniCustom>(),
                ModelDb.Card<BarrageSkyShattering>(),
                ModelDb.Card<LaserCannon>(),
                ModelDb.Card<ForgetMeNot>(),
            ],
            [],
            [],
            0,
            ClearInitialSilence: false,
            "Verify Double-Shot and Paganini add in the common multiplier, the ancient card uses its independent multiplier, and Laser Cannon/Forget-Me-Not aggregate every actually paid Ammo."),
        "overload" => new ScenarioDefinition(
            name,
            AmmoResource.MaxAmount,
            [
                ModelDb.Card<FlammableAndExplosive>(),
                ModelDb.Card<Shootoholic>(),
                ModelDb.Card<LaserCannon>(),
                ModelDb.Card<ForgetMeNot>(),
                ModelDb.Card<PartyTime>(),
            ],
            [],
            [
                ModelDb.Card<SweepingTheSkies>(),
                ModelDb.Card<ChargingMode>(),
                ModelDb.Card<WelcomeToLaterano>(),
                ModelDb.Card<ShootingMode>(),
                ModelDb.Card<PrecisionStrike>(),
            ],
            0,
            ClearInitialSilence: false,
            "Ammo must stay at 30 and cannot be gained. Shootoholic must use its 5-Ammo prepaid bonus on exactly five prepared attacks; spend-all attacks use the 30-Ammo bonus."),
        "explosive" => new ScenarioDefinition(
            name,
            10,
            [
                ModelDb.Card<ExplosiveAmmo>(),
                ModelDb.Card<ExplosiveAmmo>(),
                ModelDb.Card<ExplosiveAmmo>(),
                ModelDb.Card<ExusiaiStrike>(),
                ModelDb.Card<ChargingMode>(),
                ModelDb.Card<WelcomeToLaterano>(),
            ],
            [],
            [],
            0,
            ClearInitialSilence: false,
            "Pair one Explosive Ammo with each attack. Its single AOE follow-up must equal 50% of that card's total actual damage across hits and targets; stacked copies apply to successive attacks."),
        "angel" => new ScenarioDefinition(
            name,
            0,
            [
                ModelDb.Card<Confession>(),
                ModelDb.Card<EmpathyForm>(),
                ModelDb.Card<HolyCityMercy>(),
                ModelDb.Card<CrossOfDevotion>(),
                ModelDb.Card<HolyCityPurge>(),
                ModelDb.Card<ApplePieWithCharSiu>(),
                ModelDb.Card<ExusiaiStrike>(),
                ModelDb.Card<LookingBack>(),
                ModelDb.Card<Clumsy>(),
                ModelDb.Card<Wound>(),
            ],
            [
                ModelDb.Card<ExusiaiStrike>(),
                ModelDb.Card<ExusiaiDefend>(),
                ModelDb.Card<LockedAndLoaded>(),
            ],
            [],
            0,
            ClearInitialSilence: false,
            "Rerun between paths. Play Empathy, advance to the next turn, choose a hand card (an existing Angel is valid), and verify each Angel played draws exactly 1 card. Curse cards Angelized by other effects exhaust immediately; Status cards do not. Separately, give Strike Angel via Cross, play it, return it with Looking Back, then use 'exusiai angel <index>' to refresh its free play."),
        "angel-x-empathy" => new ScenarioDefinition(
            name,
            0,
            [
                ModelDb.Card<EmpathyForm>(),
                ModelDb.Card<LoadEmUp>(),
                ModelDb.Card<ExusiaiStrike>(),
            ],
            [],
            [],
            0,
            ClearInitialSilence: false,
            "Play Empathy Form, advance to the next turn and choose Load 'Em Up. The Angel X-cost card must still spend all remaining Energy and resolve normally. Every Angel play draws exactly 1 card and must not recursively replay."),
        "interference" => new ScenarioDefinition(
            name,
            10,
            [
                ModelDb.Card<RockNRoll>(),
                ModelDb.Card<RockNGospel>(),
                ModelDb.Card<Brawl>(),
                ModelDb.Card<DisruptiveStrike>(),
                ModelDb.Card<OutstandingGraduate>(),
                ModelDb.Card<TargetTheWeakSpot>(),
                ModelDb.Card<Talent>(),
            ],
            [],
            [],
            10,
            ClearInitialSilence: true,
            "Enemy 0 starts at Interference 10 and Silence 0. Run 'exusiai interference 0 6': it must become I16/S2; verify damage reduction caps at 50%, per-layer attack hits, Firepower condition, and Talent scaling."),
        "new-cards" => new ScenarioDefinition(
            name,
            10,
            [
                ModelDb.Card<Star>(),
                ModelDb.Card<RockNRoll>(),
                ModelDb.Card<DisruptiveStrike>(),
                ModelDb.Card<Brawl>(),
                ModelDb.Card<CovenantOfBullets>(),
                ModelDb.Card<LogisticsSupport>(),
                ModelDb.Card<Piety>(),
                ModelDb.Card<ChaoticRampage>(),
                ModelDb.Card<Wayward>(),
            ],
            [],
            [],
            5,
            ClearInitialSilence: true,
            "Verify the V1.2 cards and reworks: Star hand upgrades, Interference scaling, Covenant discard retrieval plus immediate Ammo, Piety draw-pile Angel, Chaotic Rampage drawing 2/3 then adding Wayward to discard, and Wayward penalties only on exhaust or in-combat transform."),
        "v1.1-core" => new ScenarioDefinition(
            name,
            10,
            [
                ModelDb.Card<CovenantOfBullets>(),
                ModelDb.Card<LogisticsSupport>(),
                ModelDb.Card<PatronFirearm>(),
                ModelDb.Card<TheLordsForgiveness>(),
                ModelDb.Card<Blessing>(),
                ModelDb.Card<RadiantWingStrike>(),
                ModelDb.Card<LogisticsOutsourcing>(),
                ModelDb.Card<HolyCityCalling>(),
            ],
            [],
            [ModelDb.Card<ExusiaiStrike>(), ModelDb.Card<ExusiaiDefend>()],
            0,
            ClearInitialSilence: false,
            "Test both upgrade states. Covenant must retrieve 1 discard card, grant Angel, immediately gain Ammo equal to hand size (+2 upgraded), and end the turn. Logistics Support draws per 4/3 actual Ammo spent; Patron Firearm grants 2/3 unpowered Block per Ammo; Forgiveness grants 3/4 Block per retained Angel; Radiant Wing Strike gains temporary Soar after five paid or overload-backed hits."),
        "v1.1-multiplayer" => new ScenarioDefinition(
            name,
            10,
            [
                ModelDb.Card<Karaoke>(),
                ModelDb.Card<Unboxing>(),
                ModelDb.Card<AngelsBlessings>(),
                ModelDb.Card<YaneseCanFly>(),
                ModelDb.Card<Compassion>(),
                ModelDb.Card<Talent>(),
                ModelDb.Card<LogisticsSupport>(),
                ModelDb.Card<NecklaceOfThePresence>(),
            ],
            [],
            [],
            0,
            ClearInitialSilence: false,
            "Run in multiplayer. Random results must be rolled separately per player. Compassion must refresh existing Angels, allow any Angel Power card to target a teammate, charge/remove the caster's card and trigger caster play hooks, then make the recipient execute the full free clone exactly once with its replay, enchantment, and affliction effects."),
        _ => null,
    };

    private static async Task SetupScenarioAsync(
        Player player,
        ScenarioDefinition scenario,
        bool upgraded)
    {
        HookPlayerChoiceContext choiceContext = new(
            player,
            player.NetId,
            GameActionType.Combat);
        Task setupTask = SetupScenarioWithContextAsync(choiceContext, player, scenario, upgraded);
        await choiceContext.AssignTaskAndWaitForPauseOrCompletion(setupTask);
    }

    private static async Task SetupScenarioWithContextAsync(
        HookPlayerChoiceContext choiceContext,
        Player player,
        ScenarioDefinition scenario,
        bool upgraded)
    {
        await ClearScenarioPowers(player);
        await SecondaryResourceCmd.Set(player, AmmoResource.Id, 0, player.Character);

        List<CardModel> oldCombatCards = CardPile.GetCards(
                player,
                PileType.Draw,
                PileType.Hand,
                PileType.Discard,
                PileType.Exhaust,
                PileType.Play)
            .ToList();
        await CardPileCmd.RemoveFromCombat(oldCombatCards, skipVisuals: true);

        await PlayerCmd.SetEnergy(TestEnergy, player);
        await AddScenarioCards(player, scenario.Hand, PileType.Hand, upgraded);
        await AddScenarioCards(player, scenario.Draw, PileType.Draw, upgraded);
        await AddScenarioCards(player, scenario.Discard, PileType.Discard, upgraded);

        if (scenario.InitialInterference > 0 &&
            player.Creature.CombatState!.HittableEnemies.FirstOrDefault() is { } firstEnemy)
        {
            await InterferenceCmd.Apply(
                choiceContext,
                firstEnemy,
                scenario.InitialInterference,
                player.Creature,
                null);
            if (scenario.ClearInitialSilence && firstEnemy.GetPower<SilencePower>() is { } silence)
                await PowerCmd.Remove(silence);
        }

        await SecondaryResourceCmd.Set(player, AmmoResource.Id, scenario.Ammo, player.Character);
    }

    private static async Task AddScenarioCards(
        Player player,
        IReadOnlyList<CardModel> canonicals,
        PileType pile,
        bool upgraded)
    {
        if (canonicals.Count == 0)
            return;

        List<CardModel> cards = canonicals
            .Select(canonical => player.Creature.CombatState!.CreateCard(canonical, player))
            .ToList();
        if (upgraded)
        {
            foreach (CardModel card in cards.Where(card => card.IsUpgradable))
                CardCmd.Upgrade(card);
        }

        await CardPileCmd.AddGeneratedCardsToCombat(cards, pile, player);
    }

    private static async Task ClearScenarioPowers(Player player)
    {
        IEnumerable<Creature> participants =
            [.. player.Creature.CombatState!.Players.Select(p => p.Creature), .. player.Creature.CombatState.HittableEnemies];
        foreach (Creature creature in participants)
        {
            List<PowerModel> powers = creature.Powers
                .Where(power => power.GetType().Namespace?.StartsWith("AK_Exusiai", StringComparison.Ordinal) == true)
                .ToList();
            foreach (PowerModel power in powers)
                await PowerCmd.Remove(power);
        }
    }

    private sealed record ScenarioDefinition(
        string Name,
        int Ammo,
        IReadOnlyList<CardModel> Hand,
        IReadOnlyList<CardModel> Draw,
        IReadOnlyList<CardModel> Discard,
        int InitialInterference,
        bool ClearInitialSilence,
        string Checks,
        bool RequiresEnemy = true);

    private static CmdResult SetupLogic(Player? player, string[] args)
    {
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;

        if (!TryParseLogicArgs(args, out bool useTransitCards, out bool upgraded))
        {
            return new CmdResult(
                success: false,
                "Usage: exusiai logic [delivery|transit] [base|upgraded]");
        }

        IReadOnlyList<CardModel> canonicals = useTransitCards
            ? TransitTestCards
            : DeliveryTestCards;
        if (canonicals.Count > CardPile.MaxCardsInHand)
        {
            return new CmdResult(
                success: false,
                $"The selected test group contains {canonicals.Count} cards, exceeding the hand limit of {CardPile.MaxCardsInHand}. Split the group before running it.");
        }

        Task task = SetupLogicAsync(combatPlayer, canonicals, upgraded);
        return new CmdResult(
            task,
            success: true,
            $"Replacing the hand with {canonicals.Count} {(useTransitCards ? "Transit" : "Delivery")} relic-logistics cards ({(upgraded ? "upgraded" : "base")}) and setting energy to {TestEnergy}.");
    }

    private static bool TryParseLogicArgs(
        string[] args,
        out bool useTransitCards,
        out bool upgraded)
    {
        useTransitCards = false;
        upgraded = false;
        bool groupSpecified = false;
        bool upgradeSpecified = false;

        foreach (string arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "delivery":
                case "1":
                    if (groupSpecified)
                        return false;
                    useTransitCards = false;
                    groupSpecified = true;
                    break;
                case "transit":
                case "2":
                    if (groupSpecified)
                        return false;
                    useTransitCards = true;
                    groupSpecified = true;
                    break;
                case "base":
                    if (upgradeSpecified)
                        return false;
                    upgraded = false;
                    upgradeSpecified = true;
                    break;
                case "upgraded":
                    if (upgradeSpecified)
                        return false;
                    upgraded = true;
                    upgradeSpecified = true;
                    break;
                default:
                    return false;
            }
        }

        return args.Length <= 2;
    }

    private static async Task SetupLogicAsync(
        Player player,
        IReadOnlyList<CardModel> canonicals,
        bool upgraded)
    {
        HookPlayerChoiceContext choiceContext = new(
            player,
            player.NetId,
            GameActionType.Combat);
        Task setupTask = SetupLogicWithContextAsync(
            choiceContext,
            player,
            canonicals,
            upgraded);
        await choiceContext.AssignTaskAndWaitForPauseOrCompletion(setupTask);
    }

    private static async Task SetupLogicWithContextAsync(
        HookPlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> canonicals,
        bool upgraded)
    {
        await PlayerCmd.SetEnergy(TestEnergy, player);

        CardPile hand = player.PlayerCombatState!.Hand;
        await CardCmd.Discard(choiceContext, hand.Cards.ToList());

        List<CardModel> cards = canonicals
            .Select(canonical => player.Creature.CombatState!.CreateCard(canonical, player))
            .ToList();
        if (upgraded)
        {
            foreach (CardModel card in cards.Where(card => card.IsUpgradable))
                CardCmd.Upgrade(card);
        }

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, player);
    }

    private static CmdResult ShowHand(Player? player, string[] args)
    {
        if (args.Length != 0)
            return new CmdResult(success: false, "Usage: exusiai hand");
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;

        CardPile hand = combatPlayer.PlayerCombatState!.Hand;
        if (hand.Cards.Count == 0)
            return new CmdResult(success: true, "The hand is empty.");

        StringBuilder message = new();
        message.AppendLine($"Energy: {combatPlayer.PlayerCombatState.Energy}; Ammo: {SecondaryResourceCmd.Get(combatPlayer, AmmoResource.Id)}");
        for (int i = 0; i < hand.Cards.Count; i++)
        {
            CardModel card = hand.Cards[i];
            message.Append($"[{i}] {card.Title} ({card.Id.Entry}) replay={card.BaseReplayCount}");
            if (AngelCmd.IsAngel(card))
                message.Append(" angel");
            if (card.IsUpgraded)
                message.Append(" upgraded");
            message.AppendLine();
        }

        return new CmdResult(success: true, message.ToString().TrimEnd());
    }

    private static CmdResult ShowState(Player? player, string[] args)
    {
        if (args.Length != 0)
            return new CmdResult(success: false, "Usage: exusiai state");
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;

        StringBuilder message = new();
        int ammo = SecondaryResourceCmd.Get(combatPlayer, AmmoResource.Id);
        AmmoResource.AmmoDamageBreakdown breakdown = AmmoResource.GetCurrentDamageBreakdown(combatPlayer);
        message.AppendLine(
            $"Player HP {combatPlayer.Creature.CurrentHp}/{combatPlayer.Creature.MaxHp}; Energy {combatPlayer.PlayerCombatState!.Energy}; Ammo {ammo}; per-Ammo damage +{breakdown.DamagePerAmmo:0.##}");
        message.AppendLine($"Player powers: {FormatPowers(combatPlayer.Creature)}");

        IReadOnlyList<Creature> enemies = combatPlayer.Creature.CombatState!.HittableEnemies;
        if (enemies.Count == 0)
        {
            message.Append("Enemies: none");
        }
        else
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                Creature enemy = enemies[i];
                int interference = enemy.GetPower<InterferencePower>()?.Amount ?? 0;
                int silence = enemy.GetPower<SilencePower>()?.Amount ?? 0;
                message.AppendLine(
                    $"Enemy [{i}] {enemy.Monster?.Id.Entry ?? "unknown"} HP {enemy.CurrentHp}/{enemy.MaxHp}; Interference {interference}; Silence {silence}; powers: {FormatPowers(enemy)}");
            }
        }

        return new CmdResult(success: true, message.ToString().TrimEnd());
    }

    private static string FormatPowers(Creature creature)
    {
        if (creature.Powers.Count == 0)
            return "none";
        return string.Join(", ", creature.Powers.Select(power => $"{power.Id.Entry}={power.Amount}"));
    }

    private static CmdResult AddReplay(Player? player, string[] args)
    {
        if (!TryGetHandCard(player, args, "replay", out CardModel card, out int amount, out CmdResult error))
            return error;
        if (amount < 0)
            return new CmdResult(success: false, "Replay amount cannot be negative.");
        if (card.BaseReplayCount + amount > MaxSafeReplayCount)
        {
            return new CmdResult(
                success: false,
                $"Replay total cannot exceed {MaxSafeReplayCount}; requested {card.BaseReplayCount + amount}.");
        }

        int oldAmount = card.BaseReplayCount;
        card.BaseReplayCount += amount;
        return new CmdResult(
            success: true,
            $"Added {amount} Replay to '{card.Title}': {oldAmount} -> {card.BaseReplayCount} (effective {card.GetEnchantedReplayCount()}).");
    }

    private static CmdResult SetAmmo(Player? player, string[] args)
    {
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;
        if (args.Length != 1 || !int.TryParse(args[0], out int amount))
            return new CmdResult(success: false, "Usage: exusiai ammo <amount:int>");
        if (amount is < 0 or > 30)
            return new CmdResult(success: false, "Ammunition must be between 0 and 30.");

        Task task = SecondaryResourceCmd.Set(
            combatPlayer,
            AmmoResource.Id,
            amount,
            combatPlayer.Character);
        return new CmdResult(task, success: true, $"Setting ammunition to {amount}.");
    }

    private static CmdResult AddAngel(Player? player, string[] args)
    {
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;
        if (args.Length != 1 || !int.TryParse(args[0], out int handIndex))
            return new CmdResult(success: false, "Usage: exusiai angel <hand-index:int>");

        IReadOnlyList<CardModel> hand = combatPlayer.PlayerCombatState!.Hand.Cards;
        if (handIndex < 0 || handIndex >= hand.Count)
        {
            return new CmdResult(
                success: false,
                hand.Count == 0
                    ? "The hand is empty; there is no card index to target."
                    : $"Invalid hand index {handIndex}. Valid range: 0-{hand.Count - 1}.");
        }

        CardModel card = hand[handIndex];
        Task task = AddAngelAsync(combatPlayer, card);
        return new CmdResult(
            task,
            success: true,
            $"Giving or refreshing Angel on '{card.Title}'. Curse cards will exhaust immediately.");
    }

    private static async Task AddAngelAsync(Player player, CardModel card)
    {
        HookPlayerChoiceContext choiceContext = new(player, player.NetId, GameActionType.Combat);
        Task task = AngelCmd.Add(choiceContext, card);
        await choiceContext.AssignTaskAndWaitForPauseOrCompletion(task);
    }

    private static CmdResult AddInterference(Player? player, string[] args)
    {
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;
        if (args.Length != 2 ||
            !int.TryParse(args[0], out int enemyIndex) ||
            !int.TryParse(args[1], out int amount))
        {
            return new CmdResult(
                success: false,
                "Usage: exusiai interference <enemy-index:int> <amount:int>");
        }
        if (amount <= 0 || amount > 100)
            return new CmdResult(success: false, "Interference amount must be between 1 and 100.");

        IReadOnlyList<Creature> enemies = combatPlayer.Creature.CombatState!.HittableEnemies;
        if (enemyIndex < 0 || enemyIndex >= enemies.Count)
        {
            return new CmdResult(
                success: false,
                enemies.Count == 0
                    ? "There are no living enemies to target."
                    : $"Invalid enemy index {enemyIndex}. Valid range: 0-{enemies.Count - 1}.");
        }

        Creature enemy = enemies[enemyIndex];
        int oldInterference = enemy.GetPower<InterferencePower>()?.Amount ?? 0;
        int oldSilence = enemy.GetPower<SilencePower>()?.Amount ?? 0;
        Task task = AddInterferenceAsync(combatPlayer, enemy, amount);
        return new CmdResult(
            task,
            success: true,
            $"Adding {amount} Interference to enemy [{enemyIndex}] from I{oldInterference}/S{oldSilence}. Run 'exusiai state' after resolution to verify crossed thresholds.");
    }

    private static async Task AddInterferenceAsync(Player player, Creature enemy, int amount)
    {
        HookPlayerChoiceContext choiceContext = new(player, player.NetId, GameActionType.Combat);
        Task task = InterferenceCmd.Apply(
            choiceContext,
            enemy,
            amount,
            player.Creature,
            null);
        await choiceContext.AssignTaskAndWaitForPauseOrCompletion(task);
    }

    private static CmdResult ShowRelics(Player? player, string[] args)
    {
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;
        if (args.Length > 1 ||
            args.Length == 1 &&
            !args[0].Equals("owned", StringComparison.OrdinalIgnoreCase) &&
            !args[0].Equals("transit", StringComparison.OrdinalIgnoreCase))
        {
            return new CmdResult(
                success: false,
                "Usage: exusiai relics [owned|transit]");
        }

        bool showOwned = args.Length == 0 ||
                         args[0].Equals("owned", StringComparison.OrdinalIgnoreCase);
        bool showTransitPool = args.Length == 0 ||
                               args[0].Equals("transit", StringComparison.OrdinalIgnoreCase);
        StringBuilder message = new();

        if (showOwned)
        {
            IReadOnlyList<RelicModel> relics = GetIndexedOwnedRelics(combatPlayer);
            message.AppendLine("[gold]Current relics (use with 'exusiai delivery')[/gold]");
            if (relics.Count == 0)
            {
                message.AppendLine("  none");
            }
            else
            {
                for (int i = 0; i < relics.Count; i++)
                {
                    RelicModel relic = relics[i];
                    RelicLogisticsCapability? logistics =
                        relic.Capability<RelicLogisticsCapability>();
                    message.Append($"  [{i}] {relic.Title} ({relic.Id.Entry})");
                    if (logistics?.IsTransit == true)
                    {
                        message.Append(
                            $" transit={logistics.TransitRemaining}{(logistics.IsExpiredTransit ? " expired" : string.Empty)}");
                    }
                    if (logistics?.DeliveryRemaining > 0)
                        message.Append($" delivery={logistics.DeliveryRemaining}");
                    if (!RelicLogisticsCmd.IsDeliveryTarget(relic))
                        message.Append(" [not a valid Delivery target]");
                    message.AppendLine();
                }
            }
        }

        if (showOwned && showTransitPool)
            message.AppendLine();

        if (showTransitPool)
        {
            IReadOnlyList<RelicModel> pool = GetIndexedTransitPool(combatPlayer);
            message.AppendLine("[gold]Transit relic pool (use with 'exusiai transit')[/gold]");
            for (int i = 0; i < pool.Count; i++)
                message.AppendLine($"  [{i}] {pool[i].Title} ({pool[i].Id.Entry})");
        }

        return new CmdResult(success: true, message.ToString().TrimEnd());
    }

    private static CmdResult AddTransitRelic(Player? player, string[] args)
    {
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;
        if (args.Length is < 1 or > 2 ||
            args.Length == 2 && !int.TryParse(args[1], out _))
        {
            return new CmdResult(
                success: false,
                "Usage: exusiai transit <relic-id|name|pool-index> [amount:int=1]");
        }

        int amount = args.Length == 2 ? int.Parse(args[1]) : 1;
        if (amount is < 1 or > 999)
            return new CmdResult(success: false, "Transit amount must be between 1 and 999.");

        IReadOnlyList<RelicModel> pool = GetIndexedTransitPool(combatPlayer);
        if (!TryResolveTransitRelic(pool, args[0], out RelicModel relic, out int? poolIndex, out string resolveError))
            return new CmdResult(success: false, resolveError);

        Task task = RelicLogisticsCmd.AddNamedTransit(combatPlayer, relic, amount);
        return new CmdResult(
            task,
            success: true,
            $"Giving Transit {amount} for{(poolIndex.HasValue ? $" [{poolIndex}]" : string.Empty)} {relic.Title} ({relic.Id.Entry}).");
    }

    private CompletionResult CompleteTransitRelicArgument(
        Player? player,
        string subcommand,
        string partial)
    {
        if (!CombatManager.Instance.IsInProgress || player?.PlayerCombatState == null)
        {
            return new CompletionResult
            {
                Type = CompletionType.Argument,
                ArgumentContext = CmdName,
            };
        }

        string[] entryIds = GetIndexedTransitPool(player)
            .Select(relic => relic.Id.Entry)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        IReadOnlyDictionary<string, string> englishTitles = GetEnglishRelicTitles();
        Func<string, string, bool> idMatcher = (candidate, term) =>
            candidate.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            NormalizeRelicSearchText(candidate).Contains(
                NormalizeRelicSearchText(term),
                StringComparison.OrdinalIgnoreCase) ||
            englishTitles.TryGetValue(candidate, out string? englishTitle) &&
            (englishTitle.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             NormalizeRelicSearchText(englishTitle).Contains(
                 NormalizeRelicSearchText(term),
                 StringComparison.OrdinalIgnoreCase));
        Func<string, string, bool> matcher =
            DevConsoleAutocompleteMatchExtensions.WithLocalizedModelTitleMatch(idMatcher);
        CompletionResult result = CompleteArgument(
            entryIds,
            [subcommand],
            partial,
            CompletionType.Argument,
            matcher);
        DevConsoleAutocompleteMatchExtensions.ApplyLocalizedDisplayLabels(ref result);
        return result;
    }

    private static bool TryResolveTransitRelic(
        IReadOnlyList<RelicModel> pool,
        string input,
        out RelicModel relic,
        out int? poolIndex,
        out string error)
    {
        relic = null!;
        poolIndex = null;
        error = string.Empty;
        if (pool.Count == 0)
        {
            error = "The Transit relic pool is empty.";
            return false;
        }

        string token = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(input).Trim();
        if (int.TryParse(token, out int numericIndex))
        {
            if (numericIndex < 0 || numericIndex >= pool.Count)
            {
                error = $"Invalid Transit pool index {numericIndex}. Valid range: 0-{pool.Count - 1}.";
                return false;
            }

            relic = pool[numericIndex];
            poolIndex = numericIndex;
            return true;
        }

        string normalizedToken = NormalizeRelicSearchText(token);
        IReadOnlyDictionary<string, string> englishTitles = GetEnglishRelicTitles();
        List<(RelicModel Relic, int Index)> exactMatches = pool
            .Select((candidate, index) => (Relic: candidate, Index: index))
            .Where(candidate =>
                candidate.Relic.Id.Entry.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                candidate.Relic.Id.ToString().Equals(token, StringComparison.OrdinalIgnoreCase) ||
                candidate.Relic.Title.GetFormattedText().Equals(token, StringComparison.CurrentCultureIgnoreCase) ||
                englishTitles.TryGetValue(candidate.Relic.Id.Entry, out string? englishTitle) &&
                englishTitle.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                NormalizeRelicSearchText(candidate.Relic.Id.Entry)
                    .Equals(normalizedToken, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exactMatches.Count == 1)
        {
            (relic, int index) = exactMatches[0];
            poolIndex = index;
            return true;
        }

        List<(RelicModel Relic, int Index)> partialMatches = pool
            .Select((candidate, index) => (Relic: candidate, Index: index))
            .Where(candidate =>
                candidate.Relic.Id.Entry.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                candidate.Relic.Title.GetFormattedText().Contains(token, StringComparison.CurrentCultureIgnoreCase) ||
                englishTitles.TryGetValue(candidate.Relic.Id.Entry, out string? englishTitle) &&
                englishTitle.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                NormalizeRelicSearchText(candidate.Relic.Id.Entry)
                    .Contains(normalizedToken, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (partialMatches.Count == 1)
        {
            (relic, int index) = partialMatches[0];
            poolIndex = index;
            return true;
        }

        error = partialMatches.Count == 0
            ? $"No Transit relic matches '{input}'. Type part of its Chinese/localized title or English ID, then press Tab."
            : $"'{input}' matches {partialMatches.Count} Transit relics. Type more characters and press Tab to choose one.";
        return false;
    }

    private static string NormalizeRelicSearchText(string value) =>
        value.Replace('_', ' ').Replace('-', ' ').Trim();

    private static IReadOnlyDictionary<string, string> GetEnglishRelicTitles()
    {
        if (_englishRelicTitles != null)
            return _englishRelicTitles;

        Dictionary<string, string> titles = new(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> paths = [
            "res://localization/eng/relics.json",
            .. ModManager.GetModdedLocTables("eng", "relics.json"),
        ];
        foreach (string path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            using Godot.FileAccess? file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
                continue;
            Dictionary<string, string>? entries =
                JsonSerializer.Deserialize<Dictionary<string, string>>(file.GetAsText());
            if (entries == null)
                continue;

            foreach ((string key, string title) in entries)
            {
                const string suffix = ".title";
                if (key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(title))
                {
                    titles.TryAdd(key[..^suffix.Length], title.Trim());
                }
            }
        }

        _englishRelicTitles = titles;
        return titles;
    }

    private static CmdResult AddRelicDelivery(Player? player, string[] args)
    {
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;
        if (args.Length != 2 ||
            !int.TryParse(args[0], out int relicIndex) ||
            !int.TryParse(args[1], out int amount))
        {
            return new CmdResult(
                success: false,
                "Usage: exusiai delivery <relic-index:int> <amount:int>");
        }
        if (amount is < 1 or > 999)
            return new CmdResult(success: false, "Delivery amount must be between 1 and 999.");

        IReadOnlyList<RelicModel> relics = GetIndexedOwnedRelics(combatPlayer);
        if (relicIndex < 0 || relicIndex >= relics.Count)
        {
            return new CmdResult(
                success: false,
                relics.Count == 0
                    ? "The player has no relics."
                    : $"Invalid relic index {relicIndex}. Valid range: 0-{relics.Count - 1}.");
        }

        RelicModel relic = relics[relicIndex];
        if (!RelicLogisticsCmd.IsDeliveryTarget(relic))
        {
            return new CmdResult(
                success: false,
                $"[{relicIndex}] {relic.Title} is not a valid Delivery target.");
        }

        Task task = AddRelicDeliveryAsync(combatPlayer, relic, amount);
        return new CmdResult(
            task,
            success: true,
            $"Giving Delivery {amount} to [{relicIndex}] {relic.Title} ({relic.Id.Entry}).");
    }

    private static async Task AddRelicDeliveryAsync(
        Player player,
        RelicModel relic,
        int amount)
    {
        HookPlayerChoiceContext choiceContext = new(player, player.NetId, GameActionType.Combat);
        Task task = RelicLogisticsCmd.AddDelivery(choiceContext, relic, amount);
        await choiceContext.AssignTaskAndWaitForPauseOrCompletion(task);
    }

    private static IReadOnlyList<RelicModel> GetIndexedOwnedRelics(Player player) =>
        player.Relics.ToList();

    private static IReadOnlyList<RelicModel> GetIndexedTransitPool(Player player) =>
        RelicLogisticsCmd.GetTransitPool(player)
            .OrderBy(relic => relic.Id.ToString(), StringComparer.Ordinal)
            .ToList();

    private static bool TryGetHandCard(
        Player? player,
        string[] args,
        string subcommand,
        out CardModel card,
        out int amount,
        out CmdResult error)
    {
        card = null!;
        amount = 0;
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out error))
            return false;
        if (args.Length != 2 ||
            !int.TryParse(args[0], out int handIndex) ||
            !int.TryParse(args[1], out amount))
        {
            error = new CmdResult(
                success: false,
                $"Usage: exusiai {subcommand} <hand-index:int> <amount:int>");
            return false;
        }

        IReadOnlyList<CardModel> hand = combatPlayer.PlayerCombatState!.Hand.Cards;
        if (hand.Count == 0)
        {
            error = new CmdResult(
                success: false,
                "The hand is empty; there is no card index to target.");
            return false;
        }

        if (handIndex < 0 || handIndex >= hand.Count)
        {
            error = new CmdResult(
                success: false,
                $"Invalid hand index {handIndex}. Valid range: 0-{hand.Count - 1}.");
            return false;
        }

        card = hand[handIndex];
        error = default;
        return true;
    }

    private static bool TryGetCombatPlayer(
        Player? player,
        out Player combatPlayer,
        out CmdResult error)
    {
        if (!CombatManager.Instance.IsInProgress || player?.PlayerCombatState == null)
        {
            combatPlayer = null!;
            error = new CmdResult(
                success: false,
                "This Exusiai test command only works during combat.");
            return false;
        }

        combatPlayer = player;
        error = default;
        return true;
    }

    private static IEnumerable<string> GetHandIndices(Player? player)
    {
        if (!CombatManager.Instance.IsInProgress || player?.PlayerCombatState == null)
            return [];
        return Enumerable.Range(0, player.PlayerCombatState.Hand.Cards.Count)
            .Select(index => index.ToString());
    }

    private static IEnumerable<string> GetEnemyIndices(Player? player)
    {
        if (!CombatManager.Instance.IsInProgress || player?.PlayerCombatState == null)
            return [];
        return Enumerable.Range(0, player.Creature.CombatState!.HittableEnemies.Count)
            .Select(index => index.ToString());
    }

    private static IEnumerable<string> GetOwnedRelicIndices(Player? player)
    {
        if (!CombatManager.Instance.IsInProgress || player?.PlayerCombatState == null)
            return [];
        return Enumerable.Range(0, GetIndexedOwnedRelics(player).Count)
            .Select(index => index.ToString());
    }

    private static IEnumerable<string> GetTransitRelicIndices(Player? player)
    {
        if (!CombatManager.Instance.IsInProgress || player?.PlayerCombatState == null)
            return [];
        return Enumerable.Range(0, GetIndexedTransitPool(player).Count)
            .Select(index => index.ToString());
    }
}

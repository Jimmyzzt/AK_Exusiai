using System.Text;
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
using STS2RitsuLib.Combat.SecondaryResources;

namespace AK_Exusiai.Debug;

/// <summary>
/// Extensible developer-console entry point for Exusiai combat test fixtures.
/// </summary>
public sealed class ExusiaiConsoleCmd : AbstractConsoleCmd
{
    private const int TestEnergy = 100;
    private const int MaxSafeReplayCount = 100;

    private static readonly string[] ScenarioNames =
    [
        "ammo-partial",
        "ammo-snapshot",
        "ammo-multipliers",
        "overload",
        "explosive",
        "angel",
        "interference",
        "new-cards",
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
        "scenario",
        "state",
        "logic",
        "hand",
        "replay",
        "ammo",
        "angel",
        "interference",
    ];

    public override string CmdName => "exusiai";

    public override string Args => "<scenario|state|logic|hand|replay|ammo|angel|interference> [args]";

    public override string Description =>
        "Runs AK_Exusiai combat test fixtures and card-state utilities.";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            return Help();

        string subcommand = args[0].ToLowerInvariant();
        string[] subArgs = args.Skip(1).ToArray();
        return subcommand switch
        {
            "scenario" or "test" => SetupScenario(issuingPlayer, subArgs),
            "state" => ShowState(issuingPlayer, subArgs),
            "logic" or "logistics" => SetupLogic(issuingPlayer, subArgs),
            "hand" => ShowHand(issuingPlayer, subArgs),
            "replay" => AddReplay(issuingPlayer, subArgs),
            "ammo" => SetAmmo(issuingPlayer, subArgs),
            "angel" => AddAngel(issuingPlayer, subArgs),
            "interference" => AddInterference(issuingPlayer, subArgs),
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

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName,
        };
    }

    private static CmdResult Help() => new(
        success: true,
        "[gold]Exusiai test commands[/gold]\n" +
        "  exusiai scenario <name> [base|upgraded] - Build an isolated V1 combat fixture; use 'scenario list' for names.\n" +
        "  exusiai state - Show Ammo, relevant player powers, and indexed enemy Interference/Silence.\n" +
        "  exusiai logic [delivery|transit] [base|upgraded] - Replace the hand with new relic-logistics test cards and set energy to 100.\n" +
        "  exusiai hand - List the current hand with zero-based indices and test-relevant state.\n" +
        "  exusiai replay <hand-index> <amount> - Add Replay to one card (maximum total: 100).\n" +
        "  exusiai ammo <amount> - Set ammunition (0-30).\n" +
        "  exusiai angel <hand-index> - Give or refresh Angel on one card.\n" +
        "  exusiai interference <enemy-index> <amount> - Add Interference and resolve crossed Silence thresholds."
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
        "  angel - Confession/Empathy, Angel refresh, natural Angels, and curse exhaustion.\n" +
        "  interference - First enemy starts at I10/S0; add 6 to verify two crossed Silence thresholds.\n" +
        "  new-cards - The six cards introduced by the V1 card-list conversion.\n" +
        "Use: exusiai scenario <name> [base|upgraded]. Scenario setup replaces all combat-pile copies, never the run deck."
    );

    private static CmdResult ScenarioUsage(string? prefix = null) => new(
        success: false,
        (prefix == null ? string.Empty : prefix + "\n") +
        "Usage: exusiai scenario <ammo-partial|ammo-snapshot|ammo-multipliers|overload|explosive|angel|interference|new-cards> [base|upgraded]\n" +
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
            "Rerun between paths. Confession then Empathy makes all combat cards Angel, exhausts Curse/Status cards, triggers draws, and repeats the first free play. Separately, give Strike Angel via Cross, play it, return it with Looking Back, then use 'exusiai angel <index>' to refresh its free play."),
        "interference" => new ScenarioDefinition(
            name,
            10,
            [
                ModelDb.Card<RockNRoll>(),
                ModelDb.Card<Flashbang>(),
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
            ],
            [],
            [],
            5,
            ClearInitialSilence: true,
            "Verify all six newly introduced V1 cards, including Star's hand upgrade, target-selected interference effects, Firepower gain, and next-turn Ammo."),
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
            [player.Creature, .. player.Creature.CombatState!.HittableEnemies];
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
            $"Giving or refreshing Angel on '{card.Title}'. Status/Curse cards will exhaust immediately.");
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
}

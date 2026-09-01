using System.Text;
using AK_Exusiai.Cards;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;

namespace AK_Exusiai.Debug;

/// <summary>
/// Extensible developer-console entry point for Exusiai combat test fixtures.
/// </summary>
public sealed class ExusiaiConsoleCmd : AbstractConsoleCmd
{
    private const int TestEnergy = 100;
    private const int MaxSafeReplayCount = 100;

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
        "logic",
        "hand",
        "replay",
        "ammo",
    ];

    public override string CmdName => "exusiai";

    public override string Args => "<logic|hand|replay|ammo> [args]";

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
            "logic" or "logistics" => SetupLogic(issuingPlayer, subArgs),
            "hand" => ShowHand(issuingPlayer, subArgs),
            "replay" => AddReplay(issuingPlayer, subArgs),
            "ammo" => SetAmmo(issuingPlayer, subArgs),
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

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName,
        };
    }

    private static CmdResult Help() => new(
        success: true,
        "[gold]Exusiai test commands[/gold]\n" +
        "  exusiai logic [delivery|transit] [base|upgraded] - Replace the hand with new relic-logistics test cards and set energy to 100.\n" +
        "  exusiai hand - List the current hand with zero-based indices and test-relevant state.\n" +
        "  exusiai replay <hand-index> <amount> - Add Replay to one card (maximum total: 100).\n" +
        "  exusiai ammo <amount> - Set ammunition (0-30)."
    );

    private static string UsageSummary() =>
        "Use 'exusiai help' for the available test commands.";

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
        await PlayerCmd.SetEnergy(TestEnergy, player);

        CardPile hand = player.PlayerCombatState!.Hand;
        await CardPileCmd.RemoveFromCombat(hand.Cards.ToList());

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
}

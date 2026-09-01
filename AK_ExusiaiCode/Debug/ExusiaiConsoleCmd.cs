using System.Text;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Debug;

/// <summary>
/// Extensible developer-console entry point for Exusiai combat test fixtures.
/// </summary>
public sealed class ExusiaiConsoleCmd : AbstractConsoleCmd
{
    private const int TestEnergy = 100;
    private const int MaxSafeReplayCount = 100;

    private static readonly string[] Subcommands =
    [
        "help",
        "logic",
        "hand",
        "replay",
        "ammo",
        "delivery",
    ];

    public override string CmdName => "exusiai";

    public override string Args => "<logic|hand|replay|ammo|delivery> [args]";

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
            "delivery" => AddDelivery(issuingPlayer, subArgs),
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
                ["base", "upgraded"],
                [args[0]],
                args[1]);
        }

        if (subcommand is "replay" or "delivery")
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
                string[] amounts = subcommand == "replay"
                    ? ["1", "2", "5", "10"]
                    : ["1", "2", "3", "7", "99"];
                return CompleteArgument(
                    amounts,
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
        "  exusiai logic [base|upgraded] - Replace the hand with all 8 Logistics cards and set energy to 100.\n" +
        "  exusiai hand - List the current hand with zero-based indices and test-relevant state.\n" +
        "  exusiai replay <hand-index> <amount> - Add Replay to one card (maximum total: 100).\n" +
        "  exusiai ammo <amount> - Set ammunition (0-30).\n" +
        "  exusiai delivery <hand-index> <amount> - Add Delivery to one card (1-99)."
    );

    private static string UsageSummary() =>
        "Use 'exusiai help' for the available test commands.";

    private static CmdResult SetupLogic(Player? player, string[] args)
    {
        if (!TryGetCombatPlayer(player, out Player combatPlayer, out CmdResult error))
            return error;

        if (args.Length > 1 ||
            (args.Length == 1 &&
             !args[0].Equals("base", StringComparison.OrdinalIgnoreCase) &&
             !args[0].Equals("upgraded", StringComparison.OrdinalIgnoreCase)))
        {
            return new CmdResult(
                success: false,
                "Usage: exusiai logic [base|upgraded]");
        }

        bool upgraded = args.Length == 1 &&
                        args[0].Equals("upgraded", StringComparison.OrdinalIgnoreCase);
        Task task = SetupLogicAsync(combatPlayer, upgraded);
        return new CmdResult(
            task,
            success: true,
            $"Replacing the hand with all Logistics cards ({(upgraded ? "upgraded" : "base")}) and setting energy to {TestEnergy}.");
    }

    private static async Task SetupLogicAsync(Player player, bool upgraded)
    {
        await PlayerCmd.SetEnergy(TestEnergy, player);

        CardPile hand = player.PlayerCombatState!.Hand;
        await CardPileCmd.RemoveFromCombat(hand.Cards.ToList(), skipVisuals: true);

        List<CardModel> cards = LogisticsCardCatalog.CanonicalCards
            .Select(canonical => LogisticsCardCatalog.Create(player, canonical))
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
            int delivery = card.Capabilities().Get<DeliveryCapability>()?.Amount ?? 0;
            message.Append($"[{i}] {card.Title} ({card.Id.Entry}) replay={card.BaseReplayCount}");
            if (delivery > 0)
                message.Append($" delivery={delivery}");
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

    private static CmdResult AddDelivery(Player? player, string[] args)
    {
        if (!TryGetHandCard(player, args, "delivery", out CardModel card, out int amount, out CmdResult error))
            return error;
        if (amount is < 1 or > 99)
            return new CmdResult(success: false, "Delivery amount must be between 1 and 99.");

        Task task = AddDeliveryAsync(card, amount);
        return new CmdResult(
            task,
            success: true,
            $"Adding {amount} Delivery to '{card.Title}'.");
    }

    private static async Task AddDeliveryAsync(CardModel card, int amount)
    {
        Player player = card.Owner;
        HookPlayerChoiceContext context = new(
            player,
            player.NetId,
            GameActionType.Combat);
        Task task = DeliveryCmd.Add(context, card, amount);
        await context.AssignTaskAndWaitForPauseOrCompletion(task);
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

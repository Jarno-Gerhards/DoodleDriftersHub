using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Builds narrative prompts with story elements based on position changes.
/// Equivalent to PromptBuilder.js.
/// </summary>
public static class PromptBuilder
{
    private const int MaxHistorySize = 3;
    private const int LowerWordLimit = 35;
    private const int UpperWordLimit = 40;
    private const int MaxNarrativeBeatCount = 2;

    public static string BuildNarrativePrompt(NarrativeContext context)
    {
        string promptHistory = string.Join(" ",
            context.History.Skip(Math.Max(0, context.History.Count - MaxHistorySize)));

        int numberOfPlayers = context.Players.Count;

        var currentPositions = context.Positions[context.Positions.Count - 1];
        string polePosition = string.Join(", ", currentPositions.Select(p => p.Name));

        int maxWords = UnityEngine.Random.Range(LowerWordLimit, UpperWordLimit + 1);

        string storyElements = GenerateStoryElements(context);

        return $@"There is a race with {numberOfPlayers} players. Their names, in order of pole position are: {polePosition}.
Each racer is using unique, gadget-filled vehicles in a comedic and often chaotic pursuit of victory.
Their sole purpose in the race is to sabotage the other racers.
This is the narrative of the race up to this point: ""{promptHistory}""
Please continue the story with the following elements: {storyElements}
Use at most {maxWords} words for the continuation of the narrative.";
    }

    private static string GenerateStoryElements(NarrativeContext context)
    {
        var positions = context.Positions;
        var elements = new List<List<string>>();

        var unusedPlayers = new List<PlayerPositionSnapshot>(
            positions[positions.Count - 1]
                .OrderByDescending(p => p.VotesInLastRound)
                .Where(p => !p.Crashed));

        if (ElementsForWhenFirstPlayerFinished(context, elements, unusedPlayers))
        {
            ElementsForWhenFirstPlayerFinishedButPlayerCrashes(context, elements, unusedPlayers);
            return FormatElements(elements);
        }

        if (!ElementsForWhenLastPlayerCrashes(positions, elements, unusedPlayers))
            ElementsForWhenPlayerCrashes(positions, elements, unusedPlayers);

        if (!ElementsForWhenFirstPlayerBecomesLastPlayer(positions, elements, unusedPlayers))
            ElementsForWhenPlayerLosesMultiplePositions(positions, elements, unusedPlayers);

        if (!ElementsForWhenLastPlayerBecomesFirstPlayer(positions, elements, unusedPlayers))
            ElementsForWhenPlayerGainsMultiplePositions(positions, elements, unusedPlayers);

        bool firstCheck = ElementsForWhenFirstPlayerRemainsInLead(positions, elements, unusedPlayers);
        bool lastCheck = ElementsForWhenLastPlayerRemainsLast(positions, elements, unusedPlayers);
        if (!firstCheck && !lastCheck)
            ElementsForWhenPlayerRemainsAtPosition(positions, elements, unusedPlayers);

        ElementsForWhenPlayerOvertakesNextPlayer(positions, elements, unusedPlayers);

        return FormatElements(elements);
    }

    private static string FormatElements(List<List<string>> elements)
    {
        // Shuffle and pick up to MaxNarrativeBeatCount
        Shuffle(elements);
        var selected = elements.Take(MaxNarrativeBeatCount).ToList();
        var parts = new List<string>();
        foreach (var group in selected)
        {
            Shuffle(group);
            parts.Add($"'{group[0]}'");
        }
        return string.Join(", ", parts);
    }

    // ============================================================
    // Story element generators (matching PromptBuilder.js logic)
    // ============================================================

    private static bool ElementsForWhenFirstPlayerRemainsInLead(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        if (positions.Count < 2) return false;
        var prev = positions[positions.Count - 2];

        if (unusedPlayers.All(p => p.Id != current[0].Id)) return false;
        if (current[0].Id != prev[0].Id) return false;

        elements.Add(new List<string>
        {
            $"{current[0].Name} remains in lead, even though {current[1].Name} tries to use {current[1].Item}.",
            $"{current[0].Name} remains in lead because {current[0].Name} uses {current[0].Item} on {current[1].Name}."
        });
        return true;
    }

    private static bool ElementsForWhenLastPlayerRemainsLast(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        if (positions.Count < 2) return false;
        var prev = positions[positions.Count - 2];

        int lastIdx = current.Count - 1;
        int prevLastIdx = prev.Count - 1;

        if (unusedPlayers.All(p => p.Id != current[lastIdx].Id)) return false;
        if (current[lastIdx].Id != prev[prevLastIdx].Id) return false;

        elements.Add(new List<string>
        {
            $"{current[lastIdx].Name} remains last despite using {current[lastIdx].Item}.",
            $"{current[lastIdx].Name} remains last because {current[lastIdx - 1].Name} uses {current[lastIdx - 1].Item}."
        });
        return true;
    }

    private static bool ElementsForWhenLastPlayerCrashes(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        var lastPlayer = current[current.Count - 1];
        if (!lastPlayer.Crashed) return false;

        elements.Add(new List<string>
        {
            $"{lastPlayer.Name} tries to catch up with {lastPlayer.Item} but crashes."
        });
        return true;
    }

    private static bool ElementsForWhenPlayerCrashes(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        var crashed = current.Where(p => p.Crashed).ToList();
        if (crashed.Count == 0 || unusedPlayers.Count == 0) return false;

        var player = unusedPlayers[0];
        unusedPlayers.RemoveAt(0);

        var newElements = new List<string>
        {
            $"{player.Name} uses {player.Item} and makes {crashed[0].Name} crash."
        };

        if (unusedPlayers.Count > 0)
        {
            var otherPlayer = unusedPlayers[0];
            unusedPlayers.RemoveAt(0);
            newElements.Add($"{player.Name} and {otherPlayer.Name} team up and use both {player.Item} and {otherPlayer.Item} targeting {crashed[0].Name}. {crashed[0].Name} crashes.");
        }

        elements.Add(newElements);
        return true;
    }

    private static bool ElementsForWhenPlayerOvertakesNextPlayer(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        if (positions.Count < 2) return false;
        var prev = positions[positions.Count - 2];

        for (int i = 0; i < current.Count - 1; i++)
        {
            var currentPlayer = current[i];
            var nextPlayer = current[i + 1];

            if (unusedPlayers.All(p => p.Id != currentPlayer.Id)) continue;
            if (unusedPlayers.All(p => p.Id != nextPlayer.Id)) continue;

            int prevCurrentIdx = prev.FindIndex(p => p.Id == currentPlayer.Id);
            int prevNextIdx = prev.FindIndex(p => p.Id == nextPlayer.Id);

            if (prevCurrentIdx > prevNextIdx)
            {
                RemoveById(unusedPlayers, currentPlayer.Id);
                RemoveById(unusedPlayers, nextPlayer.Id);

                elements.Add(new List<string>
                {
                    $"{currentPlayer.Name} uses {currentPlayer.Item} and overtakes {nextPlayer.Name}.",
                    $"{nextPlayer.Name} uses {nextPlayer.Item}, but fails and {currentPlayer.Name} gets ahead."
                });
                return true;
            }
        }
        return false;
    }

    private static bool ElementsForWhenPlayerRemainsAtPosition(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        if (positions.Count < 2) return false;
        var prev = positions[positions.Count - 2];

        for (int i = 0; i < current.Count; i++)
        {
            var currentPlayer = current[i];
            if (i >= prev.Count || prev[i].Id != currentPlayer.Id) continue;
            if (unusedPlayers.All(p => p.Id != currentPlayer.Id)) continue;

            var localElements = new List<string>
            {
                $"{currentPlayer.Name} uses {currentPlayer.Item} with no effect."
            };
            RemoveById(unusedPlayers, currentPlayer.Id);

            if (unusedPlayers.Count > 0)
            {
                var other = unusedPlayers[0];
                unusedPlayers.RemoveAt(0);
                localElements.Add($"{other.Name} uses {other.Item}, but {currentPlayer.Name} counters with {currentPlayer.Item}.");
                localElements.Add($"{other.Name} targets {currentPlayer.Name} with {other.Item} with no effect.");
            }

            elements.Add(localElements);
            return true;
        }
        return false;
    }

    private static bool ElementsForWhenFirstPlayerBecomesLastPlayer(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        if (positions.Count < 2) return false;
        var prev = positions[positions.Count - 2];

        var lastPlayer = current[current.Count - 1];
        if (unusedPlayers.All(p => p.Id != lastPlayer.Id)) return false;
        if (prev[0].Id != lastPlayer.Id) return false;

        var localElements = new List<string>
        {
            $"{lastPlayer.Name} tries to use {lastPlayer.Item}, but fails horribly. {lastPlayer.Name} goes from first to last place."
        };
        RemoveById(unusedPlayers, lastPlayer.Id);

        if (unusedPlayers.Count > 0)
        {
            var other = unusedPlayers[0];
            unusedPlayers.RemoveAt(0);
            localElements.Add($"{lastPlayer.Name} gets targeted with {other.Item} by {other.Name}. {other.Name} attempt was very successful and {lastPlayer.Name} goes from first to last place.");

            if (unusedPlayers.Count > 0)
            {
                var third = unusedPlayers[0];
                unusedPlayers.RemoveAt(0);
                localElements.Add($"{other.Item} and {third.Item} work together to target {lastPlayer.Name}. {lastPlayer.Name} goes from first to last place.");
            }
        }

        elements.Add(localElements);
        return true;
    }

    private static bool ElementsForWhenLastPlayerBecomesFirstPlayer(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        if (positions.Count < 2) return false;
        var prev = positions[positions.Count - 2];

        var firstPlayer = current[0];
        if (unusedPlayers.All(p => p.Id != firstPlayer.Id)) return false;
        if (prev[prev.Count - 1].Id != firstPlayer.Id) return false;

        var localElements = new List<string>
        {
            $"{firstPlayer.Name} uses {firstPlayer.Item}, overtaking everybody else."
        };
        RemoveById(unusedPlayers, firstPlayer.Id);

        if (unusedPlayers.Count > 0)
        {
            var other = unusedPlayers[0];
            unusedPlayers.RemoveAt(0);
            localElements.Add($"{other.Name} uses {other.Item} affecting everybody except {firstPlayer.Name}. {firstPlayer.Name} now takes the lead.");
        }

        elements.Add(localElements);
        return true;
    }

    private static bool ElementsForWhenPlayerLosesMultiplePositions(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        if (positions.Count < 2) return false;
        var prev = positions[positions.Count - 2];

        for (int i = current.Count - 1; i > 0; i--)
        {
            var player = current[i];
            int prevPos = prev.FindIndex(p => p.Id == player.Id);
            if (i - prevPos > 1)
            {
                var localElements = new List<string>
                {
                    $"{player.Name} tries to use {player.Item}, but fails. Multiple racers take advantage."
                };
                RemoveById(unusedPlayers, player.Id);

                if (unusedPlayers.Count > 0)
                {
                    var other = unusedPlayers[0];
                    unusedPlayers.RemoveAt(0);
                    localElements.Add($"{other.Name} uses {other.Item} on {player.Name}. {player.Name} loses multiple positions.");
                }

                elements.Add(localElements);
                return true;
            }
        }
        return false;
    }

    private static bool ElementsForWhenPlayerGainsMultiplePositions(
        List<List<PlayerPositionSnapshot>> positions, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        var current = positions[positions.Count - 1];
        if (positions.Count < 2) return false;
        var prev = positions[positions.Count - 2];

        for (int i = 0; i < current.Count; i++)
        {
            var player = current[i];
            int prevPos = prev.FindIndex(p => p.Id == player.Id);
            if (prevPos - i > 1)
            {
                var localElements = new List<string>
                {
                    $"{player.Name} uses {player.Item} and overtakes multiple racers."
                };
                RemoveById(unusedPlayers, player.Id);

                if (unusedPlayers.Count > 0)
                {
                    var other = unusedPlayers[unusedPlayers.Count - 1];
                    unusedPlayers.RemoveAt(unusedPlayers.Count - 1);
                    localElements.Add($"{other.Name} tries to use {other.Item}, distracting multiple racers. {player.Name} takes advantage of the situation.");
                }

                elements.Add(localElements);
                return true;
            }
        }
        return false;
    }

    private static bool ElementsForWhenFirstPlayerFinished(
        NarrativeContext context, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        if (!context.Finished) return false;
        var current = context.Positions[context.Positions.Count - 1];

        var first = current[0];
        RemoveById(unusedPlayers, first.Id);
        var second = current[1];
        RemoveById(unusedPlayers, second.Id);

        elements.Add(new List<string>
        {
            $"{first.Name} reaches the finish line despite {second.Name} using {second.Item}.",
            $"{first.Name} uses {first.Item} and barely reaches the finish line before {second.Name}.",
            $"{first.Name} uses {first.Item} and reaches the finish line, leaving everyone far behind."
        });
        return true;
    }

    private static void ElementsForWhenFirstPlayerFinishedButPlayerCrashes(
        NarrativeContext context, List<List<string>> elements,
        List<PlayerPositionSnapshot> unusedPlayers)
    {
        if (!context.Finished) return;
        var current = context.Positions[context.Positions.Count - 1];
        var crashed = current.Where(p => p.Crashed).ToList();
        if (crashed.Count == 0) return;

        if (unusedPlayers.Count > 0)
        {
            var other = unusedPlayers[0];
            unusedPlayers.RemoveAt(0);
            elements.Add(new List<string>
            {
                $"{crashed[0].Name} never reaches the finish line because {other.Item} being used by {other.Name}."
            });
        }
        else
        {
            elements.Add(new List<string>
            {
                $"{crashed[0].Name} tries to win with {crashed[0].Item} but fails. {crashed[0].Name} never reach the finish line."
            });
        }
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static void RemoveById(List<PlayerPositionSnapshot> list, string id)
    {
        int idx = list.FindIndex(p => p.Id == id);
        if (idx >= 0) list.RemoveAt(idx);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

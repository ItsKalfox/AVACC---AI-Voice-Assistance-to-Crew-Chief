using System.Text.RegularExpressions;

namespace AVACC.Core.Helpers;

public static class CrewChiefPhraseHelper
{
    private static readonly Dictionary<int, string> SimpleNumberWords = new()
    {
        [0] = "zero",
        [1] = "one",
        [2] = "two",
        [3] = "three",
        [4] = "four",
        [5] = "five",
        [6] = "six",
        [7] = "seven",
        [8] = "eight",
        [9] = "nine",
        [10] = "ten",
        [11] = "eleven",
        [12] = "twelve",
        [13] = "thirteen",
        [14] = "fourteen",
        [15] = "fifteen",
        [16] = "sixteen",
        [17] = "seventeen",
        [18] = "eighteen",
        [19] = "nineteen",
        [20] = "twenty",
        [30] = "thirty",
        [40] = "fourty", // Note: Crew Chief specifically uses 'fourty' in speech_recognition_config.txt
        [50] = "fifty",
        [60] = "sixty",
        [70] = "seventy",
        [80] = "eighty",
        [90] = "ninety",
        [100] = "one-hundred"
    };

    private static readonly Dictionary<string, int> WordToNumberMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["first"] = 1, ["1st"] = 1, ["one"] = 1,
        ["second"] = 2, ["2nd"] = 2, ["two"] = 2,
        ["third"] = 3, ["3rd"] = 3, ["three"] = 3,
        ["fourth"] = 4, ["4th"] = 4, ["four"] = 4,
        ["fifth"] = 5, ["5th"] = 5, ["five"] = 5,
        ["sixth"] = 6, ["6th"] = 6, ["six"] = 6,
        ["seventh"] = 7, ["7th"] = 7, ["seven"] = 7,
        ["eighth"] = 8, ["8th"] = 8, ["eight"] = 8,
        ["ninth"] = 9, ["9th"] = 9, ["nine"] = 9,
        ["tenth"] = 10, ["10th"] = 10, ["ten"] = 10,
        ["eleventh"] = 11, ["11th"] = 11, ["eleven"] = 11,
        ["twelfth"] = 12, ["12th"] = 12, ["twelve"] = 12,
        ["thirteenth"] = 13, ["13th"] = 13, ["thirteen"] = 13,
        ["fourteenth"] = 14, ["14th"] = 14, ["fourteen"] = 14,
        ["fifteenth"] = 15, ["15th"] = 15, ["fifteen"] = 15,
        ["sixteenth"] = 16, ["16th"] = 16, ["sixteen"] = 16,
        ["seventeenth"] = 17, ["17th"] = 17, ["seventeen"] = 17,
        ["eighteenth"] = 18, ["18th"] = 18, ["eighteen"] = 18,
        ["nineteenth"] = 19, ["19th"] = 19, ["nineteen"] = 19,
        ["twentieth"] = 20, ["20th"] = 20, ["twenty"] = 20
    };

    public static string NumberToWord(int number)
    {
        if (SimpleNumberWords.TryGetValue(number, out var exact))
        {
            return exact;
        }

        if (number > 20 && number < 100)
        {
            var tens = (number / 10) * 10;
            var units = number % 10;
            if (units == 0)
            {
                return SimpleNumberWords.TryGetValue(tens, out var t) ? t : tens.ToString();
            }
            return $"{NumberToWord(tens)}-{NumberToWord(units)}";
        }

        if (number > 100 && number < 200)
        {
            var rem = number - 100;
            return $"one-hundred-and-{NumberToWord(rem)}";
        }

        return number.ToString();
    }

    public static string FormatDigitsToWords(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        // Match standalone numbers like 1, 4, 25, 100
        return Regex.Replace(input, @"\b(\d{1,3})\b", match =>
        {
            if (int.TryParse(match.Value, out var n))
            {
                return NumberToWord(n);
            }
            return match.Value;
        });
    }

    public static string ResolvePhrase(string? commandId, string? spokenPhrase, string transcript, string? voiceLineTemplate)
    {
        // 1. If an explicit spoken phrase was returned by LLM, ensure numbers are words if applicable
        if (!string.IsNullOrWhiteSpace(spokenPhrase) && !spokenPhrase.Contains('{'))
        {
            return FormatDigitsToWords(spokenPhrase.Trim());
        }

        // 2. If no voice line template is provided, fallback to spokenPhrase or commandId
        if (string.IsNullOrWhiteSpace(voiceLineTemplate))
        {
            return !string.IsNullOrWhiteSpace(spokenPhrase)
                ? FormatDigitsToWords(spokenPhrase.Trim())
                : (commandId ?? string.Empty).Replace('_', ' ').ToLowerInvariant();
        }

        var result = voiceLineTemplate.Trim();

        // Check for position query templates: {number}
        if (result.Contains("{number}", StringComparison.OrdinalIgnoreCase))
        {
            var positionNum = ExtractPositionNumber(transcript);
            var numWord = positionNum.HasValue ? NumberToWord(positionNum.Value) : "one";
            result = Regex.Replace(result, @"\{number\}", numWord, RegexOptions.IgnoreCase);
        }

        // Check for fuel calculation templates: {amount} {unit}
        if (result.Contains("{amount}", StringComparison.OrdinalIgnoreCase))
        {
            var (amount, unit) = ExtractFuelCalculationParameters(transcript);
            var amountWord = amount.HasValue ? NumberToWord(amount.Value) : "twenty";
            result = Regex.Replace(result, @"\{amount\}", amountWord, RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\{unit\}", unit ?? "laps", RegexOptions.IgnoreCase);
        }

        // Check for named driver templates: {driver}
        if (result.Contains("{driver}", StringComparison.OrdinalIgnoreCase))
        {
            var driverName = ExtractDriverName(transcript) ?? "leader";
            result = Regex.Replace(result, @"\{driver\}", driverName, RegexOptions.IgnoreCase);
        }

        // Check for time / alarm templates: {time}
        if (result.Contains("{time}", StringComparison.OrdinalIgnoreCase))
        {
            var timeSpec = ExtractTimeSpecification(transcript) ?? "ten minutes";
            result = Regex.Replace(result, @"\{time\}", timeSpec, RegexOptions.IgnoreCase);
        }

        return FormatDigitsToWords(result);
    }

    public static int? ExtractPositionNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Match P4, P 4, pos 4, position 4, p-4
        var match = Regex.Match(text, @"\b(?:p|pos|position)\s*[-#]?\s*(\d{1,3})\b", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var p))
        {
            return p;
        }

        // Match ordinal or numeric positions like "4th place", "2nd", "in 5th"
        var ordinalMatch = Regex.Match(text, @"\b(\d{1,3})(?:st|nd|rd|th)\b", RegexOptions.IgnoreCase);
        if (ordinalMatch.Success && int.TryParse(ordinalMatch.Groups[1].Value, out var ord))
        {
            return ord;
        }

        // Check word numbers like "third place", "who's in second", "position four"
        foreach (var kvp in WordToNumberMap)
        {
            var pattern = $@"\b(?:p|position|pos|in|place)?\s*{kvp.Key}\b";
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                return kvp.Value;
            }
        }

        // Generic standalone number in short phrase (e.g. "what's 4's last lap")
        var genericMatch = Regex.Match(text, @"\b(\d{1,3})\b");
        if (genericMatch.Success && int.TryParse(genericMatch.Groups[1].Value, out var gen))
        {
            return gen;
        }

        return null;
    }

    public static (int? Amount, string? Unit) ExtractFuelCalculationParameters(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, null);

        var match = Regex.Match(text, @"(\d{1,3})\s*(laps?|minutes?|mins?|hours?|hrs?)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var amt))
        {
            var rawUnit = match.Groups[2].Value.ToLowerInvariant();
            var unit = rawUnit.StartsWith("lap") ? "laps" : (rawUnit.StartsWith("hour") || rawUnit.StartsWith("hr") ? "hours" : "minutes");
            return (amt, unit);
        }

        // Check for words like "twenty laps", "fifteen minutes"
        foreach (var kvp in WordToNumberMap)
        {
            var wordMatch = Regex.Match(text, $@"\b{kvp.Key}\s+(laps?|minutes?|mins?|hours?|hrs?)\b", RegexOptions.IgnoreCase);
            if (wordMatch.Success)
            {
                var rawUnit = wordMatch.Groups[1].Value.ToLowerInvariant();
                var unit = rawUnit.StartsWith("lap") ? "laps" : (rawUnit.StartsWith("hour") || rawUnit.StartsWith("hr") ? "hours" : "minutes");
                return (kvp.Value, unit);
            }
        }

        return (null, null);
    }

    public static string? ExtractDriverName(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = Regex.Match(text, @"\b(?:where is|who is|what's|whats|gap to|time for)\s+([a-zA-Z]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var word = match.Groups[1].Value.Trim();
            // Filter out common stopwords
            if (!word.Equals("the", StringComparison.OrdinalIgnoreCase) &&
                !word.Equals("my", StringComparison.OrdinalIgnoreCase) &&
                !word.Equals("car", StringComparison.OrdinalIgnoreCase) &&
                !word.Equals("our", StringComparison.OrdinalIgnoreCase) &&
                !word.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                return word;
            }
        }

        return null;
    }

    public static string? ExtractTimeSpecification(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = Regex.Match(text, @"(\d{1,2}\s*(?:minutes?|mins?|hours?|am|pm))", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return null;
    }
}

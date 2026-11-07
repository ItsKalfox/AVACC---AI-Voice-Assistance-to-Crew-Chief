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
        ["twentieth"] = 20, ["20th"] = 20, ["twenty"] = 20,
        ["thirty"] = 30, ["thirtieth"] = 30, ["30th"] = 30,
        ["forty"] = 40, ["fourty"] = 40, ["fortieth"] = 40, ["fourtieth"] = 40, ["40th"] = 40,
        ["fifty"] = 50, ["fiftieth"] = 50, ["50th"] = 50,
        ["sixty"] = 60, ["sixtieth"] = 60, ["60th"] = 60,
        ["seventy"] = 70, ["seventieth"] = 70, ["70th"] = 70,
        ["eighty"] = 80, ["eightieth"] = 80, ["80th"] = 80,
        ["ninety"] = 90, ["ninetieth"] = 90, ["90th"] = 90,
        ["hundred"] = 100, ["one-hundred"] = 100, ["one hundred"] = 100, ["100th"] = 100
    };

    private static readonly Dictionary<string, int> TensWordsMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["twenty"] = 20,
        ["thirty"] = 30,
        ["forty"] = 40,
        ["fourty"] = 40,
        ["fifty"] = 50,
        ["sixty"] = 60,
        ["seventy"] = 70,
        ["eighty"] = 80,
        ["ninety"] = 90
    };

    private static readonly Dictionary<string, int> OnesWordsMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = 1,
        ["two"] = 2,
        ["three"] = 3,
        ["four"] = 4,
        ["five"] = 5,
        ["six"] = 6,
        ["seven"] = 7,
        ["eight"] = 8,
        ["nine"] = 9
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
        // 1. If an explicit configured voice line template is available, it ALWAYS takes precedence
        if (!string.IsNullOrWhiteSpace(voiceLineTemplate))
        {
            var template = voiceLineTemplate.Trim();

            // If template contains dynamic parameters, extract and resolve them
            if (template.Contains('{'))
            {
                var result = template;

                // Check for position query templates: {number}
                if (result.Contains("{number}", StringComparison.OrdinalIgnoreCase))
                {
                    var positionNum = ExtractPositionNumber(transcript) ?? ExtractPositionNumber(spokenPhrase ?? "");
                    var numWord = positionNum.HasValue ? NumberToWord(positionNum.Value) : "one";
                    result = Regex.Replace(result, @"\{number\}", numWord, RegexOptions.IgnoreCase);
                }

                // Check for fuel calculation templates: {amount} {unit}
                if (result.Contains("{amount}", StringComparison.OrdinalIgnoreCase))
                {
                    var (amount, unit) = ExtractFuelCalculationParameters(transcript);
                    if (!amount.HasValue && !string.IsNullOrWhiteSpace(spokenPhrase))
                    {
                        (amount, unit) = ExtractFuelCalculationParameters(spokenPhrase);
                    }
                    var amountWord = amount.HasValue ? NumberToWord(amount.Value) : "twenty";
                    result = Regex.Replace(result, @"\{amount\}", amountWord, RegexOptions.IgnoreCase);

                    var defaultUnit = result.Contains("fuel", StringComparison.OrdinalIgnoreCase) || transcript.Contains("fuel", StringComparison.OrdinalIgnoreCase)
                        ? "litres"
                        : "laps";
                    result = Regex.Replace(result, @"\{unit\}", unit ?? defaultUnit, RegexOptions.IgnoreCase);
                }

                // Check for named driver templates: {driver}
                if (result.Contains("{driver}", StringComparison.OrdinalIgnoreCase))
                {
                    var driverName = ExtractDriverName(transcript) ?? ExtractDriverName(spokenPhrase ?? "") ?? "leader";
                    result = Regex.Replace(result, @"\{driver\}", driverName, RegexOptions.IgnoreCase);
                }

                // Check for time / alarm templates: {time}
                if (result.Contains("{time}", StringComparison.OrdinalIgnoreCase))
                {
                    var timeSpec = ExtractTimeSpecification(transcript) ?? ExtractTimeSpecification(spokenPhrase ?? "") ?? "ten minutes";
                    result = Regex.Replace(result, @"\{time\}", timeSpec, RegexOptions.IgnoreCase);
                }

                return NormalizeCrewChiefBritishTerms(FormatDigitsToWords(result));
            }

            // Static voice line configured by user or registry (guarantee exact canonical phrase)
            return NormalizeCrewChiefBritishTerms(FormatDigitsToWords(template));
        }

        // 2. If no voice line template is configured, fallback to LLM's spoken phrase or canonical ID
        if (!string.IsNullOrWhiteSpace(spokenPhrase))
        {
            return NormalizeCrewChiefBritishTerms(FormatDigitsToWords(spokenPhrase.Trim()));
        }

        return (commandId ?? string.Empty).Replace('_', ' ').ToLowerInvariant();
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

    private const string FuelUnitPattern = @"litres?|liters?|l\b|gallons?|gals?|laps?|minutes?|mins?|hours?|hrs?|kilograms?|kilos?|kg\b|pounds?|lbs?\b";

    public static string NormalizeCrewChiefBritishTerms(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        // Crew Chief speech recognition grammar expects British spelling: "litres" not "liters"
        var result = Regex.Replace(input, @"\bliters\b", "litres", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bliter\b", "litre", RegexOptions.IgnoreCase);
        return result;
    }

    private static string NormalizeUnit(string rawUnit)
    {
        var lower = rawUnit.ToLowerInvariant();
        if (lower.StartsWith("lit") || lower == "l") return "litres";
        if (lower.StartsWith("gal")) return "gallons";
        if (lower.StartsWith("lap")) return "laps";
        if (lower.StartsWith("hour") || lower.StartsWith("hr")) return "hours";
        if (lower.StartsWith("min")) return "minutes";
        if (lower.StartsWith("k") || lower.StartsWith("kg")) return "kilograms";
        if (lower.StartsWith("pound") || lower.StartsWith("lb")) return "pounds";
        return lower;
    }

    public static (int? Amount, string? Unit) ExtractFuelCalculationParameters(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, null);

        // 1. Digits followed by unit (e.g. "50 liters", "55 litres", "20 laps", "10 gallons")
        var match = Regex.Match(text, @"(\d{1,3})\s*(" + FuelUnitPattern + @")", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var amt))
        {
            var unit = NormalizeUnit(match.Groups[2].Value);
            return (amt, unit);
        }

        // 2. Compound number words followed by unit (e.g. "twenty-five laps", "fifty five liters", "sixty-four litres")
        var compoundMatch = Regex.Match(text, @"\b(twenty|thirty|fourty|forty|fifty|sixty|seventy|eighty|ninety)[-\s]+(one|two|three|four|five|six|seven|eight|nine)\s*(" + FuelUnitPattern + @")", RegexOptions.IgnoreCase);
        if (compoundMatch.Success)
        {
            var tensVal = TensWordsMap.TryGetValue(compoundMatch.Groups[1].Value, out var tv) ? tv : 0;
            var unitsVal = OnesWordsMap.TryGetValue(compoundMatch.Groups[2].Value, out var uv) ? uv : 0;
            var unit = NormalizeUnit(compoundMatch.Groups[3].Value);
            return (tensVal + unitsVal, unit);
        }

        // 3. Single number words followed by unit (e.g. "fifty litres", "sixty liters", "ten laps", "twenty minutes")
        foreach (var kvp in WordToNumberMap.OrderByDescending(k => k.Key.Length))
        {
            var wordMatch = Regex.Match(text, $@"(?<![-\w]){kvp.Key}\s+(" + FuelUnitPattern + @")", RegexOptions.IgnoreCase);
            if (wordMatch.Success)
            {
                var unit = NormalizeUnit(wordMatch.Groups[1].Value);
                return (kvp.Value, unit);
            }
        }

        // 4. Standalone compound number words without unit (e.g. "add fifty-five", "pit stop add thirty-two")
        var standaloneCompoundMatch = Regex.Match(text, @"\b(twenty|thirty|fourty|forty|fifty|sixty|seventy|eighty|ninety)[-\s]+(one|two|three|four|five|six|seven|eight|nine)\b", RegexOptions.IgnoreCase);
        if (standaloneCompoundMatch.Success)
        {
            var tensVal = TensWordsMap.TryGetValue(standaloneCompoundMatch.Groups[1].Value, out var tv) ? tv : 0;
            var unitsVal = OnesWordsMap.TryGetValue(standaloneCompoundMatch.Groups[2].Value, out var uv) ? uv : 0;
            return (tensVal + unitsVal, null);
        }

        // 5. Action words followed by number words (e.g. "add fifty", "put in sixty", "pit stop add thirty")
        foreach (var kvp in WordToNumberMap.OrderByDescending(k => k.Key.Length))
        {
            var actionWordMatch = Regex.Match(text, $@"\b(?:add|fuel|put in|box|load|fill|pitstop|pit stop)\s+(?:about|roughly)?\s*{kvp.Key}\b", RegexOptions.IgnoreCase);
            if (actionWordMatch.Success)
            {
                return (kvp.Value, null);
            }
        }

        // 6. Action words followed by digits (e.g. "add 50", "box add 55", "pit stop add 60")
        var actionDigitMatch = Regex.Match(text, @"\b(?:add|fuel|put in|box|load|fill|pitstop|pit stop)\s+(?:about|roughly|more)?\s*(\d{1,3})\b", RegexOptions.IgnoreCase);
        if (actionDigitMatch.Success && int.TryParse(actionDigitMatch.Groups[1].Value, out var actionAmt))
        {
            return (actionAmt, null);
        }

        // 7. Fallback: any standalone 1-3 digit number not preceded by position identifiers (e.g. "please add 50 in pit stop menu")
        var standaloneDigitMatch = Regex.Match(text, @"(?<!\b(?:p|pos|position)\s*[-#]?)\b(\d{1,3})\b", RegexOptions.IgnoreCase);
        if (standaloneDigitMatch.Success && int.TryParse(standaloneDigitMatch.Groups[1].Value, out var standaloneAmt))
        {
            return (standaloneAmt, null);
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


using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging; // Optional: if you want the parser to log independently
using System;

namespace carestream.core.utilities
{
    /// <summary>
    /// Provides utility methods for parsing and formatting medication quantities.
    /// </summary>
    public static class QuantityParser
    {
        // Regex:
        // ^\s*      -> Optional leading whitespace
        // (\d+)     -> Group 1: One or more digits (the numeric value)
        // \s*       -> Optional whitespace
        // (         -> Group 2: Start of the unit part (optional)
        //   [a-zA-Z][a-zA-Z\s\/\-\%]*?  -> Unit starts with a letter, followed by any relevant characters non-greedily
        // )?        -> Group 2 is optional
        // \s*       -> Optional trailing whitespace
        // (\(.*\))? -> Group 3: Optional parenthesized detail like (100ml) - captured but not primary unit
        // \s*$      -> Optional trailing whitespace to the end of the string
        private static readonly Regex QuantityRegex = new Regex(@"^\s*(\d+)\s*([a-zA-Z][a-zA-Z\s\/\-\%]*?)?\s*(\(.*\))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex JustNumberRegex = new Regex(@"^\s*(\d+)\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Attempts to parse a numeric quantity and its primary unit from a string.
        /// Examples: "7 tablets" -> 7, "tablets"; "21" -> 21, "units"; "1 bottle (100ml)" -> 1, "bottle"
        /// </summary>
        /// <param name="quantityString">The string representation of the quantity.</param>
        /// <param name="numericValue">Output: The parsed numeric value.</param>
        /// <param name="unit">Output: The parsed unit part. Defaults to "unit" or "units" if not explicitly found.</param>
        /// <param name="logger">Optional logger for warnings.</param>
        /// <param name="allowEmptyOrNullAsZero">If true, empty/null string results in successful parse with 0 and default unit.</param>
        /// <returns>True if parsing was successful (even if only to defaults for empty/null), false otherwise for malformed strings.</returns>
        public static bool TryParseQuantityAndUnit(string? quantityString, out int numericValue, out string unit, ILogger? logger = null, bool allowEmptyOrNullAsZero = false)
        {
            numericValue = 0;
            unit = "units";

            if (string.IsNullOrWhiteSpace(quantityString))
            {
                if (allowEmptyOrNullAsZero)
                {
                    return true;
                }
                logger?.LogWarning("TryParseQuantityAndUnit: Input quantity string is null or whitespace.");
                return false;
            }

            quantityString = quantityString.Trim();
            Match match = QuantityRegex.Match(quantityString);

            if (match.Success && int.TryParse(match.Groups[1].Value, out numericValue))
            {
                if (match.Groups[2].Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                {
                    unit = match.Groups[2].Value.Trim();
                }
                else
                {
                    unit = (numericValue == 1) ? "unit" : "units";
                }
                logger?.LogDebug("TryParseQuantityAndUnit: Parsed '{Input}' to Value: {Value}, Unit: '{Unit}'", quantityString, numericValue, unit);
                return true;
            }
            else
            {
                Match numberOnlyMatch = JustNumberRegex.Match(quantityString);
                if (numberOnlyMatch.Success && int.TryParse(numberOnlyMatch.Groups[1].Value, out numericValue))
                {
                    unit = (numericValue == 1) ? "unit" : "units";
                    logger?.LogDebug("TryParseQuantityAndUnit: Parsed '{Input}' (as number only) to Value: {Value}, Unit: '{Unit}'", quantityString, numericValue, unit);
                    return true;
                }
            }

            logger?.LogWarning("TryParseQuantityAndUnit: Failed to parse from '{QuantityString}' using main regex or number-only regex.", quantityString);
            numericValue = 0;
            unit = "parse_error";
            return false;
        }

        /// <summary>
        /// Formats a numeric quantity and its unit back into a string.
        /// </summary>
        /// <param name="numericValue">The numeric quantity.</param>
        /// <param name="unit">The unit of the quantity. If null or empty, "unit" or "units" will be used based on value.</param>
        /// <returns>A string representation like "7 tablets".</returns>
        public static string FormatQuantity(int numericValue, string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit) || unit.Equals("parse_error", StringComparison.OrdinalIgnoreCase))
            {
                unit = (numericValue == 1) ? "unit" : "units";
            }
            return $"{numericValue} {unit.Trim()}";
        }
    }
}
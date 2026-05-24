using System.Text.Json;
using CmlLib.Core.Rules;
using CmlLib.Core.Json;

namespace CmlLib.Core.Version;

public static class JsonRulesParser
{
    public static IReadOnlyCollection<LauncherRule> Parse(JsonElement element)
    {
        var list = new List<LauncherRule>();

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var rule = ParseRuleElement(item);
                if (rule != null)
                    list.Add(rule);
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            var rule = ParseRuleElement(element);
            if (rule != null)
                list.Add(rule);
        }

        return list.ToArray();
    }

    public static LauncherRule? ParseRuleElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        // Use source-generated deserialization for AOT compatibility
        // Ensure CmlLibJsonContext.Default.LauncherRule is available
        try
        {
            return element.Deserialize(CmlLibJsonContext.Default.LauncherRule);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JsonRulesParser ERROR] Failed to deserialize LauncherRule: {ex.Message}. Raw JSON: {element.ToString()}");
            return null;
        }
    }
}
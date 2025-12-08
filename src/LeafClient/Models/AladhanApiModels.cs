// LeafClient.Models/AladhanApiModels.cs

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    public class AladhanPrayerTimesResponse
    {
        [JsonPropertyName("code")]
        public int code { get; set; }

        [JsonPropertyName("status")]
        public string status { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public AladhanData? data { get; set; }
    }

    public class AladhanData
    {
        [JsonPropertyName("timings")]
        public AladhanTimings? timings { get; set; }

        [JsonPropertyName("date")]
        public AladhanDate? date { get; set; }
    }

    public class AladhanTimings
    {
        [JsonPropertyName("Fajr")]
        public string Fajr { get; set; } = string.Empty;

        [JsonPropertyName("Sunrise")]
        public string Sunrise { get; set; } = string.Empty;

        [JsonPropertyName("Dhuhr")]
        public string Dhuhr { get; set; } = string.Empty;

        [JsonPropertyName("Asr")]
        public string Asr { get; set; } = string.Empty;

        [JsonPropertyName("Sunset")]
        public string Sunset { get; set; } = string.Empty;

        [JsonPropertyName("Maghrib")]
        public string Maghrib { get; set; } = string.Empty;

        [JsonPropertyName("Isha")]
        public string Isha { get; set; } = string.Empty;

        [JsonPropertyName("Imsak")]
        public string Imsak { get; set; } = string.Empty;

        [JsonPropertyName("Midnight")]
        public string Midnight { get; set; } = string.Empty;

        [JsonPropertyName("Firstthird")]
        public string Firstthird { get; set; } = string.Empty;

        [JsonPropertyName("Lastthird")]
        public string Lastthird { get; set; } = string.Empty;
    }

    public class AladhanDate
    {
        [JsonPropertyName("readable")]
        public string Readable { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("gregorian")]
        public AladhanGregorianDate Gregorian { get; set; } = new AladhanGregorianDate();

        [JsonPropertyName("hijri")]
        public AladhanHijriDate Hijri { get; set; } = new AladhanHijriDate();
    }

    public class AladhanGregorianDate
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;

        [JsonPropertyName("day")]
        public string Day { get; set; } = string.Empty;

        [JsonPropertyName("weekday")]
        public AladhanWeekday Weekday { get; set; } = new AladhanWeekday();

        [JsonPropertyName("month")]
        public AladhanMonth Month { get; set; } = new AladhanMonth();

        [JsonPropertyName("year")]
        public string Year { get; set; } = string.Empty;

        [JsonPropertyName("designation")]
        public AladhanDesignation Designation { get; set; } = new AladhanDesignation();
    }

    public class AladhanHijriDate
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;

        [JsonPropertyName("day")]
        public string Day { get; set; } = string.Empty;

        [JsonPropertyName("weekday")]
        public AladhanWeekday Weekday { get; set; } = new AladhanWeekday();

        [JsonPropertyName("month")]
        public AladhanMonth Month { get; set; } = new AladhanMonth();

        [JsonPropertyName("year")]
        public string Year { get; set; } = string.Empty;

        [JsonPropertyName("designation")]
        public AladhanDesignation Designation { get; set; } = new AladhanDesignation();

        [JsonPropertyName("holidays")]
        public List<string> Holidays { get; set; } = new List<string>();
    }

    public class AladhanWeekday
    {
        [JsonPropertyName("en")]
        public string En { get; set; } = string.Empty;

        [JsonPropertyName("ar")]
        public string Ar { get; set; } = string.Empty;
    }

    public class AladhanMonth
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("en")]
        public string En { get; set; } = string.Empty;

        [JsonPropertyName("ar")]
        public string Ar { get; set; } = string.Empty;
    }

    public class AladhanDesignation
    {
        [JsonPropertyName("abbreviated")]
        public string Abbreviated { get; set; } = string.Empty;

        [JsonPropertyName("expanded")]
        public string Expanded { get; set; } = string.Empty;
    }
}

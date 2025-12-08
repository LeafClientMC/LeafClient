using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public class WaypointsService
    {
        private readonly string _filePath;

        public WaypointsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _filePath = Path.Combine(appData, "LeafClient", "leafclient_mod_settings.txt");
        }

        public async Task<List<Waypoint>> LoadWaypointsAsync()
        {
            if (!File.Exists(_filePath)) return new List<Waypoint>();

            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                if (string.IsNullOrWhiteSpace(json)) return new List<Waypoint>();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("WAYPOINTS", out JsonElement waypointsSection) &&
                        waypointsSection.ValueKind == JsonValueKind.Object &&
                        waypointsSection.TryGetProperty("WAYPOINTSDB", out JsonElement dbElement) &&
                        dbElement.ValueKind == JsonValueKind.Array)
                    {
                        var loadedWaypoints = new List<Waypoint>();

                        foreach (var item in dbElement.EnumerateArray())
                        {
                            var wp = new Waypoint();

                            // Basic Fields
                            if (item.TryGetProperty("waypoint_name", out var nameProp)) wp.Name = nameProp.GetString() ?? "";
                            if (item.TryGetProperty("waypoint_x", out var xProp) && int.TryParse(xProp.GetString(), out int x)) wp.X = x;
                            if (item.TryGetProperty("waypoint_y", out var yProp) && int.TryParse(yProp.GetString(), out int y)) wp.Y = y;
                            if (item.TryGetProperty("waypoint_z", out var zProp) && int.TryParse(zProp.GetString(), out int z)) wp.Z = z;
                            if (item.TryGetProperty("color", out var cProp) && int.TryParse(cProp.GetString(), out int c)) wp.Color = c;

                            // Context
                            if (item.TryGetProperty("server", out var sProp)) wp.Server = sProp.GetString() ?? "127.0.0.1";
                            if (item.TryGetProperty("dimension", out var dProp)) wp.Dimension = dProp.GetString() ?? "minecraft:overworld";

                            // --- NEW DISPLAY OPTIONS (Parse String "true"/"false") ---
                            if (item.TryGetProperty("show_text", out var st) && bool.TryParse(st.GetString(), out bool b1)) wp.ShowText = b1;
                            if (item.TryGetProperty("highlight_block", out var hb) && bool.TryParse(hb.GetString(), out bool b2)) wp.HighlightBlock = b2;
                            if (item.TryGetProperty("show_beam", out var sb) && bool.TryParse(sb.GetString(), out bool b3)) wp.ShowBeam = b3;
                            if (item.TryGetProperty("show_distance", out var sd) && bool.TryParse(sd.GetString(), out bool b4)) wp.ShowDistance = b4;

                            loadedWaypoints.Add(wp);
                        }
                        return loadedWaypoints;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading waypoints: {ex.Message}");
            }
            return new List<Waypoint>();
        }

        public async Task SaveWaypointsAsync(List<Waypoint> waypoints)
        {
            try
            {
                JsonNode rootNode = new JsonObject();

                if (File.Exists(_filePath))
                {
                    string existingJson = await File.ReadAllTextAsync(_filePath);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        try { rootNode = JsonNode.Parse(existingJson) ?? new JsonObject(); } catch { rootNode = new JsonObject(); }
                    }
                }

                var waypointsArray = new JsonArray();
                foreach (var wp in waypoints)
                {
                    var wpObj = new JsonObject();
                    wpObj["waypoint_name"] = wp.Name;
                    wpObj["waypoint_x"] = wp.X.ToString();
                    wpObj["waypoint_y"] = wp.Y.ToString();
                    wpObj["waypoint_z"] = wp.Z.ToString();
                    wpObj["color"] = wp.Color.ToString();
                    wpObj["server"] = wp.Server;
                    wpObj["dimension"] = wp.Dimension;

                    // --- SAVE DISPLAY OPTIONS ---
                    wpObj["show_text"] = wp.ShowText.ToString().ToLower();
                    wpObj["highlight_block"] = wp.HighlightBlock.ToString().ToLower();
                    wpObj["show_beam"] = wp.ShowBeam.ToString().ToLower();
                    wpObj["show_distance"] = wp.ShowDistance.ToString().ToLower();

                    waypointsArray.Add(wpObj);
                }

                JsonObject waypointsSection = rootNode["WAYPOINTS"] as JsonObject;
                if (waypointsSection == null)
                {
                    waypointsSection = new JsonObject();
                    rootNode["WAYPOINTS"] = waypointsSection;
                }

                if (waypointsSection["enabled"] == null) waypointsSection["enabled"] = true;
                waypointsSection["WAYPOINTSDB"] = waypointsArray;

                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(_filePath, rootNode.ToJsonString(options));
            }
            catch (Exception ex) { }
        }
    }

    public class Waypoint
    {
        public string Name { get; set; } = "";
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int Z { get; set; } = 0;
        public int Color { get; set; } = 16777215;
        public string Server { get; set; } = "127.0.0.1";
        public string Dimension { get; set; } = "minecraft:overworld";

        // --- NEW FIELDS ---
        public bool ShowText { get; set; } = true;
        public bool HighlightBlock { get; set; } = true;
        public bool ShowBeam { get; set; } = true;
        public bool ShowDistance { get; set; } = true;
    }
}

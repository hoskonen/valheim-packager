using System.Text.Json.Serialization;

internal sealed class GlobalConfig
{
    [JsonPropertyName("bepInExPluginsDir")]
    public string BepInExPluginsDir { get; set; } = "";
}
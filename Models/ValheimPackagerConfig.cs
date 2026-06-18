using System.Text.Json.Serialization;

internal sealed class ValheimPackagerConfig
{
    [JsonPropertyName("packageDir")]
    public string PackageDir { get; set; } = "Thunderstore";

    [JsonPropertyName("outputDir")]
    public string OutputDir { get; set; } = "dist";

    [JsonPropertyName("dllName")]
    public string DllName { get; set; } = "";

    [JsonPropertyName("pluginFolderName")]
    public string PluginFolderName { get; set; } = "";

    [JsonPropertyName("dllSource")]
    public string DllSource { get; set; } = "";
}
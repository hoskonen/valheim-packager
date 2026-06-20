using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

internal static class Program
{
    private const string ManifestFileName = "manifest.json";
    private const string ReadmeFileName = "README.md";
    private const string IconFileName = "icon.png";
    private const string ChangelogFileName = "CHANGELOG.md";
    private const string DefaultPackageDirName = "Thunderstore";
    private const string DefaultOutputDirName = "dist";
    private const string ProjectConfigFileName = "valheim-packager.json";

    private static int Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        string command = args[0].ToLowerInvariant();

        try
        {

            return command switch
            {
                "init" => RunInit(args),
                "validate" => RunValidate(args),
                "package" => RunPackage(args),
                _ => Fail($"Unknown command: {args[0]}")
            };
        }
        catch (Exception exception)
        {
            WriteError($"Unexpected error: {exception.Message}");
            return 1;
        }
    }

    private static int RunValidate(string[] args)
    {
        string rootDir = Directory.GetCurrentDirectory();
        ValheimPackagerConfig config = ReadProjectConfig(rootDir);

        string packageDir = GetPackageDir(args, config, rootDir);

        Console.WriteLine("ValheimPackager - Validate");
        Console.WriteLine();

        ValidationReport report = ValidatePackageFolder(
            packageDir,
            dllPath: null,
            requireDll: false
        );

        report.Print();

        return report.HasErrors ? 1 : 0;
    }

    private static string GetGlobalConfigPath()
    {
        string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataDir, "ValheimPackager", "config.json");
    }

    private static GlobalConfig ReadGlobalConfig()
    {
        string configPath = GetGlobalConfigPath();

        if (!File.Exists(configPath))
        {
            return new GlobalConfig();
        }

        string json = File.ReadAllText(configPath);

        GlobalConfig? config = JsonSerializer.Deserialize<GlobalConfig>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        return config ?? new GlobalConfig();
    }

    private static int RunInit(string[] args)
    {
        string rootDir = Directory.GetCurrentDirectory();
        string packageDir = Path.Combine(rootDir, DefaultPackageDirName);
        string projectConfigPath = Path.Combine(rootDir, ProjectConfigFileName);

        Console.WriteLine("ValheimPackager - Init");
        Console.WriteLine();

        Directory.CreateDirectory(packageDir);

        string modName = GetOptionValue(args, "--name") ?? "MyValheimMod";
        string version = GetOptionValue(args, "--version") ?? "1.0.0";
        string websiteUrl = GetOptionValue(args, "--url") ?? "https://github.com/YOUR_USERNAME/YOUR_REPOSITORY";
        string description = GetOptionValue(args, "--description") ?? "TODO: Add a short mod description.";
        string dllName = GetOptionValue(args, "--dll-name") ?? $"{modName}.dll";
        string pluginFolderName = GetOptionValue(args, "--plugin-folder") ?? $"petri-{modName}";

        WriteFileIfMissing(
            Path.Combine(packageDir, ManifestFileName),
            CreateManifestTemplate(modName, version, websiteUrl, description)
        );

        WriteFileIfMissing(
            Path.Combine(packageDir, ReadmeFileName),
            CreateReadmeTemplate(modName, description, websiteUrl)
        );

        WriteFileIfMissing(
            Path.Combine(packageDir, ChangelogFileName),
            CreateChangelogTemplate(version)
        );

        WriteFileIfMissing(
            projectConfigPath,
            CreateProjectConfigTemplate(dllName, pluginFolderName)
        );

        Console.WriteLine("Next steps:");
        Console.WriteLine($"  1. Add a 256x256 icon.png to {DefaultPackageDirName}/");
        Console.WriteLine($"  2. Edit {DefaultPackageDirName}/manifest.json");
        Console.WriteLine($"  3. Edit {DefaultPackageDirName}/README.md");
        Console.WriteLine($"  4. Add your mod DLL to {DefaultPackageDirName}/ manually, or configure {ProjectConfigFileName} later");
        Console.WriteLine($"  5. Run: valheim-packager validate");
        Console.WriteLine($"  6. Run: valheim-packager package");

        return 0;
    }

    private static void WriteFileIfMissing(string path, string content)
    {
        if (File.Exists(path))
        {
            Console.WriteLine($"Skipped existing file: {path}");
            return;
        }

        File.WriteAllText(path, content);
        Console.WriteLine($"Created file: {path}");
    }

    private static string CreateManifestTemplate(string modName, string version, string websiteUrl, string description)
    {
        return $$"""
    {
      "name": "{{modName}}",
      "version_number": "{{version}}",
      "website_url": "{{websiteUrl}}",
      "description": "{{description}}",
      "dependencies": [
        "denikson-BepInExPack_Valheim-5.4.2333"
      ]
    }
    """;
    }

    private static string CreateReadmeTemplate(string modName, string description, string websiteUrl)
    {
        return
            $"# {modName}\n\n" +
            $"{description}\n\n" +
            $"![{modName}](https://i.imgur.com/IMAGE_ID.png)\n\n" +
            "## Features\n\n" +
            "- TODO: Add main feature\n" +
            "- TODO: Add another feature\n" +
            "- Lightweight Harmony patch\n\n" +
            "## Installation\n\n" +
            "Install with a mod manager, or manually place the DLL into your BepInEx plugins folder:\n\n" +
            "```text\n" +
            "BepInEx/plugins/\n" +
            "```\n\n" +
            "## Configuration\n\n" +
            "After launching the game once, the config file will be generated here:\n\n" +
            "```text\n" +
            "BepInEx/config/TODO_PLUGIN_GUID.cfg\n" +
            "```\n\n" +
            "## Notes\n\n" +
            "TODO: Add any gameplay notes, compatibility notes, or known limitations.\n\n" +
            "## Source\n\n" +
            $"Source: {websiteUrl}\n";
    }

    private static string CreateChangelogTemplate(string version)
    {
        return
            "# Changelog\n\n" +
            $"## {version}\n\n" +
            "Initial release.\n\n" +
            "- TODO: Add release note.\n";
    }

    private static string CreateProjectConfigTemplate(string dllName, string pluginFolderName)
    {
        return
            "{\n" +
            "  \"packageDir\": \"Thunderstore\",\n" +
            "  \"outputDir\": \"dist\",\n" +
            $"  \"dllName\": \"{dllName}\",\n" +
            $"  \"pluginFolderName\": \"{pluginFolderName}\",\n" +
            "  \"dllSource\": \"\"\n" +
            "}\n";
    }

    private static ValheimPackagerConfig ReadProjectConfig(string rootDir)
    {
        string configPath = Path.Combine(rootDir, ProjectConfigFileName);

        if (!File.Exists(configPath))
        {
            return new ValheimPackagerConfig();
        }

        string json = File.ReadAllText(configPath);

        ValheimPackagerConfig? config = JsonSerializer.Deserialize<ValheimPackagerConfig>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        return config ?? new ValheimPackagerConfig();
    }

    private static string ResolvePathFromRoot(string rootDir, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return rootDir;
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(rootDir, path);
    }

    private static int RunPackage(string[] args)
    {
        string rootDir = Directory.GetCurrentDirectory();

        ValheimPackagerConfig projectConfig = ReadProjectConfig(rootDir);
        GlobalConfig globalConfig = ReadGlobalConfig();

        string packageDir = GetPackageDir(args, projectConfig, rootDir);

        string? dllPath = GetOptionValue(args, "--dll");

        if (string.IsNullOrWhiteSpace(dllPath))
        {
            dllPath = ResolveConfiguredDllSource(projectConfig, globalConfig, rootDir);
        }

        string outputDir = GetOptionValue(args, "--out")
                           ?? ResolvePathFromRoot(rootDir, projectConfig.OutputDir);

        Console.WriteLine("ValheimPackager - Package");
        Console.WriteLine();

        ValidationReport preReport = ValidatePackageFolder(
            packageDir,
            dllPath,
            requireDll: true
        );

        if (preReport.HasErrors)
        {
            preReport.Print();
            return 1;
        }

        ThunderstoreManifest manifest = ReadManifest(Path.Combine(packageDir, ManifestFileName));

        string packageName = $"{manifest.Name}-v{manifest.VersionNumber}";
        string releaseDir = Path.Combine(outputDir, packageName);
        string zipPath = Path.Combine(outputDir, $"{packageName}.zip");

        if (Directory.Exists(releaseDir))
        {
            Directory.Delete(releaseDir, recursive: true);
        }

        Directory.CreateDirectory(releaseDir);
        Directory.CreateDirectory(outputDir);

        CopyRequiredFile(packageDir, releaseDir, ManifestFileName);
        CopyRequiredFile(packageDir, releaseDir, ReadmeFileName);
        CopyRequiredFile(packageDir, releaseDir, IconFileName);

        string changelogPath = Path.Combine(packageDir, ChangelogFileName);
        if (File.Exists(changelogPath))
        {
            File.Copy(changelogPath, Path.Combine(releaseDir, ChangelogFileName), overwrite: true);
        }

        ValidationReport dllResolveReport = new();
        List<string> dllSources = ResolveDllSources(dllPath, manifest, dllResolveReport);

        if (!string.IsNullOrWhiteSpace(dllPath))
        {
            if (dllSources.Count == 0)
            {
                dllResolveReport.Print();
                return 1;
            }

            foreach (string dllSource in dllSources)
            {
                File.Copy(
                    dllSource,
                    Path.Combine(releaseDir, Path.GetFileName(dllSource)),
                    overwrite: true
                );
            }
        }
        else
        {
            foreach (string dll in Directory.GetFiles(packageDir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                File.Copy(dll, Path.Combine(releaseDir, Path.GetFileName(dll)), overwrite: true);
            }
        }

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(
            sourceDirectoryName: releaseDir,
            destinationArchiveFileName: zipPath,
            compressionLevel: CompressionLevel.Optimal,
            includeBaseDirectory: false
        );

        ValidationReport finalReport = ValidatePackageFolder(
            releaseDir,
            dllPath: null,
            requireDll: true
        );
        finalReport.AddInfo($"Created release folder: {releaseDir}");
        finalReport.AddInfo($"Created zip: {zipPath}");

        finalReport.Print();

        return finalReport.HasErrors ? 1 : 0;
    }

    private static string? ResolveConfiguredDllSource(
    ValheimPackagerConfig projectConfig,
    GlobalConfig globalConfig,
    string rootDir
)
    {
        if (!string.IsNullOrWhiteSpace(projectConfig.DllSource))
        {
            return ResolvePathFromRoot(rootDir, projectConfig.DllSource);
        }

        if (!string.IsNullOrWhiteSpace(globalConfig.BepInExPluginsDir) &&
            !string.IsNullOrWhiteSpace(projectConfig.PluginFolderName) &&
            !string.IsNullOrWhiteSpace(projectConfig.DllName))
        {
            return Path.Combine(
                globalConfig.BepInExPluginsDir,
                projectConfig.PluginFolderName,
                projectConfig.DllName
            );
        }

        if (!string.IsNullOrWhiteSpace(projectConfig.DllName))
        {
            string packageDllPath = Path.Combine(
                ResolvePathFromRoot(rootDir, projectConfig.PackageDir),
                projectConfig.DllName
            );

            if (File.Exists(packageDllPath))
            {
                return packageDllPath;
            }
        }

        return null;
    }

    private static ValidationReport ValidatePackageFolder(string packageDir, string? dllPath, bool requireDll = false)
    {
        ValidationReport report = new();

        packageDir = Path.GetFullPath(packageDir);

        report.AddInfo($"Package folder: {packageDir}");

        if (!Directory.Exists(packageDir))
        {
            report.AddError($"Package folder does not exist: {packageDir}");
            return report;
        }

        string manifestPath = Path.Combine(packageDir, ManifestFileName);
        string readmePath = Path.Combine(packageDir, ReadmeFileName);
        string iconPath = Path.Combine(packageDir, IconFileName);
        string changelogPath = Path.Combine(packageDir, ChangelogFileName);

        ValidateRequiredFile(report, manifestPath, ManifestFileName);
        ValidateRequiredFile(report, readmePath, ReadmeFileName);
        ValidateRequiredFile(report, iconPath, IconFileName);

        if (File.Exists(changelogPath))
        {
            report.AddOk($"{ChangelogFileName} found");
        }
        else
        {
            report.AddWarning($"{ChangelogFileName} not found. This is optional, but recommended.");
        }

        ThunderstoreManifest? manifest = null;

        if (File.Exists(manifestPath))
        {
            ValidateManifest(report, manifestPath);

            try
            {
                manifest = ReadManifest(manifestPath);
            }
            catch
            {
                // ValidateManifest already reports manifest read errors.
            }
        }

        if (File.Exists(iconPath))
        {
            ValidatePngIcon(report, iconPath);
        }

        if (!string.IsNullOrWhiteSpace(dllPath))
        {
            ResolveDllSources(dllPath, manifest, report);
        }
        else
        {
            string[] dlls = Directory.GetFiles(packageDir, "*.dll", SearchOption.TopDirectoryOnly);

            if (dlls.Length == 0)
            {
                if (requireDll)
                {
                    report.AddError("No DLL found. Package creation requires a DLL in the package folder or a valid --dll source.");
                }
                else
                {
                    report.AddWarning("No DLL found in package folder. This is okay if you use --dll during package creation.");
                }
            }
            else
            {
                foreach (string dll in dlls)
                {
                    report.AddOk($"DLL found: {Path.GetFileName(dll)}");
                }
            }
        }

        DetectBadZipFolderStructure(report, packageDir);

        return report;
    }

    private static List<string> ResolveDllSources(string? dllPathOrFolder, ThunderstoreManifest? manifest, ValidationReport report)
    {
        List<string> resolvedDlls = [];

        if (string.IsNullOrWhiteSpace(dllPathOrFolder))
        {
            return resolvedDlls;
        }

        string fullPath = Path.GetFullPath(dllPathOrFolder);

        if (File.Exists(fullPath))
        {
            if (Path.GetExtension(fullPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                resolvedDlls.Add(fullPath);
                report.AddOk($"DLL source found: {fullPath}");
            }
            else
            {
                report.AddError($"DLL source is not a .dll file: {fullPath}");
            }

            return resolvedDlls;
        }

        if (Directory.Exists(fullPath))
        {
            string[] dlls = Directory.GetFiles(fullPath, "*.dll", SearchOption.TopDirectoryOnly);

            if (dlls.Length == 0)
            {
                report.AddError($"DLL source folder contains no .dll files: {fullPath}");
                return resolvedDlls;
            }

            if (dlls.Length == 1)
            {
                resolvedDlls.Add(dlls[0]);
                report.AddOk($"DLL source found in folder: {dlls[0]}");
                return resolvedDlls;
            }

            if (!string.IsNullOrWhiteSpace(manifest?.Name))
            {
                string? preferredDll = dlls.FirstOrDefault(dll =>
                    Path.GetFileNameWithoutExtension(dll).Equals(manifest.Name, StringComparison.OrdinalIgnoreCase)
                );

                if (!string.IsNullOrWhiteSpace(preferredDll))
                {
                    resolvedDlls.Add(preferredDll);
                    report.AddOk($"DLL source matched manifest name: {preferredDll}");
                    return resolvedDlls;
                }
            }

            report.AddError($"DLL source folder contains multiple .dll files and no clear match: {fullPath}");

            foreach (string dll in dlls)
            {
                report.AddInfo($"Found DLL candidate: {Path.GetFileName(dll)}");
            }

            return resolvedDlls;
        }

        report.AddError($"DLL source not found: {fullPath}");
        return resolvedDlls;
    }

    private static void ValidateManifest(ValidationReport report, string manifestPath)
    {
        ThunderstoreManifest? manifest;

        try
        {
            manifest = ReadManifest(manifestPath);
        }
        catch (Exception exception)
        {
            report.AddError($"Could not read manifest.json: {exception.Message}");
            return;
        }

        if (manifest == null)
        {
            report.AddError("manifest.json could not be parsed");
            return;
        }

        ValidateRequiredManifestValue(report, manifest.Name, "name");
        ValidateRequiredManifestValue(report, manifest.VersionNumber, "version_number");
        ValidateRequiredManifestValue(report, manifest.Description, "description");

        if (!string.IsNullOrWhiteSpace(manifest.Name))
        {
            if (Regex.IsMatch(manifest.Name, "^[a-zA-Z0-9_]+$"))
            {
                report.AddOk($"Manifest name: {manifest.Name}");
            }
            else
            {
                report.AddError("Manifest name should only contain letters, numbers, and underscores");
            }
        }

        if (!string.IsNullOrWhiteSpace(manifest.VersionNumber))
        {
            if (Regex.IsMatch(manifest.VersionNumber, @"^\d+\.\d+\.\d+$"))
            {
                report.AddOk($"Manifest version: {manifest.VersionNumber}");
            }
            else
            {
                report.AddError("Manifest version_number should use format x.y.z, for example 1.0.0");
            }
        }

        if (!string.IsNullOrWhiteSpace(manifest.WebsiteUrl))
        {
            if (Uri.TryCreate(manifest.WebsiteUrl, UriKind.Absolute, out _))
            {
                report.AddOk($"Website URL: {manifest.WebsiteUrl}");
            }
            else
            {
                report.AddWarning("website_url is not a valid absolute URL");
            }
        }

        if (manifest.Dependencies == null || manifest.Dependencies.Count == 0)
        {
            report.AddWarning("No dependencies listed");
        }
        else
        {
            foreach (string dependency in manifest.Dependencies)
            {
                ValidateDependency(report, dependency);
            }
        }
    }

    private static ThunderstoreManifest ReadManifest(string manifestPath)
    {
        string json = File.ReadAllText(manifestPath);

        ThunderstoreManifest? manifest = JsonSerializer.Deserialize<ThunderstoreManifest>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        return manifest ?? throw new InvalidOperationException("manifest.json deserialized to null");
    }

    private static void ValidateDependency(ValidationReport report, string dependency)
    {
        if (string.IsNullOrWhiteSpace(dependency))
        {
            report.AddError("Dependency entry is empty");
            return;
        }

        // Common Thunderstore dependency format:
        // owner-package-version
        // Example:
        // denikson-BepInExPack_Valheim-5.4.2333
        if (Regex.IsMatch(dependency, @"^[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+(\.\d+)?$"))
        {
            report.AddOk($"Dependency: {dependency}");
        }
        else
        {
            report.AddWarning($"Dependency format looks unusual: {dependency}");
        }
    }

    private static void ValidatePngIcon(ValidationReport report, string iconPath)
    {
        try
        {
            PngSize size = ReadPngSize(iconPath);

            if (size.Width == 256 && size.Height == 256)
            {
                report.AddOk("icon.png size: 256x256");
            }
            else
            {
                report.AddError($"icon.png must be 256x256, but was {size.Width}x{size.Height}");
            }
        }
        catch (Exception exception)
        {
            report.AddError($"Could not validate icon.png: {exception.Message}");
        }
    }

    private static PngSize ReadPngSize(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        if (bytes.Length < 24)
        {
            throw new InvalidOperationException("file is too small to be a valid PNG");
        }

        byte[] pngSignature =
        [
            137, 80, 78, 71, 13, 10, 26, 10
        ];

        for (int i = 0; i < pngSignature.Length; i++)
        {
            if (bytes[i] != pngSignature[i])
            {
                throw new InvalidOperationException("file is not a PNG");
            }
        }

        int width = ReadBigEndianInt32(bytes, 16);
        int height = ReadBigEndianInt32(bytes, 20);

        return new PngSize(width, height);
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
    {
        return
            (bytes[offset] << 24) |
            (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }

    private static void DetectBadZipFolderStructure(ValidationReport report, string packageDir)
    {
        string[] nestedManifestFiles = Directory.GetFiles(packageDir, ManifestFileName, SearchOption.AllDirectories)
            .Where(path => !Path.GetDirectoryName(path)!.Equals(packageDir, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (string nestedManifest in nestedManifestFiles)
        {
            report.AddWarning($"Nested manifest.json detected. Make sure the zip root is not a parent folder: {nestedManifest}");
        }
    }

    private static void ValidateRequiredFile(ValidationReport report, string path, string displayName)
    {
        if (File.Exists(path))
        {
            report.AddOk($"{displayName} found");
        }
        else
        {
            report.AddError($"{displayName} missing");
        }
    }

    private static void ValidateRequiredManifestValue(ValidationReport report, string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            report.AddError($"manifest.json missing required field: {fieldName}");
        }
    }

    private static void CopyRequiredFile(string sourceDir, string targetDir, string fileName)
    {
        File.Copy(
            Path.Combine(sourceDir, fileName),
            Path.Combine(targetDir, fileName),
            overwrite: true
        );
    }

    private static string GetPackageDir(string[] args, ValheimPackagerConfig config, string rootDir)
    {
        if (args.Length >= 2 && !args[1].StartsWith("--"))
        {
            return args[1];
        }

        string configuredPackageDir = ResolvePathFromRoot(rootDir, config.PackageDir);

        if (Directory.Exists(configuredPackageDir))
        {
            return configuredPackageDir;
        }

        if (File.Exists(Path.Combine(rootDir, ManifestFileName)))
        {
            return rootDir;
        }

        return configuredPackageDir;
    }

    private static string? GetOptionValue(string[] args, string optionName)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 >= args.Length)
            {
                return null;
            }

            return args[i + 1];
        }

        return null;
    }

    private static bool IsHelp(string arg)
    {
        return arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("help", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("ValheimPackager");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  init [options]");
        Console.WriteLine("  validate [packageDir]");
        Console.WriteLine("  package [packageDir] [--dll <dllPathOrFolder>] [--out <outputDir>]");
        Console.WriteLine();
        Console.WriteLine("Default workflow from a mod repo:");
        Console.WriteLine("  valheim-packager init");
        Console.WriteLine("  valheim-packager validate");
        Console.WriteLine("  valheim-packager package");
        Console.WriteLine();
        Console.WriteLine("Init options:");
        Console.WriteLine("  --name <modName>");
        Console.WriteLine("  --version <x.y.z>");
        Console.WriteLine("  --url <websiteUrl>");
        Console.WriteLine("  --description <description>");
        Console.WriteLine("  --dll-name <dllName>");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine(@"  dotnet run -- init --name TakeAllCooked --version 1.1.0 --url https://github.com/hoskonen/valheim-takeallcooked");
        Console.WriteLine(@"  dotnet run -- validate");
        Console.WriteLine(@"  dotnet run -- package --dll ""C:\Path\To\BepInEx\plugins\petri-TakeAllCooked""");
    }

    private static int Fail(string message)
    {
        WriteError(message);
        Console.WriteLine();
        PrintHelp();
        return 1;
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}

internal sealed class ThunderstoreManifest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("version_number")]
    public string? VersionNumber { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = [];
}

internal sealed record PngSize(int Width, int Height);

internal sealed class ValidationReport
{
    private readonly List<ReportLine> lines = [];

    public bool HasErrors => lines.Any(line => line.Level == ReportLevel.Error);

    public void AddOk(string message)
    {
        lines.Add(new ReportLine(ReportLevel.Ok, message));
    }

    public void AddInfo(string message)
    {
        lines.Add(new ReportLine(ReportLevel.Info, message));
    }

    public void AddWarning(string message)
    {
        lines.Add(new ReportLine(ReportLevel.Warning, message));
    }

    public void AddError(string message)
    {
        lines.Add(new ReportLine(ReportLevel.Error, message));
    }

    public void Print()
    {
        foreach (ReportLine line in lines)
        {
            Console.ForegroundColor = line.Level switch
            {
                ReportLevel.Ok => ConsoleColor.Green,
                ReportLevel.Info => ConsoleColor.Cyan,
                ReportLevel.Warning => ConsoleColor.Yellow,
                ReportLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            Console.Write(line.Level switch
            {
                ReportLevel.Ok => "OK      ",
                ReportLevel.Info => "INFO    ",
                ReportLevel.Warning => "WARNING ",
                ReportLevel.Error => "ERROR   ",
                _ => "        "
            });

            Console.ResetColor();
            Console.WriteLine(line.Message);
        }

        Console.WriteLine();

        if (HasErrors)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Result: package has errors.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Result: package looks ready.");
        }

        Console.ResetColor();
    }
}

internal sealed record ReportLine(ReportLevel Level, string Message);

internal enum ReportLevel
{
    Info,
    Ok,
    Warning,
    Error
}

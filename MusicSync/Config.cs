using Newtonsoft.Json;

namespace MusicSync;

public class Config
{
    public static Config Load(
        string path)
    {
        Thread.Sleep(1000);

        if (File.Exists(Path.Combine(AppContext.BaseDirectory, path)))
        {
            string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, path));
            return JsonConvert.DeserializeObject<Config>(json) ?? new();
        }

        return new();
    }

    public void Save(
        string path)
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, path), json);
        Console.WriteLine("Configuration saved.");
    }


    public static bool ValidateConfig(
        bool condition,
        string errorMessage)
    {
        if (!condition)
            return true;

        ConsoleHelpers.Write(errorMessage);
        return false;
    }

    public static bool ValidateAdbConfig(
        Config config) =>
        ValidateConfig(string.IsNullOrEmpty(config.AdbExecutable) || !File.Exists(config.AdbExecutable), "\nADB Executable is empty or does not exist. Please first update the config.");

    public static bool ValidateSyncConfig(
        Config config) =>
        ValidateConfig(string.IsNullOrEmpty(config.SyncFromLocation) || !Directory.Exists(config.SyncFromLocation), "\nSync From Location is empty or does not exist. Please first update the config.");


    public string SyncFromLocation { get; set; } = string.Empty;

    public string SyncToLocation { get; set; } = string.Empty;

    public int SyncMaxCount { get; set; } = 100;

    public bool OverwriteAlreadySyncred { get; set; } = false;

    public string AdbExecutable { get; set; } = "adb.exe";

}
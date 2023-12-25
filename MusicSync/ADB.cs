using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;

namespace MusicSync;

public class ADB
{
    static readonly AdbClient client = new();

    public static async Task<bool> StartServerASync(
        string adbExecutable)
    {
        try
        {

            if (AdbServer.Instance.GetStatus().IsRunning)
                return true;

            AdbServer server = new();
            StartServerResult result = await server.StartServerAsync(adbExecutable, false);

            return result == StartServerResult.Started;
        }
        catch
        {
            ConsoleHelpers.Write("Failed to start ADB server.");
            return false;
        }
    }


    public static async Task<(DeviceData, string)> SelectDeviceAsync()
    {
        DeviceData[] devices = (await client.GetDevicesAsync()).ToArray();

        if (devices.Length == 0)
            throw new Exception("No Devices found.");

        if (devices.Length == 1)
        {
            Dictionary<string, string> properties = await client.GetPropertiesAsync(devices[0]);
            string deviceInfo = $"{properties["ro.product.manufacturer"]} {properties["ro.product.model"]} (Android {properties["ro.build.version.release"]})";

            return (devices[0], deviceInfo);
        }

        while (true)
        {
            ConsoleHelpers.WriteClear("Select a Device:\n");

            for (int i = 0; i < devices.Length; i++)
            {
                Dictionary<string, string> properties = await client.GetPropertiesAsync(devices[i]);

                Console.WriteLine($"[{i + 1}]  {properties["ro.product.manufacturer"]} {properties["ro.product.model"]} (Android {properties["ro.build.version.release"]})");
            }

            string? choice = ConsoleHelpers.GetResponse("\nSelect a device by pressing [1] - [" + devices.Length + "]", "Invalid choice. Please select a valid device.");
            if (choice is null) continue;

            if (int.TryParse(choice, out int selectedIndex) && selectedIndex >= 1 && selectedIndex <= devices.Length)
            {
                Dictionary<string, string> properties = await client.GetPropertiesAsync(devices[selectedIndex - 1]);
                string deviceInfo = $"{properties["ro.product.manufacturer"]} {properties["ro.product.model"]} (Android {properties["ro.build.version.release"]})";

                return (devices[selectedIndex - 1], deviceInfo);
            }
        }
    }


    public static async Task UploadFileAsync(
        DeviceData device,
        string file,
        string destination,
        DateTimeOffset? timestamp = null,
        IProgress<int>? progress = null)
    {
        using SyncService service = new(client, device);
        using FileStream stream = File.OpenRead(file);

        await service.PushAsync(stream, destination, 777, timestamp ?? DateTimeOffset.Now, progress);
    }

    public static string[] GetFiles(
        DeviceData device,
        string directory)
    {
        IShellOutputReceiver rec = new ConsoleOutputReceiver();

        client.ExecuteShellCommand(device, $"ls {directory}", rec);
        return rec.ToString()?.Split("\r\n") ?? Array.Empty<string>();
    }
}
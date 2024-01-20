namespace MusicSync;

public static class ConsoleHelpers
{
    public static void Write(
        string message)
    {
        Console.WriteLine($"{message}\nPress any key to continue.");
        Console.ReadKey();
    }

    public static void WriteClear(
        string message)
    {
        Console.Clear();
        Console.WriteLine($"{message}\n");
    }

    public static void WriteClearLine(
        string message)
    {
        if (Console.CursorLeft > message.Length)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        Console.Write($"\r{message}");
    }


    public static string? GetResponse(
        string message,
        string? errorMessage = "Invalid response.")
    {
        Console.Write($"{message}: ");
        string? response = Console.ReadLine();

        if (errorMessage is not null && string.IsNullOrEmpty(response))
        {
            Console.Clear();
            Write(errorMessage);

            return null;
        }

        return response;
    }

    public static bool AskResponse(
        string message)
    {
        while (true)
        {
            Console.Clear();
            string? choice = GetResponse($"{message} [Y/N]", null)?.ToLower();

            if (choice == "y") return true;
            else if (choice == "n") return false;
            else
            {
                Console.Clear();
                Write("Invalid response. Please enter [Y] or [N].");
            }
        }
    }
}
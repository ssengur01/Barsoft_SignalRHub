using System;

/// <summary>
/// Simple tool to generate BCrypt password hashes
/// Usage: dotnet script HashPasswordTool.cs -- password123
/// </summary>
class HashPasswordTool
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== BCrypt Password Hash Generator ===\n");

        // Common test passwords
        string[] passwords = new[] { "password", "123456", "admin", "test123" };

        if (args.Length > 0)
        {
            // Use command line arguments
            passwords = args;
        }

        foreach (var password in passwords)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            Console.WriteLine($"Plain Text: {password}");
            Console.WriteLine($"BCrypt Hash: {hash}");
            Console.WriteLine($"\nSQL Update:");
            Console.WriteLine($"UPDATE TBL_USER_MAIN SET [PASSWORD] = '{hash}' WHERE USERCODE = '0001';");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine();
        }
    }
}

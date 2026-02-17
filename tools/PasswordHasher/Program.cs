using System;

Console.WriteLine("=== BCrypt Password Hash Generator ===\n");
Console.WriteLine("This tool generates BCrypt hashes for passwords.");
Console.WriteLine("Work Factor: 12 (same as backend)\n");
Console.WriteLine(new string('=', 80));
Console.WriteLine();

// Common test passwords
var passwords = new[] { "password", "123456", "admin", "test123", "0001" };

foreach (var pwd in passwords)
{
    var hash = BCrypt.Net.BCrypt.HashPassword(pwd, workFactor: 12);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Plain Text Password: {pwd}");
    Console.ResetColor();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"BCrypt Hash: {hash}");
    Console.ResetColor();

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\nSQL Update Query:");
    Console.WriteLine($"UPDATE TBL_USER_MAIN SET [PASSWORD] = '{hash}' WHERE USERCODE = '0001';");
    Console.ResetColor();

    Console.WriteLine(new string('-', 80));
    Console.WriteLine();
}

Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("Copy the SQL query above and run it in your SQL Server Management Studio");
Console.WriteLine("or Azure Data Studio to update the password in your database.");
Console.ResetColor();

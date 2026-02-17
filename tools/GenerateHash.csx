#r "nuget: BCrypt.Net-Next, 4.0.3"

using System;
using BCrypt.Net;

// Generate BCrypt hash for common passwords
var passwords = new[] { "password", "123456", "admin", "test123", "0001" };

Console.WriteLine("=== BCrypt Password Hashes ===\n");

foreach (var pwd in passwords)
{
    var hash = BCrypt.HashPassword(pwd, 12);
    Console.WriteLine($"Password: {pwd}");
    Console.WriteLine($"Hash: {hash}");
    Console.WriteLine($"\nSQL:");
    Console.WriteLine($"UPDATE TBL_USER_MAIN SET [PASSWORD] = '{hash}' WHERE USERCODE = '0001';\n");
    Console.WriteLine(new string('-', 80));
}

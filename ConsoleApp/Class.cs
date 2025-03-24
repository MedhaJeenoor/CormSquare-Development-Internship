using System;
using Microsoft.AspNetCore.Identity;

class Class
{
    static void Main()
    {
        var passwordHasher = new PasswordHasher<IdentityUser>();
        string password = "Admin@CormSquare123";
        string hashedPassword = passwordHasher.HashPassword(new IdentityUser(), password);

        Console.WriteLine("Hashed Password: " + hashedPassword);
    }
}

using System;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "Server=(localdb)\\mssqllocaldb;Database=PerfumeShopDb;Trusted_Connection=True;MultipleActiveResultSets=true";
        
        Console.WriteLine("Admin-Benutzer wird erstellt...");
        
        // Admin-Benutzerdaten
        string email = "admin@perfumeshop.com";
        string password = "Admin123";
        string firstName = "Admin";
        string lastName = "User";
        string address = "Admin Address";
        bool isAdmin = true;
        bool isActive = true;
        DateTime createdAt = DateTime.Now;
        
        // Password hashen
        string hashedPassword = HashPassword(password);
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hashed Password: {hashedPassword}");
        
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Verbindung zur Datenbank hergestellt.");
                
                // Prüfen, ob Admin-Benutzer bereits existiert
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Email", email);
                    int count = (int)checkCommand.ExecuteScalar();
                    
                    if (count > 0)
                    {
                        Console.WriteLine("Admin-Benutzer existiert bereits. Passwort wird aktualisiert.");
                        
                        string updateQuery = "UPDATE Users SET Password = @Password WHERE Email = @Email";
                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Email", email);
                            updateCommand.Parameters.AddWithValue("@Password", hashedPassword);
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Admin-Benutzer wird neu erstellt.");
                        
                        string insertQuery = @"
                            INSERT INTO Users (Email, Password, FirstName, LastName, Address, IsAdmin, IsActive, CreatedAt)
                            VALUES (@Email, @Password, @FirstName, @LastName, @Address, @IsAdmin, @IsActive, @CreatedAt)";
                        
                        using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Email", email);
                            insertCommand.Parameters.AddWithValue("@Password", hashedPassword);
                            insertCommand.Parameters.AddWithValue("@FirstName", firstName);
                            insertCommand.Parameters.AddWithValue("@LastName", lastName);
                            insertCommand.Parameters.AddWithValue("@Address", address);
                            insertCommand.Parameters.AddWithValue("@IsAdmin", isAdmin);
                            insertCommand.Parameters.AddWithValue("@IsActive", isActive);
                            insertCommand.Parameters.AddWithValue("@CreatedAt", createdAt);
                            
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
                
                Console.WriteLine("Admin-Benutzer erfolgreich erstellt oder aktualisiert.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }
        
        Console.WriteLine("Fertig. Drücken Sie eine Taste zum Beenden.");
        Console.ReadKey();
    }
    
    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
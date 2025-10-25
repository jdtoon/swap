using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ttw.Data.Models;
using ttw.Embedded;

namespace ttw.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            // Seed Roles
            var roles = new[] { "owner", "agent" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new AppRole { Name = roleName });
                }
            }

            // Seed Admin Users
            var ownerUsers = new[]
            {
                new { Email = "cooljunz29@gmail.com", Password = "Test@1234" },
                new { Email = "sp@ttw.co", Password = "Test@1234" }
            };

            var agentUsers = new[]
            {
                new { Email = "junaidd0306@gmail.com", Password = "Test@1234" },
                new { Email = "am@ttw.co", Password = "Test@1234" }
            };

            foreach (var owner in ownerUsers)
            {
                if (await userManager.FindByEmailAsync(owner.Email) == null)
                {
                    var user = new AppUser
                    {
                        Email = owner.Email,
                        UserName = owner.Email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, owner.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "owner");
                    }
                }
            }

            foreach (var agent in agentUsers)
            {
                if (await userManager.FindByEmailAsync(agent.Email) == null)
                {
                    var user = new AppUser
                    {
                        Email = agent.Email,
                        UserName = agent.Email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, agent.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "agent");
                    }
                }
            }

            await context.SaveChangesAsync();

            // Seed Cities
            if (!context.City.Any())
            {
                var cities = new[]
                {
                    new City { Id = 1, Name = "Makkah", Image = @"/images/makkah.png" },
                    new City { Id = 2, Name = "Madinah", Image = @"/images/medinah.png" }
                };

                context.City.AddRange(cities);
                await context.SaveChangesAsync();
            }

            // Seed Hotels
            if (!context.Hotel.Any())
            {
                var city1 = context.City.Single(x => x.Name == "Makkah");
                var city2 = context.City.Single(x => x.Name == "Madinah");

                var hotels = new[]
                {
                    new Hotel { Id = 1, Name = "Al Aqeeq", City = city2 },
                    new Hotel { Id = 2, Name = "Conrad", City = city1 },
                    new Hotel { Id = 3, Name = "Dallah Taibah Standard Floors", City = city2 },
                    new Hotel { Id = 4, Name = "Intercontinental Dar al Iman ", City = city2 },
                    new Hotel { Id = 5, Name = "Doubletree by Hilton", City = city1 },
                    new Hotel { Id = 6, Name = "Elaf Taqwa", City = city2 },
                    new Hotel { Id = 7, Name = "Fairmont", City = city1 },
                    new Hotel { Id = 8, Name = "Hilton Convention", City = city1 },
                    new Hotel { Id = 9, Name = "Hyatt Regency", City = city1 },
                    new Hotel { Id = 10, Name = "Makkah Hotel - Promo", City = city1 },
                    new Hotel { Id = 11, Name = "Madinah Hilton", City = city2 },
                    new Hotel { Id = 12, Name = "Al Anwar Movenpick - Haram Tower", City = city2 },
                    new Hotel { Id = 13, Name = "Oberoi", City = city2 },
                    new Hotel { Id = 14, Name = "Sofitel Shahd al Madinah [ex Shaza]", City = city2 },
                    new Hotel { Id = 15, Name = "Shaza Makkah", City = city1 },
                    new Hotel { Id = 16, Name = "Swiss Maqam", City = city1 },
                    new Hotel { Id = 17, Name = "Swissotel", City = city1 },
                    new Hotel { Id = 18, Name = "Taibah Front", City = city2 },
                    new Hotel { Id = 19, Name = "Dar al Taqwa", City = city2 },
                    new Hotel { Id = 20, Name = "Makkah Towers - Promo", City = city1 },
                    new Hotel { Id = 21, Name = "Pullman Zamzam", City = city1 },
                    new Hotel { Id = 22, Name = "Leader Al Muna Kareem", City = city2 },
                    new Hotel { Id = 23, Name = "Dallah Taibah Prem", City = city2 },
                    new Hotel { Id = 24, Name = "Dallah Taibah Exec", City = city2 },
                    new Hotel { Id = 25, Name = "Taiba Hotel (Ex Millennium)", City = city2 },
                    new Hotel { Id = 26, Name = "Al Anwar Movenpick - Madinah Tower", City = city2 },
                    new Hotel { Id = 27, Name = "Saja al Madinah", City = city2 },
                    new Hotel { Id = 28, Name = "Mysk Touch al Balad", City = city2 },
                    new Hotel { Id = 29, Name = "Hilton Suites", City = city1 },
                    new Hotel { Id = 30, Name = "Dorar Al Eiman Royal ", City = city1 },
                    new Hotel { Id = 31, Name = "Al Shohada", City = city1 },
                    new Hotel { Id = 32, Name = "Holiday Inn Aziziah Hotel", City = city1 },
                    new Hotel { Id = 33, Name = "Sheraton Makkah Jabal Kaaba", City = city1 },
                    new Hotel { Id = 34, Name = "Le Meridian Towers (Kudai)", City = city1 },
                    new Hotel { Id = 35, Name = "Pullman Zamzam Madinah", City = city2 },
                    new Hotel { Id = 1035, Name = "Marriott Makkah Jabal Omar", City = city1 },
                    new Hotel { Id = 1036, Name = "Park Inn by Radisson", City = city1 },
                    new Hotel { Id = 1037, Name = "Intercontinental Dar al Tawhid ", City = city1 },
                    new Hotel { Id = 1038, Name = "Marwah Rayhaan by Rotana", City = city1 },
                    new Hotel { Id = 1039, Name = "Safwa Royal Orchard", City = city1 },
                    new Hotel { Id = 1040, Name = "Anjum Hotel", City = city1 },
                    new Hotel { Id = 1041, Name = "Emaar Royal", City = city2 },
                    new Hotel { Id = 1042, Name = "Rua Al Hijra", City = city2 },
                    new Hotel { Id = 1043, Name = "Movenpick Makkah Hajar Tower", City = city1 },
                    new Hotel { Id = 1044, Name = "M Hotel by Millennium", City = city1 },
                    new Hotel { Id = 1046, Name = "Frontel Al Harthiya", City = city2 },
                    new Hotel { Id = 1047, Name = "Four Points - Al Naseem", City = city1 },
                    new Hotel { Id = 1048, Name = "Holiday Inn - Azizia", City = city1 },
                    new Hotel { Id = 1049, Name = "Al Ebaa [Behind Anjum]", City = city1 },
                    new Hotel { Id = 1050, Name = "Saraya Misk [Ajyad]", City = city1 },
                    new Hotel { Id = 1051, Name = "Al Masa Grand", City = city1 },
                    new Hotel { Id = 1052, Name = "Dallah Taibah Std", City = city2 },
                    new Hotel { Id = 1053, Name = "Elaf Ajyad ", City = city1 },
                    new Hotel { Id = 1054, Name = "Raffles Makkah Palace", City = city1 },
                    new Hotel { Id = 1055, Name = "Voco Makkah ", City = city1 },
                    new Hotel { Id = 1056, Name = "Elaf Mashaer", City = city1 },
                    new Hotel { Id = 1057, Name = "Swissotel Makkah | Swiss Maqam ", City = city1 },
                    new Hotel { Id = 1058, Name = "Emaar Elite", City = city2 },
                    new Hotel { Id = 1059, Name = "Emaar Royal", City = city2 },
                    new Hotel { Id = 1060, Name = "Emaar Maktan", City = city2 },
                    new Hotel { Id = 1061, Name = "Emaar Taiba", City = city2 },
                    new Hotel { Id = 1062, Name = "Shaza Regency Plaza", City = city2 },
                    new Hotel { Id = 1063, Name = "Mysk Touch ", City = city2 },
                    new Hotel { Id = 1064, Name = "SwissOtel", City = city1 },
                    new Hotel { Id = 1065, Name = "Address Jabal Omar Makkah", City = city1 },
                    new Hotel { Id = 1066, Name = "Rua Al Hijra (Ex. Coral)", City = city2 },
                    new Hotel { Id = 1067, Name = "Al Anwar Madinah Movenpick", City = city2 },
                    new Hotel { Id = 1068, Name = "Marwa Rayhaan Rotana", City = city1 },
                    new Hotel { Id = 1069, Name = "Crown Plaza", City = city2 },
                    new Hotel { Id = 1070, Name = "Dar Al Hijra intercontinental", City = city2 },
                    new Hotel { Id = 1071, Name = "Taibah Suites", City = city2 },
                    new Hotel { Id = 1072, Name = "Al Haram Hotel", City = city2 },
                    new Hotel { Id = 1073, Name = "Hyatt Regency Ramadan", City = city1 },
                    new Hotel { Id = 1074, Name = "Conrad Ramadan", City = city1 },
                    new Hotel { Id = 1075, Name = "Hilton Suites Ramadan", City = city1 },
                    new Hotel { Id = 1076, Name = "Hilton Convention Ramadan", City = city1 },
                    new Hotel { Id = 1077, Name = "Doubletree by Hilton Ramadan", City = city1 },
                    new Hotel { Id = 1078, Name = "Dar Al Taqwa Ramadan", City = city2 },
                    new Hotel { Id = 1079, Name = "Intercontinental Dar Al Iman Ramadan", City = city2 },
                    new Hotel { Id = 1080, Name = "Dallah Taibah Std Ramadan", City = city2 },
                    new Hotel { Id = 1081, Name = "Dallah Taibah Prem Ramadan", City = city2 },
                    new Hotel { Id = 1082, Name = "Dallah Taibah Exec Ramadan", City = city2 },
                    new Hotel { Id = 1083, Name = "Voco Makkah Ramadan", City = city1 },
                    new Hotel { Id = 1084, Name = "Taibah Suites Ramadan", City = city2 },
                    new Hotel { Id = 1086, Name = "Saja Hotel (Ex Le-Meridien Towers Makkah)", City = city1 },
                    new Hotel { Id = 1087, Name = "Movenpick Makkah Ramadan", City = city1 },
                    new Hotel { Id = 1088, Name = "Taiba Front - Promo", City = city2 },
                    new Hotel { Id = 1089, Name = "Hilton Convention - Promo", City = city1 },
                    new Hotel { Id = 1090, Name = "Doubletree Hilton - Promo", City = city1 },
                    new Hotel { Id = 1091, Name = "Emaar Grand Hotel", City = city1 },
                    new Hotel { Id = 1092, Name = "Emaar Al Khalil Hotel", City = city1 },
                    new Hotel { Id = 1093, Name = "Makkah Hotel ", City = city1 },
                    new Hotel { Id = 1094, Name = "Makkah Towers", City = city1 },
                    new Hotel { Id = 1095, Name = "Saja Makkah - (Kudai Area) - Shuttle Available", City = city1 }
                };

                context.Hotel.AddRange(hotels);
                await context.SaveChangesAsync();
            }

            // Seed Room Types
            if (!context.RoomType.Any())
            {
                var roomTypes = new[]
                {
                    new RoomType { Id = 1, Name = "DBL" },
                    new RoomType { Id = 2, Name = "TPL" },
                    new RoomType { Id = 3, Name = "QUAD" },
                    new RoomType { Id = 4, Name = "DBL H/V" },
                    new RoomType { Id = 5, Name = "TPL H/V" },
                    new RoomType { Id = 6, Name = "QUAD H/V" },
                    new RoomType { Id = 7, Name = "FAMILY SUITE (4)" },
                    new RoomType { Id = 8, Name = "JNR SUITE (2)" },
                    new RoomType { Id = 9, Name = "1B/ROOM" },
                    new RoomType { Id = 10, Name = "FAMILY ROOM" },
                    new RoomType { Id = 11, Name = "EXEC SUITE" },
                    new RoomType { Id = 12, Name = "EXEC SUITE H/V" },
                    new RoomType { Id = 13, Name = "SUITE (2PAX)" },
                    new RoomType { Id = 14, Name = "2B/ROOM (4)" },
                    new RoomType { Id = 15, Name = "3B/ROOM (6)" },
                    new RoomType { Id = 16, Name = "DLX SUITE H/V (2)" },
                    new RoomType { Id = 17, Name = "1B/ROOM CV (2)" },
                    new RoomType { Id = 18, Name = "1B/ROOM HV (2)" },
                    new RoomType { Id = 19, Name = "2B/ROOM CV (4)" },
                    new RoomType { Id = 20, Name = "2B/ROOM HV (4)" },
                    new RoomType { Id = 21, Name = "DLX SUITE H/V (2)" },
                    new RoomType { Id = 22, Name = "EXEC SUITE (2 PAX)" },
                    new RoomType { Id = 23, Name = "EXEC SUITE (3 PAX)" },
                    new RoomType { Id = 24, Name = "DBL EXEC" },
                    new RoomType { Id = 25, Name = "QUINT (5)" },
                    new RoomType { Id = 26, Name = "JNR SUITE H/V" },
                    new RoomType { Id = 27, Name = "FAMILY SUITE (5)" },
                    new RoomType { Id = 28, Name = "JNR SUITE (4)" },
                    new RoomType { Id = 29, Name = "Classic Double Room City View" },
                    new RoomType { Id = 30, Name = "Classic Triple Room City View" },
                    new RoomType { Id = 31, Name = "Classic Quad Room City View" },
                    new RoomType { Id = 32, Name = "Superior Double Room City View" },
                    new RoomType { Id = 33, Name = "Superior Triple Room City View" },
                    new RoomType { Id = 34, Name = "Superior Quad Room City View" },
                    new RoomType { Id = 35, Name = "Standard Double City View" },
                    new RoomType { Id = 36, Name = "Standard Single City View" },
                    new RoomType { Id = 37, Name = "Standard Triple City View" },
                    new RoomType { Id = 38, Name = "Junior Suite Double City View" },
                    new RoomType { Id = 39, Name = "Junior Suite Triple City View" },
                    new RoomType { Id = 40, Name = "SGL" },
                    new RoomType { Id = 41, Name = "JNR SUITE(3)" },
                    new RoomType { Id = 42, Name = "Senior suite 3/BROOM" },
                    new RoomType { Id = 43, Name = "Junior suite 2/BROOM" },
                    new RoomType { Id = 44, Name = "Delux Suite 4 B/R C/V" },
                    new RoomType { Id = 45, Name = "Delux (2)" },
                    new RoomType { Id = 46, Name = "Exec Bs(2)" },
                    new RoomType { Id = 47, Name = "Diplomatic S(4)" },
                    new RoomType { Id = 48, Name = "Premium S(6)" },
                    new RoomType { Id = 49, Name = "Royal Suite(6)" },
                    new RoomType { Id = 50, Name = "Jnr suite 1 Room 1 Bath" },
                    new RoomType { Id = 51, Name = "Jnr suite 2 Room 2 Bath" },
                    new RoomType { Id = 52, Name = "Senior Suite 3 room 2 bath" },
                    new RoomType { Id = 53, Name = "Apartment 4 room 3 Bath" },
                    new RoomType { Id = 54, Name = "SGL" },
                    new RoomType { Id = 55, Name = "3B/ROOM HV(6 pax) 3 Bath" },
                    new RoomType { Id = 56, Name = "4B/ROOM CV(8pax) 2 Bath" },
                    new RoomType { Id = 57, Name = "4B/ROOM HV(8pax) 3 Bath" },
                    new RoomType { Id = 58, Name = "Prestige Suite(2)" },
                    new RoomType { Id = 59, Name = "3B/ROOM CV(6 pax) 2 Bath" },
                    new RoomType { Id = 60, Name = "Suite ( 5 Pax )" }
                };

                context.RoomType.AddRange(roomTypes);
                await context.SaveChangesAsync();
            }

            // Seed Currencies
            if (!context.Currency.Any())
            {
                var currencies = new[]
                {
                    new Currency
                    {
                        Id = 1,
                        Name = "Rand",
                        Rate = 4.85M,
                        LongName = "South African Rand",
                        RoundOff = 50,
                        Markup = 10
                    },
                    new Currency
                    {
                        Id = 2,
                        Name = "Riyal",
                        Rate = 1.00M,
                        LongName = "Saudi Arabian Riyal",
                        RoundOff = 10,
                        Markup = 12
                    }
                };

                context.Currency.AddRange(currencies);
                await context.SaveChangesAsync();
            }

            // Seed Suppliers
            if (!context.Supplier.Any())
            {
                var suppliers = new[]
                {
                    new Supplier { Id = 1, Name = "East Pearl" },
                    new Supplier { Id = 2, Name = "Fast" },
                    new Supplier { Id = 3, Name = "Direct" },
                    new Supplier { Id = 4, Name = "Tawaaf" },
                    new Supplier { Id = 5, Name = "Dayaal" },
                    new Supplier { Id = 6, Name = "Emaar" },
                    new Supplier { Id = 7, Name = "Mawasim" },
                    new Supplier { Id = 8, Name = "Maysan" },
                    new Supplier { Id = 9, Name = "Reh" },
                    new Supplier { Id = 10, Name = "Askant" },
                    new Supplier { Id = 11, Name = "Tabarak" },
                    new Supplier { Id = 12, Name = "Travel Gate" },
                    new Supplier { Id = 13, Name = "Burraq" }
                };

                context.Supplier.AddRange(suppliers);
                await context.SaveChangesAsync();
            }

            if (!context.RateCard.Any())
            {
                var resourceName = "ttw.Data.Scripts.RateCards.txt";
                var sqlScript = EmbeddedResourceReader.ReadEmbeddedResource(resourceName).Trim();
                var lines = sqlScript.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines) 
                {
                    var insertStatement = ExtractInsertStatement(line) + " " + "VALUES (@Name, @Json)";
                    var nameValue = ExtractNameValue(line);
                    var jsonValue = ExtractJsonValue(line);
                    var parameters = new[]
                    {
                    new SqliteParameter("@Name", nameValue),
                    new SqliteParameter("@Json", jsonValue)
                };

                    context.Database.ExecuteSqlRaw(insertStatement, parameters);
                }
            }

            await context.SaveChangesAsync();
        }
    }

    static string ExtractInsertStatement(string sqlScript)
    {
        // Extract the part before the VALUES keyword
        int valuesIndex = sqlScript.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
        if (valuesIndex == -1)
        {
            throw new InvalidOperationException("VALUES keyword not found in the SQL script.");
        }

        return sqlScript.Substring(0, valuesIndex).Trim();
    }

    static string ExtractNameValue(string sqlScript)
    {
        // Find the start and end of the name value
        int nameStart = sqlScript.IndexOf("('", StringComparison.Ordinal) + 2;
        int nameEnd = sqlScript.IndexOf("',", StringComparison.Ordinal);

        if (nameStart == -1 || nameEnd == -1)
        {
            throw new InvalidOperationException("Name value not found in the SQL script.");
        }

        return sqlScript.Substring(nameStart, nameEnd - nameStart).Trim();
    }

    static string ExtractJsonValue(string sqlScript)
    {
        // Find the start and end of the JSON value
        int jsonStart = sqlScript.IndexOf("'[", StringComparison.Ordinal) + 1;
        int jsonEnd = sqlScript.LastIndexOf("]'", StringComparison.Ordinal) + 1;

        if (jsonStart == -1 || jsonEnd == -1)
        {
            throw new InvalidOperationException("JSON value not found in the SQL script.");
        }

        return sqlScript.Substring(jsonStart, jsonEnd - jsonStart).Trim();
    }
}
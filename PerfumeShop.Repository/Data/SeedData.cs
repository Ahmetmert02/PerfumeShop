using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerfumeShop.Core.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PerfumeShop.Repository.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // Überprüfen, ob bereits Marken vorhanden sind
            if (!context.Brands.Any())
            {
                // Marken hinzufügen
                context.Brands.AddRange(
                    new Brand
                    {
                        Name = "Louis Vuitton",
                        Description = "Luxusmarke aus Frankreich, bekannt für hochwertige Parfüms.",
                        LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/76/Louis_Vuitton_logo_and_wordmark.svg/1280px-Louis_Vuitton_logo_and_wordmark.svg.png",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new Brand
                    {
                        Name = "Xerjoff",
                        Description = "Italienische Nischenparfümmarke, bekannt für luxuriöse und exklusive Düfte.",
                        LogoUrl = "https://www.xerjoffuniverse.com/wp-content/uploads/2022/02/logo-xerjoff.svg",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new Brand
                    {
                        Name = "Tom Ford",
                        Description = "Luxusmarke von Designer Tom Ford mit einer exklusiven Parfümlinie.",
                        LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/5f/Tom_Ford_logo.svg/1280px-Tom_Ford_logo.svg.png",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new Brand
                    {
                        Name = "Creed",
                        Description = "Traditionsreiche Parfümmarke mit königlicher Geschichte, bekannt für Aventus.",
                        LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/87/Creed_logo.svg/1280px-Creed_logo.svg.png",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new Brand
                    {
                        Name = "Lattafa",
                        Description = "Arabische Parfümmarke, bekannt für orientalische Düfte zu erschwinglichen Preisen.",
                        LogoUrl = "https://lattafaperfumes.com/wp-content/uploads/2022/09/Lattafa-Logo-1.png",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
} 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.WebAPI.Seed;

/// <summary>
/// Seeds sample Categories/Products/Users/Orders for local development.
/// Runs once at startup, after RolePermissionSeeder (user role assignment
/// depends on the Admin/Customer roles already existing).
/// </summary>
internal static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var categories = await SeedCategoriesAsync(context);
        var products = await SeedProductsAsync(context, categories);
        await context.SaveChangesAsync();

        var customers = await SeedUsersAsync(context, userManager);

        await SeedOrdersAsync(context, products, customers);
        await context.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Category>> SeedCategoriesAsync(ApplicationDbContext context)
    {
        string[] names = ["Electronics", "Clothing", "Books", "Home & Kitchen", "Sports & Outdoors"];

        var existing = await context.Categories
            .Where(c => names.Contains(c.Name))
            .ToDictionaryAsync(c => c.Name);

        foreach (var name in names)
        {
            if (existing.ContainsKey(name))
                continue;

            var category = new Category { Name = name };
            context.Categories.Add(category);
            existing[name] = category;
        }

        return existing;
    }

    private static async Task<List<Product>> SeedProductsAsync(ApplicationDbContext context, Dictionary<string, Category> categories)
    {
        (string Name, string Description, decimal Price, string Category)[] catalog =
        [
            ("Wireless Mouse", "Ergonomic wireless mouse with USB receiver.", 249.90m, "Electronics"),
            ("Mechanical Keyboard", "RGB backlit mechanical keyboard, blue switches.", 899.00m, "Electronics"),
            ("27-inch Monitor", "27-inch 1440p IPS monitor, 144Hz refresh rate.", 4299.00m, "Electronics"),

            ("Men's Denim Jacket", "Classic fit denim jacket.", 649.90m, "Clothing"),
            ("Women's Running Shoes", "Lightweight breathable running shoes.", 799.50m, "Clothing"),
            ("Cotton T-Shirt", "Basic crew neck cotton t-shirt.", 129.90m, "Clothing"),

            ("Clean Code", "A Handbook of Agile Software Craftsmanship.", 189.00m, "Books"),
            ("The Pragmatic Programmer", "Your journey to mastery.", 219.00m, "Books"),
            ("Design Patterns", "Elements of Reusable Object-Oriented Software.", 249.00m, "Books"),

            ("Stand Mixer", "5-quart stand mixer with multiple attachments.", 3499.00m, "Home & Kitchen"),
            ("Non-Stick Frying Pan", "28cm non-stick frying pan.", 349.90m, "Home & Kitchen"),
            ("Ceramic Dinner Set", "16-piece ceramic dinner set.", 899.00m, "Home & Kitchen"),

            ("Yoga Mat", "Non-slip yoga mat, 6mm thick.", 249.00m, "Sports & Outdoors"),
            ("Adjustable Dumbbell Set", "Adjustable dumbbell pair, 2-20kg.", 2199.00m, "Sports & Outdoors"),
            ("Camping Tent", "2-person waterproof camping tent.", 1599.00m, "Sports & Outdoors"),
        ];

        var names = catalog.Select(c => c.Name).ToArray();
        var existing = await context.Products
            .Where(p => names.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name);

        foreach (var item in catalog)
        {
            if (existing.ContainsKey(item.Name))
                continue;

            var product = new Product
            {
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                CategoryId = categories[item.Category].Id,
            };

            context.Products.Add(product);
            existing[item.Name] = product;
        }

        return existing.Values.ToList();
    }

    private static async Task<List<User>> SeedUsersAsync(ApplicationDbContext context, UserManager<User> userManager)
    {
        (string FirstName, string LastName, string Email, string Role)[] seedUsers =
        [
            ("Admin", "User", "admin@shoppy.com", "Admin"),
            ("Ayse", "Yilmaz", "ayse@shoppy.com", "Customer"),
            ("Mehmet", "Demir", "mehmet@shoppy.com", "Customer"),
        ];

        const string password = "Passw0rd!23";

        var customers = new List<User>();

        foreach (var (firstName, lastName, email, roleName) in seedUsers)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = User.Create(firstName, lastName, email, email);
                var result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                    continue;
            }

            var role = await context.AppRoles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role is null)
                continue;

            var hasRole = await context.AppUserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

            if (!hasRole)
                context.AppUserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

            if (roleName == "Customer")
                customers.Add(user);
        }

        await context.SaveChangesAsync();

        return customers;
    }

    private static async Task SeedOrdersAsync(ApplicationDbContext context, List<Product> products, List<User> customers)
    {
        if (customers.Count == 0 || await context.Orders.AnyAsync())
            return;

        var random = new Random(20260704);
        var baseDate = DateTimeOffset.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            var order = new Order
            {
                OrderDate = baseDate.AddDays(-random.Next(0, 30)),
                CreatedBy = customers[random.Next(customers.Count)].Id,
                Items = [],
            };

            var itemCount = random.Next(1, 4);
            var chosenProducts = products.OrderBy(_ => random.Next()).Take(itemCount);

            foreach (var product in chosenProducts)
            {
                order.Items.Add(new OrderItem
                {
                    Product = product,
                    Quantity = random.Next(1, 6),
                    UnitPrice = product.Price,
                });
            }

            context.Orders.Add(order);
        }
    }
}

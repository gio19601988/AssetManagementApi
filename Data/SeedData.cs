// Data/SeedData.cs
using AssetManagementApi.Data;
using AssetManagementApi.Models;
using AssetManagementApi.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManagementApi.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated(); // თუ არ გინდა მიგრაციები ხელით

            // თუ უკვე არსებობს როლები — არ ჩავამატოთ ხელახლა
            if (context.Roles.Any()) return;

            // როლები
            var roles = new List<Role>
            {
                new Role { Name = "Super Admin", Description = "სრული წვდომა ყველა სისტემაზე" },
                new Role { Name = "Admin", Description = "ადმინისტრატორი" },
                new Role { Name = "Order Manager", Description = "შეკვეთების მენეჯერი" },
                new Role { Name = "Department Head", Description = "დეპარტამენტის ხელმძღვანელი" },
                new Role { Name = "Approver", Description = "დამამტკიცებელი" },
                new Role { Name = "Requester", Description = "შეკვეთის შემქმნელი" },
                new Role { Name = "Viewer", Description = "მხოლოდ ნახვის უფლება" }
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();

            // უფლებები (შენი სიიდან)
            var permissions = new List<Permission>
            {
                new Permission { Code = "orders.view.all", Module = "orders", Action = "view", Description = "ყველა შეკვეთის ნახვა" },
                new Permission { Code = "orders.create", Module = "orders", Action = "create", Description = "შეკვეთის შექმნა" },
                // ... დაამატე ყველა შენი INSERT-დან
                new Permission { Code = "admin.users.manage", Module = "admin", Action = "manage", Description = "მომხმარებლების მართვა" },
                // ...
            };
            context.Permissions.AddRange(permissions);
            context.SaveChanges();

            // Super Admin-ს ყველა უფლება
            var superAdmin = context.Roles.First(r => r.Name == "Super Admin");
            foreach (var perm in context.Permissions.ToList())
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = superAdmin.Id,
                    PermissionId = perm.Id,
                    GrantedBy = null // ან admin user ID თუ გაქვს
                });
            }
            context.SaveChanges();

            // Order Statuses
            var statuses = new List<OrderStatus>
            {
                new OrderStatus { Code = "pending", Name = "Pending", NameKa = "მოლოდინში", Color = "#FFA500", OrderSeq = 1 },
                new OrderStatus { Code = "review", Name = "Under Review", NameKa = "განხილვაში", Color = "#2196F3", OrderSeq = 2 },
                new OrderStatus { Code = "approved", Name = "Approved", NameKa = "დამტკიცებული", Color = "#4CAF50", OrderSeq = 3 },
                new OrderStatus { Code = "completed", Name = "Completed", NameKa = "დასრულებული", Color = "#9E9E9E", OrderSeq = 4 },
                new OrderStatus { Code = "cancelled", Name = "Cancelled", NameKa = "გაუქმებული", Color = "#F44336", OrderSeq = 5 },
                new OrderStatus { Code = "rejected", Name = "Rejected", NameKa = "უარყოფილი", Color = "#FF5722", OrderSeq = 6 },
                new OrderStatus { Code = "archived", Name = "Archived", NameKa = "არქივირებული", Color = "#757575", OrderSeq = 7 }
            };
            context.OrderStatuses.AddRange(statuses);
            context.SaveChanges();

            // შეგიძლია დაამატო სხვა seed-ები: departments, users და ა.შ.
        }
    }
}
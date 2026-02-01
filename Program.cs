using AssetManagementApi.Data;
using AssetManagementApi.Services;
using AssetManagementApi.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;  // აუცილებელი Swagger JWT-ისთვის
using System.Text;
using OfficeOpenXml;
using Microsoft.Extensions.Logging; // თუ ლოგერი გჭირდება
using Microsoft.AspNetCore.Http;  // IHttpContextAccessor-ისთვის

var builder = WebApplication.CreateBuilder(args);

// ==================== Database ====================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==================== Services & Repositories ====================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AssetRepository>(); // თუ namespace სწორია
builder.Services.AddScoped<OrderService>();

// დამატებული: HttpContextAccessor-ის რეგისტრაცია DI-ში (OrderService-ისთვის საჭირო)
builder.Services.AddHttpContextAccessor();

// ==================== Controllers & Swagger ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AssetManagementApi", Version = "v1" });

    // JWT ავტორიზაციის ღილაკი Swagger-ში
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// ==================== JWT Authentication ====================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ==================== CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var app = builder.Build();

// ==================== Middleware ====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseStaticFiles();  // აუცილებელია wwwroot/files-ისთვის
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ==================== Seed Data (ახალი ნაწილი) ====================
// ეს გაუშვება ერთხელ აპლიკაციის გაშვებისას (ან შეგიძლია გააკეთო idempotent, რომ არ დუბლირდეს)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
       
        SeedData.Initialize(services);  // ან SeedData.SeedRolesAndPermissions(services); როგორც შენ გინდა დაარქვი

        // ალტერნატიულად, თუ გინდა პირდაპირ აქ დაწერო (მაგრამ უკეთესია ცალკე კლასში)
        // var context = services.GetRequiredService<ApplicationDbContext>();
        // SeedRolesAndStatuses(context); // მაგალითად
    }
    catch (Exception ex)
    {
       
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Seed data-ს დროს შეცდომა: {Message}", ex.Message);
    }
}

// ==================== Run ====================
app.Run();
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RbacSso.Common.Middleware;
using RbacSso.ProductService.Application.Common.Interfaces;
using RbacSso.ProductService.Infrastructure.Persistence;
using RbacSso.Security.Authentication;
using RbacSso.Security.Authorization;
using RbacSso.Tenant;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

// MediatR for CQRS
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<IProductRepository>();
});

// Repository
builder.Services.AddScoped<IProductRepository, EfProductRepository>();

// Security Services
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddHttpContextAccessor();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"] ?? "gateway";
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata", true);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "realm_access.roles"
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(Roles.Admin));

    options.AddPolicy("TenantAdminOrHigher", policy =>
        policy.RequireRole(Roles.Admin, Roles.TenantAdmin));

    options.AddPolicy("UserOrHigher", policy =>
        policy.RequireRole(Roles.Admin, Roles.TenantAdmin, Roles.User));

    options.AddPolicy("AllAuthenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Service API",
        Version = "v1",
        Description = "RBAC-SSO Multi-Tenant E-Commerce POC - Product Service"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline

// Correlation ID middleware (should be first)
app.UseMiddleware<CorrelationIdMiddleware>();

// Global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

// Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API v1");
    });
}

app.UseRouting();

// Tenant resolution middleware
app.UseMiddleware<TenantMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

// Database migration (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

// Make Program accessible for testing
public partial class Program { }

using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using staffnex.Api.Data;
using staffnex.Api.DTOs;
using staffnex.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var swaggerEnabled = builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? Array.Empty<string>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new ApiErrorResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Message = "One or more validation errors occurred.",
            TraceId = context.HttpContext.TraceIdentifier,
            Errors = errors
        });
    };
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "staffnex Attendance API",
        Version = "v1",
        Description = "Staff attendance, location tracking, leave approval, and payroll reporting API for staffnex."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    options.SupportNonNullableReferenceTypes();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT token.",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    var shouldSeedData = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("SeedData:Enabled");
    if (shouldSeedData)
    {
        await AppDbSeeder.SeedAsync(dbContext);
    }
}

// Configure the HTTP request pipeline.
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(handler =>
{
    handler.Run(async httpContext =>
    {
        var exception = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var response = new ApiErrorResponse
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Message = app.Environment.IsDevelopment()
                ? exception?.Message ?? "An unexpected error occurred."
                : "An unexpected error occurred.",
            TraceId = httpContext.TraceIdentifier
        };

        await httpContext.Response.WriteAsJsonAsync(response);
    });
});

app.UseHttpsRedirection();

if (corsOrigins.Length > 0)
{
    app.UseCors("ConfiguredOrigins");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();

app.Run();

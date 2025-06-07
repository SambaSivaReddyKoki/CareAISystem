using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using CareAI.Core.Configuration;
using CareAI.Infrastructure.Data;
using CareAI.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using CareAI.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CareAI API", Version = "v1" });
    
    // Add API key authentication
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key Authentication",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure database
builder.Services.AddDbContext<CareAIDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string 'DefaultConnection' not found.");
    }
    options.UseSqlServer(connectionString);
});

// Configure Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CaseyAI_";
});

// Configure and validate OpenAI options
var openAISection = builder.Configuration.GetSection("OpenAI");
var openAIOptions = new CareAI.Core.Configuration.OpenAIOptions();
openAISection.Bind(openAIOptions);

// Validate required settings
if (string.IsNullOrEmpty(openAIOptions?.ApiKey) || string.IsNullOrEmpty(openAIOptions?.Endpoint))
{
    throw new InvalidOperationException("OpenAI API key and endpoint must be configured in appsettings.json");
}

// Register services
builder.Services.AddScoped<ILangChainService, LangChainService>();

// Register Azure OpenAI client
builder.Services.AddSingleton<OpenAIClient>(sp => 
    new OpenAIClient(new Uri(openAIOptions.Endpoint), new AzureKeyCredential(openAIOptions.ApiKey)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Casey AI System API v1");
        c.RoutePrefix = string.Empty; // Serve the Swagger UI at the root
    });
    
    // Enable detailed errors in development
    app.UseExceptionHandler("/error");
    app.UseStatusCodePagesWithReExecute("/error/{0}");
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// The order of middleware is important here
app.UseHttpsRedirection();

// Add CORS policy - must be before UseRouting and after UseHttpsRedirection
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
app.UseCors(policy => 
    policy.WithOrigins(allowedOrigins)
          .AllowAnyMethod()
          .AllowAnyHeader());

app.UseRouting();

// Add API Key Authentication Middleware - after UseRouting but before UseEndpoints
app.UseApiKeyAuth();

app.UseAuthorization();

// Add error handling middleware - must be after UseRouting and before MapControllers
app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.MapControllers();

// Global exception handler
app.UseExceptionHandler(a => a.Run(async context =>
{
    var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
    var exception = exceptionHandlerPathFeature?.Error;
    
    await context.Response.WriteAsJsonAsync(new { error = exception?.Message ?? "An error occurred" });
}));

app.Run();

using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;
using MolenApplicatie.Server.Services.Database;
using MySqlConnector;
using TypedApi.Swagger;

var builder = WebApplication.CreateBuilder(args);

var databasePassword = ReadSystemdCredential("molen-db-password");

if (!string.IsNullOrWhiteSpace(databasePassword))
{
    var fallbackConnectionString =
        builder.Configuration.GetConnectionString("MolenDatabase")
        ?? throw new InvalidOperationException(
            "Connection string 'MolenDatabase' was not configured.");

    var connectionStringBuilder =
        new MySqlConnectionStringBuilder(fallbackConnectionString)
        {
            Password = databasePassword
        };

    builder.Configuration["ConnectionStrings:MolenDatabase"] =
        connectionStringBuilder.ConnectionString;
}

var fileUploadAuthorization =
    ReadSystemdCredential("file-upload-authorization");

if (!string.IsNullOrWhiteSpace(fileUploadAuthorization))
{
    builder.Configuration["FileUploadFilter:Authorization"] =
        fileUploadAuthorization;
}

if (builder.Environment.IsProduction())
{
    builder.Environment.WebRootPath = "/var/www/app/wwwroot";
}

if (string.IsNullOrEmpty(builder.Environment.WebRootPath))
{
    builder.Environment.WebRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
}

builder.Services.AddTypedApiSwagger();

builder.Services
    .AddControllers()
    .AddTypedApiJsonOptions()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<NewMolenDataService>();
builder.Services.AddTransient<PlacesService>();
builder.Services.AddTransient<PlaceTypeService>();
builder.Services.AddTransient<MolenService>();
builder.Services.AddTransient<SearchService>();
builder.Services.AddTransient<MapClusterService>();

builder.Services.AddScoped<DBMolenAddedImageService>();
builder.Services.AddScoped<DBMolenDissappearedYearsService>();
builder.Services.AddScoped<DBPlaceService>();
builder.Services.AddScoped<DBPlaceTypeService>();
builder.Services.AddScoped<DBMolenTypeService>();
builder.Services.AddScoped<DBMolenDataService>();
builder.Services.AddScoped<DBMolenMakerService>();
builder.Services.AddScoped<DBMolenImageService>();
builder.Services.AddScoped<DBMolenTBNService>();
builder.Services.AddScoped<DBMolenTypeAssociationService>();
builder.Services.AddTransient<HttpClient>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760;
});

builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileUploadFilter"));

var connectionString = builder.Configuration.GetConnectionString("MolenDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'MolenDatabase' was not configured.");

builder.Services.AddDbContext<MolenDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    if (builder.Environment.IsDevelopment()) options.EnableSensitiveDataLogging();
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MolenDbContext>();

    Console.WriteLine("Checking database migrations...");
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Database migrations applied successfully.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var molenAddedImagesPath =
    Path.Combine(app.Environment.WebRootPath, "MolenAddedImages");

var molenImagesPath =
    Path.Combine(app.Environment.WebRootPath, "MolenImages");

Directory.CreateDirectory(molenAddedImagesPath);
Directory.CreateDirectory(molenImagesPath);

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(molenAddedImagesPath),
    RequestPath = "/MolenAddedImages"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(molenImagesPath),
    RequestPath = "/MolenImages"
});

app.UseRouting();

app.UseCors(corsPolicyBuilder =>
{
    corsPolicyBuilder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
});

// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapFallbackToFile("/index.html");

app.Run();

static string? ReadSystemdCredential(string credentialName)
{
    var credentialsDirectory =
        Environment.GetEnvironmentVariable("CREDENTIALS_DIRECTORY");

    if (string.IsNullOrWhiteSpace(credentialsDirectory))
    {
        return null;
    }

    var credentialPath =
        Path.Combine(
            credentialsDirectory,
            credentialName);

    if (!File.Exists(credentialPath))
    {
        return null;
    }

    var credentialValue =
        File.ReadAllText(credentialPath)
            .TrimEnd('\r', '\n');

    return string.IsNullOrWhiteSpace(credentialValue)
        ? null
        : credentialValue;
}
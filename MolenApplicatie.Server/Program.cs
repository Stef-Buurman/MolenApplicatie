using System.Net;
using Hangfire;
using Hangfire.Console;
using Hangfire.MySql;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Jobs;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;
using MolenApplicatie.Server.Services.Database;
using MySqlConnector;
using TypedApi.Swagger;

var builder = WebApplication.CreateBuilder(args);

var configuredConnectionString = builder.Configuration.GetConnectionString("MolenDatabase") ?? throw new InvalidOperationException("Connection string 'MolenDatabase' was not configured.");

var connectionStringBuilder = new MySqlConnectionStringBuilder(configuredConnectionString) { AllowUserVariables = true };

var databasePassword = ReadSystemdCredential("molen-db-password");

if (!string.IsNullOrWhiteSpace(databasePassword))
{
    connectionStringBuilder.Password = databasePassword;
}

var connectionString = connectionStringBuilder.ConnectionString;

builder.Configuration["ConnectionStrings:MolenDatabase"] = connectionString;

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
builder.Services.AddTransient<MillDatabaseCsvImportService>();
builder.Services.AddTransient<MillDatabaseImportJob>();

builder.Services
    .AddHttpClient<MillDatabaseRemoteImportService>(httpClient =>
    {
        httpClient.Timeout = TimeSpan.FromMinutes(10);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MolenApplicatie/1.0)");
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = true,
        UseCookies = true,
        CookieContainer = new CookieContainer(),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
    });

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

builder.Services.AddDbContext<MolenDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.UseQuerySplittingBehavior(
                QuerySplittingBehavior.SplitQuery);
        });

    // if (builder.Environment.IsDevelopment())
    // {
    //     options.EnableSensitiveDataLogging();
    // }
});

builder.Services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseStorage(
            new MySqlStorage(
                connectionString,
                new MySqlStorageOptions
                {
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    DashboardJobListLimit = 50_000,
                    TablesPrefix = "hangfire__"
                }))
        .UseConsole();
});

builder.Services.AddHostedService<DatabaseInitializationHostedService>();

builder.Services.AddHangfireServer(options =>
{
    options.ServerName =
        $"{Environment.MachineName}:MolenApplicatie";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var molenAddedImagesPath = Path.Combine(app.Environment.WebRootPath, "MolenAddedImages");

var molenImagesPath = Path.Combine(app.Environment.WebRootPath, "MolenImages");

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

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
else
{
    var hangfireConfiguration =
        app.Configuration.GetSection("Hangfire");

    var hangfireDashboardUsername = ReadSystemdCredential("hangfire-dashboard-username")
        ?? Environment.GetEnvironmentVariable(
            "HANGFIRE_DASHBOARD_USERNAME")
        ?? hangfireConfiguration.GetValue<string>(
            "HANGFIRE_DASHBOARD_USERNAME")
        ?? throw new InvalidOperationException(
            "The Hangfire dashboard username was not configured.");

    var hangfireDashboardPassword = ReadSystemdCredential("hangfire-dashboard-password")
        ?? Environment.GetEnvironmentVariable(
            "HANGFIRE_DASHBOARD_PASSWORD")
        ?? hangfireConfiguration.GetValue<string>(
            "HANGFIRE_DASHBOARD_PASSWORD")
        ?? throw new InvalidOperationException(
            "The Hangfire dashboard password was not configured.");

    app.UseHangfireDashboard(
        "/api/hangfire",
        new DashboardOptions
        {
            Authorization =
            [
                new HangfireDashboardBasicAuthFilter(
                    hangfireDashboardUsername,
                    hangfireDashboardPassword)
            ]
        });
}

HangfireJobRegistry.Register();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapFallbackToFile("/index.html");

app.Run();

static string? ReadSystemdCredential(string credentialName)
{
    var credentialsDirectory = Environment.GetEnvironmentVariable("CREDENTIALS_DIRECTORY");

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
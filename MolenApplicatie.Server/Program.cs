using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    builder.Environment.WebRootPath = "/var/www/app/wwwroot";
}

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<MolenService>();
builder.Services.AddTransient<NewMolenDataService>();
builder.Services.AddTransient<NewMolenDataService2_0>();
builder.Services.AddTransient<PlacesService>();
builder.Services.AddTransient<PlacesService2_0>();
builder.Services.AddTransient<MolenService2_0>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760;
});
builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileUploadFilter"));

string connectionString = "server=localhost;user=root;database=molen_database;port=3306;password=DitIsEchtEenLastigWW";
builder.Services.AddDbContext<MolenDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddTransient<MolenDbContext>();

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
//builder.Services.AddControllers().AddJsonOptions(x =>
//{
//    x.JsonSerializerOptions.ReferenceHandler = null;
//    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
//});


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    var molenAddedImagesPath = Path.Combine(builder.Environment.WebRootPath, "MolenAddedImages");
    var molenImagesPath = Path.Combine(builder.Environment.WebRootPath, "MolenImages");
    if (Directory.Exists(molenAddedImagesPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(molenAddedImagesPath),
            RequestPath = "/MolenAddedImages"
        });
    }

    if (Directory.Exists(molenImagesPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(molenImagesPath),
            RequestPath = "/MolenImages"
        });
    }
}
else
{
    app.UseStaticFiles();
}

app.UseRouting();

app.UseCors(builder =>
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader());

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapFallbackToFile("/index.html");

app.Run();

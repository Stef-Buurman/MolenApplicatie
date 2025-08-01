using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
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
builder.Services.AddTransient<PlacesService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760;
});
builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileUploadFilter"));

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

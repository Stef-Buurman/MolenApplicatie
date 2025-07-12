using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;
using MolenApplicatie.Server.Services.Database;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    builder.Environment.WebRootPath = "/var/www/app/wwwroot";
}

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<NewMolenDataService>();
builder.Services.AddTransient<PlacesService>();
builder.Services.AddTransient<PlaceTypeService>();
builder.Services.AddTransient<MolenService>();
builder.Services.AddTransient<DBMolenAddedImageService>();
builder.Services.AddTransient<DBMolenDissappearedYearsService>();
builder.Services.AddTransient<DBPlaceService>();
builder.Services.AddTransient<DBPlaceTypeService>();
builder.Services.AddTransient<DBMolenTypeService>();
builder.Services.AddTransient<DBMolenDataService>();
builder.Services.AddTransient<DBMolenMakerService>();
builder.Services.AddTransient<DBMolenImageService>();
builder.Services.AddTransient<DBMolenTBNService>();
builder.Services.AddTransient<DBMolenTypeAssociationService>();
builder.Services.AddTransient<HttpClient>(); 
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760;
});
builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileUploadFilter"));

string connectionString = "server=localhost;user=root;password=DitIsEchtEenLastigWW;database=molen_database;port=3306";

builder.Services.AddDbContext<MolenDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)).LogTo(_ => { }, LogLevel.None)
);

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

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


#if DEBUG

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetService<MolenDbContext>();
    context!.Database.EnsureCreated();

    if (!context.Database.CanConnect())
        throw new FileLoadException("cannot connect to db!");
}
#endif

app.Run();

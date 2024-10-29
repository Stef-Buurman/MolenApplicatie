using Microsoft.AspNetCore.Http.Features;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<MolenService>();
builder.Services.AddTransient<NewMolenDataService>();
builder.Services.AddTransient<PlacesService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760;
});
builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileUploadFilter"));

var app = builder.Build();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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


app.Run();

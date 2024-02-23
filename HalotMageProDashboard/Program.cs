using HalotMageProDashboard;
using HalotMageProDashboard.Models;
using HalotMageProDashboard.ViewModels;
using HalotMageProDashboard.Views;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WpfApplication<App, MainWindow>.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options => {
    var env = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var appData = Path.Combine(env, "HalotMageProDashboard");
    Directory.CreateDirectory(appData);

    options.UseSqlite($"Filename={Path.Combine(appData, "databases.db")}");
}, ServiceLifetime.Transient);

builder.Services.AddSingleton<MainWindowViewModel>();
builder.Services.AddSingleton<PrinterManager>();
builder.Services.AddHttpClient();

var app = builder.Build();

var dbContext = app.Services.GetRequiredService<ApplicationDbContext>();
dbContext.Database.EnsureCreated();

app.Run();

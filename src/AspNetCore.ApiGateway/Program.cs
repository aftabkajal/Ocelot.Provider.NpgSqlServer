using Ocelot.DependencyInjection;
using Ocelot.Provider.NpgSqlServer.DependencyInjection;
using Ocelot.Provider.NpgSqlServer.Middleware;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var env = builder.Environment;

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddOcelot()
    .AddNpgSqlServerProvider(options =>
    {
        options.DbConnectionStrings = builder.Configuration.GetConnectionString("NpgSqlServerDb");
        options.MigrationsAssembly = Assembly.GetExecutingAssembly().FullName;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseOcelotWithNpgSqlServerProvider().Wait();

app.Run();

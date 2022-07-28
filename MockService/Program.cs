using System.Net;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;


var builder = WebApplication.CreateBuilder(args);
    
ConfigurationManager.ConfigurationRoot = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", true, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddEnvironmentVariables()
    .Build();

builder.Services.AddOptions();

builder.Services.AddControllers(opts =>
    {
        opts.SuppressInputFormatterBuffering = true;
        opts.SuppressOutputFormatterBuffering = true;
    })
    .AddNewtonsoftJson(
        options =>
        {
            options.SerializerSettings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };
        });

builder.Services.AddCors(options =>
    options.AddPolicy(CommonBehavior.AllowAllOriginsCorsPolicyName,
        builder =>
            builder.WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .AllowAnyOrigin()
                .AllowAnyHeader()));

builder.Services.AddInitializers();

builder.Services.AddSingleton(new JsonSerializer
{
    ContractResolver = new DefaultContractResolver
    {
        NamingStrategy = new SnakeCaseNamingStrategy()
    }
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Any, ConfigurationManager.GetApplicationPort());
    serverOptions.AllowSynchronousIO = true;
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseEndpoints(
    endpoints =>
    {
        endpoints.MapControllers();
    });

app.Run();
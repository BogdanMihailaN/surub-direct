using SurubDirect.Worker.BackgroundServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SurubDirect.Catalog.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCatalog(builder.Configuration.GetConnectionString("Catalog") ?? throw new InvalidOperationException("Configurează ConnectionStrings__Catalog."));
builder.Services.AddHostedService<OutboxWorker>();
await builder.Build().RunAsync();

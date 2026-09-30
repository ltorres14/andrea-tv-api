using AndreaTV.Api.Configuration;
using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Modules.Devices.Services;
using AndreaTV.Api.Modules.Movies.Catalog;
using AndreaTV.Api.Modules.Movies.Services;
using AndreaTV.Api.Modules.Security.Services;
using AndreaTV.Api.Modules.Series.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Local.json",
        optional: true,
        reloadOnChange: true);
}

// Keep environment variables as the highest-precedence local configuration source.
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.Configure<XtreamOptions>(
    builder.Configuration.GetSection(XtreamOptions.SectionName));
builder.Services.Configure<MovieCatalogOptions>(
    builder.Configuration.GetSection(MovieCatalogOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));

builder.Services
    .AddHttpClient<IXtreamClient, XtreamClient>(client =>
    {
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false
    })
    .RemoveAllLoggers();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IMovieCatalogRepository, FileMovieCatalogRepository>();
builder.Services.AddSingleton<IMovieCatalogCache, MovieCatalogCache>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<ISeriesService, SeriesService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IXtreamConnectionService, XtreamConnectionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

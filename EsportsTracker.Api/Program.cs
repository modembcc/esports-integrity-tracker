using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<PandaScoreClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PandaScore:BaseUrl"]!);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", builder.Configuration["PandaScore:ApiKey"]);
});

builder.Services.AddHttpClient("polymarket-gamma", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Polymarket:GammaBaseUrl"]!);
    c.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Polymarket:TimeoutSeconds", 15));
});

builder.Services.AddHttpClient("polymarket-clob", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Polymarket:ClobBaseUrl"]!);
    c.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Polymarket:TimeoutSeconds", 15));
});

builder.Services.AddScoped<PolymarketClient>();
builder.Services.AddSingleton<AnomalyDetectionService>();
builder.Services.AddSingleton<UpsetDetectionService>();
builder.Services.AddHostedService<TournamentSyncService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// NOTE: intentionally no UseHttpsRedirection() in dev — it turns CORS preflights
// into 307s. Deploy platform terminates TLS. Re-add behind !IsDevelopment() if wanted.
app.UseCors();

app.MapControllers();

app.Run();
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
builder.Services.AddHostedService<MsiSyncService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

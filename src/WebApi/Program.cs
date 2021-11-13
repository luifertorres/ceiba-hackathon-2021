var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddHttpClient("Api", c => 
    {
        c.Timeout = TimeSpan.FromMilliseconds(1000);
        //c.BaseAddress = new Uri($"http://{builder.Configuration["SVC_API_HOSTNAME"]}/{builder.Configuration["SVC_API_PORT"]}");
        c.BaseAddress = new("http://api-1.hack.local/");
    })
    .AddPolicyHandler(GetCircuitBreakerPolicy());
builder.Services.AddMemoryCache();
builder.Services.AddControllers();

var app = builder.Build();

app.MapHealthChecks("/healthz");
app.MapControllers();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() => HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30));
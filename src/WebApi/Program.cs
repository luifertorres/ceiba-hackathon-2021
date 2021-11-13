var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddHttpClient("Api", c => 
    {
        c.Timeout = TimeSpan.FromMilliseconds(1000);
        c.BaseAddress = new Uri($"http://{builder.Configuration["SVC_API_HOSTNAME"]}/{builder.Configuration["SVC_API_PORT"]}");
    })
    .AddPolicyHandler(GetCircuitBreakerPolicy());
builder.Services.AddMemoryCache();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/", async ([FromQuery] int number, [FromServices] IHttpClientFactory factory, [FromServices] IMemoryCache cache) =>
{
    if (cache.TryGetValue(number, out var json))
    {
        return new StringContent((string)json, Encoding.UTF8, MediaTypeNames.Application.Json);
    }

    var client = factory.CreateClient("Api");
    var response = await client.GetAsync($"/?number={number}");

    var content = await response.Content.ReadAsStringAsync();
    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content);

    cache.Set(number, content, TimeSpan.FromSeconds(apiResponse!.ValiditySeconds));

    return new StringContent(content, Encoding.UTF8, MediaTypeNames.Application.Json);
});

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() => HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30));

record ApiResponse(DateTime Date, int ValiditySeconds);

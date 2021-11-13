namespace WebApi.Controllers;

[Route("")]
[ApiController]
public class HomeController : ControllerBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] int number, [FromServices] IHttpClientFactory factory, [FromServices] IMemoryCache cache)
    {
        if (cache.TryGetValue(number, out var json))
        {
            return Content((string)json, MediaTypeNames.Application.Json);
        }

        var client = factory.CreateClient("Api");
        var response = await client.GetAsync($"/?number={number}");

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, s_jsonOptions);

        cache.Set(number, content, TimeSpan.FromSeconds(apiResponse!.ValiditySeconds));

        return Content(content, MediaTypeNames.Application.Json);
    }
    record ApiResponse(DateTime Date, int ValiditySeconds);
}

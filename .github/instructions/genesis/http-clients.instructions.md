---
applyTo: "**/*Client.cs,**/*HttpClientOptions.cs"
---

# HTTP Client Rules

## Never create HttpClient directly

- **NEVER** `new HttpClient()` — this leaks sockets and ignores DNS changes.
- **NEVER** inject `HttpClient` directly in a constructor — use `IHttpClientFactory`.
- All HTTP clients must be registered as named clients via Needlr's `[HttpClientOptions]`.

## Options record pattern

Define a sealed record decorated with `[HttpClientOptions]` that implements capability
interfaces. Needlr's source generator emits both the options binding and the
`AddHttpClient` registration automatically — no manual registration code needed.

### Preferred: explicit config section path

Always provide an explicit section path via the attribute constructor. The section should
live alongside your feature's other configuration, not in a global `HttpClients` bucket:

```csharp
[HttpClientOptions("BraveSearch")]
public sealed record BraveSearchHttpClientOptions : IStandardHttpClientOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(15);
    public string? UserAgent { get; init; } = "MyApp/1.0";
    public Uri? BaseAddress { get; init; } = new Uri("https://search.brave.com/");
    public IReadOnlyDictionary<string, string>? DefaultHeaders { get; init; }
}
```

The corresponding `appsettings.json`:

```json
{
  "BraveSearch": {
    "Timeout": "00:00:30",
    "UserAgent": "MyApp-Production/1.0"
  }
}
```

### Fallback: inferred section

If no section is provided, Needlr infers `HttpClients:{Name}` from the type name by
stripping the `HttpClientOptions` suffix. Only use this when there is no existing feature
config section:

```csharp
// Inferred name: "WebFetch", section: "HttpClients:WebFetch"
[HttpClientOptions]
public sealed record WebFetchHttpClientOptions : IStandardHttpClientOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(15);
    public string? UserAgent { get; init; }
    public Uri? BaseAddress { get; init; }
    public IReadOnlyDictionary<string, string>? DefaultHeaders { get; init; }
}
```

### Explicit name override

Use the `Name` property when the type name doesn't match the desired client name:

```csharp
[HttpClientOptions(Name = "tavily-primary")]
public sealed record TavilyPrimaryHttpClientOptions : IStandardHttpClientOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
    public string? UserAgent { get; init; }
    public Uri? BaseAddress { get; init; } = new Uri("https://api.tavily.com/");
    public IReadOnlyDictionary<string, string>? DefaultHeaders { get; init; }
}
```

### Minimal capability set

If you only need a subset of capabilities, implement individual interfaces instead of
`IStandardHttpClientOptions`. The generator only emits wiring for implemented capabilities:

```csharp
// Only timeout — no UserAgent, BaseAddress, or Headers wiring emitted
[HttpClientOptions("Upstream:Tavily")]
public sealed record TavilyHttpClientOptions : INamedHttpClientOptions, IHttpClientTimeout
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
```

## Client class conventions

Typed client classes resolve their named client from `IHttpClientFactory`:

```csharp
internal sealed class BraveSearchClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BraveSearchClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> SearchAsync(string query, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient("BraveSearch");
        var response = await client.GetAsync($"/api/search?q={Uri.EscapeDataString(query)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
```

- The client name passed to `CreateClient` must match the resolved name from the options record
- Always pass `CancellationToken` to HTTP calls
- Always `using` the `HttpClient` from the factory (it returns to the pool on dispose)
- Keep client classes thin — they make HTTP calls, not business logic

## Defaults and overrides

- Property initializers on the options record provide compile-time defaults
- `appsettings.json` values override defaults at runtime without a rebuild
- `IOptions<T>` can be injected alongside the factory for runtime access to the typed config

## Testing

- Mock `IHttpClientFactory` to return a client backed by a `MockHttpMessageHandler`
- Test the client class in isolation from the real HTTP endpoint
- Do not test Needlr's source-generated registration — test your client's behavior

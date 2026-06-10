---
name: building-seq-plug-in-apps
description: Use this when developing plug-in Seq inputs (for ingestion) and outputs (for alert notifications or streamed events).
license: Apache-2.0
metadata:
 author: Datalust and Contributors
---

This skill covers building [Seq](https://datalust.co/seq) apps (plugins) using the `Seq.Apps` runtime API. Seq apps are .NET libraries that extend Seq with custom event processing (output apps) or event generation (input apps).

## Project structure

A Seq app follows this layout:

```
src/
  Seq.App.{Name}/
    Seq.App.{Name}.csproj          # Main library
    {Name}App.cs                   # SeqApp subclass (entry point)
    ...                            # Supporting types
    Resources/                     # Embedded resources (default templates, etc.)
  Seq.App.{Name}.SmokeTest/        # Optional console app for manual testing
    Program.cs
test/
  Seq.App.{Name}.Tests/
    {Name}AppTests.cs              # App-level integration tests
    ...Tests.cs                    # Unit tests for components
    Support/
      Test{Gateway}.cs             # Test doubles
      Some.cs                      # LogEvent factory helpers
```

Input apps use `Seq.Input.{Name}` naming.

### `.csproj` conventions

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Seq.Apps" Version="..." />
    <!-- Add Seq.Syntax (project or package reference) if using the Seq template language -->
  </ItemGroup>
  <ItemGroup>
    <!-- Package dependencies into primary NUPKG file, except those shipped in the app host itself -->
    <None Include="./obj/publish/**/*" Exclude="./obj/publish/$(MSBuildProjectName).dll;./obj/publish/Seq.Apps.dll;./obj/publish/Serilog.dll" Pack="true" PackagePath="lib/$(TargetFramework)" />
  </ItemGroup>
</Project>
```

Embedded resources for default templates use `LogicalName` for clean resource stream access:

```xml
<ItemGroup>
  <EmbeddedResource Include="./Resources/DefaultHtmlTemplate.txt" LogicalName="DefaultHtmlTemplate" />
</ItemGroup>
```

Test projects target the latest stable .NET release, and use xUnit.

The runtime pins its own copies of `Seq.Apps`, `Serilog`, and `Seq.Syntax` — the app's copies of these assemblies are ignored at load time. Other dependencies are loaded from the app's package.

### Namespace disambiguation

Projects named `Seq.App.{Name}` have a root namespace that collides with `Seq.Apps`. Use `global::` where `Seq.Apps.App` or `Seq.Apps.Host` is referenced as a type within a `Seq.App.*` namespace:

```csharp
public static LogEventPropertyValue? MyAppHost(global::Seq.Apps.Host host) { ... }
public static LogEventPropertyValue? MyAppInstance(global::Seq.Apps.App app) { ... }
```

Use `InternalsVisibleTo` in the main project to expose `internal` constructors and types to the test project.

## App class structure

### Output apps (event subscribers)

Output apps derive from `SeqApp` and implement `ISubscribeToAsync<LogEvent>`:

```csharp
using Seq.Apps;
using Serilog.Events;

[SeqApp("App Name",
    Description = "A short sentence describing what the app does.")]
public class MyApp : SeqApp, ISubscribeToAsync<LogEvent>, IDisposable
{
    readonly IMyGateway _gateway;
    MyMessageFactory? _messageFactory;

    // Public parameterless constructor for production use
    public MyApp() : this(new MyHttpGateway()) { }

    // Internal constructor for testing (dependency injection)
    internal MyApp(IMyGateway gateway) { _gateway = gateway; }

    // Settings (see "App settings" section below)

    protected override void OnAttached()
    {
        // Validate required settings, resolve defaults, create factories
    }

    public async Task OnAsync(Event<LogEvent> evt)
    {
        // Process the event: render message, send via gateway
    }

    public void Dispose()
    {
        (_gateway as IDisposable)?.Dispose();
    }
}
```

The `SeqApp` attribute's `Name` parameter is the display name in Seq's UI. Keep it short (1-3 words). `Description` is a single sentence shown below the name.

### Output apps using raw JSON (`ISubscribeToJsonAsync`)

Apps that don't need Serilog `LogEvent` deserialization can implement `ISubscribeToJsonAsync` instead. This receives each event as a raw [CLEF](https://clef-json.org) JSON string, skipping `LogEvent`/`MessageTemplate`/`LogEventProperty` construction entirely:

```csharp
[SeqApp("App Name",
    Description = "A short sentence describing what the app does.")]
public class MyApp : SeqApp, ISubscribeToJsonAsync, IAsyncDisposable
{
    public async Task OnAsync(string json)
    {
        // Parse and process the raw CLEF JSON
    }

    public async ValueTask DisposeAsync() { }
}
```

Prefer `ISubscribeToJsonAsync` for apps that forward or transform events without rendering human-readable output (exporters, relays, bridges), especially at high volume. It avoids the Serilog dependency entirely. The trade-off is that the `Event<LogEvent>` wrapper (`Id`, `EventType`, `Timestamp`) and `Seq.Syntax` template rendering are not available; the app must extract CLEF fields (`@t`, `@l`, `@mt`, `@x`, etc.) from the JSON directly.

Stick with `ISubscribeToAsync<LogEvent>` when the app renders human-readable output using `Seq.Syntax` templates (emails, chat messages, tickets) or inspects structured property values using the Serilog type system (`ScalarValue`, `SequenceValue`, `StructureValue`).

### Input apps (event publishers)

Input apps implement `IPublishJson`:

```csharp
[SeqApp("My Input",
    Description = "Periodically does X and publishes metrics to Seq.")]
public class MyInput : SeqApp, IPublishJson, IDisposable
{
    public void Start(TextWriter inputWriter)
    {
        // Begin producing events; write CLEF JSON lines to inputWriter
        // Return immediately; use background tasks for ongoing work
    }

    public void Stop()
    {
        // Block until publishing has stopped
    }
}
```

The app must synchronize writes to `inputWriter` so that events are not interleaved (e.g., use a `lock` when multiple background threads produce events).

### Lifecycle

1. Seq instantiates the app using the public parameterless constructor
2. The runtime sets `[SeqAppSetting]` properties via reflection, matched by **property name**
3. `Attach()` is called, making `App`, `Host`, and `Log` available
4. `OnAttached()` runs — validate settings and initialize here
5. For subscribers: `OnAsync()` is called for each matching event
6. For publishers: `Start()` is called, then `Stop()` on shutdown
7. `Dispose()` if implemented

Each app instance runs as a separate `seqcli` process. Initialization, event dispatch, and disposal are single-threaded. Events are dispatched one at a time — if `OnAsync()` is slow, backpressure is applied naturally. Apps may create their own background threads for ingestion.

## App settings

Settings are public properties on the app class decorated with `[SeqAppSetting]`. They appear in Seq's UI for the user to configure. The runtime injects values by matching on the **property name** (not `DisplayName`), using `Enum.Parse` for enum types and `Convert.ChangeType` for everything else.

### `SeqAppSetting` attribute properties

| Property | Type | Purpose |
|---|---|---|
| `DisplayName` | `string` | User-facing label in the UI. If omitted, the property name is used. |
| `HelpText` | `string` | Descriptive text shown to the user. Should explain the setting's purpose and give examples. |
| `IsOptional` | `bool` | If `true`, the user can leave the field blank. Non-optional settings that are missing cause a startup error before `Attach()` is called. |
| `InputType` | `SettingInputType` | Controls the UI input widget: `Text`, `LongText`, `Checkbox`, `Integer`, `Decimal`, `Password`. If `Unspecified`, the runtime chooses based on the property type. |
| `Syntax` | `string` | Syntax highlighting in the UI. `"template"` for fields accepting Seq template expressions (in `{braces}`), `"code"` for structured values (JSON, headers). Omit for plain values. |
| `IsInvocationParameter` | `bool` | Marks settings that can be overridden per-invocation (e.g., destination address, channel). |

### Quality guidelines for settings

**Display names** should be concise, title-case-ish, and match how users think about the concept:

| Good | Avoid |
|---|---|
| Bot token | BotToken |
| Chat ID | Telegram Chat Identifier |
| Silent notification | DisableNotification |
| Body is plain text | BodyIsPlainText |
| Connection security | ProtocolSecurity |

**Help text** should:
- Start with a clear description of the setting's purpose
- Include concrete examples where helpful (e.g., format strings, URL patterns, expected values)
- Mention defaults explicitly (e.g., "The default is `Etc/UTC`.", "The default is `POST`.")
- Use backticks for code/values in help text
- Note when template syntax is supported: "Template syntax is supported."
- For password/token fields, explain how to obtain the value

```csharp
[SeqAppSetting(
    DisplayName = "Time zone",
    IsOptional = true,
    HelpText = "The IANA time zone name used when formatting dates and times " +
               "(e.g. Australia/Brisbane). The default is Etc/UTC.")]
public string? TimeZoneName { get; set; }

[SeqAppSetting(
    DisplayName = "Bot token",
    InputType = SettingInputType.Password,
    HelpText = "The bot's API token, in the format 123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11. " +
               "You can create a bot and obtain a token via @BotFather on Telegram.")]
public string? BotToken { get; set; }
```

### Enum settings

When a setting has a fixed set of valid values, use an `enum` type. The runtime presents enum settings as a dropdown and handles validation automatically. Apply `[Description]` to enum members to control the dropdown text when the member name doesn't read naturally:

```csharp
using System.ComponentModel;

public enum OtlpExportProtocol
{
    [Description("HTTP/Protobuf")]
    HttpProtobuf,
    [Description("gRPC")]
    Grpc
}

[SeqAppSetting(
    DisplayName = "Protocol",
    HelpText = "The OTLP export protocol. The default is `HTTP/Protobuf`.",
    IsOptional = true)]
public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.HttpProtobuf;
```

### Setting validation

Validate required settings in `OnAttached()` and throw `ArgumentException` with a message that names the setting using its display name in backticks:

```csharp
protected override void OnAttached()
{
    var botToken = NormalizeOption(BotToken)
        ?? throw new ArgumentException("A `Bot token` must be supplied.");
    var chatId = NormalizeOption(ChatId)
        ?? throw new ArgumentException("A `Chat ID` must be supplied.");
    // ...
}

// Treat empty strings as null for optional settings
static string? NormalizeOption(string? s) => s == "" ? null : s;
```

## Gateway pattern (testability)

External I/O (HTTP APIs, SMTP, cloud services) is abstracted behind an interface so tests can run without network access.

```csharp
// Interface
interface IMyGateway
{
    Task<MyResponse> SendAsync(string param1, string param2, CancellationToken cancel);
}

// Production implementation
class MyHttpGateway : IMyGateway, IDisposable
{
    readonly HttpClient _httpClient = new();

    public async Task<MyResponse> SendAsync(...)
    {
        // HTTP calls, JSON serialization, response parsing
    }

    public void Dispose() => _httpClient.Dispose();
}

// Test double
class TestMyGateway : IMyGateway
{
    public List<(string Param1, string Param2)> Sent { get; } = [];
    public MyResponse NextResponse { get; set; } = new() { Ok = true };

    public Task<MyResponse> SendAsync(string param1, string param2, CancellationToken cancel)
    {
        Sent.Add((param1, param2));
        return Task.FromResult(NextResponse);
    }
}
```

The app class has dual constructors:

```csharp
public MyApp() : this(new MyHttpGateway()) { }       // Production
internal MyApp(IMyGateway gateway) { _gateway = gateway; }  // Testing
```

Use a single long-lived `HttpClient` per gateway. Use `System.Text.Json` for serialization — no third-party HTTP or serialization libraries.

## Template support (Seq.Syntax)

Apps that support user-customizable output should use the Seq template language via `Seq.Syntax`. This is the same expression language used in Seq's UI.

### Template compilation

Templates are compiled into `ExpressionTemplate` instances in `OnAttached()` or a factory constructor:

```csharp
using Seq.Syntax.Templates;
using Seq.Syntax.Templates.Encoding;

var template = new ExpressionTemplate(
    templateText,
    nameResolver: new OrderedNameResolver(new NameResolver[]
    {
        new StaticMemberNameResolver(typeof(MyBuiltInFunctions)),
        myAppNameResolver
    }),
    encoder: isPlainText ? null : new MyHtmlEncoder());
```

### Template rendering

Render a template against a `LogEvent`:

```csharp
static string Format(ExpressionTemplate template, LogEvent evt)
{
    var writer = new StringWriter();
    template.Format(evt, writer);
    return writer.ToString();
}
```

### Custom encoding

When output is HTML (or another format requiring escaping), implement `TemplateOutputEncoder`. The encoder is applied **only to interpolated values**, not to template literal text:

```csharp
class TemplateOutputTelegramHtmlEncoder : TemplateOutputEncoder
{
    public override string Encode(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
```

### Built-in functions and name resolution

Apps can expose custom functions and properties within templates through a `NameResolver`:

```csharp
public static class MyAppBuiltInFunctions
{
    // Available as UriEncode(value) in templates
    public static LogEventPropertyValue? UriEncode(LogEventPropertyValue? value) { ... }

    // Bound to @Host in templates via the name resolver
    public static LogEventPropertyValue? MyAppHost(Host host) { ... }

    // Bound to @App in templates via the name resolver
    public static LogEventPropertyValue? MyAppInstance(App app) { ... }
}
```

The name resolver maps `@Host` and `@App` to the built-in functions and binds the actual `Host` and `App` instances as function parameters. See `TelegramAppNameResolver` or `MailAppNameResolver` in the reference apps for the implementation pattern.

### Default templates

Provide sensible default templates as embedded resources. Templates should:
- Handle both regular events and alert notifications (`@EventType = 0xA1E77001`)
- Include level indicators appropriate to the output format
- Show timestamp, message, properties, and exceptions
- Link back to the event in Seq using `@Host.BaseUri`

For HTML templates, use only tags supported by the target (e.g., Telegram supports only `<b>`, `<i>`, `<code>`, `<pre>`, `<a>`).

## Error handling

All exceptions thrown from `OnAsync()` are caught per-event by the runtime, logged to the app's diagnostic stream, and recorded against the event. The app continues processing the next event regardless.

| Scenario | Pattern |
|---|---|
| Missing required settings | `ArgumentException` from `OnAttached()` — Seq reports as a configuration error |
| Invalid template syntax | `ArgumentException` from `ExpressionTemplate` constructor — fails at attachment time |
| Transient API errors (rate limits) | Log via `Log.Warning()`, do **not** throw — avoids per-event failure noise |
| Permanent API errors (auth, bad request) | Throw `SeqAppException` with the error details — Seq records against the event |
| Network failures | Let `HttpRequestException` propagate — Seq records against the event |

```csharp
public async Task OnAsync(Event<LogEvent> evt)
{
    var response = await _gateway.SendAsync(...);
    if (!response.Ok)
    {
        if (response.ErrorCode == 429) // Rate limited
        {
            Log.Warning("Rate limit exceeded; retry after {RetryAfter}s", response.RetryAfter);
            return; // Don't throw
        }

        throw new SeqAppException($"API error {response.ErrorCode}: {response.Description}");
    }
}
```

## Testing

### Test infrastructure

**`TestAppHost`** (from `Seq.Apps.Testing`): Provides a minimal `IAppHost` with default `App` (id `"app-1"`, base URI `https://seq.example.com`), `Host`, and `Logger` instances. Used to attach the app in tests.

### Test pattern

Setting properties are set directly on the app instance before calling `Attach()`, since the runtime injects them by reflection in production but tests bypass that:

```csharp
[Fact]
public async Task EventsAreSentWithCorrectParameters()
{
    var gateway = new TestMyGateway();
    var app = new MyApp(gateway);
    app.BotToken = "test-token";
    app.ChatId = "12345";
    app.Attach(new TestAppHost());

    await app.OnAsync(Some.InformationEvent());

    Assert.Single(gateway.Sent);
    Assert.Equal("12345", gateway.Sent[0].ChatId);
}
```

For `ISubscribeToJsonAsync` apps, pass a CLEF JSON string directly:

```csharp
await app.OnAsync("""{"@t":"2024-01-15T10:30:00Z","@mt":"Test event"}""");
```

A `Some` helper class is typically used to create `Event<LogEvent>` test fixtures for `ISubscribeToAsync<LogEvent>` apps.

### Test categories

**App-level tests** exercise the full path from `OnAsync()` through the gateway using test doubles:

- Happy path: event flows end-to-end and the gateway receives the expected payload
- Different log levels produce appropriate output (level indicators, severity mapping)
- Alert events (`EventType = 0xA1E77001`) are handled distinctly from regular events
- Events with exceptions (`@x`) include the exception in output
- Events with missing optional properties degrade gracefully
- Structured property values (sequences, structures, dictionaries) render correctly

**Configuration tests** cover `OnAttached()`:

- Each required setting, when missing or empty, throws `ArgumentException` naming the setting
- Invalid combinations and out-of-range values are rejected
- Optional settings fall back to documented defaults

**Template/rendering tests** (for apps using `Seq.Syntax`):

- Default templates render sensibly for both regular events and alerts
- HTML/encoded output: interpolated values are encoded, literal template text is not
- Special characters in property values don't break the output format
- Built-in functions (`@Host`, `@App`, custom functions) resolve correctly
- Output truncation at size limits preserves valid output

**Error handling tests**:

- Gateway errors throw `SeqAppException` with a diagnostic message
- Transient errors (rate limits) log but do not throw
- Error context is sufficient for diagnosis (status codes, response excerpts)

**For `ISubscribeToJsonAsync` apps**, test with realistic CLEF inputs covering minimal events (`@t` + `@mt`), events with all optional fields (`@l`, `@x`, `@i`, `@tr`, `@sp`, `@st`), and properties containing special characters, nested structures, and arrays.

### Smoke test project

An optional console app for manual end-to-end verification against real services. Reads settings from `SEQ_APP_SETTING_{PROPERTYNAME}` environment variables (uppercase, no underscores in the property name part).

Take care that the smoke test project doesn't exit or assume completion before asynchronous background processes complete, e.g. flushing buffered output to disk or remote APIs.

## Runtime API reference

### Key types from `Seq.Apps` (in `seq-apps-runtime`)

| Type | Purpose |
|---|---|
| `SeqApp` | Abstract base class. Provides `App`, `Host`, `Log` after attachment. |
| `SeqAppAttribute` | Marks the entry point class. `Name` (short UI label), `Description` (sentence), `AllowReprocessing` (default `false`; if `true`, the app will receive events that it produced itself). |
| `SeqAppSettingAttribute` | Marks configurable properties. See "App settings" section. |
| `ISubscribeToAsync<LogEvent>` | For output apps processing Serilog `LogEvent` objects. |
| `ISubscribeToJsonAsync` | For output apps processing raw CLEF JSON strings. |
| `IPublishJson` | For input apps that generate events. `Start(TextWriter)` / `Stop()`. |
| `Event<TData>` | Wrapper with `Id`, `EventType`, `Timestamp`, `Data`. Alert events have `EventType = 0xA1E77001`. |
| `App` | `Id`, `Title`, `Settings` (`IReadOnlyDictionary<string, string>`), `StoragePath`. |
| `Host` | `BaseUri`, `InstanceName` (nullable). |
| `SeqAppException` | Throw for app-specific errors; Seq records against the event. |
| `SettingInputType` | Enum: `Unspecified`, `Text`, `LongText`, `Checkbox`, `Integer`, `Decimal`, `Password`. |

### Important constants

- Alert event type: `0xA1E77001`
- Default time zone: `Etc/UTC`
- Default date/time format: `o` (ISO-8601 round-trip)

## Gotchas

- Seq does not resolve package dependencies when installing apps. Apps must package assembly dependencies into their own NUPKG (see CSPROJ conventions above). 

## References

- [CLEF specification](https://clef-json.org) — the Compact Log Event Format (`@t`, `@mt`, `@m`, `@l`, `@x`, `@i`, `@r`)
- [Posting raw events](https://docs.datalust.co/docs/posting-raw-events) — CLEF reference including Seq trace extensions (`@tr`, `@sp`, `@ps`, `@st`, `@sa`, `@ra`, `@sk`)
- [Template syntax](https://docs.datalust.co/docs/template-syntax) — documentation for the Seq template language used in app settings
- [seq-apps-runtime](https://github.com/datalust/seq-apps-runtime) — source code for the `Seq.Apps` API (`SeqApp`, `ISubscribeToAsync<LogEvent>`, `ISubscribeToJsonAsync`, etc.)
- [seqcli](https://github.com/datalust/seqcli) — source code for the `seqcli app run` command that Seq uses to host apps at runtime
- [seq-app-mail](https://github.com/datalust/seq-app-mail) — canonical output app example (email); also the home of `Seq.Syntax` source code
- [seq-input-healthcheck](https://github.com/datalust/seq-input-healthcheck) — canonical input app example (HTTP health checks)

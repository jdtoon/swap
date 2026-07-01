# Security Checklist (CSRF, SSE/Auth, Headers)

This page is a practical checklist for building Swap.Htmx apps that are safe by default.

It focuses on:

- CSRF / anti-forgery for HTMX POST/PUT/PATCH/DELETE
- Authentication + authorization for SSE/WebSockets
- Room/event scoping to prevent cross-user data leaks
- Common header/CORS pitfalls with long-lived realtime connections
- Payload exposure risks (HX-Trigger + SSE payloads)

---

## 1) CSRF / Anti-forgery (Required for state-changing endpoints)

### Why

HTMX makes it easy to trigger server actions (POST/PUT/DELETE). If your app uses cookie auth, **CSRF is a primary risk**.

**Rule of thumb:**

- If the browser will send auth cookies automatically → you need anti-forgery on state-changing endpoints.
- If you use bearer tokens in an `Authorization` header and do not rely on cookies → CSRF is less relevant (but still consider it if cookies exist).

---

### MVC (recommended baseline)

1) Enable antiforgery validation globally:

```csharp
// Program.cs
builder.Services.AddControllersWithViews(options =>
{
    // Validates anti-forgery tokens for unsafe HTTP methods.
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
```

2) Include the token in forms:

```cshtml
<form hx-post="/tasks/complete" hx-target="#task-list" hx-swap="outerHTML" method="post">
    @Html.AntiForgeryToken()

    <input type="hidden" name="id" value="@Model.TaskId" />
    <button type="submit">Complete</button>
</form>
```

This works because the token is submitted as a form field (`__RequestVerificationToken`).

---

### Minimal APIs

If you opt into antiforgery middleware, `MapPost`/`MapPut` etc can require tokens. If you do not send a token, your endpoint will fail.

Recommended setup:

```csharp
// Program.cs
builder.Services.AddAntiforgery();

var app = builder.Build();
app.UseAntiforgery();
```

Then send a token from the browser.

**Option A (recommended): submit a normal `<form>`** (Razor Pages / MVC view) and include `@Html.AntiForgeryToken()` like above.

**Option B (advanced): send the token in a header**

HTMX supports setting headers; you can send the request token as `RequestVerificationToken`.

Example pattern:

- Render a hidden token input once (inside any form):

```cshtml
<form id="csrf">
    @Html.AntiForgeryToken()
</form>
```

- Attach the header to HTMX requests (one-time script):

```html
<script>
document.body.addEventListener('htmx:configRequest', function (evt) {
  var tokenInput = document.querySelector('#csrf input[name="__RequestVerificationToken"]');
  if (tokenInput) {
    evt.detail.headers['RequestVerificationToken'] = tokenInput.value;
  }
});
</script>
```

---

### Don’ts

- Don’t disable antiforgery on state-changing endpoints in production unless you have a deliberate alternative.
- Don’t put secrets (tokens/PII) into DOM attributes or HX-Trigger payloads “because it’s convenient”.

---

## 2) SSE/WebSockets Authentication & Authorization

### Require auth on realtime endpoints

Treat the SSE/WebSocket endpoint like any other authenticated resource.

Minimal API example:

```csharp
app.MapGet("/swap/sse", (ISseConnectionRegistry registry) =>
    SwapRealtimeResults.Sse(registry))
   .RequireAuthorization();
```

MVC example:

```csharp
[Microsoft.AspNetCore.Authorization.Authorize]
public class DashboardController : SwapRealtimeController
{
    [HttpGet("/dashboard/stream")]
    public IActionResult Stream()
        => ServerSentEvents((conn, ct) => conn.KeepAlive(TimeSpan.FromSeconds(30), ct));
}
```

---

## 3) Rooms & Scoping (Prevent cross-user leakage)

### The risk

SSE rooms and broadcast targeting are a major foot-gun:

- If users can join arbitrary rooms, they can subscribe to data they shouldn’t see.
- If you broadcast based on untrusted room names, you can leak updates across tenants/projects.

### Recommended room rules

- Use **predictable, validated room names**:
  - Good: `project-123`, `tenant-abc`, `user-<id>`
  - Bad: arbitrary user input, raw emails, unbounded strings
- Enforce authorization in `CanJoinRoom`.

Example (from a production-style pattern):

```csharp
app.MapGet("/swap/sse", (ISseConnectionRegistry registry, HttpContext ctx) =>
{
    return SwapRealtimeResults.Sse(registry, options =>
    {
        options.CanJoinRoom = (connection, roomName) =>
        {
            // Validate room naming
            if (string.IsNullOrWhiteSpace(roomName)) return Task.FromResult(false);
            if (roomName.Length > 64) return Task.FromResult(false);

            // Example policy: only allow joining "project-{id}" rooms you’re authorized for
            if (roomName.StartsWith("project-", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: check user claims / DB authorization for that project id
                return Task.FromResult(connection.User.Identity?.IsAuthenticated == true);
            }

            return Task.FromResult(false);
        };
    });
}).RequireAuthorization();
```

### Safer broadcast patterns

- Prefer broadcasting to **rooms derived from server-side data** (not client query strings).
- Prefer sending **IDs** and letting the client fetch details via an authorized endpoint when data is sensitive.

---

## 4) OOB Target ID & Redirect URL Validation (Built-in since v1.2.0)

### OOB Target IDs

All `AlsoUpdate()`, `AlsoUpdateIfExists()`, `AlsoUpdateIf()`, and `AlsoUpdateMany()` calls validate target element IDs at build time. IDs must:

- Start with a letter (`a-z` or `A-Z`)
- Contain only letters, digits, hyphens (`-`), and underscores (`_`)
- Not be empty or whitespace-only

A leading `#` is automatically stripped. Invalid IDs throw `ArgumentException`, preventing injection via crafted target IDs (e.g. `<script>alert(1)</script>`).

### Redirect/Navigation URLs

`WithRedirect()` and `WithNavigation()` validate URLs against an **allowlist** (since v1.4.0 — previously a scheme blocklist). Only these are accepted:

- Absolute `http` / `https` URLs
- Same-origin **relative** references (e.g. `/dashboard`, `products/42`)

Everything else is rejected with `ArgumentException`, including:

- Protocol-relative URLs (`//evil.com`, `/\evil.com`)
- Non-http(s) schemes (`javascript:`, `data:`, `vbscript:`, `file:`, `mailto:`, …)

This prevents open-redirect and XSS attacks via the `HX-Redirect` and `HX-Location` response headers. The same validation now also runs for the `WithNavigation(HxLocationOptions)` overload, which previously bypassed all checks.

## 4) Payload Exposure (HX-Trigger + SSE payloads)

### What gets exposed

- `HX-Trigger` events (and their payloads) are visible to the browser.
- SSE/WebSocket HTML payloads are delivered to anyone who is subscribed to that event/room.

### Do

- Keep trigger payloads small and non-sensitive.
- Treat broadcast HTML as public to the broadcast audience.
- Use server-side rendering helpers and keep authorization checks on the server.

### Don’t

- Don’t include secrets/PII in payloads.
- Don’t broadcast user-specific HTML to a shared room.

---

## 5) CORS, Cookies, and Realtime

Best default: keep everything **same-origin**.

If you must do cross-origin SSE:

- Configure CORS carefully.
- If using cookies, you must allow credentials, and you must not use wildcard origins.

Example:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", p => p
        .WithOrigins("https://app.example.com")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

app.UseCors("AppCors");
```

For SSE endpoints specifically, ensure the browser is allowed to establish the long-lived connection.

---

## 6) Security Headers (Recommended baseline)

At minimum (baseline guidance):

- `Strict-Transport-Security` (HSTS) when using HTTPS
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy`
- `Content-Security-Policy` (tailor to your app)

Example (simple middleware):

```csharp
app.Use((ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";

    // CSP must be customized for your scripts/styles.
    // Start strict and loosen intentionally.
    // ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self';";

    return next();
});
```

---

## 7) Protected `SwapState` (Tamper-Proof, Fail-Closed)

For business-critical state (prices, IDs, tenant scope), mark the state `Protected` so values are encrypted in the browser via ASP.NET Core Data Protection:

```csharp
public class PaymentState : SwapState
{
    public override bool Protected => true;
    public decimal Price { get; set; }     // encrypted
    public Guid TenantId { get; set; }      // encrypted
}
```

- **A data-protection provider is required.** `AddSwapHtmx()` registers `IDataProtectionProvider`. Rendering protected state with no provider registered now **throws** rather than leaking values as plaintext hidden fields.
- **Tamper protection fails closed (since v1.4.0).** A protected value that is present-but-empty or fails to decrypt is a hard model-binding failure: the binder records a `ModelState` error and sets `SwapState.Tampered = true`. It no longer silently falls back to the type default (`0`, `Guid.Empty`). Always check before trusting protected state:

```csharp
public IActionResult Checkout([FromSwapState] PaymentState state)
{
    if (!ModelState.IsValid || state.Tampered)
        return SwapResponse().WithErrorToast("Invalid request.").Build();
    // state is trustworthy here
}
```

- Protection is scoped to the state container **and** property name, so an attacker cannot copy an encrypted value from one field (`OrderId`) to another (`Price`).

---

## 8) Error Boundary Output

If you enable `SwapErrorBoundaries` (see [SwapErrorBoundaries](SwapErrorBoundaries.md)), the built-in fallback response is safe by default (since v1.4.0):

- **All interpolated values are HTML-encoded**, so a crafted exception message cannot inject markup or script into the page (previously a reflected-XSS risk).
- **The raw exception message is not echoed by default.** The client sees a generic message plus a request **correlation id**; full exception details are logged **server-side** via `ILogger`.
- Enable `ErrorHandling.ShowExceptionDetails` only in Development, never in production.

---

## 9) Quick Checklist

- CSRF: Enable antiforgery and include tokens in HTMX forms.
- Auth: Require authorization on SSE/WebSocket endpoints.
- Rooms: Validate room names and authorize room joins (`CanJoinRoom`).
- Payloads: Don’t put secrets/PII into HX-Trigger or broadcast HTML.
- State: For sensitive state use `Protected` SwapState and check `ModelState.IsValid`/`SwapState.Tampered` (fails closed).
- Redirects: Only `http`/`https` or same-origin relative URLs pass `WithRedirect()`/`WithNavigation()` (allowlist).
- Errors: Keep `ShowExceptionDetails` off in production; the fallback encodes output and shows a correlation id.
- CORS: Prefer same-origin; if cross-origin, use explicit origins + credentials.
- Headers: Add baseline security headers; customize CSP.

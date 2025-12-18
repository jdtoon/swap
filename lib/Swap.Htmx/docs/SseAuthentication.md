# SSE Authentication & Authorization

SSE endpoints are long-lived `GET` requests. In practice, **authentication works well with cookies** (same-origin), while **authorization is on you** (don’t let clients subscribe to rooms they shouldn’t).

This doc focuses on:

- Securing the SSE endpoint itself (`[Authorize]`)
- Safely assigning rooms (server-derived, not client-chosen)
- A minimal, copy-paste controller example

> Also see [Security Checklist](SecurityChecklist.md) for broader CSRF/XSS guidance.

---

## Recommended approach

### 1) Authenticate the SSE endpoint

Use your normal ASP.NET Core authentication, then protect the SSE action:

- Cookie auth is simplest because browsers automatically send cookies for same-origin requests.
- EventSource cannot set arbitrary request headers, so **Bearer tokens via `Authorization` header aren’t generally available** for plain SSE.

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.Realtime;

[Authorize]
public sealed class NotificationsController : SwapRealtimeController
{
    [HttpGet("/notifications/stream")]
    public IActionResult Stream()
    {
        return ServerSentEvents(async (conn, ct) =>
        {
            // Hard fail if not authenticated.
            conn.WithAuthentication();

            // Join a user-scoped room derived from the authenticated principal.
            // This prevents "subscribe to user-XYZ" attacks.
            conn.WithUserRoom();

            // Subscribe to only the events this stream should receive.
            conn.WithEvents(
                "notification",
                "toast");

            await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
        });
    }
}
```

---

## Room authorization: don’t trust the client

Rooms are powerful. Treat room names like access control boundaries.

Prefer:

- Server-derived rooms like `user-{userId}` or `role-{roleName}`
- Rooms created after a server-side authorization check (e.g., confirm the user belongs to a project)

Avoid:

- Allowing the client to specify arbitrary rooms via query string or headers
- Joining rooms based on unvalidated route parameters

Example: validate membership before joining a project room:

```csharp
[Authorize]
[HttpGet("/projects/{projectId:int}/stream")]
public IActionResult ProjectStream(int projectId)
{
    return ServerSentEvents(async (conn, ct) =>
    {
        conn.WithAuthentication();

        // Example: enforce membership before joining the room.
        // var userId = conn.User!.FindFirst("sub")?.Value ?? conn.User!.Identity!.Name;
        // if (!await _projects.IsMemberAsync(projectId, userId)) throw new UnauthorizedAccessException();

        conn.JoinRoom($"project-{projectId}");
        conn.WithEvents("project-updated");

        await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
    });
}
```

---

## User ID resolution

When broadcasting to a user, `Swap.Htmx.Realtime` resolves the connection’s user id in this order:

1. `sub` claim
2. `id` claim
3. `User.Identity.Name`

If you control the identity system, it’s best to ensure `sub` is present and stable.

---

## Common pitfalls

- **Cross-origin SSE:** cookies won’t be sent unless CORS and cookie policy allow it; avoid cross-origin SSE if you can.
- **Leaking data via broad events:** keep event names scoped and use rooms for multi-tenant apps.
- **Long-lived auth:** if you rotate permissions while a stream is open, clients may still receive updates until reconnect; design accordingly.

---

## See also

- [Server-Sent Events](ServerSentEvents.md)
- [SSE Backpressure](SseBackpressure.md)
- [Security Checklist](SecurityChecklist.md)

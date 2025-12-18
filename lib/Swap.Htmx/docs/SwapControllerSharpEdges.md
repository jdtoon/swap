# SwapController: Convenience vs Sharp Edges

`SwapController` is a convenience base class that bundles common HTMX patterns (view selection, coordinated responses, event helpers). It’s useful, but a few helpers are **sharp** and should be used intentionally.

If you prefer composition, you can use the equivalent extension methods (`this.SwapResponse()`, `this.SwapEvent(...)`, `SwapResults.*`) on standard controllers / minimal APIs.

## Safe defaults

### `SwapView(...)`

- Chooses `View(...)` vs `PartialView(...)` depending on `HX-Request`.
- Adds `Vary: HX-Request` semantics for cache correctness.

### `SwapResponse()`

- Recommended for multi-part responses (main view + OOB swaps + toasts + triggers).
- Avoids repeating response/header wiring across actions.

### `GetOrInitializeSessionId()`

- Safer than using `HttpContext.Session.Id` directly.
- Ensures the session cookie is persisted on first use.

## Sharp edges

### `SwapRedirectToAction(...)` (obsolete)

`SwapRedirectToAction` is **not a real redirect**. It is a server-side forward helper that uses reflection to invoke another action method.

Because it calls your action method directly, it can diverge from normal MVC behavior:

- Bypasses MVC routing and URL generation.
- Bypasses model binding, validation, filters, and action selection.
- Can be ambiguous or surprising with overloaded action methods.
- Can behave differently than a true browser navigation.

For these reasons it is marked obsolete and should generally be avoided.

#### Safer alternatives

- If you want the client to navigate: use `SwapRedirect(...)` / `.WithNavigation()`.
- If you want to reuse view setup: call a shared method that builds the model/view state and return the view directly.
- If you want the PRG (POST-Redirect-GET) pattern: return a standard `RedirectToAction(...)`.

## Guidance

- Prefer **composition** (extension methods) for shared libraries/framework code.
- Use `SwapController` for apps where a base controller is acceptable.
- Treat any reflection-based helper as a last resort; prefer explicit code paths.

## See also

- [Navigation](Navigation.md)
- [Events Guide](Events.md)

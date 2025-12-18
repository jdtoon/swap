# Client Assets (HTMX + Extensions)

Swap.Htmx is server-side, but it still relies on a few client assets:

- HTMX
- Optional HTMX extensions (SSE, WebSockets)
- Swap client script (bundled via `_content/Swap.Htmx/...`)

Swap supports **two official modes** for HTMX assets:

## Mode A: CDN (quick start)

Best for: prototypes, quick experiments, docs snippets.

Add to your layout (before `</head>`):

```html
<link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />

<script src="https://unpkg.com/htmx.org@2.0.8"></script>
<script src="~/_content/Swap.Htmx/js/swap.client.js"></script>
```

If you use SSE:

```html
<script src="https://unpkg.com/htmx-ext-sse@2.2.4/sse.js"></script>
```

If you use WebSockets:

```html
<script src="https://unpkg.com/htmx-ext-ws@2.0.4/ws.js"></script>
```

## Mode B: LibMan (production-ish)

Best for: repeatable builds, offline/dev-box friendly, more control.

1) Add `libman.json`:

```json
{
  "version": "1.0",
  "defaultProvider": "unpkg",
  "libraries": [
    {
      "library": "htmx.org@2.0.8",
      "destination": "wwwroot/lib/htmx",
      "files": ["dist/htmx.min.js"]
    },
    {
      "library": "htmx-ext-sse@2.2.4",
      "destination": "wwwroot/lib/htmx/ext",
      "files": ["sse.js"]
    }
  ]
}
```

2) Restore client libraries:

```bash
libman restore
```

3) Reference local files from your layout:

```html
<link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />

<script src="~/lib/htmx/dist/htmx.min.js" asp-append-version="true"></script>
<script src="~/lib/htmx/ext/sse.js" asp-append-version="true"></script>
<script src="~/_content/Swap.Htmx/js/swap.client.js" asp-append-version="true"></script>
```

Notes:

- Templates in this repo use LibMan by default.
- Destination folders can vary by project (some demos use `wwwroot/lib/htmx-ext-sse/sse.js`). That’s fine; just keep your `<script src=...>` consistent with your `libman.json`.

## Recommendation

- Use **CDN mode** for docs and fast starts.
- Use **LibMan mode** for templates and longer-lived apps.

## See also

- [Getting Started](GettingStarted.md)
- [Server-Sent Events](ServerSentEvents.md)
- [WebSockets](WebSockets.md)

using Swap.Htmx;
using SwapMinimal.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx();

app.MapGet("/", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>SwapMinimal</title>
    <script src=""https://unpkg.com/htmx.org@1.9.10""></script>
</head>
<body>
    <h1>SwapMinimal Demo</h1>
    <div id=""message-container"">
        <button hx-get=""/message"" hx-target=""#message-container"" hx-swap=""outerHTML"">
            Click me
        </button>
    </div>
</body>
</html>", "text/html"));

app.MapGet("/message", () => 
    SwapResults.Response()
        .WithView("_Message", new MessageModel("Hello from Minimal API + Swap.Htmx!"))
        .WithSuccessToast("Message loaded!")
);

app.Run();

public partial class Program { }

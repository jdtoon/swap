using Microsoft.AspNetCore.Mvc;

namespace SwapLab.Controllers;

// Live demo of the 1.7.0 client-orchestration features: fingerprint diff-skip (data-swap-hash) and
// safe optimistic UI (data-swap-optimistic), exercised end-to-end through real server round-trips,
// real htmx, and the real swap.client.js. Doubles as the browser-verification surface for those guards.
public class DogfoodController : Swap.Htmx.SwapController
{
    public IActionResult Index() => View();

    [HttpPost]
    public IActionResult Bump()
        // Identical content every call -> identical data-swap-hash -> the client skips the 2nd+ OOB swap.
        => SwapResponse()
            .AlsoUpdate("hashpanel", "_HashPanel", new HashModel("stable content"), fingerprint: true)
            .Build();

    [HttpPost]
    public IActionResult FailOptimistic() => StatusCode(400);
}

public record HashModel(string Text);

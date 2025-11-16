using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Swap.Htmx.E2ETests;

/// <summary>
/// Global fixture that boots the TestApp once for the Playwright E2E suite.
/// </summary>
[SetUpFixture]
public class TestAppServerFixture
{
    private Process? _process;
    private static readonly HttpClient Http = new();

    [OneTimeSetUp]
    public async Task StartServer()
    {
        var psi = new ProcessStartInfo("dotnet", "run --project ..\\Swap.Htmx.TestApp\\src\\Swap.Htmx.TestApp.csproj")
        {
            WorkingDirectory = TestContext.CurrentContext.WorkDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.EnvironmentVariables["ASPNETCORE_URLS"] = "http://localhost:5000";

        _process = Process.Start(psi);
        Assert.That(_process, Is.Not.Null, "Failed to start TestApp process");

        // Wait until /test responds (retry for ~20s)
        var attempts = 0;
        while (attempts < 40)
        {
            try
            {
                var resp = await Http.GetAsync("http://localhost:5000/test");
                if (resp.IsSuccessStatusCode) return;
            }
            catch { /* ignore until ready */ }
            await Task.Delay(500);
            attempts++;
        }

        Assert.Fail("TestApp did not start listening on port 5000 in time.");
    }

    [OneTimeTearDown]
    public void StopServer()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
            _process?.Dispose();
        }
        catch { /* best-effort */ }
    }
}
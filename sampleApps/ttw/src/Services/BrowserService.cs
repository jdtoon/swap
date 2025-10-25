using PuppeteerSharp;

namespace ttw.Services
{
    public class BrowserService : IDisposable
    {
        private static IBrowser? _browser;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public async Task<IBrowser> GetBrowserAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_browser == null || !_browser.IsConnected)
                {
                    // Download Chromium if not already installed
                    await new BrowserFetcher().DownloadAsync(BrowserTag.Stable);

                    // Launch the browser
                    _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                    {
                        Headless = true,
                        Args = new[] 
                        { 
                            "--no-sandbox", 
                            "--disable-dev-shm-usage",
                            "--disable-setuid-sandbox"
                        }
                    });
                }
                return _browser;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _browser?.CloseAsync().GetAwaiter().GetResult();
                    _browser?.Dispose();
                }
                _disposed = true;
            }
        }

        ~BrowserService()
        {
            Dispose(false);
        }
    }
}

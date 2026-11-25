using MainCore.Common.Models;
using Microsoft.Playwright;

namespace MainCore.Services
{
    public class ChromeBrowser : IChromeBrowser
    {
        private IPlaywright _playwright;
        private IBrowserContext _context;
        private IPage _page;

        private readonly string[] _extensionsPath;

        public ChromeBrowser(string[] extensionsPath)
        {
            _extensionsPath = extensionsPath;
        }

        public async Task Setup(ChromeSetting setting)
        {
            _playwright = await Playwright.CreateAsync();

            var args = new List<string>
            {
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--start-maximized",
                "--disable-extensions-except=C:\\travian\\tampermonkey_ext",
                "--load-extension=C:\\travian\\tampermonkey_ext"
            };

            if (_extensionsPath is not null && _extensionsPath.Length > 0)
            {
                var extPaths = string.Join(",", _extensionsPath);
                args.Add($"--disable-extensions-except={extPaths}");
                args.Add($"--load-extension={extPaths}");
            }

            var options = new BrowserTypeLaunchPersistentContextOptions
            {
                ExecutablePath = setting.ChromePath,
                Headless = false, // Luôn mở giao diện trình duyệt để nạp Extension và tránh lỗi Timeout nút bấm
                Args = args,
                ViewportSize = ViewportSize.NoViewport, // Giữ nguyên kích thước toàn màn hình
                IgnoreDefaultArgs = new[] { "--disable-extensions", "--enable-automation" }
            };

            if (!string.IsNullOrEmpty(setting.ProxyHost))
            {
                options.Proxy = new Proxy
                {
                    Server = $"{setting.ProxyHost}:{setting.ProxyPort}",
                    Username = setting.ProxyUsername,
                    Password = setting.ProxyPassword
                };
            }

            _context = await _playwright.Chromium.LaunchPersistentContextAsync(setting.UserDataDir, options);
            _page = _context.Pages.FirstOrDefault() ?? await _context.NewPageAsync();
            _page.SetDefaultTimeout(30000);
        }

        public IPage Page => _page;

        public async Task Shutdown()
        {
            if (_context is not null)
            {
                await _context.CloseAsync();
                _context = null;
            }
            if (_playwright is not null)
            {
                _playwright.Dispose();
                _playwright = null;
            }
        }

        public async Task Navigate(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            await _page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
        }

        public async Task Click(ILocator locator, string expression = "")
        {
            await locator.ScrollIntoViewIfNeededAsync();
            await locator.ClickAsync();
        }

        public async Task Input(ILocator locator, string content)
        {
            await locator.FillAsync(content);
        }
    }
}

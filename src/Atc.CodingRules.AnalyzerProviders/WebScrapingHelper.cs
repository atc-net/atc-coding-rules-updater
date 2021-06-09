using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Atc.CodingRules.AnalyzerProviders
{
    /// <summary>
    /// WebScrapingHelper.
    /// </summary>
    /// <remarks>
    /// Requires a ChromeDriver: https://chromedriver.chromium.org/downloads .
    /// </remarks>
    public static class WebScrapingHelper
    {
        private const string Chrome32BitPath = @"C:\Program Files (x86)\Google\Chrome\Application";
        private const string Chrome64BitPath = @"C:\Program Files\Google\Chrome\Application";
        private static string chrome32BitExecutablePath = @$"{Chrome32BitPath}\chrome.exe";
        private static string chrome64BitExecutablePath = @$"{Chrome64BitPath}\chrome.exe";
        private static string chromeDriver32BitExecutablePath = @$"{Chrome32BitPath}\chromedriver.exe";
        private static string chromeDriver64BitExecutablePath = @$"{Chrome64BitPath}\chromedriver.exe";
        private static IWebDriver? webDriver;

        public static HtmlDocument? GetPage(Uri uri, bool useChromeEngine = false)
        {
            HtmlDocument document;

            try
            {
                if (useChromeEngine)
                {
                    webDriver ??= InitWebDriver();
                    webDriver.Navigate().GoToUrl(uri);

                    var htmlSource = webDriver.PageSource;
                    if (htmlSource is null)
                    {
                        return null;
                    }

                    document = new HtmlDocument();
                    document.LoadHtml(htmlSource);
                }
                else
                {
                    document = new HtmlWeb().Load(uri);
                    if (document is null)
                    {
                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }

            return document;
        }

        public static void QuitWebDriver()
        {
            webDriver?.Quit();
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "OK.")]
        private static IWebDriver InitWebDriver()
        {
            if (webDriver is not null)
            {
                return webDriver;
            }

            var options = new ChromeOptions();
            options.AddArguments("--disable-extensions");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--headless");
            options.AddArgument("--log-level=3");
            options.AddArgument("--user-agent=Mozilla/5.0 (iPhone; CPU iPhone OS 7_0 like MacOS X; en-us) AppleWebKit / 537.51.1(KHTML, like Gecko) Version / 7.0 Mobile / 11A465 Safari/ 9537.53");

            ChromeDriverService? service = null;

            if (File.Exists(chrome64BitExecutablePath))
            {
                options.BinaryLocation = chrome64BitExecutablePath;
                service = ChromeDriverService.CreateDefaultService(Chrome64BitPath);

                if (!File.Exists(chromeDriver64BitExecutablePath))
                {
                    throw new IOException("ChromeDriver is not present on the system.");
                }
            }
            else
            {
                options.BinaryLocation = chrome32BitExecutablePath;
                service = ChromeDriverService.CreateDefaultService(Chrome32BitPath);

                if (!File.Exists(chromeDriver32BitExecutablePath))
                {
                    throw new IOException("ChromeDriver is not present on the system.");
                }
            }

            service.SuppressInitialDiagnosticInformation = true;

            webDriver = new ChromeDriver(service, options);
            return webDriver;
        }
    }
}
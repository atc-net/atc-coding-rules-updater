using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Atc.CodingRules.AnalyzerProviders
{
    public static class WebScrapingHelper
    {
        private const int SleepTimeDownloadHtml = 100;
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

                Thread.Sleep(SleepTimeDownloadHtml);
            }
            catch
            {
                return null;
            }

            return document;
        }

        public static void CloseWebDriver()
        {
            webDriver?.Close();
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
            options.BinaryLocation = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

            var service = ChromeDriverService.CreateDefaultService(@"C:\Program Files (x86)\Google\Chrome\Application");
            service.SuppressInitialDiagnosticInformation = true;

            webDriver = new ChromeDriver(service, options);
            return webDriver;
        }
    }
}
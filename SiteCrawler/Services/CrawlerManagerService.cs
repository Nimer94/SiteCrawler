using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SiteCrawler.Services
{
    public class CrawlerManagerService
    {
        private readonly string _url;
        private TimeSpan waitingTime => TimeSpan.FromSeconds(0.5);
        private readonly ChromeDriver driver;

        public CrawlerManagerService(string url)
        {
            _url = url;
            this.driver = new ChromeDriver(Environment.CurrentDirectory, new ChromeOptions());
        }

        public async Task Run() => await this.CheckBackupFiles(this._url);

        public async Task CheckBackupFiles(string _url)
        {
            var mainurl = _url;
            var dr = mainurl
                .Replace("/", "")
                .Replace(":", "");

            string drpath = Path.Combine(Environment.CurrentDirectory, dr);

            if (!Directory.Exists(drpath))
            {
                _ = Directory.CreateDirectory(drpath);
            }

            var scannedLinks = new List<string>();
            var visitedLinks = new List<string>();

            var linkslength = scannedLinks.Count;

            scannedLinks.Add(mainurl);

            var myUri = new Uri(mainurl);

            var domain = myUri.Host;

            while (scannedLinks.Count > linkslength)
            {
                linkslength = scannedLinks.Count();
                var list = ScanLinks(domain, scannedLinks, visitedLinks);
                scannedLinks.AddRange(list.Distinct().Except(scannedLinks));
            }

            await DownloadPages(scannedLinks, drpath);
        }

        /// <summary>
        /// Download visited pages to FileSystem.
        /// </summary>
        /// <param name="scannedLinks">List of scanned link(s).</param>
        /// <param name="directoryPath">Dicrectory path.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        private async Task DownloadPages(List<string> scannedLinks, string directoryPath)
        {
            int i = 1;

            foreach (var li in scannedLinks)
            {
                driver.Navigate().GoToUrl(li);

                Thread.Sleep(waitingTime);

                //Thread.Sleep(waitingTime);

                var js = (IJavaScriptExecutor)driver;

                var html1 = driver.PageSource;

                Thread.Sleep(waitingTime);

                string fileName1 = Path.Combine(directoryPath, i + ".html1");

                File.WriteAllText(fileName1, html1.ToString());

                Thread.Sleep(waitingTime);

                var Images = driver.FindElements(By.TagName("img"));

                WebClient downloader = new();

                foreach (var img in Images)
                {
                    var imageUrl = img.GetAttribute("src");
                    var imageName = img.GetAttribute("alt");

                    bool result = Uri.TryCreate(imageUrl, UriKind.Absolute, out var uriResult)
                        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                    if (result && uriResult != null)
                    {
                        downloader.DownloadFile(imageUrl, directoryPath + "/" + imageName + ".jpg");
                    }
                }

                i++;
            }
            driver.Close();
        }

        /// <summary>
        /// Visit given list of scanned links.
        /// </summary>
        /// <param name="domain">The domain name of start page.</param>
        /// <param name="scannedLinks">List of scanned link(s).</param>
        /// <param name="visitedLinks">List of visited link(s).</param>
        /// <returns>List of visited links.</returns>
        private List<string> ScanLinks(string domain,
            List<string> scannedLinks,
            List<string> visitedLinks)
        {
            List<string> list = new();

            foreach (var li in scannedLinks)
            {
                if (li.Contains("mail"))
                {
                    continue;
                }

                if (!visitedLinks.Contains(li, StringComparer.OrdinalIgnoreCase))
                {
                    visitedLinks.Add(li.ToLower());

                    driver
                        .Navigate()
                        .GoToUrl(li);

                    Thread.Sleep(waitingTime);

                    var elems = driver
                        .FindElements(By.XPath("//a[@href]"))
                        .Where(c => c.GetAttribute("href").Contains(domain));

                    Thread.Sleep(waitingTime);

                    foreach (var element in elems)
                    {
                        var link = element.GetAttribute("href");

                        if (!link.EndsWith('/'))
                        {
                            link += "/";
                        }

                        if (link.Contains("mail"))
                        {
                            continue;
                        }

                        if (visitedLinks.Contains(link, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        list.Add(link.ToLower());
                    }
                }
            }

            return list;
        }
    }
}


using BilibiliCommentSpider;
using LibCsv;
using Microsoft.Playwright;
using Spectre.Console;
using System.Reflection;

var version = Assembly.GetExecutingAssembly().GetName().Version;
Console.WriteLine($"Bilibili comment spider   V{version}");

using var playwright = await Playwright.CreateAsync();
using var csvWriter = new CsvWriter<BiliReply>("result.csv");

List<(string BrowserType, string ExecutablePath)> installedBrowsers = BrowserUtils.GetInstalledBrowsers()
    .Select(executablePath => (BrowserUtils.GetBrowserTypeFromExecutable(executablePath), executablePath))
    .Where(browser => browser.Item1 != null && browser.executablePath != null)
    .Select(browser => (browser.Item1!, browser.executablePath!))
    .ToList();

string? browserTypeStr = null;
string? browserExecutablePath = null;
if (installedBrowsers.Any())
{
    if (AnsiConsole.Confirm("程序在你的电脑上检测到了可用的浏览器, 你要直接使用它们吗?"))
    {
        var choseBrowser = AnsiConsole.Prompt(
            new SelectionPrompt<(string, string)>()
                .Title("选择要使用的浏览器")
                .PageSize(5)
                .AddChoices(installedBrowsers));

        (browserTypeStr, browserExecutablePath) = choseBrowser;
    }
}

if (browserTypeStr is null)
{
    var _browserType = AnsiConsole.Prompt(
        new SelectionPrompt<IBrowserType>()
            .Title("输入要下载并使用的浏览器类型")
            .UseConverter(type => type.Name)
            .AddChoices(new IBrowserType[] {
                    playwright.Chromium,
                    playwright.Firefox,
                    playwright.Webkit
            }));

    Microsoft.Playwright.Program.Main(
        new string[] { "install", _browserType.Name });

    browserTypeStr = _browserType.Name;
}

IBrowserType? browserType = null;

if (browserTypeStr.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
    browserTypeStr.Contains("Chromium", StringComparison.OrdinalIgnoreCase))
    browserType = playwright.Chromium;
else if (browserTypeStr.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
    browserType = playwright.Firefox;

if (browserType is null)
    browserType = AnsiConsole.Prompt(
        new SelectionPrompt<IBrowserType>()
            .Title("无法识别浏览器类型, 请手动选择")
            .AddChoices(
                new IBrowserType[] 
                {
                    playwright.Chromium,
                    playwright.Firefox,
                    playwright.Webkit
                }));

var browser = await browserType.LaunchAsync(
    new()
    {
        Headless = false,
        ExecutablePath = browserExecutablePath
    });

var page = await browser.NewPageAsync();

await page.GotoAsync("https://www.bilibili.com");

AnsiConsole.WriteLine("请在新打开中的浏览器中登录, 并按任意键继续");

while (true)
{
    Console.ReadKey(true);

    var cookies = await page.Context.CookiesAsync(new string[] { "https://www.bilibili.com" });
    if (cookies.Any(cookie => string.Equals(cookie.Name, "DedeUserID", StringComparison.OrdinalIgnoreCase)))
        break;

#if DEBUG
    break;
#endif

    Console.WriteLine("没有检测到登录信息, 请重新登录");
}


string videoListFileName = "VideoList.txt";
IList<string> videoList;

if (File.Exists(videoListFileName))
{
    videoList = File.ReadAllLines(videoListFileName)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToList();
}
else
{
    List<string> inputVideoList = new();
    AnsiConsole.WriteLine("输入视频 AV/BV 号, 每行一个, 如果没有更多则输入空并按 Enter");
    while (true)
    {
        Console.Write("> ");
        string? line = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(line))
            break;

        inputVideoList.Add(line);
    }

    videoList = inputVideoList;
}

int maxResultCount = AnsiConsole.Ask<int>("输入爬取结果的最大数量, 如果无限制请填 0");

var videoRootUri = new Uri("https://www.bilibili.com/video/");
foreach (var video in videoList)
{
    var link = new Uri(videoRootUri, video);
    _ = page.GotoAsync(link.ToString());

    Console.WriteLine($"正在抓取: {video}");

    var appNode = await page.WaitForSelectorAsync("#app");
    await page.WaitForSelectorAsync("#playerWrap");

    if (appNode is null)
    {
        Console.WriteLine("页面存在错误, 已跳过");
        continue;
    }

    await page.WaitForSelectorAsync(".reply-item");

    int replyIndex = 0;
    IReadOnlyList<IElementHandle>? replies = null;

    while (true)
    {
        var _replies = await page.QuerySelectorAllAsync(".reply-item");
        var replyEndNode = await page.QuerySelectorAsync(".reply-end");

        var playerNode = await page.QuerySelectorAsync("#playerWrap");
        if (playerNode is not null)
            await playerNode.EvaluateAsync("node => node.remove()", playerNode);

        bool needMore = replyEndNode is null &&
            maxResultCount != 0 &&
            _replies.Count < maxResultCount;
        bool hasNewResults = replies is null || _replies.Count > replies.Count;

        replies = _replies;
        if (!needMore)
            break;

        if (hasNewResults)
            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");

        while (replyIndex < replies.Count)
        {
            var replyNode = replies[replyIndex];
            var nameNode = await replyNode.QuerySelectorAsync(".user-name");
            var contentNode = await replyNode.QuerySelectorAsync(".reply-content");

            string name;
            string userId;
            string content;

            if (nameNode is not null)
            {
                name = await nameNode.InnerTextAsync();
                if (await nameNode.GetAttributeAsync("data-user-id") is string _userId)
                    userId = _userId;
                else
                    userId = string.Empty;
            }
            else
            {
                name = string.Empty;
                userId = string.Empty;
            }

            if (contentNode is not null)
            {
                content = await contentNode.InnerTextAsync();
            }
            else
            {
                content = string.Empty;
            }

            Console.WriteLine($"{name}(id:{userId}): {content}");

            var reply = new BiliReply()
            {
                VideoId = video,
                UserName = name,
                UserId = userId,
                Content = content
            };

            csvWriter.Write(reply);

            replyIndex++;
        }
    }
}

await Console.In.ReadLineAsync();

//string GetBilibiliReplyText(IElementHandle replyContentNode)
//{
//    string js =
//        """

//        """;
//}
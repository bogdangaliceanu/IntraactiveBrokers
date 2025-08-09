using IntraactiveBrokers.BtCapitalPartners;
using Microsoft.Playwright;

AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    Console.WriteLine($"Logging global unhandled exception {args.ExceptionObject as Exception}");
};

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false
});
await using var context = await browser.NewContextAsync();

var isMarketClosed = () => DateTime.Now.Hour is < 10 or >= 18;
if (isMarketClosed())
{
    Console.WriteLine("Market is closed so not doing anything");
    return;
}

var btTrade = new BtTrade(context, isMarketClosed);
await btTrade.Run();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

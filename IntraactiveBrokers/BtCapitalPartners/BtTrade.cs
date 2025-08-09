using System.Collections.Immutable;
using Microsoft.Playwright;

namespace IntraactiveBrokers.BtCapitalPartners;

public sealed class BtTrade
{
    private readonly IBrowserContext browser;
    private readonly ImmutableArray<IBtTradeAgent> agents;

    public const string BaseUrl = "https://evo.bt-trade.ro";
    
    public BtTrade(IBrowserContext browser, Func<bool> isMarketClosed)
    {
        this.browser = browser;
        
        this.agents =
        [
            new BuyTheDipAgent(browser, isMarketClosed, new Instrument("ANS26E", "15294", Currency.EUR),
                new BuyTheDipAgent.Parameters(2, 99, 93))
        ];
    }

    private async Task Login()
    {
        var page = await browser.NewPageAsync();

        var response = await page.GotoAsync($"{BaseUrl}/auth/login", new()
        {
            Timeout = 5_000,
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.PauseAsync();
    }

    public async Task Run()
    {
        await Login();
        await Task.Delay(Random.Shared.Next(500, 1000));
        
        foreach (var agent in agents)
        {
            _ = Task.Run(agent.Run);
            await Task.Delay(Random.Shared.Next(5000, 10000));
        }
    }
}
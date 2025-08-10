using Microsoft.Playwright;

namespace IntraactiveBrokers.BtCapitalPartners;

public class BuyTheDipAgent : IBtTradeAgent
{
    private readonly IBrowserContext browser;
    private readonly Func<bool> isMarketClosed;
    private readonly Instrument instrument;
    private readonly Parameters parameters;
    private readonly PageGotoOptions pageGotoOptions = new()
    {
        Timeout = 5_000,
        WaitUntil = WaitUntilState.DOMContentLoaded
    };

    public BuyTheDipAgent(IBrowserContext browser, Func<bool> isMarketClosed, Instrument instrument, Parameters parameters)
    {
        this.browser = browser;
        this.isMarketClosed = isMarketClosed;
        this.instrument = instrument;
        this.parameters = parameters;
    }

    public async Task Run()
    {
        var page = await browser.NewPageAsync();
        
        while (true)
        {
            if (isMarketClosed())
            {
                Console.WriteLine("Market closed");
                break;
            }
            
            try
            {
                var (success, shouldTryAgain) = await TryBuy(page);
                
                if (success)
                {
                    Console.WriteLine();
                    Console.WriteLine($"{nameof(BuyTheDipAgent)} - Success for {instrument.Symbol}");
                    Console.WriteLine();
                    break;
                }

                if (!shouldTryAgain)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine($"{nameof(BuyTheDipAgent)} - Error for {instrument.Symbol}");
                Console.WriteLine(e);
                Console.WriteLine();
            }
            
            await Task.Delay(Random.Shared.Next(30000, 90000));
        }

        await page.CloseAsync();
    }

    private async Task<(bool Success, bool ShouldTryAgain)> TryBuy(IPage page)
    {
        var response = await page.GotoAsync($"{BtTrade.BaseUrl}/platform/portfolio/symbol/{instrument.Id}", pageGotoOptions);
        
        var orderDialogLocator = await OrderDialogOperations.Open(page);
        
        if (await AreParametersOutOfRange())
        {
            return (false, true);
        }
        
        await Task.Delay(500);
        await OrderDialogOperations.FillInQuantity(orderDialogLocator, parameters.Volume);
        await Task.Delay(500);
        await OrderDialogOperations.SetTypeToFillOrKill(orderDialogLocator);
        await Task.Delay(500);
        await OrderDialogOperations.PressBuy(orderDialogLocator);
        await Task.Delay(500);
        await OrderDialogOperations.PressConfirm(orderDialogLocator);
        await Task.Delay(500);
        
        var success = !await OrderDialogOperations.IsError(orderDialogLocator);
        await Task.Delay(500);
        await OrderDialogOperations.Close(orderDialogLocator);

        return (success, false);
        
        async Task<bool> AreParametersOutOfRange()
        {
            var (lowestAskPrice, lowestAskVolume) = await OrderDialogOperations.GetLowestAsk(orderDialogLocator);

            return lowestAskVolume < parameters.Volume ||
                   lowestAskPrice <= parameters.DangerMinAsk ||
                   lowestAskPrice > parameters.MaxAcceptableAsk;
        }
    }

    public sealed record Parameters(int Volume, double MaxAcceptableAsk, double DangerMinAsk);
}

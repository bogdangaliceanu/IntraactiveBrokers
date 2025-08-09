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
                if (await TryBuy(page))
                {
                    Console.WriteLine();
                    Console.WriteLine($"{nameof(BuyTheDipAgent)} - Success for {instrument.Symbol}");
                    Console.WriteLine();
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

    private async Task<bool> TryBuy(IPage page)
    {
        var response = await page.GotoAsync($"{BtTrade.BaseUrl}/platform/portfolio/symbol/{instrument.Id}", pageGotoOptions);
        
        var orderDialogLocator = await OpenOrderDialog();
        
        if (await AreParametersOutOfRange())
        {
            return false;
        }
        
        await Task.Delay(1000);
        await FillInQuantity();
        await Task.Delay(1000);
        await SetTypeToFillOrKill();
        await Task.Delay(1000);
        await PressBuy();
        await Task.Delay(1000);
        await PressConfirm();
        await Task.Delay(1000);
        
        var success = !IsError();
        await Task.Delay(1000);
        await CloseOrderDialog();

        return success;

        async Task CloseOrderDialog() => await orderDialogLocator.Locator("div.modal__content__close").ClickAsync();

        bool IsError()
        {
            var errorMessage = orderDialogLocator.Locator("div.order-message--error");
            return errorMessage != null;
        }

        async Task PressConfirm() => await orderDialogLocator.Locator("div.confirm button#sign").ClickAsync();

        async Task PressBuy() => await orderDialogLocator.Locator("div.actions button.btn--buy").Last.ClickAsync();

        async Task SetTypeToFillOrKill()
        {
            await orderDialogLocator.Locator("div.init-order div.order-dropdown").Last.ClickAsync();
            await Task.Delay(1000);
            await orderDialogLocator.Locator("div.init-order div.order-dropdown__content > div:last-child").Last.ClickAsync();
        }

        async Task FillInQuantity() => await orderDialogLocator.Locator("div#order-quantity input").FillAsync(parameters.Volume.ToString());

        async Task<bool> AreParametersOutOfRange()
        {
            var lowestAskRow = orderDialogLocator.Locator("div.deep-market-header-ask > div.deep-market-details__row:nth-child(2)");
            var lowestAskPrice = double.Parse(await lowestAskRow.Locator("div:nth-child(2)").InnerTextAsync());
            var lowestAskVolume = int.Parse(await lowestAskRow.Locator("div:nth-child(3)").InnerTextAsync());

            return lowestAskVolume < parameters.Volume ||
                   lowestAskPrice <= parameters.DangerMinAsk ||
                   lowestAskPrice > parameters.MaxAcceptableAsk;
        }

        async Task<ILocator> OpenOrderDialog()
        {
            await page.Locator("button.btn--primary-buy").ClickAsync();
            return page.Locator("div.modal__content.new-order");
        }
    }

    public sealed record Parameters(int Volume, double MaxAcceptableAsk, double DangerMinAsk);
}
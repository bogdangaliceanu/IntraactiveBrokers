using Microsoft.Playwright;

namespace IntraactiveBrokers.BtCapitalPartners;

public static class OrderDialogOperations
{
    public static async Task<ILocator> Open(IPage page)
    {
        await page.Locator("button.btn--primary-buy").ClickAsync();
        return page.Locator("div.modal__content.new-order");
    }
    
    public static async Task Close(ILocator orderDialogLocator) =>
        await orderDialogLocator.Locator("div.modal__content__close").ClickAsync();
    
    public static async Task FillInQuantity(ILocator orderDialogLocator, int quantity) =>
        await orderDialogLocator.Locator("div#order-quantity input").FillAsync(quantity.ToString());
    
    public static async Task<bool> IsError(ILocator orderDialogLocator) =>
        await orderDialogLocator.Locator("div.order-message--error").CountAsync() == 1;

    public static async Task PressConfirm(ILocator orderDialogLocator) =>
        await orderDialogLocator.Locator("div.confirm button#sign").ClickAsync();

    public static async Task PressBuy(ILocator orderDialogLocator) =>
        await orderDialogLocator.Locator("div.actions button.btn--buy").Last.ClickAsync();

    public static async Task SetTypeToFillOrKill(ILocator orderDialogLocator)
    {
        await orderDialogLocator.Locator("div.init-order div.order-dropdown").Last.ClickAsync();
        await Task.Delay(500);
        await orderDialogLocator.Locator("div.init-order div.order-dropdown__content > div:last-child").Last.ClickAsync();
    }
    
    public static async Task<(double Price, int Volume)> GetLowestAsk(ILocator orderDialogLocator)
    {
        var lowestAskRow = orderDialogLocator.Locator("div.deep-market-header-ask > div.deep-market-details__row:nth-child(2)");
        var price = double.Parse(await lowestAskRow.Locator("div:nth-child(2)").InnerTextAsync());
        var volume = int.Parse(await lowestAskRow.Locator("div:nth-child(3)").InnerTextAsync());

        return (price, volume);
    }
    
    public static async Task<(double Price, int Volume)> GetHighestBid(ILocator orderDialogLocator)
    {
        var highestBidRow = orderDialogLocator.Locator("div.deep-market-header-bid > div.deep-market-details__row:nth-child(2)");
        var price = double.Parse(await highestBidRow.Locator("div:nth-child(2)").InnerTextAsync());
        var volume = int.Parse(await highestBidRow.Locator("div:nth-child(3)").InnerTextAsync());

        return (price, volume);
    }
}
using DevBetterWeb.Core.Interfaces;
using Stripe;
using System.Linq;
using System.Threading.Tasks;

namespace DevBetterWeb.Infrastructure.PaymentHandler.StripePaymentHandler
{
  public class StripePaymentHandlerPriceService : IPaymentHandlerPrice
  {
    private readonly PriceService _priceService;

    public StripePaymentHandlerPriceService(PriceService priceService)
    {
      _priceService = priceService;
    }

    public int GetPriceAmount(string subscriptionPriceId)
    {
      var priceObject = _priceService.Get(subscriptionPriceId);

      int price = (int)priceObject.UnitAmount!;

      return price;
    }

    public async Task<int> GetPriceCount()
    {
      var prices = await _priceService.ListAsync();

      return prices.Count();
    }
  }

}

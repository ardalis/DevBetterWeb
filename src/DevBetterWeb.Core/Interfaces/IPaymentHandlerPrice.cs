using System.Threading.Tasks;

namespace DevBetterWeb.Core.Interfaces
{
  public interface IPaymentHandlerPrice
  {
    int GetPriceAmount(string subscriptionPriceId);
    Task<int> GetPriceCount();
  }
}

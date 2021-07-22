using System.Threading.Tasks;

namespace DevBetterWeb.Core.Interfaces
{
  public interface IDailyCheckSubscriptionPlanCountService
  {
    Task WarnIfNumberOfMemberSubscriptionPlansDifferentThanExpected(AppendOnlyStringList messages);
  }
}

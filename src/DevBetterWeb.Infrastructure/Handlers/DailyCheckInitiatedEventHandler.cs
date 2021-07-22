using DevBetterWeb.Core.Entities;
using DevBetterWeb.Core.Events;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Core.Specs;
using DevBetterWeb.Infrastructure.DiscordWebooks;
using System;
using System.Threading.Tasks;

namespace DevBetterWeb.Core.Handlers
{
  public class DailyCheckInitiatedEventHandler : IHandle<DailyCheckInitiatedEvent>
  {
    private const string DAILY_CHECK_COMPLETED_MESSAGE = "Daily Check Event Completed";

    private readonly AdminUpdatesWebhook _webhook;
    private readonly IAlumniGraduationService _alumniGraduationService;
    private readonly IDailyCheckPingService _dailyCheckPingService;
    private readonly IDailyCheckSubscriptionPlanCountService _dailyCheckSubscriptionPlanCountService;
    private readonly IRepository<DailyCheck> _repository;

    public DailyCheckInitiatedEventHandler(AdminUpdatesWebhook webhook,
      IAlumniGraduationService alumniGraduationService,
      IDailyCheckPingService dailyCheckPingService,
      IDailyCheckSubscriptionPlanCountService dailyCheckSubscriptionPlanCountService,
      IRepository<DailyCheck> repository)
    {
      _webhook = webhook;
      _alumniGraduationService = alumniGraduationService;
      _dailyCheckPingService = dailyCheckPingService;
      _dailyCheckSubscriptionPlanCountService = dailyCheckSubscriptionPlanCountService;
      _repository = repository;
    }

    public async Task Handle(DailyCheckInitiatedEvent domainEvent)
    {
      AppendOnlyStringList messages = new();

      // real stuff

      // check if admins need to be reminded to avoid renewing near-alumnus's subscription
      await _dailyCheckPingService.PingAdminsAboutAlmostAlumsIfNeeded(messages);

      // check if people need upgraded to alumni
      await _alumniGraduationService.GraduateMembersIfNeeded(messages);

      // check if people need to be pinged about new member link
      await _dailyCheckPingService.SendPingIfNeeded(messages);

      // check if number of MemberSubscriptionPlans == expected number
      await _dailyCheckSubscriptionPlanCountService.WarnIfNumberOfMemberSubscriptionPlansDifferentThanExpected(messages);

      messages.Append(DAILY_CHECK_COMPLETED_MESSAGE);

      await SendMessagesToDiscord(messages);
      await StoreMessagesInTasksCompleted(messages);
    }

    private async Task SendMessagesToDiscord(AppendOnlyStringList messages)
    {
      foreach (var message in messages)
      {
        _webhook.Content = message;
        await _webhook.Send();
      }
    }

    private async Task StoreMessagesInTasksCompleted(AppendOnlyStringList messages)
    {
      var spec = new DailyCheckByDateSpec(DateTime.Today);
      var todaysDailyCheck = await _repository.GetBySpecAsync(spec);

      if (todaysDailyCheck != null)
      {
        foreach (var message in messages)
        {
          if (!message.Equals(DAILY_CHECK_COMPLETED_MESSAGE))
          {
            if (todaysDailyCheck.TasksCompleted == null) todaysDailyCheck.TasksCompleted = "";
            todaysDailyCheck.TasksCompleted += message;
          }
        }
        await _repository.UpdateAsync(todaysDailyCheck);
      }
    }
  }
}

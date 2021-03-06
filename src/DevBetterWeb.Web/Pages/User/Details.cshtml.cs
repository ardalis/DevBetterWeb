﻿using System.Threading.Tasks;
using DevBetterWeb.Core.Entities;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Core.Specs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevBetterWeb.Web.Pages.User
{
  [Authorize]
  public class DetailsModel : PageModel
  {
#pragma warning disable CS0436 // Type conflicts with imported type
    public UserDetailsViewModel? UserDetailsViewModel { get; set; }
#pragma warning restore CS0436 // Type conflicts with imported type
    private readonly IRepository<Member> _memberRepository;
    private readonly IMemberSubscriptionPeriodCalculationsService _memberSubscriptionPeriodCalculationsService;

    public MemberSubscriptionPercentBarViewModel PercentModel { get; set; } = new MemberSubscriptionPercentBarViewModel(0);

    public DetailsModel(IRepository<Member> memberRepository,
      IMemberSubscriptionPeriodCalculationsService memberSubscriptionPeriodCalculationsService)
    {
      _memberRepository = memberRepository;
      _memberSubscriptionPeriodCalculationsService = memberSubscriptionPeriodCalculationsService;
    }

    public async Task OnGet(string userId)
    {

      var spec = new MemberByUserIdWithBooksReadAndMemberSubscriptionsSpec(userId);
      var member = await _memberRepository.GetBySpecAsync(spec);

      if (member == null)
      {
        // TODO: Add logging
        BadRequest();
      }

#pragma warning disable CS0436 // Type conflicts with imported type
      UserDetailsViewModel = new UserDetailsViewModel(member!);
#pragma warning restore CS0436 // Type conflicts with imported type

      int percentageNum = _memberSubscriptionPeriodCalculationsService.GetPercentageProgressToAlumniStatus(member!);

      PercentModel.Percentage = percentageNum;
    }
  }
}

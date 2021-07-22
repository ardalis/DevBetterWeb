using System.Collections.Generic;
using System.Threading.Tasks;
using DevBetterWeb.Core;
using DevBetterWeb.Core.Entities;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Core.Specs;
using System.Linq;
using DevBetterWeb.Infrastructure.Identity.Data;
using Microsoft.AspNetCore.Identity;
using System;

namespace DevBetterWeb.Infrastructure.Services
{
  public class DailyCheckPingService : IDailyCheckPingService
  {
    private TimeSpan TWO_DAYS = new TimeSpan(2, 0, 0, 0);
    private TimeSpan FOUR_DAYS = new TimeSpan(4, 0, 0, 0);
    private int DAYS_IN_TWO_YEARS = 365 * 2;
    private int DAYS_BEFORE_GRADUATION_TO_PING = 10;

    public IRepository<Invitation> _inviteRepository;
    public IRepository<Member> _memberRepository;
    public IEmailService _emailService;
    public UserManager<ApplicationUser> _userManager;

    public DailyCheckPingService(IRepository<Invitation> repository,
      IRepository<Member> memberRepository,
      IEmailService emailService,
      UserManager<ApplicationUser> userManager)
    {
      _inviteRepository = repository;
      _memberRepository = memberRepository;
      _emailService = emailService;
      _userManager = userManager;
    }

    public async Task SendPingIfNeeded(AppendOnlyStringList messages)
    {
      var spec = new ActiveInvitationsSpec();
      var activeInvitations = await _inviteRepository.ListAsync(spec);

      var invitationsForUserPing = CheckIfAnyActiveInvitationsRequireUserPing(activeInvitations);
      if (invitationsForUserPing.Any())
      {
        foreach (var invitation in invitationsForUserPing)
        {
          messages.Append($"User at email {invitation.Email} should be reminded to finish setting up their account.");
        }

        var userPingMessages = await SendUserPing(invitationsForUserPing);
        foreach (var message in userPingMessages)
        {
          messages.Append(message);
        }
      }

      var invitationsForAdminPing = CheckIfAnyActiveInvitationsRequireAdminPing(activeInvitations);
      if (invitationsForAdminPing.Any())
      {
        var listOfEmailsToRemindAdminsAbout = "";
        foreach (var invitation in invitationsForAdminPing)
        {
          listOfEmailsToRemindAdminsAbout += $"{invitation.Email}\n";
        }
        messages.Append($"Admins should be reminded to remind these users to finish setting up their DevBetter accounts: {listOfEmailsToRemindAdminsAbout}");

        var adminPingMessage = await SendAdminPing(invitationsForAdminPing);
        messages.Append(adminPingMessage);
      }
    }

    public async Task PingAdminsAboutAlmostAlumsIfNeeded(AppendOnlyStringList messages)
    {
      var members = await _memberRepository.ListAsync();
      var membersToPingAdminsAbout = new List<Member>();

      foreach (var member in members)
      {
        if (member.TotalSubscribedDays() == DAYS_IN_TWO_YEARS - DAYS_BEFORE_GRADUATION_TO_PING)
        {
          membersToPingAdminsAbout.Add(member);
        }
      }

      if (membersToPingAdminsAbout.Any())
      {

        string listOfMembersToPingAdminsAbout = "";

        foreach (var member in membersToPingAdminsAbout)
        {
          listOfMembersToPingAdminsAbout = $"{member.UserFullName()}\n";
        }

        messages.Append($"Admins need to be reminded not to renew the subscriptions of the following members about to graduate: {listOfMembersToPingAdminsAbout}");

        var usersInAdminRole = await _userManager.GetUsersInRoleAsync(AuthConstants.Roles.ADMINISTRATORS);

        foreach(var user in usersInAdminRole)
        {
          await _emailService.SendEmailAsync(user.Email, "Upcoming DevBetter Graduation", $"Ensure that the following member(s)'s subscriptions are not renewed, as they will graduate to alumni status in {DAYS_BEFORE_GRADUATION_TO_PING} days:\n {listOfMembersToPingAdminsAbout}");
        }

        messages.Append($"Admins have been reminded not to renew the subscriptions of the following members about to graduate: {listOfMembersToPingAdminsAbout}");
      }
    }

    public List<Invitation> CheckIfAnyActiveInvitationsRequireAdminPing(List<Invitation> invitations)
    {
      var invitationsRequiringAdminPing = new List<Invitation>();

      foreach (var invite in invitations)
      {
        if (InviteRequiresAdminPing(invite)) invitationsRequiringAdminPing.Add(invite);
      }

      return invitationsRequiringAdminPing;
    }

    private bool InviteRequiresAdminPing(Invitation invite)
    {
      bool inviteRequiresAdminPing = true;

      if (invite.DateOfUserPing.Equals(DateTime.MinValue)) inviteRequiresAdminPing = false;
      if (invite.DateOfLastAdminPing.Equals(DateTime.Today)) inviteRequiresAdminPing = false;
      if (DateTime.Today - invite.DateCreated < FOUR_DAYS) inviteRequiresAdminPing = false;
      if (DateTime.Today - invite.DateOfUserPing < TWO_DAYS) inviteRequiresAdminPing = false;
      if (!invite.Active) inviteRequiresAdminPing = false;

      return inviteRequiresAdminPing;
    }

    public List<Invitation> CheckIfAnyActiveInvitationsRequireUserPing(List<Invitation> invitations)
    {
      var invitationsRequiringUserPing = new List<Invitation>();

      foreach (var invite in invitations)
      {
        if ((DateTime.Today - invite.DateCreated) >= TWO_DAYS && invite.DateOfUserPing.Equals(DateTime.MinValue))
        {
          invitationsRequiringUserPing.Add(invite);
        }
      }

      return invitationsRequiringUserPing;
    }

    private async Task<List<string>> SendUserPing(List<Invitation> invitations)
    {
      var messagesToAdd = new List<string>();

      var emailSubject = "Finish setting up your DevBetter account!";
      var emailBody = "Don't forget to click the link below to finish setting up your DevBetterAccount!\n";

      foreach (var invitation in invitations)
      {
        var url = $"https://devbetter.com/Identity/Account/NewMemberRegister/{invitation.InviteCode}/{invitation.Email}";

        await _emailService.SendEmailAsync(invitation.Email, emailSubject, $"{emailBody}{url}");
        messagesToAdd.Add($"User at email {invitation.Email} has been reminded to finish setting up their account.");
        invitation.UpdateUserPingDate();
      }

      return messagesToAdd;
    }
    private async Task<string> SendAdminPing(List<Invitation> invitations)
    {
      var emailSubject = "Remind user(s) to finish setting up their DevBetter account(s)";
      var listOfEmailsToRemindAdminsAbout = "";

      foreach (var invitation in invitations)
      {
        listOfEmailsToRemindAdminsAbout += $"{invitation.Email}\n";
      }

      var emailBody = $"Please remind these users to finish setting up their DevBetter accounts: {listOfEmailsToRemindAdminsAbout}";

      var usersInAdminRole = await _userManager.GetUsersInRoleAsync(AuthConstants.Roles.ADMINISTRATORS);

      var listOfEmailsAdminsWereRemindedAbout = "";

      foreach (var user in usersInAdminRole)
      {
        await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
        listOfEmailsAdminsWereRemindedAbout += $"{user.Email}\n";
      }

      var message = $"Admins were reminded to remind these users to finish setting up their DevBetter accounts: {listOfEmailsAdminsWereRemindedAbout}";

      foreach (var invitation in invitations)
      {
        invitation.UpdateAdminPingDate();
      }

      return message;

    }
  }
}

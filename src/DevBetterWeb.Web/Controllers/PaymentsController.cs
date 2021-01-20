﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DevBetterWeb.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;


namespace DevBetterWeb.Web.Controllers
{
  public class PaymentsController : Controller
  {
    public readonly IOptions<StripeOptions> options;
    private readonly IStripeClient client;

    public PaymentsController(IOptions<StripeOptions> options)
    {
      this.options = options;
      client = new StripeClient(this.options.Value.stripeSecretKey);
    }

    // Set your secret key. Remember to switch to your live secret key in production!
    // See your keys here: https://dashboard.stripe.com/account/apikeys

    [HttpGet("setup")]
    public SetupResponse Setup()
    {
      return new SetupResponse
      {
        //ProPrice = this.options.Value.ProPrice,
        //BasicPrice = this.options.Value.BasicPrice,
        PublishableKey = this.options.Value.stripePublishableKey,
      };
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest req)
    {
      var options = new SessionCreateOptions
      {
        // See https://stripe.com/docs/api/checkout/sessions/create
        // for additional parameters to pass.
        // {CHECKOUT_SESSION_ID} is a string literal; do not change it!
        // the actual Session ID is returned in the query parameter when your customer
        // is redirected to the success page.
        SuccessUrl = "https://example.com/success.html?session_id={CHECKOUT_SESSION_ID}",
        CancelUrl = "/checkout",
        PaymentMethodTypes = new List<string>
        {
            "card",
        },
        Mode = "subscription",
        LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                Price = req.PriceId,
                // For metered billing, do not pass quantity
                Quantity = 1,
            },
        },
      };
      var service = new SessionService(this.client);
      try
      {
        var session = await service.CreateAsync(options);
        return Ok(new CreateCheckoutSessionResponse
        {
          SessionId = session.Id,
        });
      }
      catch (StripeException e)
      {
        Console.WriteLine(e.StripeError.Message);
        return BadRequest(new ErrorResponse
        {
          ErrorMessage = new ErrorMessage
          {
            Message = e.StripeError.Message,
          }
        });
      }
    }

    [HttpGet("checkout-session")]
    public async Task<IActionResult> CheckoutSession(string sessionId)
    {
      var service = new SessionService(this.client);
      var session = await service.GetAsync(sessionId);
      return Ok(session);
    }

  }

  public class StripeOptions
  {
    public string? stripePublishableKey { get; set; }
    public string? stripeSecretKey { get; set; }
    //public string? WebhookSecret { get; set; }

    //public string? BasicPrice { get; set; }
    //public string? ProPrice { get; set; }
    //public string? Domain { get; set; }
  }
}

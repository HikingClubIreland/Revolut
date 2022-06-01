using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Payments.Revolut.Models;
using Nop.Plugin.Payments.Revolut.Services;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Payments;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Revolut.Components
{
    /// <summary>
    /// Represents the view component to display payment info in public store
    /// </summary>
    [ViewComponent(Name = Defaults.PAYMENT_INFO_VIEW_COMPONENT_NAME)]
    public class PaymentInfoViewComponent : NopViewComponent
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPaymentService _paymentService;
        private readonly OrderSettings _orderSettings;
        private readonly RevolutSettings _settings;
        private readonly ServiceManager _serviceManager;

        #endregion

        #region Ctor

        public PaymentInfoViewComponent(ILocalizationService localizationService,
            INotificationService notificationService,
            IPaymentService paymentService,
            OrderSettings orderSettings,
            RevolutSettings settings,
            ServiceManager serviceManager)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _paymentService = paymentService;
            _orderSettings = orderSettings;
            _settings = settings;
            _serviceManager = serviceManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke view component
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data</param>
        /// <returns>View component result</returns>
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {            
            var model = new PaymentInfoModel();

            //prepare order GUID
            var paymentRequest = new ProcessPaymentRequest();
            _paymentService.GenerateOrderGuid(paymentRequest);
                        
            //try to create an order
            var (order, errorMessage) = await _serviceManager.CreateOrderAsync(_settings, paymentRequest.OrderGuid);
            if (order != null)
            {
                model.OrderId = order.id;
                model.PublicId = order.public_id;
                model.isCreditCard = _settings.UseCreditCard;
                model.CreditCardTitle = _settings.CreditCardTitle;                
                model.isRevolutPay = _settings.UseRevolutPay;
                model.RevolutPayTitle = _settings.RevolutPayTitle;

                model.environment = "prod";
                if (_settings.UseSandbox)
                {
                    model.environment = "sandbox";
                }

                //Billing Address
                model.BillingAddress = order.billingAddress;
                model.CountryCode = order.countryCode;


                //save order details for future using
                var key = await _localizationService.GetResourceAsync("Plugins.Payments.Revolut.OrderId");
                paymentRequest.CustomValues.Add(key, order.id);
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            {
                model.Errors = errorMessage;
                if (_orderSettings.OnePageCheckoutEnabled)
                    ModelState.AddModelError(string.Empty, errorMessage);
                else
                    _notificationService.ErrorNotification(errorMessage);
            }
            

            HttpContext.Session.Set(Defaults.PaymentRequestSessionKey, paymentRequest);

            //return null;
            return View("~/Plugins/Payments.Revolut/Views/PaymentInfo.cshtml", model);
        }

        #endregion
    }
}
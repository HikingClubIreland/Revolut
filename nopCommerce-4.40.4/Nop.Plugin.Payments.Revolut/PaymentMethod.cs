﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Http.Extensions;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Stores;
using Nop.Web.Framework.Infrastructure;
using Nop.Plugin.Payments.Revolut.Domain;
using Nop.Plugin.Payments.Revolut.Models;
using Nop.Plugin.Payments.Revolut.Services;

namespace Nop.Plugin.Payments.Revolut
{
    /// <summary>
    /// Represents a payment method implementation
    /// </summary>
    public class PaymentMethod : BasePlugin, IPaymentMethod, IWidgetPlugin
    {
        #region Fields

        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly RevolutSettings _settings;
        private readonly ServiceManager _serviceManager;
        private readonly WidgetSettings _widgetSettings;

        #endregion

        #region Ctor

        public PaymentMethod(IActionContextAccessor actionContextAccessor,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            ISettingService settingService,
            IStoreService storeService,
            IUrlHelperFactory urlHelperFactory,
            RevolutSettings settings,
            ServiceManager serviceManager,
            WidgetSettings widgetSettings)
        {
            _actionContextAccessor = actionContextAccessor;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _settingService = settingService;
            _storeService = storeService;
            _urlHelperFactory = urlHelperFactory;
            _settings = settings;
            _serviceManager = serviceManager;
            _widgetSettings = widgetSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            //try to get an order id from custom values
            var orderIdKey = await _localizationService.GetResourceAsync("Plugins.Payments.Revolut.OrderId");
            if (!processPaymentRequest.CustomValues.TryGetValue(orderIdKey, out var orderId) || string.IsNullOrEmpty(orderId?.ToString()))
                throw new NopException("Failed to get the Revolut order ID");

            //retrieve an order
            var (order, error) = await _serviceManager.retrieveOrderAsync(_settings, orderId.ToString());
            if (!string.IsNullOrEmpty(error))
                return new ProcessPaymentResult { Errors = new[] { error } };

            //request succeeded
            var result = new ProcessPaymentResult();

            //MANUAL CAPTURE
            if (order.state.Equals(Defaults.ORDER_AUTHORISED))
            {
                result.AuthorizationTransactionId = order.id;
                result.CaptureTransactionResult = order.state;
                result.NewPaymentStatus = PaymentStatus.Authorized;

                //CAPTURE
                (order, error) = await _serviceManager.captureOrderAsync(_settings, orderId.ToString());
                if (!string.IsNullOrEmpty(error))
                    return new ProcessPaymentResult { Errors = new[] { error } };
            }

            //AUTOMATIC CAPTURE
            if (order.state.Equals(Defaults.ORDER_COMPLETED))
            {
                result.CaptureTransactionId = order.id;
                result.CaptureTransactionResult = order.state;
                result.NewPaymentStatus = PaymentStatus.Paid;
            }

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            /*
            //capture previously authorized payment
            var (capture, error) = _serviceManager
                .CaptureAuthorization(_settings, capturePaymentRequest.Order.AuthorizationTransactionId);

            if (!string.IsNullOrEmpty(error))
                return new CapturePaymentResult { Errors = new[] { error } };

            //request succeeded
            return new CapturePaymentResult
            {
                CaptureTransactionId = capture.Id,
                CaptureTransactionResult = capture.Status,
                NewPaymentStatus = PaymentStatus.Paid
            };
            */
            return null;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            /*
            //void previously authorized payment
            var (_, error) = _serviceManager.Void(_settings, voidPaymentRequest.Order.AuthorizationTransactionId);

            if (!string.IsNullOrEmpty(error))
                return new VoidPaymentResult { Errors = new[] { error } };

            //request succeeded
            return new VoidPaymentResult { NewPaymentStatus = PaymentStatus.Voided };
            */
            return null;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            /*
            //refund previously captured payment
            var amount = refundPaymentRequest.AmountToRefund != refundPaymentRequest.Order.OrderTotal
                ? (decimal?)refundPaymentRequest.AmountToRefund
                : null;
            var (refund, error) = _serviceManager.Refund(_settings, refundPaymentRequest.Order.CaptureTransactionId,
                refundPaymentRequest.Order.CustomerCurrencyCode, amount);

            if (!string.IsNullOrEmpty(error))
                return new RefundPaymentResult { Errors = new[] { error } };

            //request succeeded
            var refundIds = _genericAttributeService.GetAttribute<List<string>>(refundPaymentRequest.Order, Defaults.RefundIdAttributeName)
                ?? new List<string>();
            if (!refundIds.Contains(refund.Id))
                refundIds.Add(refund.Id);
            _genericAttributeService.SaveAttribute(refundPaymentRequest.Order, Defaults.RefundIdAttributeName, refundIds);
            return new RefundPaymentResult
            {
                NewPaymentStatus = refundPaymentRequest.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded
            };
            */
            return null;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            var notConfigured = !ServiceManager.IsConfigured(_settings);
            return Task.FromResult(notConfigured);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(decimal.Zero);
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var errors = new List<string>();

            //try to get errors from the form parameters
            if (form.TryGetValue(nameof(PaymentInfoModel.Errors), out var errorValue) && !StringValues.IsNullOrEmpty(errorValue))
                errors.Add(errorValue.ToString());

            return Task.FromResult<IList<string>>(errors);
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            //already set
            return Task.FromResult(_actionContextAccessor.ActionContext.HttpContext.Session
                .Get<ProcessPaymentRequest>(Defaults.PaymentRequestSessionKey));
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext).RouteUrl(Defaults.ConfigurationRouteName);
        }

        /// <summary>
        /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        public string GetPublicViewComponentName()
        {
            return Defaults.PAYMENT_INFO_VIEW_COMPONENT_NAME;
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string>
            {
                PublicWidgetZones.CheckoutPaymentInfoTop,
                PublicWidgetZones.OpcContentBefore,
                PublicWidgetZones.ProductDetailsTop,
                PublicWidgetZones.ProductDetailsAddInfo,
                PublicWidgetZones.OrderSummaryContentBefore,
                PublicWidgetZones.OrderSummaryContentAfter,
                PublicWidgetZones.HeaderLinksBefore,
                PublicWidgetZones.Footer
            });
        }

        /// <summary>
        /// Gets a name of a view component for displaying widget
        /// </summary>
        /// <param name="widgetZone">Name of the widget zone</param>
        /// <returns>View component name</returns>
        public string GetWidgetViewComponentName(string widgetZone)
        {
            if (widgetZone == null)
                throw new ArgumentNullException(nameof(widgetZone));

            if (widgetZone.Equals(PublicWidgetZones.CheckoutPaymentInfoTop) ||
                widgetZone.Equals(PublicWidgetZones.OpcContentBefore) ||
                widgetZone.Equals(PublicWidgetZones.ProductDetailsTop) ||
                widgetZone.Equals(PublicWidgetZones.OrderSummaryContentBefore))
            {
                return Defaults.SCRIPT_VIEW_COMPONENT_NAME;
            }

            if (widgetZone.Equals(PublicWidgetZones.ProductDetailsAddInfo) || widgetZone.Equals(PublicWidgetZones.OrderSummaryContentAfter))
                return Defaults.BUTTONS_VIEW_COMPONENT_NAME;

            if (widgetZone.Equals(PublicWidgetZones.HeaderLinksBefore) || widgetZone.Equals(PublicWidgetZones.Footer))
                return Defaults.LOGO_VIEW_COMPONENT_NAME;

            return string.Empty;
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override async Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new RevolutSettings
            {
                UseSandbox = true
            });

            if (!_widgetSettings.ActiveWidgetSystemNames.Contains(Defaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Add(Defaults.SystemName);
                await _settingService.SaveSettingAsync(_widgetSettings);
            }

            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.Revolut.Credentials.Valid"] = "The specified credentials are valid",
                ["Plugins.Payments.Revolut.Credentials.Invalid"] = "The specified credentials are invalid (see details in the <a href=\"{0}\" target=\"_blank\">log</a>)",
                ["Plugins.Payments.Revolut.Fields.ApiKey"] = "API Key",
                ["Plugins.Payments.Revolut.Fields.ApiKey.Hint"] = "Enter api key for your app.",
                ["Plugins.Payments.Revolut.Fields.ApiKey.Required"] = "API Key is required",
                ["Plugins.Payments.Revolut.Fields.UseSandbox"] = "Use sandbox",
                ["Plugins.Payments.Revolut.Fields.UseSandbox.Hint"] = "Determine whether to use the sandbox environment for testing purposes.",
                ["Plugins.Payments.Revolut.Fields.UseCreditCard"] = "Use credit card",
                ["Plugins.Payments.Revolut.Fields.UseCreditCard.Hint"] = "Determine whether to support credit cards or not.",
                ["Plugins.Payments.Revolut.Fields.CreditCardTitle"] = "Credit Card Title",
                ["Plugins.Payments.Revolut.Fields.CreditCardTitle.Hint"] = "Enter credit card field title.",
                ["Plugins.Payments.Revolut.Fields.CreditCardTitle.Required"] = "Credit card title is required.",
                ["Plugins.Payments.Revolut.Fields.UseRevolutPay"] = "Use Revolut Pay",
                ["Plugins.Payments.Revolut.Fields.UseRevolutPay.Hint"] = "Determine whether to support revolut pay or not.",
                ["Plugins.Payments.Revolut.Fields.RevolutPayTitle"] = "Revolut Pay Title",
                ["Plugins.Payments.Revolut.Fields.RevolutPayTitle.Hint"] = "Enter revolut pay field title.",
                ["Plugins.Payments.Revolut.Fields.RevolutPayTitle.Required"] = "Revolut pay title is required.",
                ["Plugins.Payments.Revolut.OrderId"] = "Revolut order ID",
                ["Plugins.Payments.Revolut.PaymentMethodDescription"] = "Revolut Pay checkout",
                ["Plugins.Payments.Revolut.RoundingWarning"] = "It looks like you have <a href=\"{0}\" target=\"_blank\">RoundPricesDuringCalculation</a> setting disabled. Keep in mind that this can lead to a discrepancy of the order total amount, as Revolut rounds to two decimals only.",
                ["Plugins.Payments.Revolut.WebhookWarning"] = "Webhook was not created, so some functions may not work correctly (see details in the <a href=\"{0}\" target=\"_blank\">log</a>)"
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override async Task UninstallAsync()
        {
            //webhooks
            var stores = await _storeService.GetAllStoresAsync();
            var storeIds = new List<int> { 0 }.Union(stores.Select(store => store.Id));
            foreach (var storeId in storeIds)
            {
                var settings = await _settingService.LoadSettingAsync<RevolutSettings>(storeId);
                //if (!string.IsNullOrEmpty(settings.WebhookId))
                    //_serviceManager.DeleteWebhook(settings);
            }

            //settings
            if (_widgetSettings.ActiveWidgetSystemNames.Contains(Defaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Remove(Defaults.SystemName);
                await _settingService.SaveSettingAsync(_widgetSettings);
            }
            await _settingService.DeleteSettingAsync<RevolutSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Enums.Nop.Plugin.Payments.Revolut");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.Revolut");

            await base.UninstallAsync();
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.Revolut.PaymentMethodDescription");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => true;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => true;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => true;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => true;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => true;

        #endregion
    }
}
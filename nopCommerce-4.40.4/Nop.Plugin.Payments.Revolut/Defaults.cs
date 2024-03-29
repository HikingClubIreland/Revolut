﻿using System.Collections.Generic;
using Nop.Core;

namespace Nop.Plugin.Payments.Revolut
{
    /// <summary>
    /// Represents plugin constants
    /// </summary>
    public class Defaults
    {
        /// <summary>
        /// Gets the plugin system name
        /// </summary>
        public static string SystemName => "Payments.Revolut";

        /// <summary>
        /// Gets the user agent used to request third-party services
        /// </summary>
        public static string UserAgent => $"nopCommerce-{NopVersion.CURRENT_VERSION}";

        /// <summary>
        /// Gets the nopCommerce partner code
        /// </summary>
        public static string PartnerCode => "NOP_Cart_REV";

        /// <summary>
        /// Gets the configuration route name
        /// </summary>
        public static string ConfigurationRouteName => "Plugin.Payments.Revolut.Configure";

        /// <summary>
        /// Gets the webhook route name
        /// </summary>
        public static string WebhookRouteName => "Plugin.Payments.Revolut.Webhook";

        /// <summary>
        /// Gets the one page checkout route name
        /// </summary>
        public static string OnePageCheckoutRouteName => "CheckoutOnePage";

        /// <summary>
        /// Gets the shopping cart route name
        /// </summary>
        public static string ShoppingCartRouteName => "ShoppingCart";

        /// <summary>
        /// Gets the session key to get process payment request
        /// </summary>
        public static string PaymentRequestSessionKey => "OrderPaymentInfo";

        /// <summary>
        /// Gets the name of a generic attribute to store the refund identifier
        /// </summary>
        public static string RefundIdAttributeName => "RevolutRefundId";

        /// <summary>
        /// Gets the service js script URL
        /// </summary>
        public static string ServiceScriptUrl => "";

        public static string SandboxURL => "https://sandbox-merchant.revolut.com";
        public static string ProductionURL => "https://merchant.revolut.com";

        /// <summary>
        /// Gets webhook event names to subscribe
        /// </summary>
        public static List<string> WebhookEventNames => new List<string>
        {
            "CHECKOUT.ORDER.APPROVED",
            "PAYMENT.AUTHORIZATION.CREATED",
            "PAYMENT.AUTHORIZATION.VOIDED",
            "PAYMENT.CAPTURE.COMPLETED",
            "PAYMENT.CAPTURE.DENIED",
            "PAYMENT.CAPTURE.PENDING",
            "PAYMENT.CAPTURE.REFUNDED"
        };

        /// <summary>
        /// Gets a name of the view component to display payment info in public store
        /// </summary>
        public const string PAYMENT_INFO_VIEW_COMPONENT_NAME = "RevolutPaymentInfo";

        /// <summary>
        /// Gets a name of the view component to add script to pages
        /// </summary>
        public const string SCRIPT_VIEW_COMPONENT_NAME = "RevolutScript";

        /// <summary>
        /// Gets a name of the view component to display buttons
        /// </summary>
        public const string BUTTONS_VIEW_COMPONENT_NAME = "RevolutButtons";

        /// <summary>
        /// Gets a name of the view component to display logo
        /// </summary>
        public const string LOGO_VIEW_COMPONENT_NAME = "RevolutLogo";

        public const string ORDER_COMPLETED = "COMPLETED";
        public const string ORDER_AUTHORISED = "AUTHORISED";
    }
}
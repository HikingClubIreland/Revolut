using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Revolut.Domain;
using Nop.Plugin.Payments.Revolut.Models;
using Nop.Plugin.Payments.Revolut.Services;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Revolut.Controllers
{
    [Area(AreaNames.Admin)]
    [HttpsRequirement]
    [AutoValidateAntiforgeryToken]
    [ValidateIpAddress]
    [AuthorizeAdmin]
    [ValidateVendor]
    public class RevolutController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly ServiceManager _serviceManager;
        private readonly ShoppingCartSettings _shoppingCartSettings;        

        #endregion

        #region Ctor

        public RevolutController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            ServiceManager serviceManager,
            ShoppingCartSettings shoppingCartSettings)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _serviceManager = serviceManager;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var settings = _settingService.LoadSetting<RevolutSettings>(storeScope);

            //prepare model
            var model = new ConfigurationModel
            {
                
                ApiKey = settings.ApiKey,
                UseSandbox = settings.UseSandbox,
                UseCreditCard = settings.UseCreditCard,
                CreditCardTitle = settings.CreditCardTitle,
                UseRevolutPay = settings.UseRevolutPay,
                RevolutPayTitle = settings.RevolutPayTitle,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.ApiKey_OverrideForStore = _settingService.SettingExists(settings, setting => setting.ApiKey, storeScope);
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(settings, setting => setting.UseSandbox, storeScope);
                model.UseCreditCard_OverrideForStore = _settingService.SettingExists(settings, setting => setting.UseCreditCard, storeScope);
                model.CreditCardTitle_OverrideForStore = _settingService.SettingExists(settings, setting => setting.CreditCardTitle, storeScope);
                model.UseRevolutPay_OverrideForStore = _settingService.SettingExists(settings, setting => setting.UseRevolutPay, storeScope);
                model.RevolutPayTitle_OverrideForStore = _settingService.SettingExists(settings, setting => setting.RevolutPayTitle, storeScope);
            }

            //prices and total aren't rounded, so display warning
            if (!_shoppingCartSettings.RoundPricesDuringCalculation)
            {
                var url = Url.Action("AllSettings", "Setting", new { settingName = nameof(ShoppingCartSettings.RoundPricesDuringCalculation) });
                var warning = string.Format(_localizationService.GetResource("Plugins.Payments.Revolut.RoundingWarning"), url);
                _notificationService.WarningNotification(warning, false);
            }

            return View("~/Plugins/Payments.Revolut/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var settings = _settingService.LoadSetting<RevolutSettings>(storeScope);

            //first delete the unused webhook on a previous client, if changed
            if (!string.IsNullOrEmpty(settings.ApiKey)) { }
                //_serviceManager.DeleteWebhook(settings);

            //set new settings values
            settings.ApiKey = model.ApiKey;
            settings.UseSandbox = model.UseSandbox;
            settings.UseCreditCard = model.UseCreditCard;
            settings.CreditCardTitle = model.CreditCardTitle;
            settings.UseRevolutPay = model.UseRevolutPay;
            settings.RevolutPayTitle = model.RevolutPayTitle;

            //ensure that webhook created, display warning in case of fail
            if (!string.IsNullOrEmpty(settings.ApiKey))
            { }
                /*
                var webhookUrl = Url.RouteUrl(Defaults.WebhookRouteName, null, _webHelper.CurrentRequestProtocol);
                var (webhook, webhookError) = _serviceManager.CreateWebHook(settings, webhookUrl);
                settings.WebhookId = webhook?.Id;
                if (string.IsNullOrEmpty(settings.WebhookId))
                {
                    var url = Url.Action("List", "Log");
                    var warning = string.Format(_localizationService.GetResource("Plugins.Payments.Revolut.WebhookWarning"), url);
                    _notificationService.WarningNotification(warning, false);
                }
                */

            //save settings
            _settingService.SaveSettingOverridablePerStore(settings, setting => setting.ApiKey, model.ApiKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(settings, setting => setting.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(settings, setting => setting.UseCreditCard, model.UseCreditCard_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(settings, setting => setting.CreditCardTitle, model.CreditCardTitle_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(settings, setting => setting.UseRevolutPay, model.UseRevolutPay_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(settings, setting => setting.RevolutPayTitle, model.RevolutPayTitle_OverrideForStore, storeScope, false);
            _settingService.ClearCache();

            //ensure credentials are valid
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                var (_, errorMessage) = _serviceManager.TestCredential(settings);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    var url = Url.Action("List", "Log");
                    var error = string.Format(_localizationService.GetResource("Plugins.Payments.Revolut.Credentials.Invalid"), url);
                    _notificationService.ErrorNotification(error, false);
                }
                else
                {
                    _notificationService.SuccessNotification(_localizationService.GetResource("Plugins.Payments.Revolut.Credentials.Valid"));
                }
            }

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}
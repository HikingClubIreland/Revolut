using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<RevolutSettings>(storeId);

            //prepare model
            var model = new ConfigurationModel
            {
                
                ApiKey = settings.ApiKey,
                UseSandbox = settings.UseSandbox,
                UseCreditCard = settings.UseCreditCard,
                CreditCardTitle = settings.CreditCardTitle,
                UseRevolutPay = settings.UseRevolutPay,
                RevolutPayTitle = settings.RevolutPayTitle,
                ActiveStoreScopeConfiguration = storeId,
                IsConfigured = ServiceManager.IsConfigured(settings)
            };

            if (storeId > 0)
            {
                model.ApiKey_OverrideForStore = await _settingService.SettingExistsAsync(settings, setting => setting.ApiKey, storeId);
                model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(settings, setting => setting.UseSandbox, storeId);
                model.UseCreditCard_OverrideForStore = await _settingService.SettingExistsAsync(settings, setting => setting.UseCreditCard, storeId);
                model.CreditCardTitle_OverrideForStore = await _settingService.SettingExistsAsync(settings, setting => setting.CreditCardTitle, storeId);
                model.UseRevolutPay_OverrideForStore = await _settingService.SettingExistsAsync(settings, setting => setting.UseRevolutPay, storeId);
                model.RevolutPayTitle_OverrideForStore = await _settingService.SettingExistsAsync(settings, setting => setting.RevolutPayTitle, storeId);
            }

            //prices and total aren't rounded, so display warning
            if (model.IsConfigured && !_shoppingCartSettings.RoundPricesDuringCalculation)
            {
                var url = Url.Action("AllSettings", "Setting", new { settingName = nameof(ShoppingCartSettings.RoundPricesDuringCalculation) });
                var warning = string.Format(await _localizationService.GetResourceAsync("Plugins.Payments.Revolut.RoundingWarning"), url);
                _notificationService.WarningNotification(warning, false);
            }

            //ensure credentials are valid
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                var (_, credentialsError) = await _serviceManager.TestCredentialAsync(settings);
                if (!string.IsNullOrEmpty(credentialsError))
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.Revolut.Credentials.Invalid"));
                else
                    _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Payments.Revolut.Credentials.Valid"));
            }

            return View("~/Plugins/Payments.Revolut/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<RevolutSettings>(storeScope);

            //set new settings values
            settings.ApiKey = model.ApiKey;
            settings.UseSandbox = model.UseSandbox;
            settings.UseCreditCard = model.UseCreditCard;
            settings.CreditCardTitle = model.CreditCardTitle;
            settings.UseRevolutPay = model.UseRevolutPay;
            settings.RevolutPayTitle = model.RevolutPayTitle;

            //save settings
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.ApiKey, model.ApiKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.UseCreditCard, model.UseCreditCard_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.CreditCardTitle, model.CreditCardTitle_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.UseRevolutPay, model.UseRevolutPay_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.RevolutPayTitle, model.RevolutPayTitle_OverrideForStore, storeScope, false);
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}
﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Plugin.Payments.Revolut.Services;
using Nop.Services.Payments;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Payments.Revolut.Components
{
    /// <summary>
    /// Represents the view component to display buttons
    /// </summary>
    [ViewComponent(Name = Defaults.BUTTONS_VIEW_COMPONENT_NAME)]
    public class ButtonsViewComponent : NopViewComponent
    {
        #region Fields

        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly RevolutSettings _settings;

        #endregion

        #region Ctor

        public ButtonsViewComponent(IPaymentPluginManager paymentPluginManager,
            IStoreContext storeContext,
            IWorkContext workContext,
            RevolutSettings settings)
        {
            _paymentPluginManager = paymentPluginManager;
            _storeContext = storeContext;
            _workContext = workContext;
            _settings = settings;
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
            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();

            if (!await _paymentPluginManager.IsPluginActiveAsync(Defaults.SystemName, customer, store?.Id ?? 0))
                return Content(string.Empty);

            if (!ServiceManager.IsConfigured(_settings))
                return Content(string.Empty);

            if (!widgetZone.Equals(PublicWidgetZones.ProductDetailsAddInfo) && !widgetZone.Equals(PublicWidgetZones.OrderSummaryContentAfter))
                return Content(string.Empty);

            if (widgetZone.Equals(PublicWidgetZones.OrderSummaryContentAfter))
            {
                if (!_settings.DisplayButtonsOnShoppingCart)
                    return Content(string.Empty);

                var routeName = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
                if (routeName != Defaults.ShoppingCartRouteName)
                    return Content(string.Empty);
            }

            if (widgetZone.Equals(PublicWidgetZones.ProductDetailsAddInfo) && !_settings.DisplayButtonsOnProductDetails)
                return Content(string.Empty);

            var productId = additionalData is ProductDetailsModel.AddToCartModel model ? model.ProductId : 0;
            return View("~/Plugins/Payments.Revolut/Views/Buttons.cshtml", (widgetZone, productId));
        }

        #endregion
    }
}
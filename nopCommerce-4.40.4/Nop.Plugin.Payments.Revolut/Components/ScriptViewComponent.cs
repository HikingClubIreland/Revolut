using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Plugin.Payments.Revolut.Services;
using Nop.Services.Payments;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Payments.Revolut.Components
{
    /// <summary>
    /// Represents the view component to add script to pages
    /// </summary>
    [ViewComponent(Name = Defaults.SCRIPT_VIEW_COMPONENT_NAME)]
    public class ScriptViewComponent : NopViewComponent
    {
        #region Fields

        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly RevolutSettings _settings;
        private readonly ServiceManager _serviceManager;

        #endregion

        #region Ctor

        public ScriptViewComponent(IPaymentPluginManager paymentPluginManager,
            IStoreContext storeContext,
            IWorkContext workContext,
            RevolutSettings settings,
            ServiceManager serviceManager)
        {
            _paymentPluginManager = paymentPluginManager;
            _storeContext = storeContext;
            _workContext = workContext;
            _settings = settings;
            _serviceManager = serviceManager;
        }

        #endregion

        #region Methods


        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            if (!await _paymentPluginManager.IsPluginActiveAsync(Defaults.SystemName, customer, store?.Id ?? 0))
                return Content(string.Empty);

            if (!ServiceManager.IsConfigured(_settings))
                return Content(string.Empty);

            if (!widgetZone.Equals(PublicWidgetZones.CheckoutPaymentInfoTop) &&
                !widgetZone.Equals(PublicWidgetZones.OpcContentBefore) &&
                !widgetZone.Equals(PublicWidgetZones.ProductDetailsTop) &&
                !widgetZone.Equals(PublicWidgetZones.OrderSummaryContentBefore))
            {
                return Content(string.Empty);
            }

            if (widgetZone.Equals(PublicWidgetZones.OrderSummaryContentBefore))
            {
                if (!_settings.DisplayButtonsOnShoppingCart)
                    return Content(string.Empty);

                var routeName = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
                if (routeName != Defaults.ShoppingCartRouteName)
                    return Content(string.Empty);
            }

            if (widgetZone.Equals(PublicWidgetZones.ProductDetailsTop) && !_settings.DisplayButtonsOnProductDetails)
                return Content(string.Empty);

            var (script, _) = await _serviceManager.GetScriptAsync(_settings, widgetZone);
            return new HtmlContentViewComponentResult(new HtmlString(script ?? string.Empty));
        }

        #endregion
    }
}
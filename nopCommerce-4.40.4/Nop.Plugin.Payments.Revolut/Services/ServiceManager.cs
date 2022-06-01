using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Payments.Revolut.Domain;
using Nop.Plugin.Payments.Revolut.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.Revolut.Services
{
    /// <summary>
    /// Represents the plugin service manager
    /// </summary>
    public class ServiceManager
    {
        #region Fields

        private readonly IAddressService _addresService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly CurrencySettings _currencySettings;
        private readonly HttpClient _httpClient;

        #endregion

        #region Ctor

        public ServiceManager(IAddressService addresService,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICountryService countryService,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IProductService productService,
            IShoppingCartService shoppingCartService,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            ITaxService taxService,
            IWorkContext workContext,
            CurrencySettings currencySettings)
        {
            _addresService = addresService;
            _checkoutAttributeParser = checkoutAttributeParser;
            _countryService = countryService;
            _currencyService = currencyService;
            _genericAttributeService = genericAttributeService;
            _logger = logger;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _productService = productService;
            _shoppingCartService = shoppingCartService;
            _stateProvinceService = stateProvinceService;
            _storeContext = storeContext;
            _taxService = taxService;
            _workContext = workContext;
            _currencySettings = currencySettings;


            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
            _httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"nopCommerce-{NopVersion.CURRENT_VERSION}");
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Check whether the plugin is configured
        /// </summary>
        /// <param name="settings">Plugin settings</param>
        /// <returns>Result</returns>
        public static bool IsConfigured(RevolutSettings settings)
        {
            //client id and secret are required to request services
            return !string.IsNullOrEmpty(settings?.ApiKey);
        }

        /// <summary>
        /// Handle function and get result
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="settings">Plugin settings</param>
        /// <param name="function">Function</param>
        /// <returns>Result; error message if exists</returns>
        private async Task<(TResult Result, string Error)> HandleFunctionAsync<TResult>(Func<Task<TResult>> function)
        {
            try
            {
                //invoke function
                return (await function(), default);
            }
            catch (Exception exception)
            {
                //get a short error message
                var message = exception.Message;
                if (exception is HttpException httpException)
                {
                    //get error details if exist
                    var details = JsonConvert.DeserializeObject<ExceptionDetails>(httpException.Message);
                    message = details.Message?.Trim('.') ?? details.Name ?? message;
                    if (details?.Details?.Any() ?? false)
                    {
                        message += details.Details.Aggregate(":", (text, issue) => $"{text} " +
                            $"{(issue.Description ?? issue.Issue).Trim('.')}{(!string.IsNullOrEmpty(issue.Field) ? $"({issue.Field})" : null)},").Trim(',');
                    }
                }

                //log errors
                var logMessage = $"{Defaults.SystemName} error: {System.Environment.NewLine}{message}";
                await _logger.ErrorAsync(logMessage, exception, await _workContext.GetCurrentCustomerAsync());

                return (default, message);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare service script
        /// </summary>
        /// <param name="settings">Plugin settings</param>
        /// <returns>Script; error message if exists</returns>
        public async Task<(string Script, string Error)> GetScriptAsync(RevolutSettings settings, string widgetZone)
        {
            return await HandleFunctionAsync(async () =>
            {
                //ensure that plugin is configured
                if (!IsConfigured(settings))
                    throw new NopException("Plugin not configured");

                return "<script>!function(e,o,t){e[t] = function(n, r){var c={sandbox:\"https://sandbox-merchant.revolut.com/embed.js\",prod:\"https://merchant.revolut.com/embed.js\",dev:\"https://merchant.revolut.codes/embed.js\"},d=o.createElement(\"script\");d.id=\"revolut-checkout\",d.src=c[r]||c.prod,d.async=!0,o.head.appendChild(d);var s={then:function(r,c){d.onload=function(){r(e[t](n))},d.onerror=function(){o.head.removeChild(d),c&&c(new Error(t+\" is failed to load\"))}}};return\"function\"==typeof Promise?Promise.resolve(s):s}}(window,document,\"RevolutCheckout\");</script>";
            });
        }

        public async Task<(RevolutOrder revolutOrder, string errorMessage)> retrieveOrderAsync(RevolutSettings settings, string orderId)
        {
            //Endpoint
            var endpoint = settings.UseSandbox ?
                Defaults.SandboxURL :
                Defaults.ProductionURL;
            endpoint += $"/api/1.0/orders/{orderId}";

            //GET
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            var response = _httpClient.GetAsync(endpoint).Result;
            var content = response.Content.ReadAsStringAsync().Result;
            RevolutOrder revolutOrder = JsonConvert.DeserializeObject<RevolutOrder>(content);

            string errorMessage = "";

            return (revolutOrder, errorMessage);
        }

        //CAPTURE ORDER
        public async Task<(RevolutOrder revolutOrder, string errorMessage)> captureOrderAsync(RevolutSettings settings, string orderId)
        {
            var endpoint = settings.UseSandbox ?
                Defaults.SandboxURL :
                Defaults.ProductionURL;
            endpoint += $"/api/1.0/orders/{orderId}/capture";

            var payload = new CaptureOrder();

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            var response = _httpClient.PostAsync(endpoint, null).Result;
            var jsonString = response.Content.ReadAsStringAsync().Result;
            RevolutOrder revolutOrder = JsonConvert.DeserializeObject<RevolutOrder>(jsonString);
            string errorMessage = "";

            if (!string.IsNullOrEmpty(revolutOrder.timestamp))
            {
                errorMessage = "Could not capture";
            }

            return (revolutOrder, errorMessage);
        }

        //TEST CREDENTIAL
        public async Task<(RevolutOrder revolutOrder, string errorMessage)> TestCredentialAsync(RevolutSettings settings)
        {
            var endpoint = settings.UseSandbox ?
                Defaults.SandboxURL :
                Defaults.ProductionURL;
            endpoint += "/api/1.0/orders";

            var body = new PostJSONOrder();
            body.amount = "100";
            body.currency = "EUR";
            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            var response = _httpClient.PostAsync(endpoint, content).Result;
            var jsonString = response.Content.ReadAsStringAsync().Result;
            RevolutOrder revolutOrder = JsonConvert.DeserializeObject<RevolutOrder>(jsonString);
            string errorMessage = "";

            if (revolutOrder.code == 1000 || revolutOrder.code == 1001)
            {
                errorMessage = "API KEY Invalid";
                //throw new NopException(errorMessage);
            }

            return (revolutOrder, errorMessage);
        }

        //CREATE ORDER REVOLUT
        public async Task<(RevolutOrder revolutOrder, string error)> CreateOrderAsync(RevolutSettings settings, Guid orderGuid)
        {

            //ensure that plugin is configured
            if (!IsConfigured(settings))
                throw new NopException("Plugin not configured");

            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();

            var currency = (await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode;
            if (string.IsNullOrEmpty(currency))
                throw new NopException("Primary store currency not set");

            var billingAddress = await _addresService.GetAddressByIdAsync(customer.BillingAddressId ?? 0)
                ?? throw new NopException("Customer billing address not set");

            var shippingAddress = await _addresService.GetAddressByIdAsync(customer.ShippingAddressId ?? 0);

            var billStateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(billingAddress);
            var shipStateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress);

            //Order details
            /*
             * amount Integer (€70.34 => 7034) REQUIRED
             * currency string (ISO 4217 Upper case) REQUIRED
             * capture_mode string AUTOMATIC or MANUAL
             * merchant_order_ext_ref string (system orderId)
             * email string
             * description string
             * shipping_address JSON Object
             * 
             * SHIPPING ADDRESS JSON OBJECT
             * country_code string REQUIRED
             * postcode string REQUIRED
             * street_line_1 string
             * street_line_2 string
             * region string
             * city string
             */
            var payloadOrder = new PostJSONOrder();
            string captureMode = "MANUAL";

            //prepare shipping address details
            if (shippingAddress != null)
            {
                var shippingAddressJsonObj = new ShippingAddress();
                shippingAddressJsonObj.country_code = (await _countryService.GetCountryByIdAsync(billingAddress.CountryId ?? 0))?.TwoLetterIsoCode;
                shippingAddressJsonObj.postcode = CommonHelper.EnsureMaximumLength(shippingAddress.ZipPostalCode, 60);
                shippingAddressJsonObj.street_line_1 = CommonHelper.EnsureMaximumLength(shippingAddress.Address1, 300);
                shippingAddressJsonObj.street_line_2 = CommonHelper.EnsureMaximumLength(shippingAddress.Address2, 300);
                shippingAddressJsonObj.city = CommonHelper.EnsureMaximumLength(shippingAddress.City, 120);

                payloadOrder.shipping_address = shippingAddressJsonObj;
            }

            //prepare purchase unit details
            var shoppingCart = (await _shoppingCartService
                .GetShoppingCartAsync(customer, Core.Domain.Orders.ShoppingCartType.ShoppingCart, store.Id))
                .ToList();

            var (shoppingCartTotal, _, _, _, _, _) = await _orderTotalCalculationService
                .GetShoppingCartTotalAsync(shoppingCart, usePaymentMethodAdditionalFee: false);
            var orderTotal = ((decimal)(Math.Round(shoppingCartTotal ?? decimal.Zero, 2))).ToString();
            var strOrderTotal = orderTotal.Replace(".", string.Empty);

            //set order items
            var itemList = await shoppingCart.SelectAwait(async item =>
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);

                return CommonHelper.EnsureMaximumLength(product.Name, 127);
            }).ToListAsync();

            string itemsStr = string.Join(",", itemList);

            var ReferenceId = CommonHelper.EnsureMaximumLength(orderGuid.ToString(), 256);
            var CustomId = CommonHelper.EnsureMaximumLength(orderGuid.ToString(), 127);
            var Description = CommonHelper.EnsureMaximumLength($"{store.Name} products: '{itemsStr}'", 256);
            var SoftDescriptor = CommonHelper.EnsureMaximumLength(store.Name, 22);

            payloadOrder.amount = strOrderTotal;
            payloadOrder.currency = currency;
            payloadOrder.email = CommonHelper.EnsureMaximumLength(billingAddress.Email, 254);
            payloadOrder.description = Description;
            payloadOrder.capture_mode = captureMode;

            var jsonPayload = JsonConvert.SerializeObject(payloadOrder);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var endpoint = settings.UseSandbox ?
                Defaults.SandboxURL :
                Defaults.ProductionURL;
            endpoint += "/api/1.0/orders";

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            var response = _httpClient.PostAsync(endpoint, content).Result;
            var jsonString = response.Content.ReadAsStringAsync().Result;
            RevolutOrder revolutOrder = JsonConvert.DeserializeObject<RevolutOrder>(jsonString);
            string errorMessage = "";

            if (revolutOrder.code == 1000 || revolutOrder.code == 1001)
            {
                errorMessage = "Internal Error";
            }

            revolutOrder.billingAddress = billingAddress;
            revolutOrder.countryCode = (await _countryService.GetCountryByIdAsync(billingAddress.CountryId ?? 0))?.TwoLetterIsoCode;

            return (revolutOrder, errorMessage);
        }
        #endregion
    }
}
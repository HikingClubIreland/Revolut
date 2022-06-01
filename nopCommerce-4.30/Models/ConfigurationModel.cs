using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Revolut.Models
{
    /// <summary>
    /// Represents configuration model
    /// </summary>
    public class ConfigurationModel : BaseNopModel
    {
        #region Ctor

        public ConfigurationModel()
        {
        }

        #endregion

        #region Properties

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Revolut.Fields.ApiKey")]
        [DataType(DataType.Password)]
        [NoTrim]
        public string ApiKey { get; set; }
        public bool ApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Revolut.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Revolut.Fields.UseCreditCard")]
        public bool UseCreditCard { get; set; }
        public bool UseCreditCard_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Revolut.Fields.CreditCardTitle")]
        public string CreditCardTitle { get; set; }
        public bool CreditCardTitle_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Revolut.Fields.UseRevolutPay")]
        public bool UseRevolutPay { get; set; }
        public bool UseRevolutPay_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Revolut.Fields.RevolutPayTitle")]
        public string RevolutPayTitle { get; set; }
        public bool RevolutPayTitle_OverrideForStore { get; set; }

        #endregion
    }
}
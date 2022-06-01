using Nop.Core.Domain.Common;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Revolut.Models
{
    /// <summary>
    /// Represents a payment info model
    /// </summary>
    public record PaymentInfoModel : BaseNopModel
    {
        #region Properties

        public string OrderId { get; set; }

        public string PublicId { get; set; }

        public string Errors { get; set; }

        public bool isCreditCard { get; set; }
        public bool isRevolutPay { get; set; }

        public string CreditCardTitle { get; set; }
        public string RevolutPayTitle { get; set; }

        public Address BillingAddress { get; set; }
        public string CountryCode { get; set; }

        public string environment { get; set; }

        #endregion
    }
}
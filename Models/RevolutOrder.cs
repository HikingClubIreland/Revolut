using Nop.Core.Domain.Common;
using System;

namespace Nop.Plugin.Payments.Revolut.Models
{
    public class RevolutOrder
    {
        #region Fields
        public string id { get; set; }
        public string public_id { get; set; }
        public string type { get; set; }
        public string state { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string description { get; set; }
        public string capture_mode { get; set; }
        public string customer_id { get; set; }
        public string email { get; set; }
        public OrderValueCurrency order_amount { get; set; }
        public OrderValueCurrency order_outstanding_amount { get; set; }
        public Object metadata { get; set; }
        public string timestamp { get; set; }
        public int code { get; set; }

        public Address billingAddress { get; set; }
        public string countryCode { get; set; }
        #endregion
    }
}

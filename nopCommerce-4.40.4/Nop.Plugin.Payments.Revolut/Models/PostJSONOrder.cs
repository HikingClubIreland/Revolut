using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.Revolut.Models
{
    class PostJSONOrder
    {
        public string amount { get; set; }
        public string currency { get; set; }
        public string email { get; set; }
        public string capture_mode { get; set; }
        public string description { get; set; }
        public string merchant_order_ext_ref { get; set; }

        public ShippingAddress shipping_address { get; set; }


        public override string ToString()
        {
            return $"amount: {amount}," +
                $"currency: {currency}," +                
                $"capture_mode: {capture_mode}," +
                $"merchant_order_ext_ref: {merchant_order_ext_ref}," +
                $"email: {email}," +                
                $"description: {description}," +
                $"shipping_address: {shipping_address?.ToString()}";
        }
    }
}

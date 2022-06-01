using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.Revolut.Models
{
    public class ShippingAddress
    {
        public string country_code { get; set; }
        public string postcode { get; set; }
        public string street_line_1 { get; set; }
        public string street_line_2 { get; set;}
        public string region { get; set; }
        public string city { get; set; }

        public override string ToString()
        {
            return $"country_code: {country_code}," +
                $"postcode: {postcode}," +
                $"street_line_1: {street_line_1}," +
                $"street_line_2: {street_line_2}," +
                $"region: {region}," +
                $"city: {city}";
        }
    }
}

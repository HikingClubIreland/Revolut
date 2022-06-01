using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.Revolut.Models
{
    class CaptureOrder
    {
        public int amount { get; set; }

        public override string ToString()
        {
            return $"amount: {amount}";
        }
    }
}

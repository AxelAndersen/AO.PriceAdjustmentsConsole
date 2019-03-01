using System;
using System.Collections.Generic;
using System.Text;

namespace AO.PriceAdjustments.Common
{
    public static class ExtensionMethods
    {
        public static string Presentation(this decimal dec)
        {
            return "DKK " + dec.ToString("N2");
        }
    }
}

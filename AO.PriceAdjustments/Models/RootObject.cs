using System;
using System.Collections.Generic;
using System.Text;

namespace AO.PriceAdjustments.Models
{
    public class RootObject
    {
        public List<Item> items { get; set; }
        public int count { get; set; }        
    }
}

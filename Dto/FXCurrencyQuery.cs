using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dto
{
    public class FXCurrencyQuery
    {
        public string StartingCurrencyCode { get; set; } 
        public string EndingCurrencyCodes { get; set; }
        public DateTime? StartingDate { get; set; } 
        public int DecimalPlaces { get; set; }
        public bool IsWoodBine { get; set; } = false;

    }
}

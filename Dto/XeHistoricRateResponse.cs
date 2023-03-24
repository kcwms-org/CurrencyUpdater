using System;
using System.Collections.Generic;
using System.Text;

namespace Dto
{

    public class XeHistoricRateResponse
    {
        public string Terms { get; set; }
        public string Privacy { get; set; }
        public string From { get; set; }
        public float Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public XeHistoricRateResponseRate[] to { get; set; }
    }

    public class XeHistoricRateResponseRate
    {
        public string Quotecurrency { get; set; }
        public decimal Mid { get; set; }
    }

}

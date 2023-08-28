using System;
using System.Collections;
using System.Collections.Generic;

namespace Dto
{
    /// <summary>
    /// defines the <see cref="IFixerIoHistoricRateResponse"/>.
    /// </summary>
    public class FixerIoHistoricRateResponse
    {
        public bool Success { get; set; }
        public bool Historical { get; set; }
        public DateTime Date { get; set; }
        public string Base { get; set; }
        public IDictionary<string, decimal> Rates { get; private set;} = new Dictionary<string, decimal>();
    }
}
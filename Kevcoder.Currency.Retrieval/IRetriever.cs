using Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kevcoder.Currency.Retrieval
{
    public interface IRetriever
    {   
        /// <summary>
        /// Gets the rate
        /// </summary>
        /// <param name="query"> The <see cref="FXCurrencyQuery"> query </param>
        /// <returns> The <see cref="IDictionary{string, decimal}"> rate where the key is the currency code and the value is the rate </returns>
       Task<IDictionary<string , decimal>> GetRateAsync(FXCurrencyQuery query);
    }
}

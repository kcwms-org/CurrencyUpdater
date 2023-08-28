using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dto;
using Microsoft.Extensions.Logging;

namespace Kevcoder.Currency.Retrieval
{
    /// <summary>
    /// Fixer.io implementation of the <see cref="IRetriever"/>
    /// </summary>
    public class FixerIoRetriever : IRetriever
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IApplicationCredentials _credentials;

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="httpClient">a <see cref="HttpClient"/> instance.</param>
        /// <param name="credentials"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FixerIoRetriever(HttpClient httpClient, IApplicationCredentials credentials, ILogger logger)
        {
            if (httpClient is null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (credentials is null)
            {
                throw new ArgumentNullException(nameof(credentials));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _httpClient = httpClient;
            _credentials = credentials;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(_credentials.BaseUrl);

        }

        public async Task<IDictionary<string, decimal>> GetRateAsync(FXCurrencyQuery query)
        {
            if (query is null)
            {
                _logger.LogError($"GetRateAsync: {nameof(query)} is null");
                throw new ArgumentNullException(nameof(query));
            }

             if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                _httpClient.DefaultRequestHeaders.Remove("Authorization");

            var urlQuery = $"access_key={_credentials.ApiKey}&base={query.StartingCurrencyCode}&symbols={string.Join(",", query.EndingCurrencyCodes)}";
            var response = await _httpClient.GetAsync(urlQuery);

            if (response?.IsSuccessStatusCode == false){
                var error = $"call to {urlQuery} retuned {response.StatusCode} with message {response.ReasonPhrase}";
                _logger.LogError(error);
                throw new ArgumentException(error);
            }
            else{
                var jsonContent = await response.Content.ReadAsStringAsync();
                var historicRates = JsonSerializer.Deserialize<Dto.FixerIoHistoricRateResponse>(jsonContent);

                if (historicRates?.Rates.Any() == false)
                    _logger.LogDebug($"the call to {urlQuery} returned no rates");


                _httpClient.BaseAddress = null;

                return historicRates?.Rates;
            }
        }
    }
}
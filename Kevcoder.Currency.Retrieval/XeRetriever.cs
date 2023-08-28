using Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kevcoder.Currency.Retrieval
{
    public class XeRetriever : IRetriever
    {
        private readonly ILogger _logger;
        private readonly HttpClient _http;
        private readonly IApplicationCredentials _creds;
        private readonly JsonSerializerOptions _jsonOpts;

        public XeRetriever(HttpClient httpClient, IApplicationCredentials credentials, ILogger logger)
        {
            _logger = logger;
            _jsonOpts = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            _http = httpClient;
            _creds = credentials;

        }

        public async Task<IDictionary<string, decimal>> GetRateAsync(FXCurrencyQuery query)
        {
            string error = null;
            if (query == null)
            {
                error = $"GetRateAsync: {nameof(query)} is null";
                _logger.LogDebug(error);
                throw new ArgumentException(error);
            }

            if (string.IsNullOrWhiteSpace(query.EndingCurrencyCodes))
            {
                error = $"GetRateAsync: {nameof(query.EndingCurrencyCodes)} is null/empty";
                _logger.LogDebug(error);
                throw new ArgumentException(error);
            }



            Dictionary<string, decimal> result = null;
            string jsonContent = null;

            if (string.IsNullOrWhiteSpace(query?.StartingCurrencyCode))
                query.StartingCurrencyCode = "USD";
            //we always want the previous days rate
            string dte = query.StartingDate.HasValue ? query.StartingDate.Value.AddDays(-1).ToString("yyyy-MM-dd") : DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            string to = query?.EndingCurrencyCodes;

            var path = $"/v1/historic_rate.json/?from={query.StartingCurrencyCode.Trim()}&to={to}&date={dte}&decimal_places={query.DecimalPlaces}";
            if (!_http.DefaultRequestHeaders.Contains("Authorization"))
                _http.DefaultRequestHeaders.Add("Authorization", $"Basic {_creds.GetAuthenticationString()}");
            var response = await _http.GetAsync(path);

            jsonContent = await response.Content.ReadAsStringAsync();

            XeHistoricRateResponse xeResponse = null;
            XeErrorResponse xeErrorResponse = null;

            if (response.IsSuccessStatusCode)
            {
                xeResponse = JsonSerializer.Deserialize<XeHistoricRateResponse>(jsonContent, _jsonOpts);

                if (xeResponse?.to == null || xeResponse.to.Length == 0)
                    _logger.LogDebug($"the call to {path} returned no rates");
                else
                    result = xeResponse.to.ToDictionary(k => k.Quotecurrency, v => v.Mid);
            }
            else
            {
                xeErrorResponse = JsonSerializer.Deserialize<XeErrorResponse>(jsonContent, _jsonOpts);
                error = $"call to {path} retuned {response.StatusCode} with message {xeErrorResponse.Message}";
                _logger.LogError(error);
                throw new ArgumentException(error);
            }

            //clean up after myself: I don't know where this thing is going next
            if (_http.DefaultRequestHeaders.Contains("Authorization"))
                _http.DefaultRequestHeaders.Remove("Authorization");

            return result ?? new Dictionary<string, decimal>(0);
        }
    }
}

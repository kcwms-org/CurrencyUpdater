using Dto;
using Kevcoder.Currency.Retrieval;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

namespace Kevcoder.FXService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _http;


        private readonly IApplicationCredentials _cred;
        private readonly FXCurrencyQuery _defaultQueryValues;
        private readonly Serviceconfiguration _svcConfig;
        private readonly IRetriever _retriever;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration configuration,
            HttpClient http,
            IApplicationCredentials credentials,
            FXCurrencyQuery fXCurrencyQuery,
            Serviceconfiguration serviceconfiguration,
            IRetriever retriever)
        {
            _logger = logger;
            _config = configuration;
            _http = http;

            _cred = credentials;
            _defaultQueryValues = fXCurrencyQuery;
            _svcConfig = serviceconfiguration;

            _retriever = retriever;

            if (_svcConfig.MinutesToWaitBetweenExecutions == 0)
            {
                _svcConfig.MinutesToWaitBetweenExecutions = 5;
                _logger.LogInformation("ServiceConfiguration:MinutesToWaitBetweenExecutions missing: using the defaults {numMinutes} minutes"
                , _svcConfig.MinutesToWaitBetweenExecutions);
            }

        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("starting...");
            _http.BaseAddress = new Uri(_cred.BaseUrl);
            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("stopping...");
            _http.Dispose();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Start(_cred, _defaultQueryValues);

                await Task.Delay(_svcConfig.MinutesToWaitBetweenExecutions * 60 * 1000, stoppingToken);
            }
        }

        public async Task Start(IApplicationCredentials credentials
            , FXCurrencyQuery defaultQueryValues)
        {
            var vendorResults = new List<(FXCurrencyQuery qry, decimal rate)>();

            var missingCurrencies = GetMissingCurrencyCodesForFixerIo(); //this.GetMissingCurrencyCodes();

            if (missingCurrencies.Count() > 0)
            {
                var errorCallingService = false;
                _logger.LogInformation($"found {missingCurrencies.Count()} missing currencies");

                foreach (var currencyQry in missingCurrencies)
                {
                    currencyQry.DecimalPlaces = defaultQueryValues.DecimalPlaces;
                    //we are converting from USD to xxx currency
                    currencyQry.StartingCurrencyCode = defaultQueryValues.StartingCurrencyCode;

                    try
                    {
                        var results = await _retriever.GetRateAsync(currencyQry);
                        if (results?.Count() == 0)
                            throw new ArgumentException("no results");
                        foreach (var result in results)
                        {
                            _logger.LogInformation("{StartingCurrencyCode} = {RetrievedRate} {EndingCurrencyCode} {SpecificDate}"
                            , new object[] { currencyQry.StartingCurrencyCode, result.Value, result.Key, currencyQry.StartingDate });
                            vendorResults.Add((currencyQry, result.Value));
                        }
                    }
                    catch (Exception rateRetrievalEx)
                    {
                        _logger.LogError("while calling {CurrencyApiEndpoint}: {Error}", new object[] { _http.BaseAddress, rateRetrievalEx });

                        errorCallingService = true;
                    }
                }

                if (errorCallingService)
                {
                    var currencyRpt = string.Join("\n", missingCurrencies.Select(mc => $"{mc.EndingCurrencyCodes} {mc.StartingDate}"));
                    this.NotifyByEmail(_svcConfig.ErrorEmailConfiguration
                    , $"One or more of the following currencies may be missing\n{currencyRpt}\nFailed Retrieving one or more rates from {_http.BaseAddress}");
                }
            }

            if (vendorResults.Count > 0)
            {
                var dbInsertResults = this.AddMissingCurrencyCodes(vendorResults);
                if (dbInsertResults?.Count() != missingCurrencies.Count())
                {
                    var successfulInserts = string.Join("\n", dbInsertResults.Select(x => $"{x.qry.EndingCurrencyCodes} {x.qry.StartingDate}"));
                    var missingCodes = string.Join("\n", missingCurrencies.Select(mc => $"{mc.EndingCurrencyCodes} {mc.StartingDate}"));


                    this.NotifyByEmail(_svcConfig.ErrorEmailConfiguration
                    , $"There were {missingCurrencies.Count()} missing codes.\n{missingCodes}\n"
                    + $"Only Successfully updated {dbInsertResults.Count()}.\n{successfulInserts}");
                }
                else
                {
                    var newPrimaryKeys = string.Join(",", dbInsertResults.Select(r => r.pk));
                    _logger.LogInformation($"added the following rows in the currency conversion table: {newPrimaryKeys}");
                }
            }


        }

        protected bool NotifyByEmail(EmailSettings email, string error)
        {
            bool wasSuccessful = true;

            if (email == null)
            {
                wasSuccessful = false;
                _logger.LogError("email object invalid", new object[] { error });
            }
            else
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_svcConfig.ErrorEmailConfiguration?.FromAddress ?? ""));

                if (_svcConfig.ErrorEmailConfiguration?.To?.Count() > 0)
                    message.To.AddRange(_svcConfig.ErrorEmailConfiguration.To.Select(e => MailboxAddress.Parse(e)));
                if (_svcConfig.ErrorEmailConfiguration?.CC?.Count() > 0)
                    message.Cc.AddRange(_svcConfig.ErrorEmailConfiguration.CC.Select(e => MailboxAddress.Parse(e)));

                message.Subject = _svcConfig.ErrorEmailConfiguration?.Subject;

                message.Body = new TextPart("plain")
                {
                    Text = $"The currency updater service failed\nDetails: {error}" + "\n\n\n **do not reply to this email address"
                };
                try
                {
                    using (var client = new SmtpClient())
                    {
                        client.Connect(_svcConfig.SMTPSettings?.Server
                        , _svcConfig.SMTPSettings?.Port ?? 587
                        , SecureSocketOptions.Auto);

                        // Note: only needed if the SMTP server requires authentication
                        if (!string.IsNullOrWhiteSpace(_svcConfig.SMTPSettings?.UserName))
                            client.Authenticate(_svcConfig.SMTPSettings?.UserName, _svcConfig.SMTPSettings?.Password);

                        client.Send(message);
                        client.Disconnect(true);
                    }
                }
                catch (Exception ex)
                {
                    wasSuccessful = false;
                    _logger.LogError("failed sending {ErrorMsg} with error {SmtpError}", new object[] { error, ex });
                }
            }

            return wasSuccessful;
        }

        #region database
        protected IEnumerable<FXCurrencyQuery> GetMissingCurrencyCodes()
        {
            var results = new List<FXCurrencyQuery>();

            int countryIdx = -1;
            int dateIdx = -1;
            int isWoodbineIdx = -1;
            try
            {
                using (var con = new SqlConnection(_svcConfig.databaseConnectionString))
                {
                    con.Open();
                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = "[dbo].[spFxService_GetMissingCurrencyCodes]";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = _svcConfig.sqlCommandTimeout;

                        using (var r = cmd.ExecuteReader())
                        {
                            countryIdx = r.GetOrdinal("CurrencyCode");
                            dateIdx = r.GetOrdinal("Date");
                            isWoodbineIdx = r.GetOrdinal("IsWoodbine");

                            while (r.Read())
                            {
                                results.Add(new FXCurrencyQuery()
                                {
                                    EndingCurrencyCodes = r.GetString(countryIdx),
                                    StartingDate = r.GetDateTime(dateIdx),
                                    IsWoodBine = r.GetBoolean(isWoodbineIdx)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetMissingCurrencyCodes error: {ex.ToString()}");
            }

            return results;
        }

        protected IEnumerable<FXCurrencyQuery> GetMissingCurrencyCodesForFixerIo()
        {
            var results = new List<(FXCurrencyQuery qry, decimal rate)>();
            _http.BaseAddress = new Uri("http://localhost:3000/");
            var resultContent = _http.GetStringAsync("currencies").Result;

            var missingCodes = new List<FXCurrencyQuery>();

            var currentCodes = JsonSerializer.Deserialize<FXCurrencyQuery[]>(resultContent);
            if (currentCodes?.Length > 0)
            {
                 missingCodes.AddRange( currentCodes.Where(c => c.DecimalPlaces == 0));
            }

            return missingCodes;
        }
        protected IEnumerable<(FXCurrencyQuery qry, int pk, bool wasSuccessful)> AddMissingCurrencyCodes(IEnumerable<(FXCurrencyQuery qry, decimal rate)> newCodes)
        {
            List<(FXCurrencyQuery qry, int pk, bool wasSuccessful)> results = new List<(FXCurrencyQuery qry, int pk, bool wasSuccessful)>();

            try
            {
                using (var con = new SqlConnection(_svcConfig.databaseConnectionString))
                {
                    con.Open();
                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = "[dbo].[spFxService_CurrencyConverter_Add]";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = _svcConfig.sqlCommandTimeout;


                        var pk = cmd.CreateParameter();
                        pk.DbType = DbType.Int32;
                        pk.Direction = ParameterDirection.InputOutput;
                        pk.ParameterName = "CurrencyConverterID";

                        foreach (var code in newCodes)
                        {
                            cmd.Parameters.Add(pk);

                            cmd.Parameters.AddWithValue("StartingCurrencyCode", code.qry.EndingCurrencyCodes);
                            cmd.Parameters.AddWithValue("EndingCurrencyCode", code.qry.StartingCurrencyCode);
                            cmd.Parameters.AddWithValue("Rate", code.rate);
                            cmd.Parameters.AddWithValue("Date", code.qry.StartingDate);
                            cmd.Parameters.AddWithValue("IsWoodBine", code.qry.IsWoodBine);

                            using (var r = cmd.ExecuteReader())
                            {
                                results.Add((code.qry, (int)pk.Value, ((int)pk.Value) > 0));
                            }

                            cmd.Parameters.Clear();
                            pk.Value = null;

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AddMissingCurrencyCodes\nerror: {ex.ToString()}\ninput: {JsonSerializer.Serialize(newCodes, newCodes.GetType())}");
            }

            return results;
        }

        #endregion
    }
}

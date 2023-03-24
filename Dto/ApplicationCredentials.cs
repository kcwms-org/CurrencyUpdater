using System;
using System.Text;

namespace Dto
{
    public class ApplicationCredentials : IApplicationCredentials
    {
        public string Id { get; set; }
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public bool IsTestKey { get; set; }
        public string GetAuthenticationString()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException("Invalid/Missing Credentials");

            return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Id}:{ApiKey}"));
        }
    }
}

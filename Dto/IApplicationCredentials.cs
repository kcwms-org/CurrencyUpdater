namespace Dto
{
    public interface IApplicationCredentials
    {
        /// <summary>
        /// Gets/Sets the Id
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Gets/Sets the ApiKey
        /// </summary>
        string ApiKey { get; }
        /// <summary>
        /// Gets/Sets the IsTestKey
        /// </summary>
        bool IsTestKey { get; }     
        /// <summary>
        /// Gets/Sets the BaseUrl
        /// </summary>
        string BaseUrl { get; set; }

        /// <summary>
        /// gets the GetAuthenticationString
        /// </summary>
        /// <returns></returns>
        string GetAuthenticationString();
    }
}
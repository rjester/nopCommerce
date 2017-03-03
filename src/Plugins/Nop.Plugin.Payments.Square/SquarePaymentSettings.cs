using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Square
{
    /// <summary>
    /// Square payment settings
    /// </summary>
    public class SquarePaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the application id
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the secret key
        /// </summary>
        public string AccessToken { get; set; }

        public string LocationId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets the sandbox application id
        /// </summary>
        public string SandboxApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the sandbox access token
        /// </summary>
        public string SandboxAccessToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to pass info about purchased items to Square
        /// </summary>
        public bool PassPurchasedItems{ get; set; }
    }
}

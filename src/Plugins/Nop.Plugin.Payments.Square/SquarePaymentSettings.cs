using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Square
{
    /// <summary>
    /// PayPal Direct payment settings
    /// </summary>
    public class SquarePaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the client id
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
        /// Gets or sets a value indicating whether to pass info about purchased items to PayPal
        /// </summary>
        public bool PassPurchasedItems{ get; set; }
    }
}

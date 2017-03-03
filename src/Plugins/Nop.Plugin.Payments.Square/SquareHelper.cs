using System.Collections.Generic;
using Square.Connect.Api;
using Nop.Plugin.Payments.Square;

namespace Nop.Plugin.Payments.Square
{
    /// <summary>
    /// Represents square helper
    /// </summary>
    public class SquareHelper
    {
        #region Constants

        /// <summary>
        /// nopCommerce partner code
        /// </summary>
        private const string BN_CODE = "nopCommerce_SP";

        #endregion

        #region Methods

        /// <summary>
        /// Get Square Api context 
        /// </summary>
        /// <param name="squarePaymentSettings">SquarePayment settings</param>
        /// <returns>ApiContext</returns>
        //public static APIContext GetApiContext(SquarePaymentSettings squarePaymentSettings)
        //{
        //    var mode = squarePaymentSettings.UseSandbox ? "sandbox" : "live";

        //    var config = new Dictionary<string, string>
        //    {
        //        { "applicationId", squarePaymentSettings.ApplicationId },
        //        { "accessToken", squarePaymentSettings.AccessToken },
        //        { "mode", mode }
        //    };

        //    var accessToken = new OAuthTokenCredential(config).GetAccessToken();
        //    var apiContext = new APIContext(accessToken) { Config = config };

        //    if (apiContext.HTTPHeaders == null)
        //        apiContext.HTTPHeaders = new Dictionary<string, string>();
        //    apiContext.HTTPHeaders["PayPal-Partner-Attribution-Id"] = BN_CODE;

        //    return apiContext;
        //}

        #endregion
    }
}


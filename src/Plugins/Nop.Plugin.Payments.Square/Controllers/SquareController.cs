using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
//using Nop.Plugin.Payments.PayPalDirect.Validators;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Plugin.Payments.Square.Models;
//using PayPal.Api;
using Square.Connect;

namespace Nop.Plugin.Payments.Square.Controllers
{
    public class SquareController : BasePaymentController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;
        private readonly SquarePaymentSettings _squarePaymentSettings;

        #endregion

        #region Ctor

        public SquareController(ILocalizationService localizationService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            ISettingService settingService,
            IStoreContext storeContext,
            IStoreService storeService,
            IWorkContext workContext,
            IWebHelper webHelper,
            SquarePaymentSettings squarePaymentSettings)
        {
            this._localizationService = localizationService;
            this._logger = logger;
            this._orderProcessingService = orderProcessingService;
            this._orderService = orderService;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._storeService = storeService;
            this._workContext = workContext;
            this._webHelper = webHelper;
            this._squarePaymentSettings = squarePaymentSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Create webhook that receive events for the subscribed event types
        /// </summary>
        /// <returns>Webhook id</returns>
        //protected string CreateWebHook()
        //{
        //    var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
        //    var payPalDirectPaymentSettings = _settingService.LoadSetting<PayPalDirectPaymentSettings>(storeScope);

        //    try
        //    {
        //        var apiContext = PaypalHelper.GetApiContext(payPalDirectPaymentSettings);
        //        if (!string.IsNullOrEmpty(payPalDirectPaymentSettings.WebhookId))
        //        {
        //            try
        //            {
        //                return Webhook.Get(apiContext, payPalDirectPaymentSettings.WebhookId).id;
        //            }
        //            catch (PayPal.PayPalException) { }
        //        }

        //        var currentStore = storeScope > 0 ? _storeService.GetStoreById(storeScope) : _storeContext.CurrentStore;
        //        var webhook = new Webhook
        //        {
        //            event_types = new List<WebhookEventType> { new WebhookEventType { name = "*" } },
        //            url = string.Format("{0}Plugins/PaymentPayPalDirect/Webhook", _webHelper.GetStoreLocation(currentStore.SslEnabled))
        //        }.Create(apiContext);

        //        return webhook.id;
        //    }
        //    catch (PayPal.PayPalException exc)
        //    {
        //        if (exc is PayPal.ConnectionException)
        //        {
        //            var error = JsonFormatter.ConvertFromJson<Error>((exc as PayPal.ConnectionException).Response);
        //            if (error != null)
        //            {
        //                _logger.Error(string.Format("PayPal error: {0} ({1})", error.message, error.name));
        //                if (error.details != null)
        //                    error.details.ForEach(x => _logger.Error(string.Format("{0} {1}", x.field, x.issue)));
        //            }
        //            else
        //                _logger.Error(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
        //        }
        //        else
        //            _logger.Error(exc.InnerException != null ? exc.InnerException.Message : exc.Message);

        //        return string.Empty;
        //    }
        //}

        #endregion

        #region Methods

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var squarePaymentSettings = _settingService.LoadSetting<SquarePaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ApplicationId = squarePaymentSettings.ApplicationId,
                AccessToken = squarePaymentSettings.AccessToken,
                LocationId = squarePaymentSettings.LocationId,
                UseSandbox = squarePaymentSettings.UseSandbox,
                SandboxApplicationId = squarePaymentSettings.SandboxApplicationId,
                SandboxAccessToken = squarePaymentSettings.SandboxAccessToken,
                SandboxLocationId = squarePaymentSettings.SandboxLocationId,
                PassPurchasedItems = squarePaymentSettings.PassPurchasedItems,
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.ApplicationId_OverrideForStore = _settingService.SettingExists(squarePaymentSettings, x => x.ApplicationId, storeScope);
                model.AccessToken_OverrideForStore = _settingService.SettingExists(squarePaymentSettings, x => x.AccessToken, storeScope);
                model.LocationId_OverrideForStore = _settingService.SettingExists(squarePaymentSettings, x => x.LocationId, storeScope);
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(squarePaymentSettings, x => x.UseSandbox, storeScope);
                model.SandboxApplicationId_OverrideForStore = _settingService.SettingExists(squarePaymentSettings, x => x.SandboxApplicationId, storeScope);
                model.SandboxAccessToken_OverrideForStore = _settingService.SettingExists(squarePaymentSettings, x => x.SandboxAccessToken, storeScope);
                model.SandboxLocationId_OverrideForStore = _settingService.SettingExists(squarePaymentSettings, x => x.SandboxLocationId, storeScope);
                model.PassPurchasedItems_OverrideForStore = _settingService.SettingExists(squarePaymentSettings, x => x.PassPurchasedItems, storeScope);
            }

            return View("~/Plugins/Payments.Square/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var squarePaymentSettings = _settingService.LoadSetting<SquarePaymentSettings>(storeScope);

            ////save settings
            squarePaymentSettings.ApplicationId = model.ApplicationId;
            squarePaymentSettings.AccessToken = model.AccessToken;
            squarePaymentSettings.UseSandbox = model.UseSandbox;
            squarePaymentSettings.SandboxApplicationId = model.SandboxApplicationId;
            squarePaymentSettings.SandboxAccessToken = model.SandboxAccessToken;
            squarePaymentSettings.PassPurchasedItems = model.PassPurchasedItems;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(squarePaymentSettings, x => x.ApplicationId, model.ApplicationId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(squarePaymentSettings, x => x.AccessToken, model.AccessToken_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(squarePaymentSettings, x => x.LocationId, model.LocationId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(squarePaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(squarePaymentSettings, x => x.SandboxApplicationId, model.SandboxApplicationId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(squarePaymentSettings, x => x.SandboxAccessToken, model.SandboxAccessToken_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(squarePaymentSettings, x => x.SandboxLocationId, model.SandboxLocationId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(squarePaymentSettings, x => x.PassPurchasedItems, model.PassPurchasedItems_OverrideForStore, storeScope, false);
            
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
                var model = new PaymentInfoModel();

            //    model.CreditCardTypes = new List<SelectListItem>
            //    {
            //        new SelectListItem { Text = "Visa", Value = "visa" },
            //        new SelectListItem { Text = "Master card", Value = "MasterCard" },
            //        new SelectListItem { Text = "Discover", Value = "Discover" },
            //        new SelectListItem { Text = "Amex", Value = "Amex" },
            //    };

            //    //years
            //    for (var i = 0; i < 15; i++)
            //    {
            //        var year = (DateTime.Now.Year + i).ToString();
            //        model.ExpireYears.Add(new SelectListItem
            //        {
            //            Text = year,
            //            Value = year,
            //        });
            //    }

            //    //months
            //    for (var i = 1; i <= 12; i++)
            //    {
            //        model.ExpireMonths.Add(new SelectListItem
            //        {
            //            Text = i.ToString("D2"),
            //            Value = i.ToString(),
            //        });
            //    }

            //    //set postback values
            //    model.CardNumber = Request.Form["CardNumber"];
            //    model.CardCode = Request.Form["CardCode"];
            //    var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(Request.Form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase));
            //    if (selectedCcType != null)
            //        selectedCcType.Selected = true;
            //    var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(Request.Form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
            //    if (selectedMonth != null)
            //        selectedMonth.Selected = true;
            //    var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(Request.Form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
            //    if (selectedYear != null)
            //        selectedYear.Selected = true;

            string sandboxId = _squarePaymentSettings.SandboxApplicationId;


            return View("~/Plugins/Payments.Square/Views/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

        //    //validate
        //    var validator = new PaymentInfoValidator(_localizationService);
        //    var model = new PaymentInfoModel
        //    {
        //        CardNumber = form["CardNumber"],
        //        CardCode = form["CardCode"],
        //        ExpireMonth = form["ExpireMonth"],
        //        ExpireYear = form["ExpireYear"]
        //    };
        //    var validationResult = validator.Validate(model);
        //    if (!validationResult.IsValid)
        //        warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            return new ProcessPaymentRequest
            {
                CreditCardType = form["CreditCardType"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            };
        }
        #endregion
    }
}
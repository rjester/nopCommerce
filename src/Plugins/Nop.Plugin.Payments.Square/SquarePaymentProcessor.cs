using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.Square.Controllers;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using Nop.Core.Domain.Common;
using SqModel = Square.Connect.Model;
using Square.Connect.Api;
using Square.Connect.Client;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Services.Logging;

namespace Nop.Plugin.Payments.Square
{
    /// <summary>
    /// Square payment processor
    /// </summary>
    public class SquarePaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly SquarePaymentSettings _squarePaymentSettings;
        private readonly IWorkContext _workContext;
        private readonly ITransactionApi _transactionApi;
        private readonly IRefundApi _refundApi;
        private readonly ICheckoutApi _checkoutApi;
        private readonly ILogger _logger;

        public SquarePaymentProcessor(CurrencySettings currencySettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            ICustomerService customerService,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPaymentService paymentService,
            IPriceCalculationService priceCalculationService,
            IProductAttributeParser productAttributeParser,
            ISettingService settingService,
            IStoreContext storeContext,
            ITaxService taxService,
            IWebHelper webHelper,
            IWorkContext workContext,
            ILogger logger,
            SquarePaymentSettings squarePaymentSettings)
        {
            this._currencySettings = currencySettings;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._localizationService = localizationService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._paymentService = paymentService;
            this._priceCalculationService = priceCalculationService;
            this._productAttributeParser = productAttributeParser;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._workContext = workContext;
            this._squarePaymentSettings = squarePaymentSettings;
            this._logger = logger;

            this._transactionApi = new TransactionApi((Configuration)null);
            this._refundApi = new RefundApi((Configuration)null);
            this._checkoutApi = new CheckoutApi((Configuration)null);
            /// TODO: is this needed?
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        ///// <summary>
        ///// Gets a payment status
        ///// </summary>
        ///// <param name="state">PayPal state</param>
        ///// <returns>Payment status</returns>
        //protected PaymentStatus GetPaymentStatus(string state)
        //{
        //    state = state ?? string.Empty;
        //    var result = PaymentStatus.Pending;

        //    switch (state.ToLowerInvariant())
        //    {
        //        case "pending":
        //            result = PaymentStatus.Pending;
        //            break;
        //        case "authorized":
        //            result = PaymentStatus.Authorized;
        //            break;
        //        case "captured":
        //        case "completed":
        //            result = PaymentStatus.Paid;
        //            break;
        //        case "expired":
        //        case "voided":
        //            result = PaymentStatus.Voided;
        //            break;
        //        case "refunded":
        //            result = PaymentStatus.Refunded;
        //            break;
        //        case "partially_refunded":
        //            result = PaymentStatus.PartiallyRefunded;
        //            break;
        //        default:
        //            break;
        //    }

        //    return result;
        //}

        ///// <summary>
        ///// Get start date of recurring payments
        ///// </summary>
        ///// <param name="period">Cycle period</param>
        ///// <param name="length">Cycle length</param>
        ///// <returns>Start date in ISO8601 format</returns>
        //protected string GetStartDate(RecurringProductCyclePeriod period, int length)
        //{
        //    //PayPal expects date in PDT timezone (UTC -7)
        //    var startDate = DateTime.UtcNow.AddHours(-7);
        //    switch (period)
        //    {
        //        case RecurringProductCyclePeriod.Days:
        //            startDate = startDate.AddDays(length);
        //            break;
        //        case RecurringProductCyclePeriod.Weeks:
        //            startDate = startDate.AddDays(length * 7);
        //            break;
        //        case RecurringProductCyclePeriod.Months:
        //            startDate = startDate.AddMonths(length);
        //            break;
        //        case RecurringProductCyclePeriod.Years:
        //            startDate = startDate.AddYears(length);
        //            break;
        //    }

        //    return string.Format("{0}Z", startDate.ToString("s"));
        //}

        ///// <summary>
        ///// Get PayPal items
        ///// </summary>
        ///// <param name="shoppingCart">Shopping cart</param>
        ///// <param name="customer">Customer</param>
        ///// <param name="storeId">Store identifier</param>
        ///// <param name="currencyCode">Currency code</param>
        ///// <returns>List of PayPal items</returns>
        //protected List<Item> GetItems(IList<ShoppingCartItem> shoppingCart, Customer customer, int storeId, string currencyCode)
        //{
        //    var items = new List<Item>();

        //    if (!_squarePaymentSettings.PassPurchasedItems)
        //        return items;

        //    //create PayPal items from shopping cart items
        //    items.AddRange(CreateItems(shoppingCart));

        //    //create PayPal items from checkout attributes
        //    items.AddRange(CreateItemsForCheckoutAttributes(customer, storeId));

        //    //create PayPal item for payment method additional fee
        //    items.Add(CreateItemForPaymentAdditionalFee(shoppingCart, customer));

        //    //currently there are no ways to add discount for all order directly to amount details, so we add them as extra items 
        //    //create PayPal item for subtotal discount
        //    items.Add(CreateItemForSubtotalDiscount(shoppingCart));

        //    //create PayPal item for total discount
        //    items.Add(CreateItemForTotalDiscount(shoppingCart));

        //    items.RemoveAll(item => item == null);

        //    //add currency code for all items
        //    items.ForEach(item => item.currency = currencyCode);

        //    return items;
        //}

        ///// <summary>
        ///// Create items from shopping cart
        ///// </summary>
        ///// <param name="shoppingCart">Shopping cart</param>
        ///// <returns>Collection of PayPal items</returns>
        //protected IEnumerable<Item> CreateItems(IEnumerable<ShoppingCartItem> shoppingCart)
        //{
        //    return shoppingCart.Select(shoppingCartItem =>
        //    {
        //        if (shoppingCartItem.Product == null)
        //            return null;

        //        var item = new Item();

        //        //name
        //        item.name = shoppingCartItem.Product.Name;

        //        //sku
        //        if (!string.IsNullOrEmpty(shoppingCartItem.AttributesXml))
        //        {
        //            var combination = _productAttributeParser.FindProductAttributeCombination(shoppingCartItem.Product, shoppingCartItem.AttributesXml);
        //            item.sku = combination != null && !string.IsNullOrEmpty(combination.Sku) ? combination.Sku : shoppingCartItem.Product.Sku;
        //        }
        //        else
        //            item.sku = shoppingCartItem.Product.Sku;

        //        //item price
        //        decimal taxRate;
        //        var unitPrice = _priceCalculationService.GetUnitPrice(shoppingCartItem);
        //        var price = _taxService.GetProductPrice(shoppingCartItem.Product, unitPrice, false, shoppingCartItem.Customer, out taxRate);
        //        item.price = price.ToString("N", new CultureInfo("en-US"));

        //        //quantity
        //        item.quantity = shoppingCartItem.Quantity.ToString();

        //        return item;
        //    });
        //}

        ///// <summary>
        ///// Create items for checkout attributes
        ///// </summary>
        ///// <param name="customer">Customer</param>
        ///// <param name="storeId">Store identifier</param>
        ///// <returns>Collection of PayPal items</returns>
        //protected IEnumerable<Item> CreateItemsForCheckoutAttributes(Customer customer, int storeId)
        //{
        //    var checkoutAttributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, storeId);
        //    if (string.IsNullOrEmpty(checkoutAttributesXml))
        //        return new List<Item>();

        //    //get attribute values
        //    var attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(checkoutAttributesXml);

        //    return attributeValues.Select(checkoutAttributeValue =>
        //    {
        //        if (checkoutAttributeValue.CheckoutAttribute == null)
        //            return null;

        //        //get price
        //        var attributePrice = _taxService.GetCheckoutAttributePrice(checkoutAttributeValue, false, customer);

        //        //create item
        //        return new Item
        //        {
        //            name = string.Format("{0} ({1})", checkoutAttributeValue.CheckoutAttribute.Name, checkoutAttributeValue.Name),
        //            price = attributePrice.ToString("N", new CultureInfo("en-US")),
        //            quantity = "1"
        //        };
        //    });
        //}

        ///// <summary>
        ///// Create item for payment method additional fee
        ///// </summary>
        ///// <param name="shoppingCart">Shopping cart</param>
        ///// <param name="customer">Customer</param>
        ///// <returns>PayPal item</returns>
        //protected Item CreateItemForPaymentAdditionalFee(IList<ShoppingCartItem> shoppingCart, Customer customer)
        //{
        //    //get price
        //    var paymentAdditionalFee = _paymentService.GetAdditionalHandlingFee(shoppingCart, PluginDescriptor.SystemName);
        //    var paymentPrice = _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, false, customer);

        //    if (paymentPrice <= decimal.Zero)
        //        return null;

        //    //create item
        //    return new Item
        //    {
        //        name = string.Format("Payment method ({0}) additional fee", PluginDescriptor.FriendlyName),
        //        price = paymentPrice.ToString("N", new CultureInfo("en-US")),
        //        quantity = "1"
        //    };
        //}

        ///// <summary>
        ///// Create item for discount to order subtotal
        ///// </summary>
        ///// <param name="shoppingCart">Shopping cart</param>
        ///// <returns>PayPal item</returns>
        //protected Item CreateItemForSubtotalDiscount(IList<ShoppingCartItem> shoppingCart)
        //{
        //    //get subtotal discount amount
        //    decimal discountAmount;
        //    List<DiscountForCaching> discounts;
        //    decimal withoutDiscount;
        //    decimal subtotal;
        //    _orderTotalCalculationService.GetShoppingCartSubTotal(shoppingCart, false, out discountAmount, out discounts, out withoutDiscount, out subtotal);

        //    if (discountAmount <= decimal.Zero)
        //        return null;

        //    //create item with negative price
        //    return new Item
        //    {
        //        name = "Discount for the subtotal of order",
        //        price = (-discountAmount).ToString("N", new CultureInfo("en-US")),
        //        quantity = "1"
        //    };
        //}

        ///// <summary>
        ///// Create item for discount to order total 
        ///// </summary>
        ///// <param name="shoppingCart">Shopping cart</param>
        ///// <returns>PayPal item</returns>
        //protected Item CreateItemForTotalDiscount(IList<ShoppingCartItem> shoppingCart)
        //{
        //    //get total discount amount
        //    decimal discountAmount;
        //    List<AppliedGiftCard> giftCards;
        //    List<DiscountForCaching> discounts;
        //    int rewardPoints;
        //    decimal rewardPointsAmount;
        //    var orderTotal = _orderTotalCalculationService.GetShoppingCartTotal(shoppingCart, out discountAmount,
        //        out discounts, out giftCards, out rewardPoints, out rewardPointsAmount);

        //    if (discountAmount <= decimal.Zero)
        //        return null;

        //    //create item with negative price
        //    return new Item
        //    {
        //        name = "Discount for the total of order",
        //        price = (-discountAmount).ToString("N", new CultureInfo("en-US")),
        //        quantity = "1"
        //    };
        //}

        ///// <summary>
        ///// Get transaction amount details
        ///// </summary>
        ///// <param name="paymentRequest">Payment info required for an order processing</param>
        ///// <param name="shoppingCart">Shopping cart</param>
        ///// <param name="items">List of PayPal items</param>
        ///// <returns>Amount details object</returns>
        //protected Details GetAmountDetails(ProcessPaymentRequest paymentRequest, IList<ShoppingCartItem> shoppingCart, IList<Item> items)
        //{
        //    //get shipping total
        //    var shipping = _orderTotalCalculationService.GetShoppingCartShippingTotal(shoppingCart, false);
        //    var shippingTotal = shipping.HasValue ? shipping.Value : 0;

        //    //get tax total
        //    SortedDictionary<decimal, decimal> taxRatesDictionary;
        //    var taxTotal = _orderTotalCalculationService.GetTaxTotal(shoppingCart, out taxRatesDictionary);

        //    //get subtotal
        //    var subTotal = decimal.Zero;
        //    if (items != null && items.Any())
        //    {
        //        //items passed to PayPal, so calculate subtotal based on them
        //        var tmpPrice = decimal.Zero;
        //        var tmpQuantity = 0;
        //        subTotal = items.Sum(item => !decimal.TryParse(item.price, out tmpPrice) || !int.TryParse(item.quantity, out tmpQuantity) ? 0 : tmpPrice * tmpQuantity);
        //    }
        //    else
        //        subTotal = paymentRequest.OrderTotal - shippingTotal - taxTotal;

        //    //adjust order total to avoid PayPal payment error: "Transaction amount details (subtotal, tax, shipping) must add up to specified amount total"
        //    paymentRequest.OrderTotal = Math.Round(shippingTotal, 2) + Math.Round(subTotal, 2) + Math.Round(taxTotal, 2);

        //    //create amount details
        //    return new Details
        //    {
        //        shipping = shippingTotal.ToString("N", new CultureInfo("en-US")),
        //        subtotal = subTotal.ToString("N", new CultureInfo("en-US")),
        //        tax = taxTotal.ToString("N", new CultureInfo("en-US"))
        //    };
        //}

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            // get nonce
            string cardNonce = processPaymentRequest.CustomValues["Authorization"].ToString();
            Customer customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            if (customer == null)
                throw new Exception("Customer cannot be loaded");

            try
            {
                Currency currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
                Address nopBillingAddress = _workContext.CurrentCustomer.BillingAddress;
                Address nopShippingAddress = _workContext.CurrentCustomer.ShippingAddress;
                SqModel.Address billingAddress = new SqModel.Address();
                SqModel.Address shippingAddress = new SqModel.Address();
                var shoppingCart = _workContext.CurrentCustomer.ShoppingCartItems.Where(shoppingCartItem => shoppingCartItem.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                var chargeAmount = (long?)Math.Ceiling(processPaymentRequest.OrderTotal * new decimal(100));
                var amountMoney = new SqModel.Money(chargeAmount, (SqModel.Money.CurrencyEnum)Enum.Parse(typeof(SqModel.Money.CurrencyEnum), currency.CurrencyCode));
                string customerCardId = null;
                // add authorizeonly to the settings
                //bool? nullable1 = new bool?(_squarePaymentSettings.AuthorizeOnly);
                // default value is false
                bool? delayCapture = false;

                // use as idempotency key and reference id
                Guid orderGuid = processPaymentRequest.OrderGuid;
                string idempotencyKey = orderGuid.ToString();
                string note = null;
                string customerId = null;

                if (nopBillingAddress != null)
                {
                    billingAddress.AddressLine1 = nopBillingAddress.Address1 != null ? nopBillingAddress.Address1 : String.Empty;
                    billingAddress.AddressLine2 = nopBillingAddress.Address2 != null ? nopBillingAddress.Address2 : String.Empty;
                    billingAddress.Locality = nopBillingAddress.City != null ? nopBillingAddress.City : String.Empty;
                    billingAddress.AdministrativeDistrictLevel1 = nopBillingAddress.StateProvince != null ? nopBillingAddress.StateProvince.Abbreviation : String.Empty;
                    billingAddress.PostalCode = nopBillingAddress.ZipPostalCode != null ? nopBillingAddress.ZipPostalCode : String.Empty;
                    billingAddress.Country = nopBillingAddress.Country != null ? 
                                            (SqModel.Address.CountryEnum)Enum.Parse(typeof(SqModel.Address.CountryEnum), nopBillingAddress.Country.TwoLetterIsoCode) : 
                                            default(SqModel.Address.CountryEnum?);
                    billingAddress.FirstName = nopBillingAddress.FirstName != null ? nopBillingAddress.FirstName : String.Empty;
                    billingAddress.LastName = nopBillingAddress.LastName != null ? nopBillingAddress.LastName : String.Empty;


                    //SqModel.Address billingAddress = new SqModel.Address(nopBillingAddress.Address1, nopBillingAddress.Address2, Locality: nopBillingAddress.City,
                    //                                            AdministrativeDistrictLevel1: nopBillingAddress.StateProvince.Abbreviation, PostalCode: nopBillingAddress.ZipPostalCode,
                    //                                            Country: (SqModel.Address.CountryEnum)Enum.Parse(typeof(SqModel.Address.CountryEnum), nopBillingAddress.Country.TwoLetterIsoCode),
                    //                                            FirstName: nopBillingAddress.FirstName, LastName: nopBillingAddress.LastName);
                }

                if (nopShippingAddress != null)
                {
                    shippingAddress.AddressLine1 = nopShippingAddress.Address1 != null ? nopShippingAddress.Address1 : String.Empty;
                    shippingAddress.AddressLine2 = nopShippingAddress.Address2 != null ? nopShippingAddress.Address2 : String.Empty;
                    shippingAddress.Locality = nopShippingAddress.City != null ? nopShippingAddress.City : String.Empty;
                    shippingAddress.AdministrativeDistrictLevel1 = nopShippingAddress.StateProvince != null ? nopShippingAddress.StateProvince.Abbreviation : String.Empty;
                    shippingAddress.PostalCode = nopShippingAddress.ZipPostalCode != null ? nopShippingAddress.ZipPostalCode : String.Empty;
                    shippingAddress.Country = nopShippingAddress.Country != null ?
                                            (SqModel.Address.CountryEnum)Enum.Parse(typeof(SqModel.Address.CountryEnum), nopShippingAddress.Country.TwoLetterIsoCode) :
                                            default(SqModel.Address.CountryEnum?);
                    shippingAddress.FirstName = nopShippingAddress.FirstName != null ? nopShippingAddress.FirstName : String.Empty;
                    shippingAddress.LastName = nopShippingAddress.LastName != null ? nopShippingAddress.LastName : String.Empty;
                    //    shippingAddress.AddressLine1 = nopShippingAddress.Address1 != null ? nopShippingAddress.Address1 : String.Empty;
                    //    //shippingAddress = new SqModel.Address(nopShippingAddress.Address1, nopShippingAddress.Address2, Locality: nopShippingAddress.City,
                    //    //                                            AdministrativeDistrictLevel1: nopShippingAddress.StateProvince.Abbreviation, PostalCode: nopShippingAddress.ZipPostalCode,
                    //    //                                            Country: (SqModel.Address.CountryEnum)Enum.Parse(typeof(SqModel.Address.CountryEnum), nopShippingAddress.Country.TwoLetterIsoCode),
                    //    //                                            FirstName: nopShippingAddress.FirstName, LastName: nopShippingAddress.LastName);
                }
                string buyerEmailAddress = customer.Email;
                SqModel.ChargeRequest chargeRequest = new SqModel.ChargeRequest(idempotencyKey, amountMoney, cardNonce, customerCardId, delayCapture, idempotencyKey, note,
                                                                    customerId, billingAddress, shippingAddress, buyerEmailAddress);


                //string applicationId;
                string accessToken;
                string locationId;
                if (_squarePaymentSettings.UseSandbox)
                {
                    //applicationId = _squarePaymentSettings.SandboxApplicationId;
                    accessToken = _squarePaymentSettings.SandboxAccessToken;
                    locationId = _squarePaymentSettings.SandboxLocationId;
                }
                else
                {
                    //applicationId = _squarePaymentSettings.ApplicationId;
                    accessToken = _squarePaymentSettings.AccessToken;
                    locationId = _squarePaymentSettings.LocationId;
                }

                if (_squarePaymentSettings.PassPurchasedItems)
                {
                    List<SqModel.CreateOrderRequestLineItem> lineItems = new List<SqModel.CreateOrderRequestLineItem>();
                    foreach (var shoppingCartItem in shoppingCart)
                    {
                        var lineItemAmount = (long?)Math.Ceiling(shoppingCartItem.Product.Price * new decimal(100));
                        SqModel.CreateOrderRequestLineItem lineItem = new SqModel.CreateOrderRequestLineItem(
                            shoppingCartItem.Product.Name,
                            shoppingCartItem.Quantity.ToString(),
                            new SqModel.Money(lineItemAmount, (SqModel.Money.CurrencyEnum)Enum.Parse(typeof(SqModel.Money.CurrencyEnum), currency.CurrencyCode)));

                        lineItems.Add(lineItem);
                    }

                    SqModel.CreateOrderRequestOrder orderRequest = new SqModel.CreateOrderRequestOrder(idempotencyKey, lineItems);
                    SqModel.CreateCheckoutRequest checkoutRequest = new SqModel.CreateCheckoutRequest(idempotencyKey, orderRequest);
                    SqModel.CreateCheckoutResponse checkoutResponse = this._checkoutApi.CreateCheckout(accessToken, locationId, checkoutRequest);

                    if (checkoutResponse.Errors == null ? false : checkoutResponse.Errors.Count != 0)
                    {
                        checkoutResponse.Errors.ForEach((SqModel.Error e) => result.AddError(e.Detail));
                    }
                }

                SqModel.ChargeResponse chargeResponse = this._transactionApi.Charge(accessToken, locationId, chargeRequest);
                if ((chargeResponse.Errors == null ? false : chargeResponse.Errors.Count != 0))
                {
                    chargeResponse.Errors.ForEach((SqModel.Error e) => result.AddError(e.Detail));
                }
                else
                {
                    result.AuthorizationTransactionResult = chargeResponse.Transaction.Tenders[0].Id;
                    if (delayCapture.HasValue)
                    {
                        if (!delayCapture.Value)
                        {
                            result.CaptureTransactionId = chargeResponse.Transaction.Id;
                            result.NewPaymentStatus = PaymentStatus.Paid;
                        }
                        else
                        {
                            result.AuthorizationTransactionId = chargeResponse.Transaction.Id;
                            result.NewPaymentStatus = PaymentStatus.Authorized;
                        }
                    }
                    else
                    {
                        // if delayCapture is null default to delayCapture = false
                        result.CaptureTransactionId = chargeResponse.Transaction.Id;
                        result.NewPaymentStatus = PaymentStatus.Paid;
                    }
                }
            }
            catch (ApiException exc)
            {
                var error = JObject.Parse(exc.ErrorContent);
                for (int i = 0; i < error.errors.Count; i++)
                {
                    //_logger.Error(exc.Message + ":" + exc.StackTrace, exc);
                    result.AddError(error.errors[i].detail.ToString());
                }
            }
            catch (Exception exc)
            {
                _logger.Error(exc.Message + ":" + exc.StackTrace, exc);
                //_logger.Error(exc.Message, exc);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            //var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
            //    _paypalDirectPaymentSettings.AdditionalFee, _paypalDirectPaymentSettings.AdditionalFeePercentage);

            decimal result = 0;
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();

            //    try
            //    {
            //        var apiContext = PaypalHelper.GetApiContext(_paypalDirectPaymentSettings);
            //        var authorization = Authorization.Get(apiContext, capturePaymentRequest.Order.AuthorizationTransactionId);
            //        var currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            //        var capture = new Capture
            //        {
            //            amount = new Amount
            //            {
            //                total = capturePaymentRequest.Order.OrderTotal.ToString("N", new CultureInfo("en-US")),
            //                currency = currency != null ? currency.CurrencyCode : null
            //            },
            //            is_final_capture = true
            //        };
            //        capture = authorization.Capture(apiContext, capture);

            //        result.CaptureTransactionId = capture.id;
            //        result.CaptureTransactionResult = capture.state;
            //        result.NewPaymentStatus = GetPaymentStatus(capture.state);
            //    }
            //    catch (PayPal.PayPalException exc)
            //    {
            //        if (exc is PayPal.ConnectionException)
            //        {
            //            var error = JsonFormatter.ConvertFromJson<Error>((exc as PayPal.ConnectionException).Response);
            //            if (error != null)
            //            {
            //                result.AddError(string.Format("PayPal error: {0} ({1})", error.message, error.name));
            //                if (error.details != null)
            //                    error.details.ForEach(x => result.AddError(string.Format("{0} {1}", x.field, x.issue)));
            //            }
            //        }

            //        //if there are not the specific errors add exception message
            //        if (result.Success)
            //            result.AddError(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
            //    }

            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();

            try
            {
                Currency currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
                var refundAmount = (long?)Math.Ceiling(refundPaymentRequest.AmountToRefund * new decimal(100));
                SqModel.Money refundMoney = new SqModel.Money(refundAmount, 
                                                            (SqModel.Money.CurrencyEnum)Enum.Parse(
                                                                    typeof(SqModel.Money.CurrencyEnum), 
                                                                    currency.CurrencyCode));
                Guid idempotencyKey = Guid.NewGuid();
                string transactionId = refundPaymentRequest.Order.AuthorizationTransactionResult;
                string reason = null;
                SqModel.CreateRefundRequest createRefundRequest = new SqModel.CreateRefundRequest(idempotencyKey.ToString(), 
                                                                                        transactionId, reason, refundMoney);

                string accessToken;
                string locationId;
                if (_squarePaymentSettings.UseSandbox)
                {
                    accessToken = _squarePaymentSettings.SandboxAccessToken;
                    locationId = _squarePaymentSettings.SandboxLocationId;
                }
                else
                {
                    accessToken = _squarePaymentSettings.AccessToken;
                    locationId = _squarePaymentSettings.LocationId;
                }

                SqModel.CreateRefundResponse refundResponse = this._refundApi.CreateRefund(accessToken, 
                                                                    locationId, 
                                                                    refundPaymentRequest.Order.CaptureTransactionId, 
                                                                    createRefundRequest);
                if ((refundResponse.Errors == null ? false : refundResponse.Errors.Count != 0))
                {
                    refundResponse.Errors.ForEach((SqModel.Error e) => result.AddError(e.Detail));
                }
                else
                {
                    result.NewPaymentStatus = refundPaymentRequest.IsPartialRefund ? 
                                                                    PaymentStatus.PartiallyRefunded : 
                                                                    PaymentStatus.Refunded;

                }
            }
            catch (ApiException exc)
            {
                var error = JObject.Parse(exc.ErrorContent);
                for (int i = 0; i < error.errors.Count; i++)
                {
                    result.AddError(error.errors[i].detail.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();

            //    try
            //    {
            //        var apiContext = PaypalHelper.GetApiContext(_paypalDirectPaymentSettings);
            //        var authorization = Authorization.Get(apiContext, voidPaymentRequest.Order.AuthorizationTransactionId);
            //        authorization = authorization.Void(apiContext);

            //        result.NewPaymentStatus = GetPaymentStatus(authorization.state);
            //    }
            //    catch (PayPal.PayPalException exc)
            //    {
            //        if (exc is PayPal.ConnectionException)
            //        {
            //            var error = JsonFormatter.ConvertFromJson<Error>((exc as PayPal.ConnectionException).Response);
            //            if (error != null)
            //            {
            //                result.AddError(string.Format("PayPal error: {0} ({1})", error.message, error.name));
            //                if (error.details != null)
            //                    error.details.ForEach(x => result.AddError(string.Format("{0} {1}", x.field, x.issue)));
            //            }
            //        }

            //        //if there are not the specific errors add exception message
            //        if (result.Success)
            //            result.AddError(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
            //    }

            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();

            //    var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            //    if (customer == null)
            //        throw new Exception("Customer cannot be loaded");

            //    try
            //    {
            //        var apiContext = PaypalHelper.GetApiContext(_paypalDirectPaymentSettings);
            //        var currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);

            //        //check that webhook exists
            //        if (string.IsNullOrEmpty(_paypalDirectPaymentSettings.WebhookId))
            //        {
            //            result.AddError("Recurring payments are not available until you create a webhook");
            //            return result;
            //        }

            //        Webhook.Get(apiContext, _paypalDirectPaymentSettings.WebhookId);

            //        //create the plan
            //        var url = _webHelper.GetStoreLocation(_storeContext.CurrentStore.SslEnabled);
            //        var billingPlan = new Plan
            //        {
            //            name = processPaymentRequest.OrderGuid.ToString(),
            //            description = string.Format("nopCommerce billing plan for the {0} order", processPaymentRequest.OrderGuid),
            //            type = "fixed",
            //            merchant_preferences = new MerchantPreferences
            //            {
            //                return_url = url,
            //                cancel_url = url,
            //                auto_bill_amount = "YES",
            //                //setting setup fee as the first payment (workaround for the processing first payment immediately)
            //                setup_fee = new PayPal.Api.Currency
            //                {
            //                    currency = currency != null ? currency.CurrencyCode : null,
            //                    value = processPaymentRequest.OrderTotal.ToString("N", new CultureInfo("en-US"))
            //                }
            //            },
            //            payment_definitions = new List<PaymentDefinition>
            //            {
            //                new PaymentDefinition
            //                {
            //                     name = "nopCommerce payment for the billing plan",
            //                     type = "REGULAR",
            //                     frequency_interval = processPaymentRequest.RecurringCycleLength.ToString(),
            //                     frequency = processPaymentRequest.RecurringCyclePeriod.ToString().TrimEnd('s'),
            //                     cycles = (processPaymentRequest.RecurringTotalCycles - 1).ToString(),
            //                     amount = new PayPal.Api.Currency
            //                     {
            //                         currency = currency != null ? currency.CurrencyCode : null,
            //                         value = processPaymentRequest.OrderTotal.ToString("N", new CultureInfo("en-US"))
            //                     }
            //                }
            //            }
            //        }.Create(apiContext);

            //        //activate the plan
            //        var patchRequest = new PatchRequest()
            //        {
            //            new Patch()
            //            {
            //                op = "replace",
            //                path = "/",
            //                value = new Plan
            //                {
            //                    state = "ACTIVE"
            //                }
            //            }
            //        };
            //        billingPlan.Update(apiContext, patchRequest);

            //        //create subscription
            //        var subscription = new Agreement
            //        {
            //            name = string.Format("nopCommerce subscription for the {0} order", processPaymentRequest.OrderGuid),
            //            //we set order guid in the description, then use it in the webhook handler
            //            description = processPaymentRequest.OrderGuid.ToString(),
            //            //setting start date as the next date of recurring payments as the setup fee was the first payment
            //            start_date = GetStartDate(processPaymentRequest.RecurringCyclePeriod, processPaymentRequest.RecurringCycleLength),

            //            payer = new Payer()
            //            {
            //                payment_method = "credit_card",

            //                funding_instruments = new List<FundingInstrument>
            //                {
            //                    new FundingInstrument
            //                    {
            //                        credit_card = new CreditCard
            //                        {
            //                            type = processPaymentRequest.CreditCardType.ToLowerInvariant(),
            //                            number = processPaymentRequest.CreditCardNumber,
            //                            cvv2 = processPaymentRequest.CreditCardCvv2,
            //                            expire_month = processPaymentRequest.CreditCardExpireMonth,
            //                            expire_year = processPaymentRequest.CreditCardExpireYear
            //                        }
            //                    }
            //                },

            //                payer_info = new PayerInfo
            //                {
            //                    billing_address = customer.BillingAddress == null ? null : new Address
            //                    {
            //                        country_code = customer.BillingAddress.Country != null ? customer.BillingAddress.Country.TwoLetterIsoCode : null,
            //                        state = customer.BillingAddress.StateProvince != null ? customer.BillingAddress.StateProvince.Abbreviation : null,
            //                        city = customer.BillingAddress.City,
            //                        line1 = customer.BillingAddress.Address1,
            //                        line2 = customer.BillingAddress.Address2,
            //                        phone = customer.BillingAddress.PhoneNumber,
            //                        postal_code = customer.BillingAddress.ZipPostalCode
            //                    },

            //                    email = customer.BillingAddress.Email,
            //                    first_name = customer.BillingAddress.FirstName,
            //                    last_name = customer.BillingAddress.LastName
            //                }

            //            },

            //            shipping_address = customer.ShippingAddress == null ? null : new ShippingAddress
            //            {
            //                country_code = customer.ShippingAddress.Country != null ? customer.ShippingAddress.Country.TwoLetterIsoCode : null,
            //                state = customer.ShippingAddress.StateProvince != null ? customer.ShippingAddress.StateProvince.Abbreviation : null,
            //                city = customer.ShippingAddress.City,
            //                line1 = customer.ShippingAddress.Address1,
            //                line2 = customer.ShippingAddress.Address2,
            //                phone = customer.ShippingAddress.PhoneNumber,
            //                postal_code = customer.ShippingAddress.ZipPostalCode
            //            },

            //            plan = new Plan
            //            {
            //                id = billingPlan.id
            //            }
            //        }.Create(apiContext);

            //        //if first payment failed, try again
            //        if (string.IsNullOrEmpty(subscription.agreement_details.last_payment_date))
            //            subscription.BillBalance(apiContext, new AgreementStateDescriptor { amount = subscription.agreement_details.outstanding_balance });

            //        result.SubscriptionTransactionId = subscription.id;
            //    }
            //    catch (PayPal.PayPalException exc)
            //    {
            //        if (exc is PayPal.ConnectionException)
            //        {
            //            var error = JsonFormatter.ConvertFromJson<Error>((exc as PayPal.ConnectionException).Response);
            //            if (error != null)
            //            {
            //                result.AddError(string.Format("PayPal error: {0} ({1})", error.message, error.name));
            //                if (error.details != null)
            //                    error.details.ForEach(x => result.AddError(string.Format("{0} {1}", x.field, x.issue)));
            //            }
            //        }

            //        //if there are not the specific errors add exception message
            //        if (result.Success)
            //            result.AddError(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
            //    }

            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();

            //    try
            //    {
            //        var apiContext = PaypalHelper.GetApiContext(_paypalDirectPaymentSettings);
            //        var subscription = Agreement.Get(apiContext, cancelPaymentRequest.Order.SubscriptionTransactionId);
            //        var reason = new AgreementStateDescriptor
            //        {
            //            note = string.Format("Cancel subscription {0}", cancelPaymentRequest.Order.OrderGuid)
            //        };
            //        subscription.Cancel(apiContext, reason);
            //    }
            //    catch (PayPal.PayPalException exc)
            //    {
            //        if (exc is PayPal.ConnectionException)
            //        {
            //            var error = JsonFormatter.ConvertFromJson<Error>((exc as PayPal.ConnectionException).Response);
            //            if (error != null)
            //            {
            //                result.AddError(string.Format("PayPal error: {0} ({1})", error.message, error.name));
            //                if (error.details != null)
            //                    error.details.ForEach(x => result.AddError(string.Format("{0} {1}", x.field, x.issue)));
            //            }
            //        }

            //        //if there are not the specific errors add exception message
            //        if (result.Success)
            //            result.AddError(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
            //    }

            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Core.Domain.Orders.Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "Square";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.Square.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "Square";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.Square.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Get type of controller
        /// </summary>
        /// <returns>Type</returns>
        public Type GetControllerType()
        {
            return typeof(SquareController);
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new SquarePaymentSettings
            {
                //TransactMode = TransactMode.Authorize,
                UseSandbox = true,
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.LocationId", "Location Id");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.LocationId.Hint", "Specify location Id.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.ApplicationId", "Application Id");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.ApplicationId.Hint", "Specify application Id.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.AccessToken", "Personal Access Token");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.AccessToken.Hint", "Specify personal access token.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxApplicationId", "Sandbox Application Id");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxApplicationId.Hint", "Specify sandbox application Id.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxAccessToken", "Sandbox Access Token");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxAccessToken.Hint", "Specify sandbox access token.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxLocationId", "Sandbox Location Id");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxLocationId.Hint", "Specify a sandbox location Id.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.PassPurchasedItems", "Pass Purchased Items");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.PassPurchasedItems.Hint", "Check to pass purchased item information to Square.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.PaymentMethodDescription", "Pay by credit / debit card");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.CardNumber", "Card Number");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.CVV", "CVV");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.ExpirationDate", "Expiration Date");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Fields.PostalCode", "Postal Code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Errors.NonceRequired", "Card authorization required.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Errors.InvalidCard", "Invalid card number.", null);
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Errors.ExpiredCard", "Your card is expired.", null);
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Errors.CvcRequired", "Cvc code is required.", null);
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Square.Errors.UnsupportedBrowser", "The browser you are using is not supported. Please try a different browser.", null);

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //delete webhook
            var settings = _settingService.LoadSetting<SquarePaymentSettings>();

            //settings
            _settingService.DeleteSetting<SquarePaymentSettings>();

            //    //locales
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.LocationId");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.LocationId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.ApplicationId");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.ApplicationId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.AccessToken");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.AccessToken.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxLocationId");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxLocationId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxApplicationId");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxApplicationId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxAccessToken");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.SandboxAccessToken.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.PassPurchasedItems");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Fields.PassPurchasedItems.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.PaymentMethodDescription");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Errors.NonceRequired");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Errors.InvalidCard");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Errors.ExpiredCard");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Errors.CvcRequired");
            this.DeletePluginLocaleResource("Plugins.Payments.Square.Errors.UnsupportedBrowser");

            base.Uninstall();
        }

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.Automatic; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Standard; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            get { return _localizationService.GetResource("Plugins.Payments.Square.PaymentMethodDescription"); }
        }
    }
}
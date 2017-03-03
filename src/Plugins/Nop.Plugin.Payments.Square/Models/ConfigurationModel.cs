using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.Square.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Square.Fields.LocationId")]
        public string LocationId { get; set; }
        public bool LocationId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Square.Fields.AccessToken")]
        public string AccessToken { get; set; }
        public bool AccessToken_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Square.Fields.ApplicationId")]
        public string ApplicationId { get; set; }
        public bool ApplicationId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Square.Fields.SandboxAccessToken")]
        public string SandboxAccessToken { get; set; }
        public bool SandboxAccessToken_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Square.Fields.SandboxApplicationId")]
        public string SandboxApplicationId { get; set; }
        public bool SandboxApplicationId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Square.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Square.Fields.PassPurchasedItems")]
        public bool PassPurchasedItems { get; set; }
        public bool PassPurchasedItems_OverrideForStore { get; set; }
    }
}
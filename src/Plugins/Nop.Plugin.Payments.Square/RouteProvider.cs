using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;
using Nop.Plugin.Payments.Square.ViewEngines;

namespace Nop.Plugin.Payments.Square
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}

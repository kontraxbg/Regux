using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Kontrax.Regux.Model.Call;
using Kontrax.Regux.Service;

namespace Kontrax.Regux.Portal.Controllers
{
    public class CallController : BaseController
    {
        [HttpGet]
        public Task<ActionResult> RegiXTestConnection()
        {
            RegiXTestModel model = null;
            return TryAsync(false,
                async () =>
                {
                    using (CallService service = new CallService())
                    {
                        model = await service.RegiXTestConnectionAsync();
                    }
                },
                () => View(model),
                () => View(model)
            );
        }

        public const string XsdRootPath = @"\\files\Documents\Projects\Execution\2018-04-15 МС - удостоверения\RegiX\XSD\XSD от regixaisweb.egov.bg";

        [HttpGet]
        public ActionResult Index()
        {
            ServicesModel model = new ServicesModel();
            using (CallService service = new CallService())
            {
                model.Services = service.GetXsdPaths(XsdRootPath);
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult Form(string xsdPath)
        {
            RequestFormModel model = CreateRequestForm(xsdPath);
            return View(model);
        }

        [HttpPost]
        public ActionResult Form(string xsdPath, RequestFormModel model)
        {
            RequestFormModel emptyModel = CreateRequestForm(xsdPath);

            XmlElementModel toRoot = model.Root;
            XmlElementModel fromRoot = toRoot.QName == emptyModel.Root.QName ? emptyModel.Root : null;
            RestoreViewData(toRoot, fromRoot);

            model.Warning = emptyModel.Warning;
            model.XsdDescription = emptyModel.XsdDescription;

            return View(model);
        }

        private void RestoreViewData(XmlElementModel toElement, XmlElementModel fromElement)
        {
            if (fromElement == null)
            {
                Danger($"Вече няма елемент с qualified name {toElement.QName}.");
                return;
            }

            toElement.Title = fromElement.Title;
            toElement.TypeTitle = fromElement.TypeTitle;
            toElement.Min = fromElement.Min;
            toElement.Max = fromElement.Max;
            toElement.MultiplicitySymbol= fromElement.MultiplicitySymbol;
            if (!toElement.IsSimpleType)
            {
                foreach (XmlElementModel toChild in toElement.Children)
                {
                    XmlElementModel fromChild = fromElement.Children.FirstOrDefault(c => c.QName == toChild.QName);
                    RestoreViewData(toChild, fromChild);
                }
            }
        }

        private static RequestFormModel CreateRequestForm(string xsdPath)
        {
            using (CallService service = new CallService())
            {
                return service.CreateRequestForm(xsdPath);
            }
        }
    }
}

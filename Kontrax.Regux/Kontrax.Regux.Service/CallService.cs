using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.Call;
using Kontrax.Regux.RegiXClient.Model;

namespace Kontrax.Regux.Service
{
    public class CallService : BaseService
    {
        public async Task<RegiXTestModel> RegiXTestConnectionAsync()
        {
            string[] resultAndError = await RegiXClient.RegiXUtil.TestConnectionAsync();
            return new RegiXTestModel { Result = resultAndError[0], Error = resultAndError[1] };
        }

        public CodeNameModel[] GetXsdPaths(string rootPath)
        {
            string[] filePaths = Directory.GetFiles(rootPath, "*.xsd", SearchOption.AllDirectories);
            return filePaths.Select(p => new CodeNameModel(
                p,
                $"{Path.GetFileName(Path.GetDirectoryName(p))}\\{Path.GetFileNameWithoutExtension(p)}"
            )).ToArray();
        }

        public RequestFormModel CreateRequestForm(string xsdPath)
        {
            // TODO: Временно, за тестване на файлове при Иван.
            // ако filePath == "RegistrationInfoRequest" или "RegistrationInfoResponse";
            if (!Path.IsPathRooted(xsdPath))
            {
                xsdPath = $@"d:\Projects\DefaultCollection\Ministry Council Services Portal\Documents\RegiX\XSD\Проба\{xsdPath}.xsd";
            }

            Xsd xsd = RegiXClient.RegiXUtil.ParseXsd(xsdPath);

            XsdElement root = null;
            string warning = null;
            if (xsd.Roots.Count > 0)
            {
                root = xsd.Roots[0];
                if (xsd.Roots.Count > 1)
                {
                    warning = $"В схемата има {xsd.Roots.Count} root елемента. Ще бъде използван само първият.";
                }
            }
            else
            {
                warning = "В схемата няма нито един root елемент.";
            }

            RequestFormModel model = new RequestFormModel
            {
                Warning = warning,
                XsdDescription = xsd.ToString()
            };

            if (root != null)
            {
                model.Root = CreateXmlElementModel(root);
            }
            return model;
        }

        private static XmlElementModel CreateXmlElementModel(XsdObject o)
        {
            if (o is XsdElement element)
            {
                XmlElementModel model = new XmlElementModel
                {
                    QName = element.QName,
                    Title = element.Description ?? $"[{element.Name}]",
                    Min = element.Multiplicity.Min,
                    Max = element.Multiplicity.Max,
                    MultiplicitySymbol = element.Multiplicity.ToString()
                };

                XsdType type = element.Type;
                if (type != null)
                {
                    string simpleTypeCode = type.SimpleTypeCode;
                    if (simpleTypeCode != null)
                    {
                        model.TypeTitle = simpleTypeCode;

                        // TODO: Засега са една-две стойности. Да се поддържат min и max по-пълноценно.
                        List<string> values = new List<string>();
                        for (int i = 0; i < model.Min; i++)
                        {
                            values.Add(null);
                        }
                        if (!model.Max.HasValue || model.Max > model.Min)
                        {
                            values.Add(null);
                        }

                        model.Values = values.ToArray();
                    }
                    else
                    {
                        model.TypeTitle = type.Description;  // ?? $"[{type.Name}]",

                        // TODO: Засега е един набор children. Да се поддържат min и max по някакъв начин.
                        if (type.Sequence != null)
                        {
                            model.Children = type.Sequence.Items.Select(i => CreateXmlElementModel(i)).ToArray();
                        }
                        else if (type.Choice != null)
                        {
                            model.Children = new XmlElementModel[] { CreateXmlElementModel(type.Choice) };
                        }
                        else
                        {
                            model.Children = new XmlElementModel[0];
                        }
                    }
                }
                return model;
            }

            if (o is XsdGroup group)  // Sequence или choice.
            {
                // а) Обособен sequence се среща само в choice. За sequence елементите се създава изкуствена група от контроли.
                // На теория sequence поддържа анотация/документация, но на практика тя не се прочита от xsd-то.
                // б) За choice елементите се създава изкуствена група от контроли със указание да се въведе само едно.
                // На теория choice поддържа анотация/документация, но на практика тя не се прочита от xsd-то.
                return new XmlElementModel
                {
                    Title = group.Description ?? (group.IsChoice ? "Въведете само едно от изброените" : "Група"),
                    Min = group.Multiplicity.Min,
                    Max = group.Multiplicity.Max,
                    MultiplicitySymbol = group.Multiplicity.ToString(),
                    Children = group.Items.Select(i => CreateXmlElementModel(i)).ToArray()
                };
            }

            throw new Exception($"Не се поддържа {o.GetType().Name}.");
        }
    }
}

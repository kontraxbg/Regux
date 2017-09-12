using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Kontrax.Regux.RegiXClient.Model;
using Kontrax.Regux.RegiXClient.RegiXServiceReference;

namespace Kontrax.Regux.RegiXClient
{
    public static class RegiXUtil
    {
        /// <summary>
        /// Оригиналният примерен код е копиран от тук:
        /// http://regixaisweb.egov.bg/RegiXInfo/RegiXGuides/#!Documents/executesynchronous.htm
        /// </summary>
        public static async Task<string[]> TestConnectionAsync()
        {
            string operation = "TechnoLogica.RegiX.GraoNBDAdapter.APIService.INBDAPI.ValidPersonSearch";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(@"<ValidPersonRequest
                            xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                            xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                           xmlns=""http://egov.bg/RegiX/GRAO/NBD/ValidPersonRequest"">
                            <EGN>8506258485</EGN>
                          </ValidPersonRequest>");

            CallContext callContext = new CallContext
            {
                AdministrationName = "Администрация",
                AdministrationOId = "1.2.3.4.5.6.7.8.9",
                EmployeeIdentifier = "myusername@administration.domain",
                EmployeeNames = "Първо Второ Трето",
                EmployeePosition = "Експерт в отдел",
                LawReason = "На основание чл. X от Наредба/Закон/Нормативен акт",
                Remark = "За тестване на системата",
                ServiceType = "За административна услуга",
                ServiceURI = "123-12345-01.01.2017"
            };

            ServiceRequestData request = new ServiceRequestData
            {
                Operation = operation,
                Argument = doc.DocumentElement,
                CallContext = callContext,
                CitizenEGN = "XXXXXXXXXX",
                EmployeeEGN = "XXXXXXXXXX",
                ReturnAccessMatrix = false,
                SignResult = true
            };

            ServiceResultData response;
            using (RegiXEntryPointClient client = new RegiXEntryPointClient())
            {
                response = await client.ExecuteSynchronousAsync(request);
            }

            if (response.HasError)
            {
                return new string[] { null, response.Error };
            }
            return new string[] { response.Data.Response.Any.OuterXml, null };
        }

        public static Xsd ParseXsd(string filePath)
        {
            Xsd xsd = new Xsd();

            using (XmlReader reader = XmlReader.Create(filePath))
            {
                var schema = XmlSchema.Read(reader, null);

                XmlSchemaSet set = new XmlSchemaSet();
                set.Add(schema);
                set.Compile();

                foreach (XmlSchemaElement element in set.GlobalElements.Values)
                {
                    xsd.AddRoot(CreateXsdElement(element, xsd));
                }

                // До момента са създадени всички типове, които се използват пряко или косвено от root елементите.
                // Обикновено не е нужно да се създават и неизползваните типове, но за пълнота:
                AddUnusedTypes(set, xsd);

                // Нещо от интернет на тема MaxLength
                //var results = GetElementMaxLength(set, "Setup_Type");
                //foreach (var item in results)
                //{
                //    builder.AppendFormat("{0} | {1}", item.Key, item.Value);
                //}
            }
            return xsd;
        }

        private static void AddUnusedTypes(XmlSchemaSet set, Xsd xsd)
        {
            foreach (XmlSchemaType type in set.GlobalTypes.Values)
            {
                xsd.Types.TryGetValue(FormatQName(type.QualifiedName), out XsdType xsdType);
                if (xsdType == null)
                {
                    xsd.AddType(CreateXsdType(type, xsd));
                }
            }
        }

        private static XsdType CreateXsdType(XmlSchemaType type, Xsd xsd)
        {
            XsdType xsdType = new XsdType(
                type.Name,
                FormatQName(type.QualifiedName),
                FormatAnnotation(type),
                type.TypeCode != XmlTypeCode.None ? type.TypeCode.ToString() : null);

            if (type is XmlSchemaComplexType complexType)
            {
                if (complexType.ContentTypeParticle is XmlSchemaSequence sequence)
                {
                    xsdType.Sequence = CreateSequence(sequence, xsd);
                }
                else if (complexType.ContentTypeParticle is XmlSchemaChoice choice)
                {
                    xsdType.Choice = CreateChoice(choice, xsd);
                }
                else if (type.TypeCode != XmlTypeCode.None)
                {
                    // Типът е съставен само на теория, защото съдържанието е от прост тип.
                }
                else
                {
                    xsdType.Sequence = new XsdGroup(false, new XsdMultiplicity(1, 1), null);
                    xsdType.Sequence.Add(CreateDummyXsdElement("TODO: Неочаквано съдържание на съставен тип", complexType.ContentTypeParticle.GetType()));
                }
            }
            else if (type is XmlSchemaSimpleType simpleType)
            {
                // Process simpleType.Content ...
            }
            return xsdType;
        }

        private static void AddSequenceItems(XsdGroup xsdSequence, XmlSchemaSequence sequence, Xsd xsd)
        {
            foreach (XmlSchemaObject child in sequence.Items)
            {
                if (child is XmlSchemaElement element)
                {
                    xsdSequence.Add(CreateXsdElement(element, xsd));
                }
                else if (child is XmlSchemaAny any)
                {
                    xsdSequence.Add(CreateXsdAny(any));
                }
                else if (child is XmlSchemaSequence childSequence)
                {
                    // Няма смисъл от влагане на sequence-и, затова елементите на вложения се изсипват в основния.
                    AddSequenceItems(xsdSequence, childSequence, xsd);
                }
                else if (child is XmlSchemaChoice childChoice)
                {
                    xsdSequence.Add(CreateChoice(childChoice, xsd));
                }
                else
                {
                    xsdSequence.Add(CreateDummyXsdElement($"TODO: Неочакван обект в sequence.Items", child.GetType()));
                }
            }
        }
        private static void AddChoices(XsdGroup xsdChoice, XmlSchemaChoice choice, Xsd xsd)
        {
            foreach (XmlSchemaObject child in choice.Items)
            {
                if (child is XmlSchemaElement element)
                {
                    xsdChoice.Add(CreateXsdElement(element, xsd));
                }
                else if (child is XmlSchemaAny any)
                {
                    xsdChoice.Add(CreateXsdAny(any));
                }
                else if (child is XmlSchemaSequence childSequence)
                {
                    xsdChoice.Add(CreateSequence(childSequence, xsd));
                }
                else if (child is XmlSchemaChoice childChoice)
                {
                    // Няма смисъл от влагане на choice-ове, затова елементите на вложения се изсипват в основния.
                    AddChoices(xsdChoice, childChoice, xsd);
                }
                else
                {
                    xsdChoice.Add(CreateDummyXsdElement($"TODO: Неочакван обект в choice.Items", child.GetType()));
                }
            }
        }

        private static XsdElement CreateDummyXsdElement(string name, Type t)
        {
            return new XsdElement(name, null, new XsdMultiplicity(0, 0), new XsdType(t.Name, t.FullName, null, null), null);
        }

        private static XsdElement CreateXsdElement(XmlSchemaElement element, Xsd xsd)
        {
            if (element.Constraints != null && element.Constraints.Count > 0)
            {
                foreach (XmlSchemaObject constraint in element.Constraints)
                {
                    // TODO
                }
            }

            XmlSchemaType type = element.ElementSchemaType;
            string typeQName;
            XsdType xsdType;
            if (!type.QualifiedName.IsEmpty)
            {
                // Използване или добавяне на именуван тип.
                typeQName = FormatQName(type.QualifiedName);
                xsd.Types.TryGetValue(typeQName, out xsdType);
                if (xsdType == null)
                {
                    xsdType = CreateXsdType(type, xsd);
                    xsd.AddType(xsdType);
                }
            }
            else
            {
                // Създаване на inline тип.
                typeQName = null;
                xsdType = CreateXsdType(type, xsd);
            }

            return new XsdElement(
                element.Name,
                FormatQName(element.QualifiedName),
                CreateMultiplicity(element),
                xsdType,
                FormatAnnotation(element));
        }

        private static XsdAny CreateXsdAny(XmlSchemaAny any)
        {
            return new XsdAny(
                any.Namespace,
                any.ProcessContents.ToString(),
                CreateMultiplicity(any),
                FormatAnnotation(any));
        }

        private static XsdGroup CreateSequence(XmlSchemaSequence childSequence, Xsd xsd)
        {
            XsdGroup xsdSequence = new XsdGroup(false, CreateMultiplicity(childSequence), FormatAnnotation(childSequence));
            AddSequenceItems(xsdSequence, childSequence, xsd);
            return xsdSequence;
        }

        private static XsdGroup CreateChoice(XmlSchemaChoice childChoice, Xsd xsd)
        {
            XsdGroup xsdChoice = new XsdGroup(true, CreateMultiplicity(childChoice), FormatAnnotation(childChoice));
            AddChoices(xsdChoice, childChoice, xsd);
            return xsdChoice;
        }

        private static XsdMultiplicity CreateMultiplicity(XmlSchemaParticle particle)
        {
            return new XsdMultiplicity(
                (int)particle.MinOccurs,
                particle.MaxOccurs < decimal.MaxValue ? (int)particle.MaxOccurs : (int?)null);
        }

        private static string FormatAnnotation(XmlSchemaAnnotated node)
        {
            List<string> lines = new List<string>();
            XmlSchemaAnnotation annotation = node.Annotation;
            if (annotation != null)
            {
                foreach (var annItem in annotation.Items)
                {
                    if (annItem is XmlSchemaDocumentation doc)
                    {
                        foreach (XmlNode docNode in doc.Markup)
                        {
                            lines.Add(docNode.OuterXml);
                        }
                    }
                    else
                    {
                        lines.Add("TODO: Неочакван обект в Annotation.Items от тип " + annItem.GetType().Name);
                    }
                }
            }
            return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : null;
        }

        private static string FormatQName(XmlQualifiedName qName)
        {
            return qName.IsEmpty ? null : qName.ToString();
        }

        #region Нещо от интернет на тема MaxLength

        public static Dictionary<string, int> GetElementMaxLength(XmlSchemaSet set, String xsdElementName)
        {
            if (xsdElementName == null) throw new ArgumentException();
            // if your XSD has a target namespace, you need to replace null with the namespace name
            var qname = new XmlQualifiedName(xsdElementName, null);

            // find the type you want in the XmlSchemaSet    
            var parentType = set.GlobalTypes[qname];

            // call GetAllMaxLength with the parentType as parameter
            var results = GetAllMaxLength(set, parentType);

            return results;
        }

        private static Dictionary<string, int> GetAllMaxLength(XmlSchemaSet set, XmlSchemaObject obj)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();

            // do some type checking on the XmlSchemaObject
            if (obj is XmlSchemaSimpleType)
            {
                // if it is a simple type, then call GetMaxLength to get the MaxLength restriction
                var st = obj as XmlSchemaSimpleType;
                dict[st.QualifiedName.Name] = GetMaxLength(st);
            }
            else if (obj is XmlSchemaComplexType)
            {

                // if obj is a complexType, cast the particle type to a sequence
                //  and iterate the sequence
                //  warning - this will fail if it is not a sequence, so you might need
                //  to make some adjustments if you have something other than a xs:sequence
                var ct = obj as XmlSchemaComplexType;
                var seq = ct.ContentTypeParticle as XmlSchemaSequence;

                foreach (var item in seq.Items)
                {
                    // item will be an XmlSchemaObject, so just call this same method
                    //  with item as the parameter to parse it out
                    var rng = GetAllMaxLength(set, item);

                    // add the results to the dictionary
                    foreach (var kvp in rng)
                    {
                        dict[kvp.Key] = kvp.Value;
                    }
                }
            }
            else if (obj is XmlSchemaElement)
            {
                // if obj is an XmlSchemaElement, the you need to find the type
                //  based on the SchemaTypeName property.  This is why your 
                //  XmlSchemaSet needs to have class-level scope
                var ele = obj as XmlSchemaElement;
                var type = set.GlobalTypes[ele.SchemaTypeName];

                // once you have the type, call this method again and get the dictionary result
                var rng = GetAllMaxLength(set, type);

                // put the results in this dictionary.  The difference here is the dictionary
                //  key is put in the format you specified
                foreach (var kvp in rng)
                {
                    dict[string.Format("{0}/{1}", ele.QualifiedName.Name, kvp.Key)] = kvp.Value;
                }
            }

            return dict;
        }

        private static int GetMaxLength(XmlSchemaSimpleType xsdSimpleType)
        {
            // get the content of the simple type
            var restriction = xsdSimpleType.Content as XmlSchemaSimpleTypeRestriction;

            // if it is null, then there are no restrictions and return -1 as a marker value
            if (restriction == null) return -1;

            int result = -1;

            // iterate the facets in the restrictions, look for a MaxLengthFacet and parse the value
            foreach (XmlSchemaObject facet in restriction.Facets)
            {
                if (facet is XmlSchemaMaxLengthFacet)
                {
                    result = int.Parse(((XmlSchemaFacet)facet).Value);
                    break;
                }
            }

            return result;
        }

        #endregion
    }
}

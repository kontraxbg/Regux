namespace Kontrax.Regux.Model.Call
{
    public class XmlElementModel
    {
        public string QName { get; set; }

        public string Title { get; set; }

        public string TypeTitle { get; set; }

        public int Min { get; set; }

        public int? Max { get; set; }

        public string MultiplicitySymbol { get; set; }

        public string[] Values { get; set; }

        public XmlElementModel[] Children { get; set; }

        public bool IsSimpleType
        {
            get { return Children == null; }
        }
    }
}

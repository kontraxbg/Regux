namespace Kontrax.Regux.RegiXClient.Model
{
    public class XsdElement : XsdNamedObject
    {
        private readonly XsdMultiplicity _multiplicity;
        private readonly XsdType _type;

        public XsdElement(string name, string qName, XsdMultiplicity multiplicity, XsdType type, string description)
            : base(name, qName, description)
        {
            _multiplicity = multiplicity;
            _type = type;
        }

        public XsdMultiplicity Multiplicity { get { return _multiplicity; } }

        public XsdType Type { get { return _type; } }

        public override string ToString()
        {
            string typeText;
            if (_type != null)
            {
                string typeTitle = _type.Name ?? _type.SimpleTypeCode ?? _type.QName;
                typeText = typeTitle != null ? "тип " + typeTitle : "inline тип";
            }
            else
            {
                typeText = "неизвестен тип";
            }
            return $"Елемент {Name ?? QName} {_multiplicity} {typeText}, {Description ?? "без описание"}";
        }
    }
}

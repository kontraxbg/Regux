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
            return $"Елемент {Name ?? QName} {_multiplicity} тип {_type.Name ?? _type.SimpleTypeCode ?? _type.QName}, {Description ?? "без описание"}";
        }
    }
}

using System.Collections.Generic;

namespace Kontrax.Regux.RegiXClient.Model
{
    public class XsdType : XsdNamedObject
    {
        private readonly string _simpleTypeCode;
        private readonly List<string> _enumValues = new List<string>();

        public XsdType(string name, string qName, string description, string simpleTypeCode)
            : base(name, qName, description)
        {
            _simpleTypeCode = simpleTypeCode;
        }

        public string SimpleTypeCode { get { return _simpleTypeCode; } }

        public XsdGroup Sequence { get; internal set; }

        public XsdGroup Choice { get; internal set; }

        public IReadOnlyList<string> EnumValues
        {
            get { return _enumValues; }
        }

        internal void AddEnumValue(string enumValue)
        {
            _enumValues.Add(enumValue);
        }

        public override string ToString()
        {
            string prefix = _simpleTypeCode != null ? "Прост" : "Съставен";
            string baseType = _simpleTypeCode != null ? $" : {_simpleTypeCode}" : null;
            string text = $"{prefix} тип {Name ?? QName}{baseType}, {Description ?? "без описание"}";
            string items = Sequence != null ? Sequence.ToString() : Choice != null ? Choice.ToString() : null;
            if (items != null)
            {
                return $"{text} {items}";
            }
            return text;
        }
    }
}

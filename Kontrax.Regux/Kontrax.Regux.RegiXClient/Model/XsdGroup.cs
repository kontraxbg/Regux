using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontrax.Regux.RegiXClient.Model
{
    public class XsdGroup : XsdObject
    {
        private readonly bool _isChoice;
        private readonly string _description;
        private readonly XsdMultiplicity _multiplicity;
        private readonly List<XsdObject> _items = new List<XsdObject>();

        public XsdGroup(bool isChoice, XsdMultiplicity multiplicity, string description)
            : base(description)
        {
            _isChoice = isChoice;
            _description = description;
            _multiplicity = multiplicity;
        }

        public bool IsChoice { get { return _isChoice; } }

        public XsdMultiplicity Multiplicity { get { return _multiplicity; } }

        public IReadOnlyList<XsdObject> Items { get { return _items; } }

        public void Add(XsdObject item)
        {
            _items.Add(item);
        }

        public override string ToString()
        {
            string prefix = _isChoice
                ? $"Choice {_multiplicity} "
                // Точно един брой sequence е масовият случай, затова той не се изписва изрично.
                : _multiplicity.Min == 1 && _multiplicity.Max == 1
                    ? null
                    : $"Sequence {_multiplicity} ";
            string items = string.Join(string.Empty, _items.Select(alt => $"{_nl}{_indent}{alt.ToString().Replace(_nl, _nl + _indent)}"));
            return $"{prefix}{Description}{items}";
        }
    }
}

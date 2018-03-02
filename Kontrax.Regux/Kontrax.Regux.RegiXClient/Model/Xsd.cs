using System;
using System.Linq;
using System.Collections.Generic;

namespace Kontrax.Regux.RegiXClient.Model
{
    public class Xsd
    {
        private readonly Dictionary<string, XsdType> _types = new Dictionary<string, XsdType>();
        private readonly List<XsdElement> _roots = new List<XsdElement>();

        public IReadOnlyDictionary<string, XsdType> Types { get { return _types; } }

        public IReadOnlyList<XsdElement> Roots { get { return _roots; } }

        internal void AddType(XsdType type)
        {
            _types.Add(type.QName, type);
        }

        internal void AddRoot(XsdElement element)
        {
            _roots.Add(element);
        }

        //private int _nextChoiceId = 1;
        //public int GenerateChoiceId()
        //{
        //    return _nextChoiceId++;
        //}

        public override string ToString()
        {
            string separator = Environment.NewLine + new string('-', 60) + Environment.NewLine;
            return string.Join(separator, _types.Values.Cast<object>().Union(_roots));
        }
    }
}

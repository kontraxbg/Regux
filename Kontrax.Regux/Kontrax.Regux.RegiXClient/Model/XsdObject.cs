using System;

namespace Kontrax.Regux.RegiXClient.Model
{
    public abstract class XsdObject
    {
        protected static readonly string _nl = Environment.NewLine;
        protected const string _indent = "    ";

        private readonly string _description;

        public XsdObject(string description)
        {
            _description = description;
        }

        public string Description { get { return _description; } }
    }
}

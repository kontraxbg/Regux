namespace Kontrax.Regux.RegiXClient.Model
{
    public class XsdAny : XsdObject
    {
        private readonly string _namespace;
        private readonly string _processContents;
        private readonly XsdMultiplicity _multiplicity;

        public XsdAny(string ns, string processContents, XsdMultiplicity multiplicity, string description)
            : base(description)
        {
            _namespace = ns;
            _processContents = processContents;
            _multiplicity = multiplicity;
        }

        public XsdMultiplicity Multiplicity { get { return _multiplicity; } }

        public override string ToString()
        {
            return $"Any {_multiplicity} ns={_namespace} processContents={_processContents}, {Description ?? "без описание"}";
        }
    }
}

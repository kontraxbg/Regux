namespace Kontrax.Regux.RegiXClient.Model
{
    public abstract class XsdNamedObject : XsdObject
    {
        private readonly string _name;
        private readonly string _qName;

        public XsdNamedObject(string name, string qName, string description)
            : base(description)
        {
            _name = name;
            _qName = qName;
        }

        public string Name { get { return _name; } }

        public string QName { get { return _qName; } }
    }
}

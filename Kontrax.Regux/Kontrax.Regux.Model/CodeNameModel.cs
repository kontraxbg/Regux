namespace Kontrax.Regux.Model
{
    public class CodeNameModel
    {
        private readonly string _code;
        private readonly string _name;

        public CodeNameModel(string code, string name)
        {
            _code = code;
            _name = name;
        }

        public string Code
        {
            get { return _code; }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}

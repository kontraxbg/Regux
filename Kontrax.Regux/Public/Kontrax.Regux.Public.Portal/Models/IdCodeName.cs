using System.Collections.Generic;
using System.Web.Mvc;

namespace Kontrax.Regux.Public.Portal.Models
{
    public class IdNameModel
    {
        private readonly int _id;
        private readonly string _name;

        public IdNameModel(int id, string name)
        {
            _id = id;
            _name = name;
        }

        public int Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }
    }

    public class IdNameSelectList : SelectList
    {
        public IdNameSelectList(IEnumerable<IdNameModel> items)
            : base(items, "Id", "Name")
        {
        }

        //public IdNameSelectList(IEnumerable<INomenclatureValue> items)
        //    : this(items.ToIdName())
        //{
        //}
    }

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

    public class CodeNameSelectList : SelectList
    {
        public CodeNameSelectList(IEnumerable<CodeNameModel> items)
            : base(items, "Code", "Name")
        {
        }

        //public CodeNameSelectList(IEnumerable<ICodeName> items)
        //    : this(items.ToCodeName())
        //{
        //}
    }
}
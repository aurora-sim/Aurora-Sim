using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Modules.Web
{
    public interface ITranslator
    {
        string LanguageName { get; }
        string GetTranslatedString(string key);
    }
}

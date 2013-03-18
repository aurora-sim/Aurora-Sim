
namespace Aurora.Modules.Web
{
    public interface ITranslator
    {
        string LanguageName { get; }
        string GetTranslatedString(string key);
    }
}

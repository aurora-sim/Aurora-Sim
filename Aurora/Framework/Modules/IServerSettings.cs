namespace Aurora.Framework
{
    public delegate void UpdatedSetting(string value);

    public delegate string GetSetting();

    public class ServerSetting
    {
        public string Name;
        public string Comment;
        public string Type;
        //Full XML representation
        public event GetSetting OnGetSetting;

        public string GetValue()
        {
            if (OnGetSetting != null)
                return OnGetSetting();
            return "";
        }

        public void SetValue(string value)
        {
            if (OnUpdatedSetting != null)
                OnUpdatedSetting(value);
        }

        public event UpdatedSetting OnUpdatedSetting;
    }

    public interface IServerSettings
    {
        void RegisterSetting(ServerSetting setting);
        void UnregisterSetting(ServerSetting setting);
    }
}
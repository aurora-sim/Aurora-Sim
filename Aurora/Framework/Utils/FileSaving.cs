namespace Aurora.Framework
{
    public class FileSaving
    {
        private string fileName;

        public FileSaving()
        {
        }

        public FileSaving(string file)
        {
            fileName = file;
        }

        public static T LoadFromFile<T>(string file) where T : FileSaving
        {
            try
            {
                if (file == "" || !System.IO.File.Exists(file))
                    return null;
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof (T));
                var stream = System.IO.File.OpenRead(file);
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load(stream);
                FileSaving config;
                if (typeof (T).Name != "opencv_storage")
                {
                    System.IO.MemoryStream r =
                        new System.IO.MemoryStream(
                            System.Text.Encoding.UTF8.GetBytes(xmlHeader + doc.DocumentElement.InnerXml));
                    config = (FileSaving) x.Deserialize(r);
                }
                else
                {
                    stream.Close();
                    stream = System.IO.File.OpenRead(file);
                    config = (FileSaving) x.Deserialize(stream);
                }

                stream.Close();
                config.fileName = file;
                return (T) config;
            }
            catch
            {
            }
            return null;
        }

        private static object _lock = new object();
        private const string xmlHeader = "<?xml version=\"1.0\"?>";

        public static void SaveToFile(string file, FileSaving config)
        {
            lock (_lock)
            {
                var xns = new System.Xml.Serialization.XmlSerializerNamespaces();
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(config.GetType());
                System.IO.Stream stream;
                if (config.GetType().Name != "opencv_storage")
                    stream = new System.IO.MemoryStream();
                else
                {
                    System.IO.File.WriteAllText(file, "");
                    stream = System.IO.File.OpenWrite(file);
                }
                xns.Add(string.Empty, string.Empty);
                x.Serialize(stream, config, xns);
                if (config.GetType().Name != "opencv_storage")
                {
                    stream.Position = 0;
                    byte[] bs = new byte[stream.Length];
                    ((System.IO.MemoryStream) stream).Read(bs, 0, bs.Length);
                    string s = xmlHeader + "<opencv_storage>" +
                               System.Text.Encoding.UTF8.GetString(bs).Replace(xmlHeader, "") + "</opencv_storage>";
                    System.IO.File.WriteAllText(file, s);
                }
                stream.Close();
            }
        }

        public virtual void Save()
        {
            FileSaving.SaveToFile(fileName, this);
        }
    }
}
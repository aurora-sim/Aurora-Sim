using System;
using System.Collections.Generic;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;
using System.Xml;
using System.Xml.Serialization;

namespace Aurora.Services.DataService
{
    public class LocalOfflineMessagesConnector : IOfflineMessagesConnector, IAuroraDataPlugin
	{
        private IGenericData GD = null;

        public void Initialise(IGenericData GenericData, IConfigSource source)
        {
            if (source.Configs["AuroraConnectors"].GetString("OfflineMessagesConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IOfflineMessagesConnector"; }
        }

        public void Dispose()
        {
        }

        public GridInstantMessage[] GetOfflineMessages(UUID agentID)
		{
            List<GridInstantMessage> messages = new List<GridInstantMessage>();
            List<string> Messages = GD.Query("ToUUID", agentID, "offlinemessages", "Message");
			GD.Delete("offlinemessages", new string[] { "ToUUID" }, new object[] { agentID });
            if (Messages.Count == 0)
                return messages.ToArray();
            GridInstantMessage Message = new GridInstantMessage();
            foreach (string part in Messages)
            {
                byte[] byteArray = new byte[part.Length];
                System.Text.ASCIIEncoding encoding = new
                System.Text.ASCIIEncoding();
                byteArray = encoding.GetBytes(part);

                // Load the memory stream
                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(byteArray);
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);

                XmlSerializer deserializer = new XmlSerializer(typeof(GridInstantMessage));
                Message = (GridInstantMessage)deserializer.Deserialize(memoryStream);
                messages.Add(Message);
                Message = new GridInstantMessage();
            }
			return messages.ToArray();
		}

		public void AddOfflineMessage(GridInstantMessage message)
		{
            System.IO.MemoryStream buffer = new System.IO.MemoryStream();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;

            using (XmlWriter writer = XmlWriter.Create(buffer, settings))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GridInstantMessage));
                serializer.Serialize(writer, message);
                writer.Flush();
            }
            byte[] bytes = buffer.ToArray();
            string array = OpenMetaverse.Utils.BytesToString(bytes);
            array = array.Remove(0, 1); //Theres a space in front of it for some reason
			GD.Insert("offlinemessages", new object[] {
				message.toAgentID,
                array
			});
		}
	}
}

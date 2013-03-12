using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aurora.Modules
{
    public class ProtobufRegionDataLoader : IRegionDataLoader
    {
        public string FileType { get { return ".sim"; } }

        public RegionData LoadBackup(string file)
        {
            if (!File.Exists(file))
                return null;
            try
            {
                FileStream stream = File.OpenRead(file);
                RegionData regiondata = ProtoBuf.Serializer.Deserialize<RegionData>(stream);
                stream.Close();

                List<SceneObjectGroup> grps = new List<SceneObjectGroup>();
                
                foreach (SceneObjectGroup grp in regiondata.Groups)
                {
                    SceneObjectGroup sceneObject = new SceneObjectGroup(grp.ChildrenList[0], null);
                    foreach (SceneObjectPart part in grp.ChildrenList)
                    {
                        if (part.UUID == sceneObject.UUID)
                            continue;
                        sceneObject.AddChild(part, part.LinkNum);

                        part.StoreUndoState();
                    }
                    grps.Add(sceneObject);
                }
                regiondata.Groups = grps;
                return regiondata;
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[ProtobufRegionLoader]: Failed to load backup: " + ex.ToString());
                return null; 
            }
        }

        public bool SaveBackup(string file, RegionData regiondata)
        {
            FileStream stream = null;
            try
            {
                stream = File.OpenWrite(file);
                ProtoBuf.Serializer.Serialize<RegionData>(stream, regiondata);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[ProtobufRegionLoader]: Failed to save backup: " + ex.ToString());
                return false;
            }
            finally
            {
                if (stream != null && stream.CanWrite)
                    stream.Close();
            }
            return true;
        }
    }
}

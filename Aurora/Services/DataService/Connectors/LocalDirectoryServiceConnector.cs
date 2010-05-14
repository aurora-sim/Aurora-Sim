using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Services.DataService.Connectors
{
    public class LocalDirectoryServiceConnector
    {
        private IGenericData GD = null;
        public LocalDirectoryServiceConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}
		
        public void AddLandObject(OpenSim.Framework.LandData args)
        {
            try
            {
                GD.Delete("auroraland", new string[] { "UUID" }, new string[] { args.GlobalID.ToString() });
            }
            catch (Exception) { }
            List<string> Values = new List<string>();
            Values.Add(args.GlobalID.ToString());
            Values.Add(args.LocalID.ToString());
            Values.Add(args.MediaDesc.ToString());
            Values.Add(args.MediaSize[1].ToString());
            Values.Add(args.MediaLoop.ToString());
            Values.Add(args.MediaType.ToString());
            Values.Add(args.MediaSize[0].ToString());
            Values.Add(args.ObscureMedia.ToString());
            Values.Add(args.ObscureMusic.ToString());
            GD.Insert("auroraland", Values.ToArray());
        }
    }
}

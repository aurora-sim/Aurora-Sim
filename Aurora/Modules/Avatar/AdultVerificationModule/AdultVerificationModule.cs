using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.Framework;
using Aurora.DataManager;

namespace Aurora.Modules.Avatar.AdultVerificationModule
{
	public class AdultVerificationModule: IRegionModule, IAdultVerificationModule
	{
        private IGenericData GenericData = null;
		private IRegionData RegionData = null;
        private bool m_Enabled = true;
		public void Initialise(Scene scene, IConfigSource source)
		{
            if (source.Configs["AdultVerification"] != null)
            {
                if (source.Configs["AdultVerification"].GetString(
                        "AdultVerification", Name) !=
                        Name)
                {
                    m_Enabled = false;
                    return;
                }
            }
			scene.RegisterModuleInterface<IAdultVerificationModule>(this);
		}
		
		public void PostInitialise()
		{
            if (!m_Enabled)
                return;
			GenericData = Aurora.DataManager.DataManager.GetGenericPlugin();
			RegionData = Aurora.DataManager.DataManager.GetRegionPlugin();
		}
		
		public void Close(){}
		
		public string Name {get {return "AdultVerificationModule";}}
		
		public bool IsSharedModule {get {return true;}}

		public bool GetIsRegionMature(UUID regionID)
		{
			return RegionData.GetIsRegionMature(regionID.ToString());
		}
	}
}

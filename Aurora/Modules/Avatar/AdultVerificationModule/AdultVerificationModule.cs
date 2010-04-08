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
		public void Initialise(Scene scene, IConfigSource source)
		{
			
		}
		
		public void PostInitialise()
		{
			
		}
		
		public void Close(){}
		
		public string Name {get {return "AdultVerificationModule";}}
		
		public bool IsSharedModule {get {return true;}}
		
		
		public bool GetIsRegionMature(UUID regionID)
		{
			return false;
		}
	}
}

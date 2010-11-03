#region Copyright
//
// Nini Configuration Project.
// Copyright (C) 2006 Brent R. Matzelle.  All rights reserved.
//
// This software is published under the terms of the MIT X11 license, a copy of 
// which has been included with this distribution in the LICENSE.txt file.
// 
#endregion

using System;
using System.IO;
using Nini.Config;
using NUnit.Framework;

namespace Nini.Test.Config
{
	[TestFixture]
	public class ConfigCollectionTests
	{
		#region Private variables
		IConfig eventConfig = null;
		ConfigCollection eventCollection = null;
		int configAddedCount = 0;
		int configRemovedCount = 0;
		#endregion

		#region Unit tests
		[Test]
		public void GetConfig ()
		{
			ConfigBase config1 = new ConfigBase ("Test1", null);
			ConfigBase config2 = new ConfigBase ("Test2", null);
			ConfigBase config3 = new ConfigBase ("Test3", null);
			ConfigCollection collection = new ConfigCollection (null);
			
			collection.Add (config1);
			Assert.AreEqual (1, collection.Count);
			Assert.AreEqual (config1, collection[0]);
			
			collection.Add (config2);
			collection.Add (config3);
			Assert.AreEqual (3, collection.Count);
			
			Assert.AreEqual (config2, collection["Test2"]);
			Assert.AreEqual (config3, collection["Test3"]);
			Assert.AreEqual (config3, collection[2]);
		}
		
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void AlreadyExistsException ()
		{
			ConfigBase config = new ConfigBase ("Test", null);
			ConfigCollection collection = new ConfigCollection (null);
			collection.Add (config);
			collection.Add (config); // exception
		}
		
		[Test]
		public void NameAlreadyExists ()
		{
			ConfigBase config1 = new ConfigBase ("Test", null);
			ConfigBase config2 = new ConfigBase ("Test", null);
			ConfigCollection collection = new ConfigCollection (null);
			collection.Add (config1);
			collection.Add (config2); // merges, no exception
		}
		
		[Test]
		public void AddAndRemove ()
		{
			ConfigBase config1 = new ConfigBase ("Test", null);
			ConfigBase config2 = new ConfigBase ("Another", null);
			ConfigCollection collection = new ConfigCollection (null);
			collection.Add (config1);
			collection.Add (config2);
			
			Assert.AreEqual (2, collection.Count);
			Assert.IsNotNull (collection["Test"]);
			Assert.IsNotNull (collection["Another"]);

			collection.Remove (config2);
			Assert.AreEqual (1, collection.Count);
			Assert.IsNotNull (collection["Test"]);
			Assert.IsNull (collection["Another"]);
		}

		[Test]
		public void ConfigCollectionEvents ()
		{
			IniConfigSource source = new IniConfigSource ();
			source.Configs.ConfigAdded += 
							new ConfigEventHandler (this.source_configAdded);
			source.Configs.ConfigRemoved += 
							new ConfigEventHandler (this.source_configRemoved);

			Assert.AreEqual (configAddedCount, 0);

			eventCollection = null;
			IConfig config = source.AddConfig ("Test");
			Assert.IsTrue (source.Configs == eventCollection);
			Assert.AreEqual (configAddedCount, 1);
			Assert.AreEqual ("Test", eventConfig.Name);

			eventCollection = null;
			config = source.Configs.Add ("Test 2");
			Assert.IsTrue (source.Configs == eventCollection);
			Assert.AreEqual (configAddedCount, 2);
			Assert.AreEqual ("Test 2", eventConfig.Name);

			eventCollection = null;
			source.Configs.RemoveAt (0);
			Assert.IsTrue (source.Configs == eventCollection);
			Assert.AreEqual (configAddedCount, 2);
			Assert.AreEqual ("Test", eventConfig.Name);
		}

		[SetUp]
		public void Setup ()
		{
			eventConfig = null;
			eventCollection = null;
			configAddedCount = 0;
			configRemovedCount = 0;
		}
		#endregion

		#region Private methods
		private void source_configAdded (object sender, ConfigEventArgs e)
		{
			configAddedCount++;
			eventConfig = e.Config;
			eventCollection = (ConfigCollection)sender;
		}

		private void source_configRemoved (object sender, ConfigEventArgs e)
		{
			configRemovedCount++;
			eventConfig = e.Config;
			eventCollection = (ConfigCollection)sender;
		}
		#endregion
	}
}
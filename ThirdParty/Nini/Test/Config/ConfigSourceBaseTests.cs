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
using System.Xml;
using Nini.Config;
using NUnit.Framework;

namespace Nini.Test.Config
{
	[TestFixture]
	public class ConfigSourceBaseTests
	{
		#region Private variables
		IConfig eventConfig = null;
		IConfigSource eventSource = null;
		int reloadedCount = 0;
		int savedCount = 0;
		string keyName = null;
		string keyValue = null;
		int keySetCount = 0;
		int keyRemovedCount = 0;
		#endregion

		#region Unit tests
		[Test]
		public void Merge ()
		{
			StringWriter textWriter = new StringWriter ();
			XmlTextWriter xmlWriter = NiniWriter (textWriter);
			WriteSection (xmlWriter, "Pets");
			WriteKey (xmlWriter, "cat", "muffy");
			WriteKey (xmlWriter, "dog", "rover");
			WriteKey (xmlWriter, "bird", "tweety");
			xmlWriter.WriteEndDocument ();
			
			StringReader reader = new StringReader (textWriter.ToString ());
			XmlTextReader xmlReader = new XmlTextReader (reader);
			XmlConfigSource xmlSource = new XmlConfigSource (xmlReader);
			
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[People]");
			writer.WriteLine (" woman = Jane");
			writer.WriteLine (" man = John");
			IniConfigSource iniSource = 
					new IniConfigSource (new StringReader (writer.ToString ()));
			
			xmlSource.Merge (iniSource);
			
			IConfig config = xmlSource.Configs["Pets"];
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual ("muffy", config.Get ("cat"));
			Assert.AreEqual ("rover", config.Get ("dog"));
			
			config = xmlSource.Configs["People"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Jane", config.Get ("woman"));
			Assert.AreEqual ("John", config.Get ("man"));
		}
		
		[ExpectedException (typeof (ArgumentException))]
		public void MergeItself ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[People]");
			writer.WriteLine (" woman = Jane");
			writer.WriteLine (" man = John");
			IniConfigSource iniSource = 
					new IniConfigSource (new StringReader (writer.ToString ()));
			
			iniSource.Merge (iniSource); // exception
		}
		
		[ExpectedException (typeof (ArgumentException))]
		public void MergeExisting ()
		{
			StringWriter textWriter = new StringWriter ();
			XmlTextWriter xmlWriter = NiniWriter (textWriter);
			WriteSection (xmlWriter, "Pets");
			WriteKey (xmlWriter, "cat", "muffy");
			xmlWriter.WriteEndDocument ();
			
			StringReader reader = new StringReader (xmlWriter.ToString ());
			XmlTextReader xmlReader = new XmlTextReader (reader);
			XmlConfigSource xmlSource = new XmlConfigSource (xmlReader);
			
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[People]");
			writer.WriteLine (" woman = Jane");
			IniConfigSource iniSource = 
					new IniConfigSource (new StringReader (writer.ToString ()));
			
			xmlSource.Merge (iniSource);
			xmlSource.Merge (iniSource); // exception
		}
		
		[Test]
		public void AutoSave ()
		{
			string filePath = "AutoSaveTest.ini";

			StreamWriter writer = new StreamWriter (filePath);
			writer.WriteLine ("; some comment");
			writer.WriteLine ("[new section]");
			writer.WriteLine (" dog = Rover");
			writer.WriteLine (""); // empty line
			writer.WriteLine ("; a comment");
			writer.WriteLine (" cat = Muffy");
			writer.Close ();
			
			IniConfigSource source = new IniConfigSource (filePath);
			source.AutoSave = true;
			IConfig config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			config.Set ("dog", "Spots");
			config.Set ("cat", "Misha");
			
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual ("Misha", config.Get ("cat"));
			
			source = new IniConfigSource (filePath);
			config = source.Configs["new section"];
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual ("Misha", config.Get ("cat"));
			
			File.Delete (filePath);
		}
		
		[Test]
		public void AddConfig ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Test]");
			writer.WriteLine (" bool 1 = TrUe");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));

			IConfig newConfig = source.AddConfig ("NewConfig");
			newConfig.Set ("NewKey", "NewValue");
			newConfig.Set ("AnotherKey", "AnotherValue");
			
			IConfig config = source.Configs["NewConfig"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("NewValue", config.Get ("NewKey"));
			Assert.AreEqual ("AnotherValue", config.Get ("AnotherKey"));
		}
		
		[Test]
		public void ExpandText ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Test]");
			writer.WriteLine (" author = Brent");
			writer.WriteLine (" domain = ${protocol}://nini.sf.net/");
			writer.WriteLine (" apache = Apache implements ${protocol}");
			writer.WriteLine (" developer = author of Nini: ${author} !");
			writer.WriteLine (" love = We love the ${protocol} protocol");
			writer.WriteLine (" combination = ${author} likes ${protocol}");
			writer.WriteLine (" fact = fact: ${apache}");
			writer.WriteLine (" protocol = http");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));
			source.ExpandKeyValues ();

			IConfig config = source.Configs["Test"];
			Assert.AreEqual ("http", config.Get ("protocol"));
			Assert.AreEqual ("fact: Apache implements http", config.Get ("fact"));
			Assert.AreEqual ("http://nini.sf.net/", config.Get ("domain"));
			Assert.AreEqual ("Apache implements http", config.Get ("apache"));
			Assert.AreEqual ("We love the http protocol", config.Get ("love"));
			Assert.AreEqual ("author of Nini: Brent !", config.Get ("developer"));
			Assert.AreEqual ("Brent likes http", config.Get ("combination"));
		}
		
		[Test]
		public void ExpandTextOtherSection ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[web]");
			writer.WriteLine (" apache = Apache implements ${protocol}");
			writer.WriteLine (" protocol = http");
			writer.WriteLine ("[server]");
			writer.WriteLine (" domain = ${web|protocol}://nini.sf.net/");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));
			source.ExpandKeyValues ();

			IConfig config = source.Configs["web"];
			Assert.AreEqual ("http", config.Get ("protocol"));
			Assert.AreEqual ("Apache implements http", config.Get ("apache"));
			config = source.Configs["server"];
			Assert.AreEqual ("http://nini.sf.net/", config.Get ("domain"));
		}
		
		[Test]
		public void ExpandKeyValuesMerge ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[web]");
			writer.WriteLine (" protocol = http");
			writer.WriteLine ("[server]");
			writer.WriteLine (" domain1 = ${web|protocol}://nini.sf.net/");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));
									
			StringWriter newWriter = new StringWriter ();
			newWriter.WriteLine ("[web]");
			newWriter.WriteLine (" apache = Apache implements ${protocol}");
			newWriter.WriteLine ("[server]");
			newWriter.WriteLine (" domain2 = ${web|protocol}://nini.sf.net/");
			IniConfigSource newSource = new IniConfigSource 
									(new StringReader (newWriter.ToString ()));
			source.Merge (newSource);
			source.ExpandKeyValues ();

			IConfig config = source.Configs["web"];
			Assert.AreEqual ("http", config.Get ("protocol"));
			Assert.AreEqual ("Apache implements http", config.Get ("apache"));
			config = source.Configs["server"];
			Assert.AreEqual ("http://nini.sf.net/", config.Get ("domain1"));
			Assert.AreEqual ("http://nini.sf.net/", config.Get ("domain2"));
		}
		
		[Test]
		public void AddNewConfigsAndKeys ()
		{
			// Add some new configs and keys here and test.
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine (" cat = muffy");
			writer.WriteLine (" dog = rover");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));
			
			IConfig config = source.Configs["Pets"];
			Assert.AreEqual ("Pets", config.Name);
			Assert.AreEqual (2, config.GetKeys ().Length);
			
			IConfig newConfig = source.AddConfig ("NewTest");
			newConfig.Set ("Author", "Brent");
			newConfig.Set ("Birthday", "February 8th");
			
			newConfig = source.AddConfig ("AnotherNew");
			
			Assert.AreEqual (3, source.Configs.Count);
			config = source.Configs["NewTest"];
			Assert.IsNotNull (config);
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("February 8th", config.Get ("Birthday"));
			Assert.AreEqual ("Brent", config.Get ("Author"));
		}

		[Test]
		public void GetBooleanSpace ()
		{
			StringWriter textWriter = new StringWriter ();
			XmlTextWriter xmlWriter = NiniWriter (textWriter);
			WriteSection (xmlWriter, "Pets");
			WriteKey (xmlWriter, "cat", "muffy");
			WriteKey (xmlWriter, "dog", "rover");
			WriteKey (xmlWriter, "Is Mammal", "False");
			xmlWriter.WriteEndDocument ();
			
			StringReader reader = new StringReader (textWriter.ToString ());
			XmlTextReader xmlReader = new XmlTextReader (reader);
			XmlConfigSource source = new XmlConfigSource (xmlReader);

			source.Alias.AddAlias ("true", true);
			source.Alias.AddAlias ("false", false);
			
			Assert.IsFalse (source.Configs["Pets"].GetBoolean ("Is Mammal", false));
		}

		[Test]
		public void RemoveNonExistingKey ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine (" cat = muffy");
			writer.WriteLine (" dog = rover");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));
			
			// This should not throw an exception
			source.Configs["Pets"].Remove ("Not here");
		}

		[Test]
		public void SavingWithNonStrings ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine (" cat = muffy");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));
			
			StringWriter newWriter = new StringWriter ();
			IConfig config = source.Configs["Pets"];
			Assert.AreEqual ("Pets", config.Name);
			config.Set ("count", 1);

			source.Save (newWriter);
		}

		[Test]
		public void ConfigSourceEvents ()
		{
			string filePath = "EventTest.ini";
			IniConfigSource source = new IniConfigSource ();
			source.Saved += new EventHandler (this.source_saved);
			source.Reloaded += new EventHandler (this.source_reloaded);

			Assert.IsNull (eventConfig);
			Assert.IsNull (eventSource);

			IConfig config = source.AddConfig ("Test");

			eventSource = null;
			Assert.AreEqual (savedCount, 0);
			source.Save (filePath);
			Assert.AreEqual (savedCount, 1);
			Assert.IsTrue (source == eventSource);

			eventSource = null;
			source.Save ();
			Assert.AreEqual (savedCount, 2);
			Assert.IsTrue (source == eventSource);

			eventSource = null;
			Assert.AreEqual (reloadedCount, 0);
			source.Reload ();
			Assert.AreEqual (reloadedCount, 1);
			Assert.IsTrue (source == eventSource);

			File.Delete (filePath);
		}

		[Test]
		public void ConfigEvents ()
		{
			IConfigSource source = new IniConfigSource ();

			IConfig config = source.AddConfig ("Test");
			config.KeySet += new ConfigKeyEventHandler (this.config_keySet);
			config.KeyRemoved += new ConfigKeyEventHandler (this.config_keyRemoved);

			// Set key events
			Assert.AreEqual (keySetCount, 0);

			config.Set ("Test 1", "Value 1");
			Assert.AreEqual (keySetCount, 1);
			Assert.AreEqual ("Test 1", keyName);
			Assert.AreEqual ("Value 1", keyValue);

			config.Set ("Test 2", "Value 2");
			Assert.AreEqual (keySetCount, 2);
			Assert.AreEqual ("Test 2", keyName);
			Assert.AreEqual ("Value 2", keyValue);

			// Remove key events
			Assert.AreEqual (keyRemovedCount, 0);

			config.Remove ("Test 1");
			Assert.AreEqual (keyRemovedCount, 1);
			Assert.AreEqual ("Test 1", keyName);
			Assert.AreEqual ("Value 1", keyValue);

			config.Remove ("Test 2");
			Assert.AreEqual (keyRemovedCount, 2);
			Assert.AreEqual ("Test 2", keyName);
			Assert.AreEqual ("Value 2", keyValue);
		}

		[Test]
		public void ExpandKeyValuesConfigError ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[server]");
			writer.WriteLine (" domain1 = ${web|protocol}://nini.sf.net/");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));

			try {
				source.ExpandKeyValues ();
			}
			catch (Exception ex) {
				Assert.AreEqual ("Expand config not found: web", ex.Message);
			}
		}

		[Test]
		public void ExpandKeyValuesKeyError ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[web]");
			writer.WriteLine ("not-protocol = hah!");
			writer.WriteLine ("[server]");
			writer.WriteLine (" domain1 = ${web|protocol}://nini.sf.net/");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));

			try {
				source.ExpandKeyValues ();
			}
			catch (Exception ex) {
				Assert.AreEqual ("Expand key not found: protocol", ex.Message);
			}
		}

		[Test]
		public void ExpandKeyInfiniteRecursion ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[replace]");
			writer.WriteLine ("test = ${test} broken");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));

			try {
				source.ExpandKeyValues ();
			}
			catch (ArgumentException ex) {
				Assert.AreEqual 
					("Key cannot have a expand value of itself: test", ex.Message);
			}
		}

		[Test]
		public void ConfigBaseGetErrors ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[web]");
			writer.WriteLine ("; No keys");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));
			IConfig config = source.Configs["web"];

			try {
				config.GetInt ("not_there");
			} catch (Exception ex) {
				Assert.AreEqual ("Value not found: not_there", ex.Message);
			}
			try {
				config.GetFloat ("not_there");
			} catch (Exception ex) {
				Assert.AreEqual ("Value not found: not_there", ex.Message);
			}
			try {
				config.GetDouble ("not_there");
			} catch (Exception ex) {
				Assert.AreEqual ("Value not found: not_there", ex.Message);
			}
			try {
				config.GetLong ("not_there");
			} catch (Exception ex) {
				Assert.AreEqual ("Value not found: not_there", ex.Message);
			}
			try {
				config.GetBoolean ("not_there");
			} catch (Exception ex) {
				Assert.AreEqual ("Value not found: not_there", ex.Message);
			}
		}

		[SetUp]
		public void Setup ()
		{
			eventConfig = null;
			eventSource = null;
			savedCount = 0;
			keySetCount = 0;
			keyRemovedCount = 0;
		}
		#endregion

		#region Private methods
		private void source_saved (object sender, EventArgs e)
		{
			savedCount++;
			eventSource = (IConfigSource)sender;
		}

		private void source_reloaded (object sender, EventArgs e)
		{
			reloadedCount++;
			eventSource = (IConfigSource)sender;
		}

		private void config_keySet (object sender, ConfigKeyEventArgs e)
		{
			keySetCount++;
			keyName = e.KeyName;
			keyValue = e.KeyValue;
			eventConfig = (IConfig)sender;
		}

		private void config_keyRemoved (object sender, ConfigKeyEventArgs e)
		{
			keyRemovedCount++;
			keyName = e.KeyName;
			keyValue = e.KeyValue;
			eventConfig = (IConfig)sender;
		}

		private XmlTextWriter NiniWriter (TextWriter writer)
		{
			XmlTextWriter result = new XmlTextWriter (writer);
			result.WriteStartDocument ();
			result.WriteStartElement ("Nini");
			
			return result;
		}
		
		private void WriteSection (XmlWriter writer, string sectionName)
		{
			writer.WriteStartElement ("Section");
			writer.WriteAttributeString ("Name", sectionName);
		}
		
		private void WriteKey (XmlWriter writer, string key, string value)
		{
			writer.WriteStartElement ("Key");
			writer.WriteAttributeString ("Name", key);
			writer.WriteAttributeString ("Value", value);
			writer.WriteEndElement ();
		}
		#endregion
	}
}

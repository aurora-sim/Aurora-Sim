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
using NUnit.Framework;
using Nini.Config;

namespace Nini.Test.Config
{
	[TestFixture]
	public class DotNetConfigSourceTests
	{
		#region Tests
		[Test]
		public void GetConfig ()
		{
			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Pets");
			AddKey (doc, "Pets", "cat", "muffy");
			AddKey (doc, "Pets", "dog", "rover");
			AddKey (doc, "Pets", "bird", "tweety");

			DotNetConfigSource source = 
							new DotNetConfigSource (DocumentToReader (doc));
			
			IConfig config = source.Configs["Pets"];
			Assert.AreEqual ("Pets", config.Name);
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual (source, config.ConfigSource);
		}
		
		[Test]
		public void GetString ()
		{
			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Pets");
			AddKey (doc, "Pets", "cat", "muffy");
			AddKey (doc, "Pets", "dog", "rover");
			AddKey (doc, "Pets", "bird", "tweety");

			DotNetConfigSource source = 
							new DotNetConfigSource (DocumentToReader (doc));
			IConfig config = source.Configs["Pets"];
			
			Assert.AreEqual ("muffy", config.Get ("cat"));
			Assert.AreEqual ("rover", config.Get ("dog"));
			Assert.AreEqual ("muffy", config.GetString ("cat"));
			Assert.AreEqual ("rover", config.GetString ("dog"));
			Assert.AreEqual ("my default", config.Get ("Not Here", "my default"));
			Assert.IsNull (config.Get ("Not Here 2"));
		}
		
		[Test]
		public void GetInt ()
		{
			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Pets");
			AddKey (doc, "Pets", "value 1", "49588");

			DotNetConfigSource source = 
							new DotNetConfigSource (DocumentToReader (doc));

			IConfig config = source.Configs["Pets"];
			
			Assert.AreEqual (49588, config.GetInt ("value 1"));
			Assert.AreEqual (12345, config.GetInt ("Not Here", 12345));
			
			try
			{
				config.GetInt ("Not Here Also");
				Assert.Fail ();
			}
			catch
			{
			}
		}

		[Test]
		public void SetAndSave ()
		{
			string filePath = "Test.xml";

			XmlDocument doc = NiniDoc ();
			AddSection (doc, "NewSection");
			AddKey (doc, "NewSection", "dog", "Rover");
			AddKey (doc, "NewSection", "cat", "Muffy");
			doc.Save (filePath);

			DotNetConfigSource source = new DotNetConfigSource (filePath);
			
			IConfig config = source.Configs["NewSection"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			config.Set ("dog", "Spots");
			config.Set ("cat", "Misha");
			config.Set ("DoesNotExist", "SomeValue");
			
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual ("Misha", config.Get ("cat"));
			Assert.AreEqual ("SomeValue", config.Get ("DoesNotExist"));
			source.Save ();
			
			source = new DotNetConfigSource (filePath);
			config = source.Configs["NewSection"];
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual ("Misha", config.Get ("cat"));
			Assert.AreEqual ("SomeValue", config.Get ("DoesNotExist"));
			
			File.Delete (filePath);
		}
		
		[Test]
		public void MergeAndSave ()
		{
			string xmlFileName = "NiniConfig.xml";

			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Pets");
			AddKey (doc, "Pets", "cat", "Muffy");
			AddKey (doc, "Pets", "dog", "Rover");
			AddKey (doc, "Pets", "bird", "Tweety");
			doc.Save (xmlFileName);
			
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine ("cat = Becky"); // overwrite
			writer.WriteLine ("lizard = Saurus"); // new
			writer.WriteLine ("[People]");
			writer.WriteLine (" woman = Jane");
			writer.WriteLine (" man = John");
			IniConfigSource iniSource = new IniConfigSource 
									(new StringReader (writer.ToString ()));

			DotNetConfigSource xmlSource = new DotNetConfigSource (xmlFileName);

			xmlSource.Merge (iniSource);
			
			IConfig config = xmlSource.Configs["Pets"];
			Assert.AreEqual (4, config.GetKeys ().Length);
			Assert.AreEqual ("Becky", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Saurus", config.Get ("lizard"));
		
			config = xmlSource.Configs["People"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Jane", config.Get ("woman"));
			Assert.AreEqual ("John", config.Get ("man"));
			
			config.Set ("woman", "Tara");
			config.Set ("man", "Quentin");
			
			xmlSource.Save ();
			
			xmlSource = new DotNetConfigSource (xmlFileName);
			
			config = xmlSource.Configs["Pets"];
			Assert.AreEqual (4, config.GetKeys ().Length);
			Assert.AreEqual ("Becky", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Saurus", config.Get ("lizard"));
			
			config = xmlSource.Configs["People"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Tara", config.Get ("woman"));
			Assert.AreEqual ("Quentin", config.Get ("man"));
			
			File.Delete  (xmlFileName);
		}
		
		[Test]
		public void SaveToNewPath ()
		{
			string filePath = "Test.xml";
			string newPath = "TestNew.xml";
			
			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Pets");
			AddKey (doc, "Pets", "cat", "Muffy");
			AddKey (doc, "Pets", "dog", "Rover");
			doc.Save (filePath);

			DotNetConfigSource source = new DotNetConfigSource (filePath);
			IConfig config = source.Configs["Pets"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			source.Save (newPath);
			
			source = new DotNetConfigSource (newPath);
			config = source.Configs["Pets"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			File.Delete (filePath);
			File.Delete (newPath);
		}
		
		[Test]
		public void SaveToWriter ()
		{
			string newPath = "TestNew.xml";

			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Pets");
			AddKey (doc, "Pets", "cat", "Muffy");
			AddKey (doc,  "Pets","dog", "Rover");

			DotNetConfigSource source = 
							new DotNetConfigSource (DocumentToReader (doc));
			IConfig config = source.Configs["Pets"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			StreamWriter textWriter = new StreamWriter (newPath);
			source.Save (textWriter);
			textWriter.Close (); // save to disk
			
			source = new DotNetConfigSource (newPath);
			config = source.Configs["Pets"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			File.Delete (newPath);
		}

		[Test]
		public void ReplaceText ()
		{		
			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Test");
			AddKey (doc, "Test", "author", "Brent");
			AddKey (doc, "Test", "domain", "${protocol}://nini.sf.net/");
			AddKey (doc, "Test", "apache", "Apache implements ${protocol}");
			AddKey (doc, "Test", "developer", "author of Nini: ${author} !");
			AddKey (doc, "Test", "love", "We love the ${protocol} protocol");
			AddKey (doc, "Test", "combination", "${author} likes ${protocol}");
			AddKey (doc, "Test", "fact", "fact: ${apache}");
			AddKey (doc, "Test", "protocol", "http");

			DotNetConfigSource source = 
							new DotNetConfigSource (DocumentToReader (doc));
			source.ReplaceKeyValues ();
			
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
		public void SaveNewSection ()
		{
			string filePath = "Test.xml";

			XmlDocument doc = NiniDoc ();
			AddSection (doc, "NewSection");
			AddKey (doc, "NewSection", "dog", "Rover");
			AddKey (doc, "NewSection", "cat", "Muffy");
			doc.Save (filePath);

			DotNetConfigSource source = new DotNetConfigSource (filePath);
			IConfig config = source.AddConfig ("test");
			Assert.IsNotNull (source.Configs["test"]);
			source.Save ();
			
			source = new DotNetConfigSource (filePath);
			config = source.Configs["NewSection"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			Assert.IsNotNull (source.Configs["test"]);
			
			File.Delete (filePath);
		}

		[Test]
		public void ToStringTest ()
		{
			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Pets");
			AddKey (doc, "Pets", "cat", "Muffy");
			AddKey (doc, "Pets", "dog", "Rover");

			DotNetConfigSource source = 
							new DotNetConfigSource (DocumentToReader (doc));
			string eol = Environment.NewLine;

			string compare = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" + eol
							 + "<configuration>" + eol
							 + "  <configSections>" + eol
							 + "    <section name=\"Pets\" "
							 + "type=\"System.Configuration.NameValueSectionHandler\" />" + eol
							 + "  </configSections>" + eol
							 + "  <Pets>" + eol
							 + "    <add key=\"cat\" value=\"Muffy\" />" + eol
							 + "    <add key=\"dog\" value=\"Rover\" />" + eol
							 + "  </Pets>" + eol
							 + "</configuration>";
			Assert.AreEqual (compare, source.ToString ());
		}

		[Test]
		public void EmptyConstructor ()
		{
			string filePath = "EmptyConstructor.xml";
			DotNetConfigSource source = new DotNetConfigSource ();

			IConfig config = source.AddConfig ("Pets");
			config.Set ("cat", "Muffy");
			config.Set ("dog", "Rover");
			config.Set ("bird", "Tweety");
			source.Save (filePath);

			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Tweety", config.Get ("bird"));

			source = new DotNetConfigSource (filePath);
			config = source.Configs["Pets"];
			
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Tweety", config.Get ("bird"));

			File.Delete (filePath);
		}

		[Test]
		public void Reload ()
		{
			string filePath = "ReloadDot.xml";
			DotNetConfigSource source = new DotNetConfigSource ();

			IConfig petConfig = source.AddConfig ("Pets");
			petConfig.Set ("cat", "Muffy");
			petConfig.Set ("dog", "Rover");
			IConfig weatherConfig = source.AddConfig ("Weather");
			weatherConfig.Set ("skies", "cloudy");
			weatherConfig.Set ("precipitation", "rain");
			source.Save (filePath);

			Assert.AreEqual (2, petConfig.GetKeys ().Length);
			Assert.AreEqual ("Muffy", petConfig.Get ("cat"));
			Assert.AreEqual (2, source.Configs.Count);

			DotNetConfigSource newSource = new DotNetConfigSource (filePath);

			IConfig compareConfig = newSource.Configs["Pets"];
			Assert.AreEqual (2, compareConfig.GetKeys ().Length);
			Assert.AreEqual ("Muffy", compareConfig.Get ("cat"));
			Assert.IsTrue (compareConfig == newSource.Configs["Pets"],
							"References before are not equal");

			// Set the new values to source
			source.Configs["Pets"].Set ("cat", "Misha");
			source.Configs["Pets"].Set ("lizard", "Lizzy");
			source.Configs["Pets"].Set ("hampster", "Surly");
			source.Configs["Pets"].Remove ("dog");
			source.Configs.Remove (weatherConfig);
			source.Save (); // saves new value

			// Reload the new source and check for changes
			newSource.Reload ();
			Assert.IsTrue (compareConfig == newSource.Configs["Pets"],
							"References after are not equal");
			Assert.AreEqual (1, newSource.Configs.Count);
			Assert.AreEqual (3, newSource.Configs["Pets"].GetKeys ().Length);
			Assert.AreEqual ("Lizzy", newSource.Configs["Pets"].Get ("lizard"));
			Assert.AreEqual ("Misha", newSource.Configs["Pets"].Get ("cat"));
			Assert.IsNull (newSource.Configs["Pets"].Get ("dog"));

			File.Delete (filePath);
		}

		[Test]
		public void SaveToStream ()
		{
			string filePath = "SaveToStream.ini";
			FileStream stream = new FileStream (filePath, FileMode.Create);

			// Create a new document and save to stream
			DotNetConfigSource source = new DotNetConfigSource ();
			IConfig config = source.AddConfig ("Pets");
			config.Set ("dog", "rover");
			config.Set ("cat", "muffy");
			source.Save (stream);
			stream.Close ();

			DotNetConfigSource newSource = new DotNetConfigSource (filePath);
			config = newSource.Configs["Pets"];
			Assert.IsNotNull (config);
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("rover", config.GetString ("dog"));
			Assert.AreEqual ("muffy", config.GetString ("cat"));
			
			stream.Close ();

			File.Delete (filePath);
		}

		[Test]
		public void NoConfigSectionsNode ()
		{
			string filePath = "AppSettings.xml";

			// Create an XML document with no configSections node
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml ("<configuration></configuration>");

			XmlNode node = doc.CreateElement ("appSettings");
			doc.DocumentElement.AppendChild (node);
			AddKey (doc, "appSettings", "Test", "Hello");
			

			doc.Save (filePath);

			DotNetConfigSource source = new DotNetConfigSource (filePath);
			
			IConfig config = source.Configs["appSettings"];
			Assert.AreEqual ("Hello", config.GetString ("Test"));

			File.Delete (filePath);
		}

		[Test]
		public void LoadReader ()
		{
			XmlDocument doc = NiniDoc ();
			AddSection (doc, "Pets");
			AddKey (doc, "Pets", "cat", "muffy");
			AddKey (doc, "Pets", "dog", "rover");
			AddKey (doc, "Pets", "bird", "tweety");

			DotNetConfigSource source = 
							new DotNetConfigSource (DocumentToReader (doc));
			
			IConfig config = source.Configs["Pets"];
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual ("rover", config.Get ("dog"));

			config.Set ("dog", "new name");
			config.Remove ("bird");

			source.Load (DocumentToReader (doc));

			config = source.Configs["Pets"];
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual ("rover", config.Get ("dog"));
		}
		#endregion

		#region Private methods
		private XmlDocument NiniDoc ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml ("<configuration><configSections/></configuration>");
			
			return doc;
		}
		
		private void AddSection (XmlDocument doc, string sectionName)
		{
			XmlNode node = doc.SelectSingleNode ("/configuration/configSections");
			
			XmlNode sectionNode = doc.CreateElement ("section");
			node.AppendChild (sectionNode);

			XmlNode attrNode = doc.CreateAttribute ("name");
			attrNode.Value = sectionName;
			sectionNode.Attributes.SetNamedItem (attrNode);

			attrNode = doc.CreateAttribute ("type");
			attrNode.Value = "System.Configuration.NameValueSectionHandler";
			sectionNode.Attributes.SetNamedItem (attrNode);
			
			if (sectionName.IndexOf (' ') != -1) {
				Console.WriteLine (sectionName);
			}
			sectionNode = doc.CreateElement (sectionName);
			doc.DocumentElement.AppendChild (sectionNode);
		}
		
		private void AddKey (XmlDocument doc, string section, 
							 string key, string value)
		{
			XmlNode sectionNode = doc.SelectSingleNode ("/configuration/" + section);

			XmlNode keyNode = doc.CreateElement ("add");
			XmlNode attrNode = doc.CreateAttribute ("key");
			attrNode.Value = key;
			keyNode.Attributes.SetNamedItem (attrNode);
			
			attrNode = doc.CreateAttribute ("value");
			attrNode.Value = value;
			keyNode.Attributes.SetNamedItem (attrNode);
			
			sectionNode.AppendChild (keyNode);
		}

		private XmlTextReader DocumentToReader (XmlDocument doc)
		{
			StringReader reader = new StringReader (doc.OuterXml);
			return new XmlTextReader (reader);
		}
		#endregion
	}
}

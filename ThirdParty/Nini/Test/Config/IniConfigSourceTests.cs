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
	public class IniConfigSourceTests
	{
		[Test]
		public void SetAndSave ()
		{
			string filePath = "Test.ini";

			StreamWriter writer = new StreamWriter (filePath);
			writer.WriteLine ("; some comment");
			writer.WriteLine ("[new section]");
			writer.WriteLine (" dog = Rover");
			writer.WriteLine (""); // empty line
			writer.WriteLine ("; a comment");
			writer.WriteLine (" cat = Muffy");
			writer.Close ();
			
			IniConfigSource source = new IniConfigSource (filePath);
			IConfig config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			config.Set ("dog", "Spots");
			config.Set ("cat", "Misha");
			config.Set ("DoesNotExist", "SomeValue");
			
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual ("Misha", config.Get ("cat"));
			Assert.AreEqual ("SomeValue", config.Get ("DoesNotExist"));
			source.Save ();
			
			source = new IniConfigSource (filePath);
			config = source.Configs["new section"];
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual ("Misha", config.Get ("cat"));
			Assert.AreEqual ("SomeValue", config.Get ("DoesNotExist"));
			
			File.Delete (filePath);
		}
		
		[Test]
		public void MergeAndSave ()
		{
			string fileName = "NiniConfig.ini";

			StreamWriter fileWriter = new StreamWriter (fileName);
			fileWriter.WriteLine ("[Pets]");
			fileWriter.WriteLine ("cat = Muffy"); // overwrite
			fileWriter.WriteLine ("dog = Rover"); // new
			fileWriter.WriteLine ("bird = Tweety");
			fileWriter.Close ();
			
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine ("cat = Becky"); // overwrite
			writer.WriteLine ("lizard = Saurus"); // new
			writer.WriteLine ("[People]");
			writer.WriteLine (" woman = Jane");
			writer.WriteLine (" man = John");
			IniConfigSource iniSource = new IniConfigSource 
									(new StringReader (writer.ToString ()));

			IniConfigSource source = new IniConfigSource (fileName);

			source.Merge (iniSource);
			
			IConfig config = source.Configs["Pets"];
			Assert.AreEqual (4, config.GetKeys ().Length);
			Assert.AreEqual ("Becky", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Saurus", config.Get ("lizard"));
		
			config = source.Configs["People"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Jane", config.Get ("woman"));
			Assert.AreEqual ("John", config.Get ("man"));
			
			config.Set ("woman", "Tara");
			config.Set ("man", "Quentin");
			
			source.Save ();
			
			source = new IniConfigSource (fileName);
			
			config = source.Configs["Pets"];
			Assert.AreEqual (4, config.GetKeys ().Length);
			Assert.AreEqual ("Becky", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Saurus", config.Get ("lizard"));
			
			config = source.Configs["People"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Tara", config.Get ("woman"));
			Assert.AreEqual ("Quentin", config.Get ("man"));
			
			File.Delete  (fileName);
		}
		
		[Test]
		public void SaveToNewPath ()
		{
			string filePath = "Test.ini";
			string newPath = "TestNew.ini";

			StreamWriter writer = new StreamWriter (filePath);
			writer.WriteLine ("; some comment");
			writer.WriteLine ("[new section]");
			writer.WriteLine (" dog = Rover");
			writer.WriteLine (" cat = Muffy");
			writer.Close ();
			
			IniConfigSource source = new IniConfigSource (filePath);
			IConfig config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			source.Save (newPath);
			
			source = new IniConfigSource (newPath);
			config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			File.Delete (filePath);
			File.Delete (newPath);
		}
		
		[Test]
		public void SaveToWriter ()
		{
			string newPath = "TestNew.ini";

			StringWriter writer = new StringWriter ();
			writer.WriteLine ("; some comment");
			writer.WriteLine ("[new section]");
			writer.WriteLine (" dog = Rover");
			writer.WriteLine (" cat = Muffy");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));

			Assert.IsNull (source.SavePath);
			IConfig config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			StreamWriter textWriter = new StreamWriter (newPath);
			source.Save (textWriter);
			textWriter.Close (); // save to disk
			
			source = new IniConfigSource (newPath);
			Assert.AreEqual (newPath, source.SavePath);
			config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			File.Delete (newPath);
		}
		
		[Test]
		public void SaveAfterTextWriter ()
		{
			string filePath = "Test.ini";

			StreamWriter writer = new StreamWriter (filePath);
			writer.WriteLine ("[new section]");
			writer.WriteLine (" dog = Rover");
			writer.Close ();

			IniConfigSource source = new IniConfigSource (filePath);
			Assert.AreEqual (filePath, source.SavePath);
			StringWriter textWriter = new StringWriter ();
			source.Save (textWriter);
			Assert.IsNull (source.SavePath);

			File.Delete (filePath);
		}
		
		[Test]
		public void SaveNewSection ()
		{
			string filePath = "Test.xml";

			StringWriter writer = new StringWriter ();
			writer.WriteLine ("; some comment");
			writer.WriteLine ("[new section]");
			writer.WriteLine (" dog = Rover");
			writer.WriteLine (" cat = Muffy");
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));
			
			IConfig config = source.AddConfig ("test");
			Assert.IsNotNull (source.Configs["test"]);
			source.Save (filePath);
			
			source = new IniConfigSource (filePath);
			config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			Assert.IsNotNull (source.Configs["test"]);
			
			File.Delete (filePath);
		}
		
		[Test]
		public void RemoveConfigAndKeyFromFile ()
		{
			string filePath = "Test.ini";

			StreamWriter writer = new StreamWriter (filePath);
			writer.WriteLine ("[test 1]");
			writer.WriteLine (" dog = Rover");
			writer.WriteLine ("[test 2]");
			writer.WriteLine (" cat = Muffy");
			writer.WriteLine (" lizard = Lizzy");
			writer.Close ();

			IniConfigSource source = new IniConfigSource (filePath);
			Assert.IsNotNull (source.Configs["test 1"]);
			Assert.IsNotNull (source.Configs["test 2"]);
			Assert.IsNotNull (source.Configs["test 2"].Get ("cat"));
			
			source.Configs.Remove (source.Configs["test 1"]);
			source.Configs["test 2"].Remove ("cat");
			source.AddConfig ("cause error");
			source.Save ();

			source = new IniConfigSource (filePath);
			Assert.IsNull (source.Configs["test 1"]);
			Assert.IsNotNull (source.Configs["test 2"]);
			Assert.IsNull (source.Configs["test 2"].Get ("cat"));

			File.Delete (filePath);
		}

		[Test]
		public void ToStringTest ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Test]");
			writer.WriteLine (" cat = muffy");
			writer.WriteLine (" dog = rover");
			writer.WriteLine (" bird = tweety");
			IniConfigSource source = 
				new IniConfigSource (new StringReader (writer.ToString ()));

			string eol = Environment.NewLine;

			string compare = "[Test]" + eol
							 + "cat = muffy" + eol
							 + "dog = rover" + eol
							 + "bird = tweety" + eol;
			Assert.AreEqual (compare, source.ToString ());
		}

		[Test]
		public void EmptyConstructor ()
		{
			string filePath = "EmptyConstructor.ini";
			IniConfigSource source = new IniConfigSource ();

			IConfig config = source.AddConfig ("Pets");
			config.Set ("cat", "Muffy");
			config.Set ("dog", "Rover");
			config.Set ("bird", "Tweety");
			source.Save (filePath);

			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Tweety", config.Get ("bird"));

			source = new IniConfigSource (filePath);
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
			string filePath = "Reload.ini";

			// Create the original source file
			IniConfigSource source = new IniConfigSource ();

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

			// Create another source file to set values and reload
			IniConfigSource newSource = new IniConfigSource (filePath);

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
		public void FileClosedOnParseError ()
		{
			string filePath = "Reload.ini";

			StreamWriter writer = new StreamWriter (filePath);
			writer.WriteLine (" no section = boom!");
			writer.Close ();

			try {
				IniConfigSource source = new IniConfigSource (filePath);
			} catch {
				// The file was still opened on a parse error
				File.Delete (filePath);
			}
		}

		[Test]
		public void SaveToStream ()
		{
			string filePath = "SaveToStream.ini";
			FileStream stream = new FileStream (filePath, FileMode.Create);

			// Create a new document and save to stream
			IniConfigSource source = new IniConfigSource ();
			IConfig config = source.AddConfig ("Pets");
			config.Set ("dog", "rover");
			config.Set ("cat", "muffy");
			source.Save (stream);
			stream.Close ();

			IniConfigSource newSource = new IniConfigSource (filePath);
			config = newSource.Configs["Pets"];
			Assert.IsNotNull (config);
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("rover", config.GetString ("dog"));
			Assert.AreEqual ("muffy", config.GetString ("cat"));
			
			stream.Close ();

			File.Delete (filePath);
		}

		[Test]
		public void CaseInsensitive()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine ("cat = Becky"); // overwrite
			IniConfigSource source = new IniConfigSource 
									(new StringReader (writer.ToString ()));

			source.CaseSensitive = false;
			Assert.AreEqual("Becky", source.Configs["Pets"].Get("CAT"));

			source.Configs["Pets"].Set("cAT", "New Name");
			Assert.AreEqual("New Name", source.Configs["Pets"].Get("CAt"));

			source.Configs["Pets"].Remove("CAT");
			Assert.IsNull(source.Configs["Pets"].Get("CaT"));
		}

		[Test]
		public void LoadPath ()
		{
			string filePath = "Test.ini";

			StreamWriter writer = new StreamWriter (filePath);
			writer.WriteLine ("; some comment");
			writer.WriteLine ("[new section]");
			writer.WriteLine (" dog = Rover");
			writer.WriteLine (""); // empty line
			writer.WriteLine ("; a comment");
			writer.WriteLine (" cat = Muffy");
			writer.Close ();
			
			IniConfigSource source = new IniConfigSource (filePath);
			IConfig config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			config.Set ("dog", "Spots");
			config.Set ("cat", "Misha");
			config.Set ("DoesNotExist", "SomeValue");
			
			source.Load (filePath);

			config = config = source.Configs["new section"];
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			
			File.Delete (filePath);
		}
	}
}
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
using Nini.Ini;
using NUnit.Framework;

namespace Nini.Test.Ini
{
	[TestFixture]
	public class IniDocumentTests
	{
		[Test]
		public void GetSection ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("; Test");
			writer.WriteLine ("[Nini Thing]");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
			
			Assert.AreEqual (1, doc.Sections.Count);
			Assert.AreEqual ("Nini Thing", doc.Sections["Nini Thing"].Name);
			Assert.AreEqual ("Nini Thing", doc.Sections[0].Name);
			Assert.IsNull (doc.Sections["Non Existant"]);
		}
		
		[Test]
		public void GetKey ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Nini]");
			writer.WriteLine (" my key = something");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
			
			IniSection section = doc.Sections["Nini"];
			Assert.IsTrue (section.Contains ("my key"));
			Assert.AreEqual ("something", section.GetValue ("my key"));
			Assert.IsFalse (section.Contains ("not here"));
		}

		[Test]
		public void SetSection ()
		{
			IniDocument doc = new IniDocument ();

			IniSection section = new IniSection ("new section");
			doc.Sections.Add (section);
			Assert.AreEqual ("new section", doc.Sections[0].Name);
			Assert.AreEqual ("new section", doc.Sections["new section"].Name);
			
			section = new IniSection ("a section", "a comment");
			doc.Sections.Add (section);
			Assert.AreEqual ("a comment", doc.Sections[1].Comment);
		}

		[Test]
		public void SetKey ()
		{
			IniDocument doc = new IniDocument ();
			
			IniSection section = new IniSection ("new section");
			doc.Sections.Add (section);

			section.Set ("new key", "some value");
			
			Assert.IsTrue (section.Contains ("new key"));
			Assert.AreEqual ("some value", section.GetValue ("new key"));
		}

		[Test]
		[ExpectedException (typeof (IniException))]
		public void ParserError ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Nini Thing");
			writer.WriteLine (" my key = something");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
		}

		[Test]
		public void RemoveSection ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Nini Thing]");
			writer.WriteLine (" my key = something");
			writer.WriteLine ("[Parser]");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
			
			Assert.IsNotNull (doc.Sections["Nini Thing"]);
			doc.Sections.Remove ("Nini Thing");
			Assert.IsNull (doc.Sections["Nini Thing"]);
		}

		[Test]
		public void RemoveKey ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Nini]");
			writer.WriteLine (" my key = something");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
			
			Assert.IsTrue (doc.Sections["Nini"].Contains ("my key"));
			doc.Sections["Nini"].Remove ("my key");
			Assert.IsFalse (doc.Sections["Nini"].Contains ("my key"));
		}

		[Test]
		public void GetAllKeys ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Nini]");
			writer.WriteLine (" ; a comment");
			writer.WriteLine (" my key = something");
			writer.WriteLine (" dog = rover");
			writer.WriteLine (" cat = muffy");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
			
			IniSection section = doc.Sections["Nini"];
			
			Assert.AreEqual (4, section.ItemCount);
			Assert.AreEqual (3, section.GetKeys ().Length);
			Assert.AreEqual ("my key", section.GetKeys ()[0]);
			Assert.AreEqual ("dog", section.GetKeys ()[1]);
			Assert.AreEqual ("cat", section.GetKeys ()[2]);
		}

		[Test]
		public void SaveDocumentWithComments ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("; some comment");
			writer.WriteLine (""); // empty line
			writer.WriteLine ("[new section]");
			writer.WriteLine (" dog = rover");
			writer.WriteLine (""); // Empty line
			writer.WriteLine ("; a comment");
			writer.WriteLine (" cat = muffy");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
			
			StringWriter newWriter = new StringWriter ();
			doc.Save (newWriter);

			StringReader reader = new StringReader (newWriter.ToString ());
			Assert.AreEqual ("; some comment", reader.ReadLine ());
			Assert.AreEqual ("", reader.ReadLine ());
			Assert.AreEqual ("[new section]", reader.ReadLine ());
			Assert.AreEqual ("dog = rover", reader.ReadLine ());
			Assert.AreEqual ("", reader.ReadLine ());
			Assert.AreEqual ("; a comment", reader.ReadLine ());
			Assert.AreEqual ("cat = muffy", reader.ReadLine ());
			
			writer.Close ();
		}

		[Test]
		public void SaveToStream ()
		{
			string filePath = "SaveToStream.ini";
			FileStream stream = new FileStream (filePath, FileMode.Create);

			// Create a new document and save to stream
			IniDocument doc = new IniDocument ();
			IniSection section = new IniSection ("Pets");
			section.Set ("dog", "rover");
			section.Set ("cat", "muffy");
			doc.Sections.Add (section);
			doc.Save (stream);
			stream.Close ();

			IniDocument newDoc = new IniDocument (new FileStream (filePath, 
																  FileMode.Open));
			section = newDoc.Sections["Pets"];
			Assert.IsNotNull (section);
			Assert.AreEqual (2, section.GetKeys ().Length);
			Assert.AreEqual ("rover", section.GetValue ("dog"));
			Assert.AreEqual ("muffy", section.GetValue ("cat"));
			
			stream.Close ();

			File.Delete (filePath);
		}

		[Test]
		public void SambaStyleDocument ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("; some comment");
			writer.WriteLine ("# another comment"); // empty line
			writer.WriteLine ("[test]");
			writer.WriteLine (" cat = cats are not tall\\ ");
			writer.WriteLine (" animals ");
			writer.WriteLine (" dog = dogs \\ ");
			writer.WriteLine ("        do not eat cats ");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()),
												IniFileType.SambaStyle);

			Assert.AreEqual ("cats are not tall animals",
							doc.Sections["test"].GetValue ("cat"));
			Assert.AreEqual ("dogs         do not eat cats",
							doc.Sections["test"].GetValue ("dog"));
		}

		[Test]
		public void PythonStyleDocument ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("; some comment");
			writer.WriteLine ("# another comment"); // empty line
			writer.WriteLine ("[test]");
			writer.WriteLine (" cat: cats are not tall animals ");
			writer.WriteLine (" dog : dogs bark");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()),
												IniFileType.PythonStyle);

			Assert.AreEqual ("cats are not tall animals",
							doc.Sections["test"].GetValue ("cat"));
			Assert.AreEqual ("dogs bark", doc.Sections["test"].GetValue ("dog"));
		}

		[Test]
		public void DuplicateSections ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Test]");
			writer.WriteLine (" my key = something");
			writer.WriteLine ("[Test]");
			writer.WriteLine (" another key = something else");
			writer.WriteLine ("[Test]");
			writer.WriteLine (" value 0 = something 0");
			writer.WriteLine (" value 1 = something 1");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
			
			Assert.IsNotNull (doc.Sections["Test"]);
			Assert.AreEqual (1, doc.Sections.Count);
			Assert.AreEqual (2, doc.Sections["Test"].ItemCount);
			Assert.IsNull (doc.Sections["Test"].GetValue ("my key"));
			Assert.IsNotNull (doc.Sections["Test"].GetValue ("value 0"));
			Assert.IsNotNull (doc.Sections["Test"].GetValue ("value 1"));
		}

		[Test]
		public void DuplicateKeys ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Test]");
			writer.WriteLine (" a value = something 0");
			writer.WriteLine (" a value = something 1");
			writer.WriteLine (" a value = something 2");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()));
			
			Assert.IsNotNull (doc.Sections["Test"]);
			Assert.AreEqual (1, doc.Sections.Count);
			Assert.AreEqual (1, doc.Sections["Test"].ItemCount);
			Assert.IsNotNull (doc.Sections["Test"].GetValue ("a value"));
			Assert.AreEqual ("something 0", doc.Sections["Test"].GetValue ("a value"));
		}

		[Test]
		public void MysqlStyleDocument ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("# another comment"); // empty line
			writer.WriteLine ("[test]");
			writer.WriteLine (" quick ");
			writer.WriteLine (" cat = cats are not tall animals ");
			writer.WriteLine (" dog : dogs bark");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()),
												IniFileType.MysqlStyle);

			Assert.IsTrue (doc.Sections["test"].Contains ("quick"));
			Assert.AreEqual ("", doc.Sections["test"].GetValue ("quick"));
			Assert.AreEqual ("cats are not tall animals",
							doc.Sections["test"].GetValue ("cat"));
			Assert.AreEqual ("dogs bark", doc.Sections["test"].GetValue ("dog"));
		}

		[Test]
		public void WindowsStyleDocument ()
		{
			StringWriter writer = new StringWriter ();
			writer.WriteLine ("; another comment"); // empty line
			writer.WriteLine ("[test]");
			writer.WriteLine (" cat = cats are not ; tall ");
			writer.WriteLine (" dog = dogs \"bark\"");
			IniDocument doc = new IniDocument (new StringReader (writer.ToString ()),
												IniFileType.WindowsStyle);

			IniSection section = doc.Sections["test"];
			Assert.AreEqual ("cats are not ; tall", section.GetValue ("cat"));
			Assert.AreEqual ("dogs \"bark\"", section.GetValue ("dog"));
		}

		[Test]
		public void SaveAsPythonStyle ()
		{
			string filePath = "Save.ini";
			FileStream stream = new FileStream (filePath, FileMode.Create);

			// Create a new document and save to stream
			IniDocument doc = new IniDocument ();
			doc.FileType = IniFileType.PythonStyle;
			IniSection section = new IniSection ("Pets");
			section.Set ("my comment");
			section.Set ("dog", "rover");
			doc.Sections.Add (section);
			doc.Save (stream);
			stream.Close ();

			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine ("# my comment");
			writer.WriteLine ("dog : rover");

			StreamReader reader = new StreamReader (filePath);
			Assert.AreEqual (writer.ToString (), reader.ReadToEnd ());
			reader.Close ();

			File.Delete (filePath);
		}

		[Test]
		public void SaveAsMysqlStyle ()
		{
			string filePath = "Save.ini";
			FileStream stream = new FileStream (filePath, FileMode.Create);

			// Create a new document and save to stream
			IniDocument doc = new IniDocument ();
			doc.FileType = IniFileType.MysqlStyle;
			IniSection section = new IniSection ("Pets");
			section.Set ("my comment");
			section.Set ("dog", "rover");
			doc.Sections.Add (section);
			doc.Save (stream);
			stream.Close ();

			StringWriter writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine ("# my comment");
			writer.WriteLine ("dog = rover");

			StreamReader reader = new StreamReader (filePath);
			Assert.AreEqual (writer.ToString (), reader.ReadToEnd ());
			reader.Close ();

			IniDocument iniDoc = new IniDocument ();
			iniDoc.FileType = IniFileType.MysqlStyle;
			iniDoc.Load (filePath);

			File.Delete (filePath);
		}

		[Test]
		[ExpectedException (typeof (IniException))]
		public void SambaLoadAsStandard ()
		{
			string filePath = "Save.ini";
			FileStream stream = new FileStream (filePath, FileMode.Create);

			// Create a new document and save to stream
			IniDocument doc = new IniDocument ();
			doc.FileType = IniFileType.SambaStyle;
			IniSection section = new IniSection ("Pets");
			section.Set ("my comment");
			section.Set ("dog", "rover");
			doc.Sections.Add (section);
			doc.Save (stream);
			stream.Close ();

			IniDocument iniDoc = new IniDocument ();
			iniDoc.FileType = IniFileType.Standard;
			iniDoc.Load (filePath);

			File.Delete (filePath);
		}
	}
}
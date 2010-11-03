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
using Nini.Ini;
using NUnit.Framework;
using System.IO;

namespace Nini.Test.Ini
{
	[TestFixture]
	public class IniWriterTests
	{
		[Test]
		public void EmptyWithComment ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			Assert.AreEqual (IniWriteState.Start, iniWriter.WriteState);
			
			iniWriter.WriteEmpty ("First INI file");
			Assert.AreEqual ("; First INI file", ReadLine (writer, 1));
			Assert.AreEqual (IniWriteState.BeforeFirstSection, iniWriter.WriteState);
		}
		
		[Test]
		public void EmptyWithoutComment ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			Assert.AreEqual (IniWriteState.Start, iniWriter.WriteState);
			
			iniWriter.WriteEmpty ();
			Assert.AreEqual ("", ReadLine (writer, 1));
			Assert.AreEqual (IniWriteState.BeforeFirstSection, iniWriter.WriteState);
		}

		[Test]
		public void SectionWithComment ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			Assert.AreEqual (IniWriteState.Start, iniWriter.WriteState);
			
			iniWriter.WriteSection ("Test Section", "My comment");
			Assert.AreEqual ("[Test Section] ; My comment", ReadLine (writer, 1));
			Assert.AreEqual (IniWriteState.Section, iniWriter.WriteState);
		}
		
		[Test]
		public void SectionWithoutComment ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			Assert.AreEqual (IniWriteState.Start, iniWriter.WriteState);
			
			iniWriter.WriteSection ("Test Section");
			Assert.AreEqual ("[Test Section]", ReadLine (writer, 1));
			Assert.AreEqual (IniWriteState.Section, iniWriter.WriteState);
		}
		
		[Test]
		public void KeyWithIndentation ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			iniWriter.Indentation = 2;
			iniWriter.WriteSection ("Required");
			iniWriter.WriteKey ("independence day", "july");
			Assert.AreEqual ("  independence day = july", ReadLine (writer, 2));
			iniWriter.Indentation = 0;
		}
		
		[Test]
		public void KeyWithQuotesAndComment ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			iniWriter.UseValueQuotes = true;
			iniWriter.WriteSection ("Required");
			iniWriter.WriteKey ("thanksgiving", "November 25th", "Football!");
			iniWriter.UseValueQuotes = false;
			Assert.AreEqual ("thanksgiving = \"November 25th\" ; Football!", 
							 ReadLine (writer, 2));
		}
		
		[Test]
		public void FlushAndClose ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			iniWriter.WriteSection ("Required");
			iniWriter.WriteKey ("thanksgiving", "november 25th", "Football!");
			
			iniWriter.Close ();
			Assert.AreEqual (IniWriteState.Closed, iniWriter.WriteState);
		}
		
		[Test]
		[ExpectedException (typeof (InvalidOperationException))]
		public void NotOrderedWriteState ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			iniWriter.WriteKey ("state", "Out of order");
		}
		
		[Test]
		public void ReplaceEndOfLine ()
		{
			StringWriter writer = new StringWriter ();
			IniWriter iniWriter = new IniWriter (writer);
			
			iniWriter.WriteSection ("Required");
			iniWriter.WriteKey ("thanksgiving", "November\n 25th");
			
			Assert.AreEqual ("thanksgiving = November 25th", ReadLine (writer, 2));
		}
		
		private string ReadLine (StringWriter writer, int line)
		{
			string result = "";
			StringReader reader = new StringReader (writer.ToString ());

			for (int i = 1; i < line + 1; i++)
			{
				if (i == line)
				{
					result = reader.ReadLine ();
					break;
				}
				reader.ReadLine ();
			}
				
			return result;
		}

	}
}
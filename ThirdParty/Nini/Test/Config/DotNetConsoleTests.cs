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

namespace Nini.Test.Config
{	
	public class DotNetConsoleTests
	{
		static int assertCount = 0;

		public static int Main ()
		{
			SetMessage ("Running tests...");
			try
			{
				WebTest ();
				FileTest ();
				FileAndSaveTest ();
			}
			catch (Exception e)
			{
				SetMessage ("Exception occurred: " + e.Message + "\r\n" +
							"Stack trace: " + e.StackTrace);
				Failure ();
			}

			DisplayResults ();
			SetMessage ("All tests passed successfully");

			return 0;
		}
		
		private static void WebTest ()
		{
			string[] sections = new string[] { "appSettings", "Pets" };
			DotNetConfigSource source = new DotNetConfigSource (sections);
			IConfig config = source.Configs["appSettings"];

			Assert (config != null, "IConfig is null");
			AssertEquals ("My App", config.Get ("App Name"));
			
			config = source.Configs["Pets"];
			AssertEquals ("rover", config.Get ("dog"));
			AssertEquals ("muffy", config.Get ("cat"));
			Assert (config.Get("not here") == null, "Should not be present");
		}
		
		private static void FileTest ()
		{
			DotNetConfigSource source = 
				new DotNetConfigSource (DotNetConfigSource.GetFullConfigPath ());
			IConfig config = source.Configs["appSettings"];

			Assert (config != null, "IConfig is null");
			AssertEquals ("My App", config.Get ("App Name"));
			
			config = source.Configs["Pets"];
			AssertEquals ("rover", config.Get ("dog"));
			AssertEquals ("muffy", config.Get ("cat"));
			Assert (config.Get("not here") == null, "Should not be present");
		}
		
		private static void FileAndSaveTest ()
		{
			DotNetConfigSource source = 
				new DotNetConfigSource (DotNetConfigSource.GetFullConfigPath ());
			IConfig config = source.Configs["appSettings"];

			config = source.Configs["Pets"];
			AssertEquals ("rover", config.Get ("dog"));
			AssertEquals ("muffy", config.Get ("cat"));
			Assert (config.Get("not here") == null, "Should not be present");
			
			config.Set ("dog", "Spots");
			config.Set ("cat", "Misha");
			
			AssertEquals ("Spots", config.Get ("dog"));
			AssertEquals ("Misha", config.Get ("cat"));

			// Cannot perform save yet until technical issues resolved
			/*
			string fileName = "DotNetConfigSourceTests.exe.config";
			source.Save ();
			
			source = new DotNetConfigSource ();
			config = source.Configs["Pets"];
			AssertEquals ("Spots", config.Get ("dog"));
			AssertEquals ("Misha", config.Get ("cat"));
			
			File.Delete (fileName);
			*/
		}
		
		#region Test methods
		private static void DisplayResults ()
		{
			SetMessage ("");
			SetMessage ("Total asserts: " + assertCount);
		}
		
		private static void Assert (bool value, string message)
		{
			assertCount++;

			if (!value) {
				SetMessage ("Assert failed: " + message);
				Failure ();
			}
		}
		
		private static void AssertEquals (string searchValue, 
										  string actualValue)
		{
			assertCount++;

			if (searchValue != actualValue) {
				SetMessage (String.Format ("Expected: [{0}], Actual: [{1}]. ", 
										   searchValue, actualValue));
				Failure ();
			}
		}
		
		private static void Failure ()
		{
			DisplayResults ();
			SetMessage ("Failure stopped operation");
			Environment.Exit (1);
		}
		
		private static void SetMessage (string message)
		{
			Console.WriteLine (message);
		}
		#endregion
	}
}

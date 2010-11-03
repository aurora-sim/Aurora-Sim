using System;

namespace Nini.Ini
{
		/// <include file='IniItem.xml' path='//Class[@name="IniItem"]/docs/*' />
		public class IniItem
		{
			#region Private variables
			IniType iniType = IniType.Empty;
			string iniName = "";
			string iniValue = "";
			string iniComment = null;
            bool m_NeedsQuotes = false;
			#endregion
			
			#region Public properties
			/// <include file='IniItem.xml' path='//Property[@name="Type"]/docs/*' />
			public IniType Type
			{
				get { return iniType; }
				set { iniType = value; }
			}
			
			/// <include file='IniItem.xml' path='//Property[@name="Value"]/docs/*' />
			public string Value
			{
				get { return iniValue; }
				set { iniValue = value; }
			}
			
			/// <include file='IniItem.xml' path='//Property[@name="Name"]/docs/*' />
			public string Name
			{
				get { return iniName; }
			}
			
			/// <include file='IniItem.xml' path='//Property[@name="Comment"]/docs/*' />
            public string Comment
            {
                get { return iniComment; }
                set { iniComment = value; }
            }
            public bool NeedsQuotes
            {
                get { return m_NeedsQuotes; }
                set { m_NeedsQuotes = value; }
            }
            #endregion
			
			/// <include file='IniItem.xml' path='//Constructor[@name="Constructor"]/docs/*' />
			internal protected IniItem (string name, string value, IniType type, string comment, bool RemoveQuotes)
            {
                if (value != null && RemoveQuotes && value.Contains("\""))
                {
                    value = value.Replace("\"", "");
                    NeedsQuotes = true;
                }
				iniName = name;
				iniValue = value;
				iniType = type;
				iniComment = comment;
			}

            public string GetValue()
            {
                if (NeedsQuotes)
                    Value = "\"" + Value + "\"";
                return Value;
            }
		}
}


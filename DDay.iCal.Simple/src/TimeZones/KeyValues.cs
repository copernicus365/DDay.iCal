using System.Linq;

namespace DDay.iCal.Simple.TZ
{
	#region KeyValues

	internal class KeyValues
	{
		string m_key;
		string m_value;
		string[] m_values;

		public KeyValues() { }

		public KeyValues(string key, string value)
		{
			m_key = key;
			m_value = value;
		}

		public KeyValues(string key, string[] values)
		{
			m_key = key;
			Values = values;
		}

		public string Key
		{
			get { return m_key; }
			set { m_key = value; }
		}
		public string Value
		{
			get { return m_value; }
			set { m_value = value; }
		}

		public string[] Values
		{
			get { return m_values; }
			set
			{
				if (m_value == null && value != null && value.Length > 0) {
					m_value = value[0];
					if (value.Length > 1)
						m_values = value.Skip(1).ToArray();
				}
				else
					m_values = value;
			}
		}

		public override string ToString()
		{
			return string.Format("{0}, {1}", m_key ?? "", m_value ?? "");
		}

	}

	#endregion
}

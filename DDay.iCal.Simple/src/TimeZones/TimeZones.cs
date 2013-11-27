using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DDay.iCal.Simple.TZ
{

	internal class TimeZones
	{
		public static Dictionary<string, string> TZDictionary;
		public static Dictionary<string, KeyValues> WinDictionary;

		#region Initialize

		public static void Initialize()
		{
			if (TZDictionary == null || WinDictionary == null) {
				TimeZonesGenerator.GetDictionaries(out WinDictionary, out TZDictionary);
			}
		}

		public static void __Init()
		{
			TimeZonesGenerator.GetDictionaries(out WinDictionary, out TZDictionary);
		}

		#endregion

		public static TimeZoneInfo GetTimeZoneInfoFromTZId(string tzId)
		{
			if (tzId == null)
				return null;
			if (TZDictionary == null)
				__Init();

			string winZone;
			if (!TZDictionary.TryGetValue(tzId, out winZone))
				return null;

			TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(winZone);
			return tz;
		}

		public static KeyValues GetTZValuesFromWindowsTimeZoneId(string winTZId)
		{
			if (winTZId == null)
				return null;
			if (WinDictionary == null)
				__Init();

			KeyValues winZone;
			if (!WinDictionary.TryGetValue(winTZId, out winZone))
				return null;

			return winZone;
		}

		public static string GetTZValueFromWindowsTimeZoneId(string winTZId)
		{
			var kv = GetTZValuesFromWindowsTimeZoneId(winTZId);
			return kv == null ? null : kv.Value;
		}

	}

	internal static class TimeZonesX
	{
		/// <summary>
		/// Gets the first TZ Id value for this TimeZoneInfo. This is an indirection 
		/// call to DotNetXtensions.Time.GetTZValueFromWindowsTimeZoneId.
		/// </summary>
		/// <param name="tzi">TimeZoneInfo</param>
		public static string TZId(this TimeZoneInfo tzi)
		{
			if (tzi == null)
				return null;
			return TimeZones.GetTZValueFromWindowsTimeZoneId(tzi.Id);
		}

		/// <summary>
		/// Gets the all TZ id values (often there is only one) for this TimeZoneInfo. 
		/// This is an indirection call to DotNetXtensions.Time.GetTZValuesFromWindowsTimeZoneId.
		/// </summary>
		/// <param name="tzi">TimeZoneInfo</param>
		public static KeyValues TZIdValues(this TimeZoneInfo tzi)
		{
			if (tzi == null)
				return null;
			return TimeZones.GetTZValuesFromWindowsTimeZoneId(tzi.Id);
		}
	}
}

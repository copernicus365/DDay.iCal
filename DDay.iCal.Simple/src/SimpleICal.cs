using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DDay.iCal.Simple;
using DotNetXtensions;
using DotNetXtensions.Globalization;

namespace DDay.iCal.Simple
{
	public class SimpleICal
	{
		#region FIELDS / PROPERTIES

		public const double NumPrevDaysToDisplay = 2;
		public static bool SetUpdatedEventTimeToStartTime = false;

		private DateTime __EarliestIncludeEventTime = EarliestDateForExcludingEvents();
		private DateTime __Now = DateTime.UtcNow;

		public TimeZoneInfo DefaultTimeZone { get; set; }

		private TimeZoneInfo CurrentTimeZone;

		public string LocalIdBase { get; set; }

		#endregion

		public static IICalendarCollection GetCalendar(byte[] data)
		{
			if (data == null)
				return null;

			IICalendarCollection collection = null;
			collection = DDay.iCal.iCalendar.LoadFromStream(new MemoryStream(data));
			return collection;
		}

		public SEvent[] GetEvents(byte[] data)
		{
			var calendarColl = GetCalendar(data);
			if (calendarColl == null)
				return null;

			return GetEvents(calendarColl.ToArray());
		}

		void GetRepeatingIds(IICalendar cal, ref IEvent[] events, ref KeyValuePair<string, IEvent> repeatingIds)
		{
			//events = cal.Events.Where(e => e != null && e.Start != null).ToArray();
			
			//Dictionary<string, 
			//int len = events.Length;

			//for (int i = 0; i < len; i++) {

			//}

			//Dictionary<string, IEvent> d
		}

		public List<GroupKV<IEvent,string>> GetEventsTEMP(IICalendar[] calCollection)
		{
			if (calCollection == null || calCollection.Length < 1)
				return null;

			__EarliestIncludeEventTime = EarliestDateForExcludingEvents();
			__Now = DateTime.UtcNow;

			var events = new List<SEvent>();

			var cal = calCollection.First();

			var list = cal.Events.GroupByQuick(e => e.UID);

			return list;
		}

		public SEvent[] GetEvents(IICalendar[] calCollection)
		{
			if (calCollection == null || calCollection.Length < 1)
				return null;

			__EarliestIncludeEventTime = EarliestDateForExcludingEvents();
			__Now = DateTime.UtcNow;

			var events = new List<SEvent>();

			foreach (var cal in calCollection) {
				
				double timeZoneUtcOffset = 0;

				CurrentTimeZone = cal.GetTimeZone();
				if (CurrentTimeZone == null)
					CurrentTimeZone = DefaultTimeZone;

			
				foreach (var evnt in cal.Events.Where(e => e != null && e.Start != null)) {

					if (evnt.End == null)
						evnt.End = evnt.Start;

					var recurrence = evnt.RecurrenceRules.FirstOrDefault();
					if (recurrence != null) {
						foreach (var ev in GetRecurringEvents(evnt, recurrence))
							if (ev != null)
								events.Add(ev);
					}
					else {
						if (evnt.End.Ticks >= __EarliestIncludeEventTime.Ticks) {
							var ev = ToEvent(evnt);
							if (ev != null)
								events.Add(ev);
						}
					}
				}
			}
			return events
				.Where(e => e != null)
				.Distinct(
					e => e.Id,
					(e, d) => {
						if (e.Id == null)
							return null;
						e.Id += "_" + e.StartDate.XmlTime();
						if (d.ContainsKey(e.Id))
							return null;
						return e;
				})
				.OrderBy(e => e.StartDate)
				.ToArray();
		}

		public IEnumerable<SEvent> GetRecurringEvents(IEvent evnt, IRecurrencePattern recur)
		{
			if (evnt == null || recur == null || evnt.UID.IsNullOrEmpty())
				yield break;
			SEvent rEvent = ToEvent(evnt, true);
			if (rEvent == null)
				yield break;

			rEvent.RRuleStr = recur.ToString();
			if (rEvent.RRuleStr.IsNullOrEmpty())
				yield break;

			DateTime defMaxDT = __Now.AddMonths(1);

			IList<Occurrence> occurrences = evnt.GetOccurrences(__Now, __Now.AddMonths(1));

			if (occurrences == null)
				yield break;

			if (occurrences.Count < 2) {
				occurrences = evnt.GetOccurrences(__Now, __Now.AddMonths(3));
				if (occurrences == null || occurrences.Count == 0)
					yield break;
			}

			if (occurrences.Count > 6)
				occurrences = occurrences.Take(6).ToList();

			DateTime updated = evnt.LastModified.ToDateTime(evnt.Start.ToDateTime(__Now));

			for (int i = 0; i < occurrences.Count; i++) {

				string idAppend = i < 1 ? null : "_recur-" + i;
				var ev = rEvent.Copy(idAppend);

				IPeriod period = occurrences[i].Period;

				bool hasTime;
				ev.StartDate = GetLocalTime(period.StartTime, CurrentTimeZone, out hasTime);
				ev.EndDate = GetLocalTime(period.EndTime, CurrentTimeZone, out hasTime);
				ev.Updated = SetUpdatedEventTimeToStartTime ? ev.StartDate : updated;

				yield return ev;
			}
		}

		public SEvent ToEvent(IEvent evnt, bool isRecurringEvent = false)
		{
			if (evnt == null || evnt.UID.IsNullOrEmpty())
				return null;

			var cEvent = new SEvent();
			cEvent.Id = evnt.UID;
			cEvent.FeedId = GetQualifiedAtomId(evnt.UID, LocalIdBase);
			cEvent.Title = evnt.Summary;
			cEvent.Description = evnt.Description;
			cEvent.LocationDescription = evnt.Location;
			cEvent.IsAllDay = evnt.IsAllDay;
			cEvent.TimeZone = CurrentTimeZone == null ? null : CurrentTimeZone.TZId();

			if (!isRecurringEvent) {
				bool hasTime;
				cEvent.StartDate = GetLocalTime(evnt.Start, CurrentTimeZone, out hasTime);
				cEvent.EndDate = GetLocalTime(evnt.End, CurrentTimeZone, out hasTime);
				cEvent.Updated = SetUpdatedEventTimeToStartTime ? cEvent.StartDate : evnt.LastModified.ToDateTime(cEvent.StartDate);
			}

			cEvent.Url = evnt.Url == null ? null : evnt.Url.OriginalString;

			var loc = evnt.GeographicLocation;
			if (loc != null && loc.Latitude != 0) {
				cEvent.Latitude = (decimal)loc.Latitude;
				cEvent.Longitude = (decimal)loc.Longitude;
			}
			return cEvent;
		}

		public DateTime GetLocalTime(IDateTime iDt, TimeZoneInfo defTimeInfo, out bool hasTime)
		{
			if (iDt == null)
				throw new ArgumentNullException();

			DateTime dt = iDt.Local;

			hasTime = iDt.HasTime;
			if (!hasTime)
				return iDt.Date;

			if (iDt.IsUniversalTime) {
				string tzid = iDt.TZID; // ?? iDt.TimeZoneName;
				if (tzid != null) { // && tzid != "UTC" && tzid != "GMT") {
					TimeZoneInfo _tzi = TimeZones.GetTimeZoneInfoFromTZId(tzid);
					if (_tzi != null)
						defTimeInfo = _tzi; // NOTE, this sets the LOCAL defTimeInfo variable, it does not reset our class field version
				}
				if (defTimeInfo != null) {
					if (dt.Kind != DateTimeKind.Utc)
						dt = new DateTime(dt.Ticks, DateTimeKind.Utc); // conversion error if you don't do this
					dt = TimeZoneInfo.ConvertTimeFromUtc(dt, defTimeInfo);
				}
			}
			return dt;
		}

		#region OTHER

		public static DateTime ClearTimeOfDay(DateTime dt)
		{
			return dt.AddTicks(-dt.TimeOfDay.Ticks);
		}

		public static DateTime EarliestDateForExcludingEvents(DateTime? fromTime = null)
		{
			DateTime dt = fromTime == null ? DateTime.UtcNow : (DateTime)fromTime;
			DateTime minDate = dt.Add(TimeSpan.FromDays(-SimpleICal.NumPrevDaysToDisplay));
			return minDate;
		}

		public static string GetQualifiedAtomId(string id, string localIdBase)
		{
			if (id.IsNullOrEmpty())
				return null;

			if (localIdBase.IsNullOrEmpty())
				return id;

			bool needsTag = true;
			if (id.Length > 5) {
				if (id[0] == 'h' && (id.StartsWith("http") && id[4] == ':' || (id[4] == 's' && id[5] == ':')))
					needsTag = false;
				else if (id[0] == 't' && id.StartsWith("tag:"))
					needsTag = false;
				else if (id[0] == 'u' && id.StartsWith("urn:"))
					needsTag = false;
				else
					needsTag = true;
			}
			if (!needsTag)
				return id;

			id = localIdBase + id; // Uri.EscapeUriString(id);
			return id;
		}

		#endregion

	}

	public static class XSimpleICal
	{
		public static TimeZoneInfo GetTimeZone(this IICalendar cal)
		{
			var tzProp = cal.Properties.FirstOrDefault(p => p.Name == "X-WR-TIMEZONE");
			if (tzProp == null)
				return null;

			string dtTimeZone = (string)(tzProp.ValueCount == 1 ? tzProp.Value : tzProp.Values.LastOrDefault());
			if (dtTimeZone.IsNullOrEmpty())
				return null;

			TimeZoneInfo tzi = TimeZones.GetTimeZoneInfoFromTZId(dtTimeZone);
			return tzi;
		}

		public static DateTime ToDateTime(this IDateTime dt, DateTime defaultDT)
		{
			if (dt == null)
				return defaultDT;

			return dt.UTC;
		}
	}
}
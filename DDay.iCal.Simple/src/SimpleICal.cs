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

		public DateTime EarliestIncludeEventTime { get; set; }

		public TimeZoneInfo DefaultTimeZone { get; set; }
		public bool SetUpdatedEventTimeToStartTime = false;
		public string FeedIdBase { get; set; }

		private DateTime _now = DateTime.UtcNow;
		private TimeZoneInfo _currentTZ;

		static readonly TimeSpan con_EmptyTime = TimeSpan.FromSeconds(0);
		static readonly SEvent[] con_EmptySEventsArr = new SEvent[0];

		#endregion

		#region CONSTRUCTOR

		public SimpleICal(DateTime? earliestIncludeEventTime = null)
		{
			EarliestIncludeEventTime = earliestIncludeEventTime ?? DateTime.MinValue;
		}

		#endregion

		#region GetCalendar / GetEvents

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

		public SEvent[] GetEvents(IICalendarCollection calCollection)
		{
			if (calCollection == null) return null;
			return GetEvents(calCollection.ToArray());
		}

		public SEvent[] GetEvents(IEnumerable<IICalendar> calendarCollection)
		{
			var calCollection = calendarCollection.ToArrayOrNull();
			if (calCollection.IsNullOrEmpty())
				return null;

			_now = DateTime.UtcNow;

			var fevents = new List<SEvent>();

			foreach (var cal in calCollection) {

				double timeZoneUtcOffset = 0;

				_currentTZ = cal.GetTimeZone();
				if (_currentTZ == null)
					_currentTZ = DefaultTimeZone;

				var events = cal.Events.Where(e => e != null).GroupByQuick(e => e.UID);

				foreach (var eventOrGroup in events) {

					// if more than 1 (is a group), we'll REQUIRE it to be a recurrant event. if not is discarded
					SEvent[] recurringEvents = GetRecurringEvents(eventOrGroup);					
					if (!recurringEvents.IsNullOrEmpty())
						fevents.AddRange(recurringEvents);
					else {
						var evnt = eventOrGroup.First;
						
						if (evnt.Start == null) continue;
						if (evnt.End == null) evnt.End = evnt.Start;

						if (evnt.End.Ticks >= EarliestIncludeEventTime.Ticks) {
							var ev = ToEvent(evnt);
							if (ev != null)
								fevents.Add(ev);
						}
					}
				}
			}
			return fevents
				.Where(e => e != null)
				//.Distinct( // not needed anymore, our use of GroupByQuick already sorts ids
				//	e => e.Id,
				//	(e, d) => {
				//		if (e.Id == null)
				//			return null;
				//		e.Id += "_" + e.Start.DateTime.XmlTime();
				//		if (d.ContainsKey(e.Id))
				//			return null;
				//		return e;
				//	})
				.OrderBy(e => e.Start)
				.ToArray();
		}

		/// <summary>
		/// Returns null if group did not contain a recurring event, or if group is null or empty.
		/// If group *does* contain a recurring event, even if no actual occurences are gotten from the rule,
		/// will still guarantee to return an empty array (to indicate it *was* a recurring, but no instances
		/// resulted).
		/// </summary>
		/// <param name="group">Event group.</param>
		public SEvent[] GetRecurringEvents(GroupKV<IEvent, string> group)
		{
			if (group == null || group.Count == 0) return null;
			
			var recurEvent = group.FirstOrDefault(e => !e.RecurrenceRules.IsNullOrEmpty());
			if(recurEvent == null) 
				return null;
		
			IRecurrencePattern recurPtrn = recurEvent.RecurrenceRules.First();
			SEvent[] results = GetRecurringEvents(recurEvent, recurPtrn).Where(e => e != null).ToArray();
			if (results == null)
				return con_EmptySEventsArr; // NULL return means was not recurring, so  we must return empty array
			if (!group.HasMany)
				return results;

				//e.RecurrenceRules.IsNullOrEmpty() &&  && 
			DateTime maxRecurDT = _now.AddMonths(3);
			SEvent[] others = group.Items
				.Where(e => e.RecurrenceID != null && e.Start != null && e.Start.Value >= EarliestIncludeEventTime && e.Start.Value < maxRecurDT)
				.Select(e => ToEvent(e)).ToArray();

			for (int i = 0; i < results.Length; i++) {
				var r = results[i];
				var o = others.FirstOrDefault(ev => ev.RecurrenceId == r.Start);
				if (o != null) {
					o.Id = r.Id;
					results[i] = o;
				}
			}
			
			return results;
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

			DateTime defMaxDT = _now.AddMonths(1);

			IList<Occurrence> occurrences = evnt.GetOccurrences(EarliestIncludeEventTime, _now.AddMonths(1));

			if (occurrences == null)
				yield break;

			if (occurrences.Count < 2) {
				occurrences = evnt.GetOccurrences(EarliestIncludeEventTime, _now.AddMonths(3));
				if (occurrences == null || occurrences.Count == 0)
					yield break;
			}

			if (occurrences.Count > 6)
				occurrences = occurrences.Take(6).ToList();

			DateTime updated = evnt.LastModified.ToDateTime(evnt.Start.ToDateTime(_now));

			for (int i = 0; i < occurrences.Count; i++) {

				string idAppend = i < 1 ? null : "_recur-" + i;
				var ev = rEvent.Copy(idAppend);

				IPeriod period = occurrences[i].Period;

				bool hasTime;
				ev.Start = GetLocalTime(period.StartTime, _currentTZ, out hasTime);
				ev.End = GetLocalTime(period.EndTime, _currentTZ, out hasTime);
				ev.Updated = SetUpdatedEventTimeToStartTime ? ev.Start.DateTime : updated;
				ev.IsAllDay = ev.Start.TimeOfDay == con_EmptyTime && ev.End.TimeOfDay == con_EmptyTime;

				yield return ev;
			}
		}

		#endregion

		//public List<GroupKV<IEvent,string>> GetEventsTEMP(IICalendar[] calCollection)
		//{
		//	if (calCollection == null || calCollection.Length < 1)
		//		return null;

		//	__EarliestIncludeEventTime = EarliestDateForExcludingEvents();
		//	__Now = DateTime.UtcNow;

		//	var events = new List<SEvent>();

		//	var cal = calCollection.First();

		//	var list = cal.Events.GroupByQuick(e => e.UID);

		//	return list;
		//}

		public SEvent ToEvent(IEvent evnt, bool isRecurrenceRuleOnly = false)
		{
			if (evnt == null || evnt.UID.IsNullOrEmpty())
				return null;

			if (evnt.Start == null) evnt.Start = new iCalDateTime(_now);
			if (evnt.End == null) evnt.End = new iCalDateTime(_now);
			if (evnt.LastModified == null) evnt.LastModified = new iCalDateTime(_now);

			var cEvent = new SEvent();
			cEvent.Event = evnt;
			cEvent.Sequence = evnt.Sequence;
			cEvent.Id = evnt.UID;
			cEvent.FeedId = GetQualifiedFeedId(evnt.UID, FeedIdBase);
			cEvent.Title = evnt.Summary;
			cEvent.Description = evnt.Description;
			cEvent.LocationDescription = evnt.Location;
			cEvent.TimeZone = _currentTZ == null ? null : _currentTZ.TZId();
			cEvent.IsAllDay = evnt.IsAllDay;

			bool hasTime = false;
			bool hasStartTime = false;
			bool hasEndTime = false;
			if (!isRecurrenceRuleOnly) {

				cEvent.Start = GetLocalTime(evnt.Start, _currentTZ, out hasStartTime);
				cEvent.End = GetLocalTime(evnt.End, _currentTZ, out hasEndTime);
				cEvent.RecurrenceId = GetLocalTime(evnt.RecurrenceID, _currentTZ, out hasTime);
			}
			cEvent.Updated = SetUpdatedEventTimeToStartTime 
				? cEvent.Start.DateTime 
				: evnt.LastModified.Value; //.ToDateTime(cEvent.Start.DateTime);
			
			if (!cEvent.IsAllDay && !hasStartTime && !hasEndTime)
				cEvent.IsAllDay = true;

			cEvent.Url = evnt.Url == null ? null : evnt.Url.OriginalString;

			var loc = evnt.GeographicLocation;
			if (loc != null && loc.Latitude != 0) {
				cEvent.Latitude = (decimal)loc.Latitude;
				cEvent.Longitude = (decimal)loc.Longitude;
			}
			return cEvent;
		}

		#region HELPERS

		public DateTimeOffset GetLocalTime(IDateTime iDt, TimeZoneInfo defTimeInfo, out bool hasTime)
		{
			hasTime = false;
			if (iDt == null)
				return DateTime.MinValue;

			DateTime dt = iDt.Value; //.Local; //.Value; //.Local; okay, this will pry break everything! what to do?

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

		public static string GetQualifiedFeedId(string id, string localIdBase)
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

		//public static DateTime ClearTimeOfDay(DateTime dt)
		//{
		//	return dt == DateTime.MinValue ? dt : dt.AddTicks(-dt.TimeOfDay.Ticks);
		//}

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
			return dt == null ? defaultDT : dt.UTC;
		}
	}
}
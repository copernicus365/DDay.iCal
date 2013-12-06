using System;
using System.Text;

namespace DDay.iCal.Simple
{
	/// <summary>
	/// SEvent ('Simple Event') entity.
	/// </summary>
	public class SEvent
	{

		public SEvent() { }

		public IEvent Event { get; set; }

		public string Id { get; set; }

		public string FeedId { get; set; }

		public DateTimeOffset Start { get; set; }

		public DateTimeOffset End { get; set; }

		public DateTimeOffset RecurrenceId { get; set; }

		public DateTime Updated { get; set; }

		public int Sequence { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string TimeZone { get; set; } // FULL TIMEZONE?

		public bool IsAllDay { get; set; }

		static readonly TimeSpan con_emptyTime = TimeSpan.FromSeconds(0);
		public bool IsMultiDay
		{
			get
			{
				long diff = End.Ticks - Start.Ticks;
				
				if (diff <= 0 || (diff < TimeSpan.TicksPerDay && Start.Day == End.Day))
					return false;

				// This allows IsMultiDay = FALSE IF: May 02, 2013 12:00 am - May 03, 2013 12:00 am, 
				// which is frequent if it really was just 'AllDay'
				if (diff == TimeSpan.TicksPerDay && Start.Hour == 0)
					return false;

				return true;
			}
		}

		public string ShortDisplay
		{
			get
			{
				return Extensions.EventDateTimeShortDisplay(Start, End, IsMultiDay, IsAllDay);
			}
		}

		public string RRuleStr { get; set; } // FULL RRule?

		bool? _isRecurring;
		public bool IsRecurring
		{
			get { return _isRecurring ?? RRuleStr != null; }
			set { _isRecurring = value; }
		}

		public string Url { get; set; }

		public string ImageUrl { get; set; }

		public decimal? Longitude { get; set; }

		public decimal? Latitude { get; set; }

		public string LongLatDisplay()
		{
			return string.Format("{0} / {1}", Longitude, Latitude);
		}

		public string LocationDescription { get; set; }

		public string Address { get; set; }

		public bool IsHomeLocation { get; set; }

		public SEvent Copy(string idAppendValue = null)
		{
			var c = new SEvent();

			c.Id = Id + idAppendValue; // order matters, as FeedId may use ItemId, ExtUrl, etc
			c.FeedId = FeedId + idAppendValue;
			c.Title = Title;
			c.Start = Start;
			c.End = End;
			c.RecurrenceId = RecurrenceId;
			c.IsAllDay = IsAllDay;
			c.TimeZone = TimeZone;
			c.Updated = Updated;
			c.Description = Description;
			c.RRuleStr = RRuleStr;
			c.Url = Url;
			c.ImageUrl = ImageUrl;
			c.Longitude = Longitude;
			c.Latitude = Latitude;
			c.LocationDescription = LocationDescription;
			c.Address = Address;
			c.IsHomeLocation = IsHomeLocation;

			return c;
		}

		public override string ToString()
		{
			return string.Format("({0}) {1} - {2}",
				Id ?? FeedId,
				Title,
				ShortDisplay);
		}
	}

}
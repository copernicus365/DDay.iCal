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


		public string Id { get; set; }

		public string FeedId { get; set; }

		public DateTime StartDate { get; set; }

		public DateTime EndDate { get; set; }

		public DateTime Updated { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string TimeZone { get; set; } // FULL TIMEZONE?

		public bool IsAllDay { get; set; }

		public bool IsMultiDay
		{
			get
			{
				return !(StartDate.Day == EndDate.Day
					&& StartDate.Month == EndDate.Month);
			}
		}

		public string ShortDisplay
		{
			get
			{
				return Extensions.EventDateTimeShortDisplay(StartDate, EndDate, IsMultiDay, IsAllDay);
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
			c.StartDate = StartDate;
			c.EndDate = EndDate;
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
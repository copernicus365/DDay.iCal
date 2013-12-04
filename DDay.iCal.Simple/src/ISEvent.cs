using System;
namespace DDay.iCal.Simple.src
{
	interface ISEvent
	{
		string Address { get; set; }
		global::DDay.iCal.Simple.SEvent Copy(string idAppendValue = null);
		string Description { get; set; }
		DateTime EndDate { get; set; }
		global::DDay.iCal.IEvent Event { get; set; }
		string FeedId { get; set; }
		string Id { get; set; }
		string ImageUrl { get; set; }
		bool IsAllDay { get; set; }
		bool IsHomeLocation { get; set; }
		bool IsMultiDay { get; }
		bool IsRecurring { get; set; }
		decimal? Latitude { get; set; }
		string LocationDescription { get; set; }
		decimal? Longitude { get; set; }
		string LongLatDisplay();
		string RRuleStr { get; set; }
		string ShortDisplay { get; }
		DateTime StartDate { get; set; }
		string TimeZone { get; set; }
		string Title { get; set; }
		string ToString();
		DateTime Updated { get; set; }
		string Url { get; set; }
	}
}

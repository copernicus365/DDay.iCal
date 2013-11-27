using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace DDay.iCal.Simple
{
	public static class Extensions
	{
		#region IsNullOrEmpty

		/// <summary>
		/// Checks if string is null or empty.
		/// </summary>
		/// <param name="str">String</param>
		[DebuggerStepThrough]
		internal static bool IsNullOrEmpty(this string str)
		{
			return str == null || str == "";
		}

		/// <summary>
		/// Returns TRUE if source is NULL or has a Length of 0.
		/// </summary>
		/// <param name="source">Source.</param>
		[DebuggerStepThrough]
		internal static bool IsNullOrEmpty<TSource>(this TSource[] source)
		{
			return (source == null || source.Length == 0)
				? true
				: false;
		}

		/// <summary>
		/// Returns TRUE if source is NULL or has a Count of 0.
		/// </summary>
		/// <param name="source">Source.</param>
		[DebuggerStepThrough]
		internal static bool IsNullOrEmpty(this ICollection source)
		{
			return (source == null || source.Count == 0)
				? true
				: false;
		}

		#endregion

		[DebuggerStepThrough]
		internal static string XmlTime(this DateTime dateTime, bool assumeUtc = true)
		{
			string val = dateTime.ToString("s");
			if (assumeUtc)
				val += "Z";
			return val;
		}

		#region Distinct

		/// <summary>
		/// Returns distinct elements from a sequence, using the default equality comparer
		/// *for the given TKey*. What sets DistinctBy apart from LINQ's Distinct is the ability
		/// to get the key from the object, rather than having to test equality on the object itself.
		/// Author: Jon Skeet: http://stackoverflow.com/a/489421/264031
		/// </summary>
		/// <typeparam name="TSource">Sequence type.</typeparam>
		/// <typeparam name="TKey">Key type</typeparam>
		/// <param name="source">Sequence</param>
		/// <param name="keySelector">Key</param>
		internal static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			HashSet<TKey> seenKeys = new HashSet<TKey>();
			foreach (TSource element in source) {
				if (seenKeys.Add(keySelector(element))) {
					yield return element;
				}
			}
		}

		/// <summary>
		/// Returns a distinct collection from the input sequence while allowing 
		/// any duplicate items to first be altered (or filtered still) with <paramref name="handleNonUniqueItem"/>.
		/// Note that if the return result of <paramref name="handleNonUniqueItem"/> is non-null it will
		/// be added to the result *even if* the key from the returned item is still a duplicate
		/// (this is why we pass into <paramref name="handleNonUniqueItem"/> the dictionary of keys already
		/// found, to allow the consumer to see if the new key they may have generated is itself unique).
		/// <para/>
		/// A typical scenario is that <paramref name="handleNonUniqueItem"/> 
		/// will be used to alter the id or key of the source item. 
		/// Other useful things can be done with this, such as using  <paramref name="handleNonUniqueItem"/> as a
		/// means to get ahold of all the items that were duplicates. Even if one still needs 
		/// to filter those from the return sequence, it could come in handy, such as for
		/// getting a list of ids that are being (perhaps erroneously) duplicated.
		/// </summary>
		/// <typeparam name="TSource">Source type.</typeparam>
		/// <typeparam name="TKey">Key type.</typeparam>
		/// <param name="source">Source sequence.</param>
		/// <param name="keySelector">Key selector.</param>
		/// <param name="handleNonUniqueItem">
		/// For every item in the source sequence whose key is null or which is a duplicate of a key already encountered,
		/// this func will be called with input parameters of that item and of the dictionary of key-items that have been
		/// built up to that point in time of enumerating the source sequence. If the *TSource returned item* 
		/// is null, it will be filtered from the return sequence, else it will be added to it, even if 
		/// the new item's key is still a duplicate or null. The returned item's key
		/// will be added to the dictionary if it is non-null and a duplicate key. 
		/// </param>
		internal static IEnumerable<TSource> Distinct<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, Dictionary<TKey, TSource>, TSource> handleNonUniqueItem)
		{
			if (source == null) return new TSource[0];

			var icoll = (source as ICollection<TSource>);
			int initCount = icoll != null ? icoll.Count : 100;
			bool hasHandler = handleNonUniqueItem != null;
			//TSource dflt = default(TSource);
			//bool isNullable = TSource is Nullable;

			var result = new List<TSource>(initCount);
			Dictionary<TKey, TSource> dict = null;
			if (dict == null)
				dict = new Dictionary<TKey, TSource>(initCount);

			foreach (TSource item in source) {

				if (item == null) // || item.Equals(dflt)) // could do this instead later, ... cogitating
					continue;

				TKey key = keySelector(item);

				if (key != null && !dict.ContainsKey(key)) {
					dict.Add(key, item);
					result.Add(item);
				}
				else if (hasHandler) {
					TSource newItem = handleNonUniqueItem(item, dict);
					if (newItem != null) {
						TKey newKey = keySelector(newItem);
						if (newKey != null && !dict.ContainsKey(newKey))
							dict.Add(newKey, newItem);
						result.Add(newItem);
					}
				}
			}
			return result;
		}

		#endregion
	
		public static string AmPm(this DateTime dt, bool lower = true)
		{
			return dt.Hour < 12
				? (lower ? "am" : "AM")
				: (lower ? "pm" : "PM");
		}

		public static string EventDateTimeShortDisplay(DateTime start, DateTime end, bool isMultiDay, bool isAllDay)
		{
			if (isMultiDay) {
				string val = null;
				if (start.Year != end.Year) {
					val = string.Format("{0}{1}",
						start.ToString("MMM dd, yyyy - "),
						end.ToString("MMM dd, yyyy"));
				}
				else if (start.Month != end.Month) {
					string time = isAllDay
						? ""
						: start.ToString(" (h:mm") + start.AmPm(true) + ")"; // +" - " + end.ToString("h:mm") + end.AmPm(true) + ")";

					val = string.Format("{0}{1}{2}",
						start.ToString("MMM dd - "),
						end.ToString("MMM dd, yyyy"),
						time);
				}
				else {
					if (isAllDay) {
						val = string.Format("{0}{1}{2}",
							start.ToString("MMM dd-"),
							end.ToString("dd, "),
							start.ToString("yyyy"));
					}
					else {
						val = string.Format("{0}{1}{2}{3})",
							start.ToString("MMM dd-"),
							end.ToString("dd, "),
							start.ToString("yyyy (h:mm"),
							start.AmPm(true));
					}
				}
				return val;
			}
			if (isAllDay) {
				return start.ToString("MMM dd, yyyy") + " All Day";
			}
			else {
				return string.Format("{0}{1} - {2}{3})",
					start.ToString("MMM dd, yyyy (h:mm"),
					start.AmPm(true),
					end.ToString("h:mm"),
					end.AmPm(true));
			}
		}
	}

}
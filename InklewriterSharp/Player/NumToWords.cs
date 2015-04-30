﻿using System;

namespace Inklewriter.Player
{
	/// <summary>
	/// Utility for converting numbers to words, such as 72 to "seventy two".
	/// </summary>
	public static class NumToWords
	{
		static string[] digits = new[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
		static string[] tens = new[] { "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
		static string[] teens = new[] { "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
		static string[] illions = new[] { "thousand", "million", "billion", "trillion" };

		/// <summary>
		/// Convert the given number to its text representation.
		/// </summary>
		public static string Convert (long number)
		{
			string text = "";
			if (number == 0) {
				text = "zero";
			} else if (number < 0) {
				text = "minus ";
				number = -number;
			}

			var fullString = PowerOfTenString (number, 0);
			var sepIndex = fullString.LastIndexOf (",");
			var hundredIndex = fullString.LastIndexOf ("hundred");

			if (hundredIndex < sepIndex) {
				fullString = fullString.Substring (0, sepIndex) + " and " + fullString.Substring (sepIndex + 2);
			}
			return text + fullString;
		}

		static string PowerOfTenString (long number, long powerOfTen)
		{
			var numString = NumToString (number % 1000);
			if (numString != "" && powerOfTen > 0) {
				numString += " " + illions [powerOfTen - 1];
			}
			if (number >= 1000) {
				string separator = "";
				if (numString != "") {
					separator = ", " + numString;
				}
				var recurse = PowerOfTenString ((long)Math.Floor (number / 1000d), powerOfTen + 1);
				numString = recurse + separator;
			}
			return numString;
		}

		static string NumToString (long input) {
			string result = "";
			if (input == 0) {
				return "";
			}
			if (input < 10) {
				result = digits [input - 1];
			} else if (input < 20) {
				result = teens [input - 10];
			} else if (input < 100) {
				long first = (long)Math.Floor (input / 10d) - 1;
				result = tens [first] + (input % 10 == 0 ? "" : "-" + digits [input % 10 - 1]);
			} else if (input < 1000) {
				long digit = (long)Math.Floor (input / 100d) - 1;
				result = digits [digit] + " hundred" + (input % 100 == 0 ? "" : " and " + NumToString(input % 100));
			}
			return result;
		}
	}
}


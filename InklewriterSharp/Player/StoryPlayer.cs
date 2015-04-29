﻿using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using Inklewriter;
using Inklewriter.MarkupConverters;

namespace Inklewriter.Player
{
	public class StoryPlayer
	{
		public List<FlagValue> AllFlagsCollected { get; private set; }

		StoryModel model;
		IMarkupConverter markupConverter;

		public StoryPlayer (StoryModel model, IMarkupConverter markupConverter)
		{
			this.model = model;
			this.markupConverter = markupConverter;
			AllFlagsCollected = new List<FlagValue> ();
		}

		List<PlayChunk> allChunks = new List<PlayChunk> ();
		List<Stitch> visitedStitches = new List<Stitch> ();

		public int WordCount { get; private set; }

		public string Title
		{
			get {
				return model.Story.Title;
			}
		}

		public string Author
		{
			get {
				return model.Story.EditorData.AuthorName;
			}
		}

		public Stitch InitialStitch {
			get {
				return model.Story.InitialStitch;
			}
		}

		public Stitch LastStitch {
			get {
				var index = visitedStitches.Count - 1;
				if (index >= 0) {
					return visitedStitches [index];
				}
				return null;
			}
		}

		public PlayChunk LastChunk {
			get {
				var index = allChunks.Count - 1;
				if (index >= 0) {
					return allChunks [index];
				}
				return null;
			}
		}

		public PlayChunk GetChunkFromStitch (Stitch stitch)
		{
			PlayChunk chunk = new PlayChunk ();

			AllFlagsCollected.Clear ();
			WordCount = 0;
			if (LastChunk != null) {
				for (var s = 0; s < LastChunk.FlagsCollected.Count; s++) {
					AllFlagsCollected.Add (LastChunk.FlagsCollected [s]);
				}
			}

			var currentStitch = stitch;
			var compiledText = "";

			// Loop through complete series of stitches.
			while (currentStitch != null) {
				visitedStitches.Add (currentStitch);
				if (currentStitch.PageNumber >= 1) {
					chunk.HasSectionHeading = true;
				}
				bool isStitchVisible = StoryModel.DoesArrayMeetConditions (currentStitch.IfConditions, currentStitch.NotIfConditions, AllFlagsCollected);
				// This stitch passes flag tests and should be included in this chunk
				if (isStitchVisible) {

					// Remove newlines in preparation for compiling run-on stitches into one paragraph
					compiledText += currentStitch.Text.Replace("\n", " ") + " ";

					// If no more processing is needed, apply text substitutions and store the paragraph
					bool isRunOn = Regex.IsMatch (currentStitch.Text, @"\[\.\.\.\]") || currentStitch.RunOn;
					if (!isRunOn || currentStitch.DivertStitch == null) {
						var styledText = ApplyRuleSubstitutions (compiledText, AllFlagsCollected);
						chunk.Paragraphs.Add (new Paragraph (styledText, currentStitch.Image, currentStitch.PageLabel));
						compiledText = "";
					}

					// Process flags
					if (currentStitch.Flags.Count > 0) {
						StoryModel.ProcessFlagSetting (currentStitch, AllFlagsCollected);
					}
				}
				// Add stitch to chunk
				chunk.Stitches.Add (new BlockContent<Stitch> (currentStitch, isStitchVisible));
				currentStitch = currentStitch.DivertStitch;
			}

			foreach (var p in chunk.Paragraphs) {
				WordCount += WordCountOf (p.Text);
			}

			// Add options to chunk
			if (LastStitch.Options.Count > 0) {
				foreach (var option in LastStitch.Options) {
					var isVisible = StoryModel.DoesArrayMeetConditions (option.IfConditions, option.NotIfConditions, AllFlagsCollected);
					if (isVisible) {
						chunk.Options.Add (new BlockContent<Option> (option, isVisible));
					}
				}
			}

			return chunk;
		}

		public static int WordCountOf (string s)
		{
			if (!string.IsNullOrEmpty (s)) {
				return Regex.Matches (s, @"\S+").Count;
			}
			return 0;
		}
			
		public string ApplyMarkupSubstitutions (string text)
		{
			text = ReplaceQuotes (text);
			text = ReplaceUrlMarkup (text);
			text = ReplaceImageMarkup (text);
			return text;
		}

		public string ApplyRuleSubstitutions (string text, List<FlagValue> flags)
		{
			text = ReplaceRunOnMarker (text);
			text = ConvertNumbersToWords (text, flags);
			string n = "";
			while (n != text) {
				n = text;
				text = ParseInLineConditionals (text, flags);
				text = ShuffleRandomElements (text);
			}
			text = ReplaceStyleMarkup (text);
			return text;
		}

		public static string ParseInLineConditionals (string text, List<FlagValue> flags)
		{
			var conditionBoundsPattern = @"\{([^\~\{]*?)\:([^\{]*?)(\|([^\{]*?))?\}";
			var orPattern = @"(^\s*|\s*$)";
			var andPattern = @"\s*(&&|\band\b)\s*";
			var notPattern = @"\s*(\!|\bnot\b)\s*(.+?)\s*$";
			var count = 0;
			var matches = Regex.Matches (text, conditionBoundsPattern);
			foreach (Match match in matches) {
				count++;
				if (count > 1000) {
					throw new System.Exception ("Error in conditional!");
				}
				if (matches.Count > 0) {
					var conditions = new List<string> ();
					var notConditions = new List<string> ();
					// Search "and" conditions
					var conditionMatches = Regex.Split (match.Groups [1].Value, andPattern);
					for (var i = 0; i < conditionMatches.Length; i++) {
						// Is not an "and" condition
						if (conditionMatches [i] != "&&" && conditionMatches [i] != "and") {
							// Search "not" conditions
							var notPatternMatches = Regex.Match (conditionMatches [i], notPattern);
							// Is a "not condition"
							if (notPatternMatches.Success) {
								notConditions.Add (notPatternMatches.Groups [2].Value.Replace (orPattern, ""));
							} else {
								conditions.Add (conditionMatches [i].Replace (orPattern, ""));
							}
						}
					}
					var replacementValue = "";
					if (StoryModel.DoesArrayMeetConditions (conditions, notConditions, flags)) {
						replacementValue = match.Groups [2].Value;
					} else if (!string.IsNullOrEmpty (match.Groups [4].Value)) {
						replacementValue = match.Groups [4].Value;
					}
					text = Regex.Replace (text, conditionBoundsPattern, replacementValue);
				}
			}
			return text;
		}

		public string ShuffleRandomElements (string text)
		{
			var pattern = @"\{\~([^\{\}]*?)\}";
			var matches = Regex.Matches (text, pattern);
			foreach (Match match in matches) {
				var group = match.Groups[1];
				var r = group.Value.Split ('|');
				var rand = new Random ();
				int i = rand.Next (0, r.Length);
				text = Regex.Replace (text, pattern, r [i]);
			}
			return text;
		}

		public string ReplaceRunOnMarker (string text)
		{
			text = Regex.Replace (text, @"\[\.\.\.\]", " ");
			return text;
		}

		public string ReplaceQuotes (string text)
		{
			// straight quotes to curly quotes
			text = Regex.Replace (text, @"\""([^\n]*?)\""", "“$1”");
			text = Regex.Replace (text, @"(\s|^|\n|<b>|<i>|\(|\“)\'", "$1‘");
			// straight apostrophe to curly apostrophe
			text = Regex.Replace (text, @"\'", "’");
			text = Regex.Replace (text, @"(^|\n)\""", "$1“");
			return text;
		}

		public string ReplaceStyleMarkup (string text)
		{
			// Replace inkle style markup with delegate method's output, or default to HTML tags
			text = Regex.Replace (text, @"\*\-(.*?)\-\*", markupConverter.ReplaceBoldStyleMarkup ("$1"));
			text = Regex.Replace (text, @"\/\=(.*?)\=\/", markupConverter.ReplaceItalicStyleMarkup ("$1"));
			// Remove inkle style markup
			text = Regex.Replace (text, @"(\/\=|\=\/|\*\-|\-\*)", "");
			return text;
		}

		public string ReplaceUrlMarkup (string text)
		{
			text = Regex.Replace (text, @"\[(.*?)\|(.*?)\]", markupConverter.ReplaceLinkUrlMarkup ("$1", "$2"));
			return text;
		}

		public string ReplaceImageMarkup (string text)
		{
			text = Regex.Replace (text, @"\%\|\%\|\%(.*?)\$\|\$\|\$", markupConverter.ReplaceImageUrlMarkup ("$1"));
			return text;
		}

		public string ConvertNumbersToWords (string text, List<FlagValue> flags)
		{
			var pattern = @"\[\s*(number|value)\s*\:\s*(.*?)\s*\]";
			var matchSet = Regex.Matches (text, pattern);
			foreach (Match match in matchSet) {
				int number = StoryModel.GetValueOfFlag (match.Groups[2].Value, flags);
				string numberWords = number.ToString ();
				if (match.Groups[1].Value == "value") {
					numberWords = NumToWords.Convert (number);
				}
				text = Regex.Replace (text, pattern, numberWords);
			}
			return text;
		}

		public static int CalculateApproximateWordCount (List<Stitch> stitches)
		{
			var wordCount = 0;
			for (int i = 0; i < stitches.Count; i++) {
				wordCount += stitches [i].WordCount;
			}
			if (wordCount <= 100) {
				wordCount = wordCount - wordCount % 10 + 10;
			} else {
				wordCount = wordCount - wordCount % 100 + 100;
			}
			return wordCount;
		}
	}
}
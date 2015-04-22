﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Inklewriter
{
	public class StoryModel
	{
		public class FlagValue {
			public string flagName;
			public int value;
		}

		public const string defaultStoryName = "Untitled Story";
		public const string defaultAuthorName = "Anonymous";
		public const int maxPreferredPageLength = 8;

		public int MaxPage { get; set; }

		public Story Story { get; private set; }

		public List<string> FlagIndex { get; set; }

		public int EndCount { get; set; }

		public int LooseEndCount { get; set; }

		public bool Loading { get; set; }

		#region IO

		public void ImportStory (string data)
		{
			Story = StoryReader.Read (data);
		}

		public string ExportStory ()
		{
			if (Story != null) {
				NameStitches ();
				var data = StoryWriter.Write (Story);
				return data;
			}
			return null;
		}

		#endregion

//		public void RebuildBacklinks ()
//		{
//			EndCount = 0;
//			var stitches = Stitches;
//			for (int e = 0; e < stitches.Count; e++) {
//				stitches [e].Backlinks = new List<Stitch> ();
//			}
//			for (int e = 0; e < stitches.Count; e++)
//				if (stitches [e].Options.Count > 0) {
//					for (var t = 0; t < stitches [e].Options.Count; t++) {
//						if (stitches [e].Options [t].LinkStitch != null) {
//							stitches [e].Options [t].LinkStitch.Backlinks.Add (stitches [e]);
//						} else {
//							LooseEndCount++;
//						}
//					}
//				} else {
//					if (stitches [e].DivertStitch != null) {
//						stitches [e].DivertStitch.Backlinks.Add (stitches [e]);
//					} else {
//						EndCount++;
//					}
//				}
//			if (WatchRefCounts ()) {
//				for (int e = 0; e < stitches.Count; e++) {
//					if (stitches[e].Backlinks.Count != stitches[e].RefCount) {
//						throw new System.Exception ("Stitch with text '" + stitches[e].Text + "' has invalid ref-count!");
//					}
//				}
//			}
//		}

		public static bool WatchRefCounts ()
		{
			// FIXME
			return false;
		}

		public List<Stitch> Stitches {
			get {
				return null;
//				return Story.data.stitches;
			}
		}

		#region Flags

		/// <summary>
		/// Preprocesses and stores all flags set in all stitches.
		/// </summary>
		public void CollateFlags ()
		{
			FlagIndex = new List<string> ();
			foreach (var stitch in Stitches) {
				foreach (var flag in stitch.Flags) {
					AddFlagToIndex (flag);
				}
				foreach (var option in stitch.Options) {
					foreach (var ifCondition in option.IfConditions) {
						AddFlagToIndex (ifCondition);
					}
					foreach (var notIfCondition in option.NotIfConditions) {
						AddFlagToIndex (notIfCondition);
					}
				}
				foreach (var ifCondition in stitch.IfConditions) {
					AddFlagToIndex (ifCondition);
				}
				foreach (var notIfCondition in stitch.NotIfConditions) {
					AddFlagToIndex (notIfCondition);
				}
			}
		}

		public void AddFlagToIndex (string flag)
		{
			Console.WriteLine ("Adding flag string " + flag);
			var name = ExtractFlagNameFromExpression (flag);
			if (!FlagIndex.Contains (name)) {
				FlagIndex.Add (name);
			}
		}

		public int GetIdxOfFlag (string flag, List<FlagValue> allFlags)
		{
			for (var i = 0; i < allFlags.Count; i++) {
				if (allFlags[i].flagName == flag) {
					return i;
				}
			}
			return -1;
		}

		public static string ExtractFlagNameFromExpression (string expression)
		{
			var regex = new Regex (@"^(.*?)\s*(\=|\+|\-|\>|\<|\!\=|$)");
			var match = regex.Match (expression);
			return match.Captures[1].Value;
		}

		public int GetValueOfFlag (string flag, List<FlagValue> allFlags)
		{
			var n = GetIdxOfFlag (flag, allFlags);
			return n >= 0 ? allFlags[n].value : 0;
		}

		public void ProcessFlagSetting (Stitch stitch, List<FlagValue> allFlags) // t == all flags
		{
			for (int n = 0; n < stitch.Flags.Count; n++) {
				string r = stitch.FlagByIndex (n);
				int i = 0;
				Console.WriteLine ("Flag directive: " + r);
				var o = new Regex (@"^(.*?)\s*(\=|\+|\-)\s*(\b.*\b)\s*$");
				var s = o.Matches (r);
				int u = -1;
				if (s.Count > 0) {
					r = s[1].ToString ();
					u = GetIdxOfFlag(r, allFlags);
					var m = new Regex (@"\d+");
					if (m.IsMatch (s [3].ToString ())) {
						if (s [2].ToString () == "=") {
							i = int.Parse (s[3].ToString ());
						}
					} else {
						if (u < 0) {
							i = 0;
						} else {
							i = allFlags[u].value;
						}
						if (s[2].ToString () == "+") {
							i += int.Parse (s[3].ToString ());
						} else {
							i -= int.Parse (s[3].ToString ());
						}
					}
					if (s[2].ToString () == "=") {
						i = ConvertStringToBooleanIfAppropriate (s[3].ToString ());
					} else {
						Console.WriteLine ("Can't add/subtract a boolean.");
					}
				} else {
					u = GetIdxOfFlag(r, allFlags);
				}
				Console.WriteLine ("Assigning value: " + i);
				if (u >= 0) {
					allFlags.RemoveAt(u);
				}
				var a = new FlagValue {
					flagName = r,
					value = i
				};
				allFlags.Add(a);
			}
		}

		public bool Test (string expression, List<FlagValue> allFlags)
		{
			Regex regex = new Regex (@"^(.*?)\s*(\<|\>|\<\=|\>\=|\=|\!\=|\=\=)\s*(\b.*\b)\s*$");
			bool result = false;
			MatchCollection matches = regex.Matches (expression);
			if (regex.IsMatch (expression)) {
				string flag = matches [1].ToString ();
				string op = matches [2].ToString ();
				string valueString = matches [3].ToString ();
				int value = int.Parse (valueString);
				
				int flagValue = GetValueOfFlag (flag, allFlags);
				Console.WriteLine ("Testing " + flagValue + " " + op + " " + value);
				if (op == "==" || op == "=") {
					result = flagValue == value;
				} else {
					if (op == "!=" || op == "<>") {
						result = flagValue != value;
					} else {
						if (Regex.IsMatch (valueString, @"\d+")) {
							throw new System.Exception ("Error - Can't perform an order-test on a boolean.");
						}
						if (op == "<") {
							result = flagValue < value;
						} else if (op == "<=") {
							result = flagValue <= value;
						} else if (op == ">") {
							result = flagValue > value;
						} else if (op == ">=") {
							result = flagValue >= value;
						}
					}
				}
			} else {
				//				result = GetValueOfFlag (expression, allFlags) == 1;
				//				result = ConvertStringToBooleanIfAppropriate(result) == 1;
				//				if (result == false || result == -1) {
				//					result = false;
				//				}
				//				result = true; // FIXME is this right?
			}
			return result;
		}

		#endregion

		#region Stitches

		public Stitch CreateStitch (string text)
		{
			Stitch s = new Stitch (text);
			Stitches.Add (s);
			return s;
		}

		public void RemoveStitch (Stitch e)
		{
			if (WatchRefCounts ()) {
				Console.WriteLine ("Removing " + e.Name + " entirely.");
			}
			if (e.RefCount != 0) {
				RepointStitchToStitch (e, null);
				Console.WriteLine ("Deleting stitch with references, so first unpointing stitches from this stitch.");
				if (e.RefCount != 0) {
					throw new System.Exception ("Fixing ref-count on stitch removal failed.");
				}
			}
			e.Undivert ();
			for (int t = e.Options.Count - 1; t >= 0; t--) {
				e.RemoveOption (e.Options [t]);
			}
			RemovePageNumber(e, true);
			for (var t = 0; t < Stitches.Count; ++t) {
				if (Stitches [t] == e) {
					Stitches.RemoveAt (t);
					return;
				}
			}
		}

		public void RepointStitchToStitch (Stitch source, Stitch target)
		{
			if (WatchRefCounts ()) {
				Console.WriteLine ("Repointing stitch links from " + source.Name + " to " + (target != null ? target.Name : "to null."));
			}
			var stitches = Stitches;
			for (int n = 0; n < stitches.Count; n++) {
				var r = stitches[n];
				if (r.DivertStitch == source) {
					r.Undivert ();
					if (target != null) {
						r.DivertTo (target);
					}
				}
				for (var i = 0; i < r.Options.Count; i++) {
					if (r.Options[i].LinkStitch == source) {
						r.Options [i].Unlink ();
						if (target != null) {
							r.Options [i].CreateLinkStitch (target);
						}
					}
				}
			}
		}

		public void NameStitches ()
		{
			HashSet<string> usedShortNames = new HashSet<string> ();
			var stitches = Stitches;
			foreach (var currentStitch in stitches) {
				string shortName = currentStitch.CreateShortName ();
				string incrementedShortName = shortName;
				for (int num = 1; usedShortNames.Contains (shortName); num++) {
					incrementedShortName = shortName + num;
				}
				shortName = incrementedShortName;
				usedShortNames.Add (shortName);
				currentStitch.Name = shortName;
			}
		}

		#endregion

		#region Options

		public Option CreateOption (Stitch stitch)
		{
			var t = stitch.AddOption ();
			return t;
		}

		public void RemoveOption (Stitch stitch, Option opt)
		{
			stitch.RemoveOption (opt);
		}

		#endregion

		#region Page Numbers

		public void InsertPageNumber (Stitch e)
		{
			if (Loading || e.VerticalDistanceFromPageNumberHeader < 2
				|| PageSize (e.PageNumber) < StoryModel.maxPreferredPageLength / 2
				|| HeaderWithinDistanceOfStitch (3, e))
			{
				return;
			}
			if (e.PageNumber != 0) {
				return;
			}
			var n = e.PageNumber + 1;
			var stitches = Stitches;
			for (var r = 0; r < stitches.Count; r++) {
				var i = stitches[r].PageNumber;
				if (i >= n) {
					stitches [r].SetPageNumberLabel (this, i + 1);
				}
			}
			e.SetPageNumberLabel (this, n);
			ComputePageNumbers ();
		}

		public void RemovePageNumber (Stitch e, bool doIt)
		{
//			var n = e.pageNumberLabel();
//			if (n <= 0) return;
//			e.pageNumberLabel(-1);
//			for (var r = 0; r < StoryModel.stitches.length; r++) {
//				var i = StoryModel.stitches[r].pageNumberLabel();
//				i > n && StoryModel.stitches[r].pageNumberLabel(i - 1)
//			}
//			t || StoryModel.computePageNumbers()
		}

		public void ComputePageNumbers ()
		{
			var e = new List<Stitch> ();
			var t = 0;
			//			var n = {}		;
			//			var r = {};
			var stitches = Stitches;
			for (var i = 0; i < stitches.Count; i++) {
				var s = stitches[i].PageNumber;
				if (s > 0) {
					e.Add (stitches [i]);
					if (s > t) {
						t = s;
					}
					stitches [i].SetPageNumberLabel (this, s);
					//					n[s] = [];
					//					r[s] = !0;
				} else {
					stitches [i].SetPageNumberLabel (this, 0);
				}
			}
			//			e.sort(function(e, t) {
			//				return e.pageNumberLabel() - t.pageNumberLabel()
			//				});
			for (var i = e.Count - 1; i >= 0; i--) {
				//				var o = function(t, r, s) {
				//					if (!t) return;
				//					if (!r && t.pageNumber() > 0) {
				//						t.verticalDistanceFromHeader() > s && t.pageNumber() == e[i].pageNumber() && t.verticalDistanceFromHeader(s), n[e[i].pageNumber()].push(t.pageNumber());
				//						return
				//						}
				//					t.pageNumber(e[i].pageNumber()), t.headerStitch(e[i]), e[i].sectionStitches.push(t), t.verticalDistanceFromHeader(s), o(t.divertStitch, !1, s + .01);
				//					for (var u = 0; u < t.options.length; u++) o(t.options[u].linkStitch(), !1, s + 1 + .1 * u)
				//					};
				//				o(e[i], !0, 0)
			}
			//			var u = [];
			//			u.push(initialStitch.pageNumber());
			//			while (u.length > 0) {
			//				var a = [];
			//				for (var i = 0; i < u.length; i++)
			//					if (r[u[i]]) {
			//						r[u[i]] = !1;
			//						for (var f = 0; f < n[u[i]].length; f++) a.push(n[u[i]][f])
			//						}
			//				u = a
			//			}
			//			for (var i = 0; i < StoryModel.stitches.length; i++) {
			//				var l = StoryModel.stitches[i].pageNumber();
			//				l && r[l] && (StoryModel.stitches[i].pageNumber(0), StoryModel.stitches[i].sectionStitches = [])
			//			}
		}

		#endregion

		public void Purge ()
		{
			if (Stitches.Count == 0) {
				return;
			}
			var stitches = Stitches;
			List<Stitch> stitchesToRemove = new List<Stitch> ();
			for (var t = 0; t < stitches.Count; ++t) {
				var n = stitches[t];
				if (n.IsDead) {
					stitchesToRemove.Add (n);
				}
			}
			for (var t = 0; t < stitchesToRemove.Count; t++) {
				RemoveStitch (stitchesToRemove [t]);
			}
		}

		public int ConvertStringToBooleanIfAppropriate (string s)
		{
			// FIXME bools represented as 0 and 1
			return 0;
		}

		public bool HeaderWithinDistanceOfStitch (int distance, Stitch stitch)
		{
//			var n = [],
//			r = [];
//			n.push(t);
//			for (var i = 0; i <= e; i++) {
//				for (var s = 0; s < n.length; s++) {
//					var o = n[s];
//					if (o) {
//						if (o.pageNumberLabel() > 0) return !0;
//						r.push(o.divertStitch);
//						for (var u = 0; u < o.options.length; u++) r.push(o.options[u]._linkStitch)
//						}
//				}
//				n = r, r = []
//			}
//			return !1

			// FIXME
			return false;
		}

		public int PageSize (int pageNumber)
		{
			var t = 0;
			var stitches = Stitches;
			for (var n = 0; n < stitches.Count; n++) {
				if (stitches [n].PageNumber == pageNumber) {
					t++;
				}
			}
			return t;
		}

		public void ComputeVerticalHeuristic ()
		{
			if (Story.InitialStitch == null) {
				return;
			}
//			var e = [];
//			t = [];
			var stitches = Stitches;
//			for (var n = 0; n < stitches.Count; n++) {
//				var r = stitches[n];
//				r.VerticalDistance (-1);
//			}
//			e.push(StoryModel.initialStitch), StoryModel.initialStitch.verticalDistance(1);
//			while (e.length > 0) {
//				for (var n = 0; n < e.length; n++) {
//					var r = e[n];
//					if (r.divertStitch) {
//						var i = r.divertStitch;
//						i.verticalDistance() == -1 && (i.verticalDistance(r.verticalDistance() + .01), t.push(i))
//					} else
//						for (var s = 0; s < r.options.length; s++)
//							if (r.options[s].linkStitch()) {
//								var i = r.options[s].linkStitch();
//								i.verticalDistance() == -1 && (i.verticalDistance(r.verticalDistance() + 1 + .1 * s), t.push(i))
//							}
//				}
//				e = t, t = []
//			}
//			for (var n = 0; n < StoryModel.stitches.length; n++) {
//				var r = StoryModel.stitches[n];
//				r.verticalDistance() == -1 && r.verticalDistance(StoryModel.stitches.length + 1)
//			}
		}
	}
}

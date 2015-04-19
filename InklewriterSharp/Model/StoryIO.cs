﻿using System.Collections.Generic;
using LitJson;

namespace Inklewriter
{
	public class StoryIO
	{
		public static Story Read (string data)
		{
			JsonReader reader = new JsonReader (data);
			Story story = new Story ();
			ReadStoryRoot (reader, story);
			return story;
		}

		static void ReadStoryRoot (JsonReader reader, Story story)
		{
			// read object start
			reader.Read ();

			while (reader.Read ()) {
				if (reader.Token == JsonToken.ObjectEnd) {
					return;
				}
				if (reader.Token != JsonToken.PropertyName) {
					continue;
				}
				string propertyName = (string)reader.Value;
				reader.Read ();
				switch (propertyName) {
				case "created_at":
					story.createdAt = (string)reader.Value;
					break;
				case "data":
					ReadData (reader, story);
					break;
				case "title":
					story.title = (string)reader.Value;
					break;
				case "updated_at":
					story.updatedAt = (string)reader.Value;
					break;
				case "url_key":
					story.urlKey = (string)reader.Value;
					break;
				}
			}
		}

		static void ReadData (JsonReader reader, Story story)
		{
			story.data = new Data ();

			// read object start
			reader.Read ();

			while (reader.Read ()) {
				if (reader.Token == JsonToken.ObjectEnd) {
					return;
				}
				if (reader.Token != JsonToken.PropertyName) {
					continue;
				}
				string propertyName = (string)reader.Value;
				reader.Read ();
				switch (propertyName) {
				case "allowCheckpoints":
					story.data.allowCheckpoints = (bool)reader.Value;
					break;
				case "editorData":
					ReadDataEditor (reader, story);
					break;
				case "initial":
					story.data.initial = (string)reader.Value;
					break;
				case "optionMirroring":
					story.data.optionMirroring = (bool)reader.Value;
					break;
				case "stitches":
					ReadStitches (reader, story);
					break;
				}
			}
		}

		static void ReadDataEditor (JsonReader reader, Story story)
		{
			story.data.editorData = new EditorData ();

			// read object start
			reader.Read ();

			while (reader.Read ()) {
				if (reader.Token == JsonToken.ObjectEnd) {
					return;
				}
				if (reader.Token != JsonToken.PropertyName) {
					continue;
				}
				string propertyName = reader.Value as string;
				reader.Read ();
				switch (propertyName) {
				case "authorName":
					story.data.editorData.authorName = (string)reader.Value;
					break;
				case "libraryVisible":
					story.data.editorData.libraryVisible = (bool)reader.Value;
					break;
				case "playPoint":
					story.data.editorData.playPoint = (string)reader.Value;
					break;
				case "textSize":
					story.data.editorData.textSize = (int)reader.Value;
					break;
				}
			}
		}

		static void ReadStitches (JsonReader reader, Story story)
		{
			story.data.stitches = new Dictionary<string, Stitch> ();

			while (reader.Read ()) {
				// We've overrun the stitches
				if (reader.Token == JsonToken.ObjectEnd) {
					return;
				}

				string key = (string)reader.Value;
				Stitch stitch = new Stitch ();
				//Begin object containing only 'content' array
				reader.Read ();

				if (reader.Token == JsonToken.ObjectStart) {
					// reader 'content'
					reader.Read ();
					// enter 'content' array start
					reader.Read ();
					// read stitch's raw text
					reader.Read ();

					// Main text
					stitch.text = (string)reader.Value;

					// Read all stitch options. Exits when the array end token is read.
					bool haveMoreStitchContent = false;
					do {
						haveMoreStitchContent = ReadStitchContent (reader, story, stitch);
					} while (haveMoreStitchContent);

					// Read stitch's object end token, save the stitch
					reader.Read ();
					story.data.stitches.Add (key, stitch);
				}
			}
		}

		static bool ReadStitchContent (JsonReader reader, Story story, Stitch stitch)
		{
			// Start object
			reader.Read ();
			if (reader.Token != JsonToken.ObjectStart) {
				return false;
			}

			string linkPath = null;
			string option = null;

			List<string> ifConditions = null;
			List<string> notIfConditions = null;

			string image = null;
			int? pageNum = null;
			string pageLabel = null;
			bool? runOn = null;
			string divert = null;
			List<string> flagNames = null;

			while (reader.Token != JsonToken.ObjectEnd) {
				reader.Read ();
				if (reader.Token != JsonToken.PropertyName) {
					continue;
				}
				string propertyName = reader.Value as string;
				reader.Read ();

				if (reader.Token == JsonToken.Null) {
					continue;
				}

				switch (propertyName) {
				case "option":
					option = (string)reader.Value;
					break;
				case "linkPath":
					linkPath = (string)reader.Value;
					break;
				case "ifCondition": // belongs to stitch
					if (ifConditions == null) {
						ifConditions = new List<string> ();
					}
					string ifCondition = (string)reader.Value;
					ifConditions.Add (ifCondition);
					break;
				case "notIfCondition": // belongs to stitch
					if (notIfConditions == null) {
						notIfConditions = new List<string> ();
					}
					string notIfCondition = (string)reader.Value;
					notIfConditions.Add (notIfCondition);
					break;
				case "ifConditions": // belongs to option
					ifConditions = ReadOptionConditions (reader);
					break;
				case "notIfConditions": // belongs to option
					notIfConditions = ReadOptionConditions (reader);
					break;
				case "image":
					image = (string)reader.Value;
					break;
				case "pageNum":
					pageNum = (int)reader.Value;
					break;
				case "pageLabel":
					pageLabel = (string)reader.Value;
					break;
				case "runOn":
					runOn = (bool)reader.Value;
					break;
				case "divert":
					divert = (string)reader.Value;
					break;
				case "flagName":
					if (flagNames == null) {
						flagNames = new List<string> ();
					}
					flagNames.Add ((string)reader.Value);
					break;
				}
			}

			if (option != null) { // is an option object
				var newOption = new Option ();
				newOption.option = option;
				newOption.ifConditions = ifConditions;
				newOption.notIfConditions = notIfConditions;
				newOption.linkPath = linkPath;
				if (stitch.options == null) {
					stitch.options = new List<Option> ();
				}
				stitch.options.Add (newOption);
			} else {
				stitch.ifConditions = ifConditions;
				stitch.notIfConditions = notIfConditions;
				stitch.image = image;
				if (pageNum.HasValue) {
					stitch.pageNum = pageNum.Value;
				}
				stitch.pageLabel = pageLabel;
				if (runOn.HasValue) {
					stitch.runOn = runOn.Value;
				}
				stitch.divert = divert;
				stitch.flagNames = flagNames;
			}
			return true;
		}

		static List<string> ReadOptionConditions (JsonReader reader)
		{
			if (reader.Token == JsonToken.Null) {
				return null;
			}

			// Read array start
			reader.Read ();

			// Read first object start
			reader.Read ();

			List<string> conditions = new List<string> ();

			while (reader.Token != JsonToken.ArrayEnd) {
				reader.Read ();
				string condition = (string)reader.Value;
				conditions.Add (condition);
				reader.Read ();
				// Read next object start, or the end of the array
				reader.Read ();
			}
			return conditions;
		}

		public static string Write (Story story)
		{
			JsonWriter writer = new JsonWriter ();
			writer.PrettyPrint = true;
			writer.WriteObjectStart ();

			writer.WritePropertyName ("created_at");
			// FIXME doesn't match correct date formatting
			writer.Write (story.createdAt);

			// Data
			writer.WritePropertyName ("data");
			writer.WriteObjectStart ();

			writer.WritePropertyName ("allowCheckpoints");
			writer.Write (story.data.allowCheckpoints);

			writer.WritePropertyName ("editorData");
			{
				// Editor data
				writer.WriteObjectStart ();

				writer.WritePropertyName ("authorName");
				writer.Write (story.data.editorData.authorName);

				writer.WritePropertyName ("libraryVisible");
				writer.Write (story.data.editorData.libraryVisible);

				writer.WritePropertyName ("playPoint");
				writer.Write (story.data.editorData.playPoint);

				writer.WritePropertyName ("textSize");
				writer.Write (story.data.editorData.textSize);

				writer.WriteObjectEnd ();
			}

			writer.WritePropertyName ("initial");
			writer.Write (story.data.initial);

			writer.WritePropertyName ("optionMirroring");
			writer.Write (story.data.optionMirroring);

			writer.WritePropertyName ("stitches");
			{
				// Stitches
				writer.WriteObjectStart ();
				foreach (var kvp in story.data.stitches) {
					writer.WritePropertyName (kvp.Key);
					writer.WriteObjectStart ();
					writer.WritePropertyName ("content");
					writer.WriteArrayStart ();
					{
						var stitch = kvp.Value;
						writer.Write (stitch.text);

						if (stitch.divert != null) {
							writer.WriteObjectStart ();
							writer.WritePropertyName ("divert");
							writer.Write (stitch.divert);
							writer.WriteObjectEnd ();
						}
						if (stitch.flagNames != null) {
							foreach (var flag in stitch.flagNames) {
								writer.WriteObjectStart ();
								writer.WritePropertyName ("flagName");
								writer.Write (flag);
								writer.WriteObjectEnd ();
							}
						}
						if (stitch.image != null) {
							writer.WriteObjectStart ();
							writer.WritePropertyName ("image");
							writer.Write (stitch.image);
							writer.WriteObjectEnd ();
						}
						if (stitch.options != null) {
							foreach (var opt in stitch.options) {
								writer.WriteObjectStart ();

								writer.WritePropertyName ("ifConditions");
								if (opt.ifConditions != null) {
									foreach (var condition in opt.ifConditions) {
										writer.WriteObjectStart ();
										writer.WritePropertyName ("ifCondition");
										writer.Write (condition);
										writer.WriteObjectEnd ();
									}
								} else {
									writer.Write (null);
								}

								writer.WritePropertyName ("linkPath");
								writer.Write (opt.linkPath);

								writer.WritePropertyName ("notIfConditions");
								if (opt.notIfConditions != null) {
									foreach (var condition in opt.notIfConditions) {
										writer.WriteObjectStart ();
										writer.WritePropertyName ("notIfCondition");
										writer.Write (condition);
										writer.WriteObjectEnd ();
									}
								} else {
									writer.Write (null);
								}

								writer.WritePropertyName ("option");
								writer.Write (opt.option);

								writer.WriteObjectEnd ();
							}
						}
						if (stitch.pageLabel != null) {
							writer.WriteObjectStart ();
							writer.WritePropertyName ("pageLabel");
							writer.Write (stitch.pageLabel);
							writer.WriteObjectEnd ();
						}
						if (stitch.pageNum != -1) {
							writer.WriteObjectStart ();
							writer.WritePropertyName ("pageNum");
							writer.Write (stitch.pageNum);
							writer.WriteObjectEnd ();
						}
						if (stitch.runOn) {
							writer.WriteObjectStart ();
							writer.WritePropertyName ("runOn");
							writer.Write (stitch.runOn);
							writer.WriteObjectEnd ();
						}
						if (stitch.ifConditions != null) {
							writer.WriteObjectStart ();
							foreach (var condition in stitch.ifConditions) {
								writer.WritePropertyName ("ifCondition");
								writer.Write (condition);
							}
							writer.WriteObjectEnd ();
						}
						if (stitch.notIfConditions != null) {
							writer.WriteObjectStart ();
							foreach (var condition in stitch.notIfConditions) {
								writer.WritePropertyName ("notIfCondition");
								writer.Write (condition);
							}
							writer.WriteObjectEnd ();
						}
					}
					writer.WriteArrayEnd ();
					writer.WriteObjectEnd ();
				}
				writer.WriteObjectEnd ();
			}

			writer.WriteObjectEnd ();

			writer.WritePropertyName ("title");
			writer.Write (story.title);

			writer.WritePropertyName ("updated_at");
			writer.Write (story.updatedAt);

			writer.WritePropertyName ("url_key");
			writer.Write (story.urlKey);

			writer.WriteObjectEnd ();

			return writer.ToString ();
		}
	}
}


﻿using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected readonly Bk2Header Header = new Bk2Header();
		private string _syncSettingsJson = "";

		public IDictionary<string, string> HeaderEntries => Header;

		public SubtitleList Subtitles { get; } = new SubtitleList();
		public IList<string> Comments { get; } = new List<string>();

		public string SyncSettingsJson
		{
			get => _syncSettingsJson;
			set
			{
				if (_syncSettingsJson != value)
				{
					Changes = true;
					_syncSettingsJson = value;
				}
			}
		}

		public ulong Rerecords
		{
			get => Header.TryGetValue(HeaderKeys.Rerecords, out var s)
				? ulong.Parse(s)
				: 0UL; // Modifying the header itself can cause a race condition between loading a movie and rendering the rerecord count, causing a movie's rerecord count to be overwritten with 0 during loading.
			set
			{
				if (Header[HeaderKeys.Rerecords] != value.ToString())
				{
					Changes = true;
					Header[HeaderKeys.Rerecords] = value.ToString();
				}
			}
		}

		public virtual bool StartsFromSavestate
		{
			// ReSharper disable SimplifyConditionalTernaryExpression
			get => Header.TryGetValue(HeaderKeys.StartsFromSavestate, out var s) ? bool.Parse(s) : false;
			// ReSharper restore SimplifyConditionalTernaryExpression
			set
			{
				if (value)
				{
					Header[HeaderKeys.StartsFromSavestate] = "True";
				}
				else
				{
					Header.Remove(HeaderKeys.StartsFromSavestate);
				}
			}
		}

		public bool StartsFromSaveRam
		{
			// ReSharper disable SimplifyConditionalTernaryExpression
			get => Header.TryGetValue(HeaderKeys.StartsFromSaveram, out var s) ? bool.Parse(s) : false;
			// ReSharper restore SimplifyConditionalTernaryExpression
			set
			{
				if (value)
				{
					if (!Header.ContainsKey(HeaderKeys.StartsFromSaveram))
					{
						Header.Add(HeaderKeys.StartsFromSaveram, "True");
					}
				}
				else
				{
					Header.Remove(HeaderKeys.StartsFromSaveram);
				}
			}
		}

		public string GameName
		{
			get => Header.TryGetValue(HeaderKeys.GameName, out var s) ? s : string.Empty;
			set
			{
				if (Header[HeaderKeys.GameName] != value)
				{
					Changes = true;
					Header[HeaderKeys.GameName] = value;
				}
			}
		}

		public string SystemID
		{
			get => Header.TryGetValue(HeaderKeys.Platform, out var s) ? s : string.Empty;
			set
			{
				if (Header[HeaderKeys.Platform] != value)
				{
					Changes = true;
					Header[HeaderKeys.Platform] = value;
				}
			}
		}

		public string Hash
		{
			get => Header[HeaderKeys.Sha1];
			set
			{
				if (Header[HeaderKeys.Sha1] != value)
				{
					Changes = true;
					Header[HeaderKeys.Sha1] = value;
				}
			}
		}

		public string Author
		{
			get => Header[HeaderKeys.Author];
			set
			{
				if (Header[HeaderKeys.Author] != value)
				{
					Changes = true;
					Header[HeaderKeys.Author] = value;
				}
			}
		}

		public string Core
		{
			get => Header[HeaderKeys.Core];
			set
			{
				if (Header[HeaderKeys.Core] != value)
				{
					Changes = true;
					Header[HeaderKeys.Core] = value;
				}
			}
		}

		public string BoardName
		{
			get => Header[HeaderKeys.BoardName];
			set
			{
				if (Header[HeaderKeys.BoardName] != value)
				{
					Changes = true;
					Header[HeaderKeys.BoardName] = value;
				}
			}
		}

		public string EmulatorVersion
		{
			get => Header[HeaderKeys.EmulatorVersion];
			set
			{
				if (Header[HeaderKeys.EmulatorVersion] != value)
				{
					Changes = true;
					Header[HeaderKeys.EmulatorVersion] = value;
				}
			}
		}

		public string OriginalEmulatorVersion
		{
			get => Header[HeaderKeys.OriginalEmulatorVersion];
			set
			{
				if (Header[HeaderKeys.OriginalEmulatorVersion] != value)
				{
					Changes = true;
					Header[HeaderKeys.OriginalEmulatorVersion] = value;
				}
			}
		}

		public string FirmwareHash
		{
			get => Header[HeaderKeys.FirmwareSha1];
			set
			{
				if (Header[HeaderKeys.FirmwareSha1] != value)
				{
					Changes = true;
					Header[HeaderKeys.FirmwareSha1] = value;
				}
			}
		}

		protected string CommentsString()
		{
			var sb = new StringBuilder();

			foreach (var comment in Comments)
			{
				sb.AppendLine(comment);
			}

			return sb.ToString();
		}

		// ReSharper disable SimplifyConditionalTernaryExpression
		public bool IsPal => Header.TryGetValue(HeaderKeys.Pal, out var s) ? s == "1" : false;
		// ReSharper restore SimplifyConditionalTernaryExpression

		public string TextSavestate { get; set; }
		public byte[] BinarySavestate { get; set; }
		public int[] SavestateFramebuffer { get; set; }
		public byte[] SaveRam { get; set; }
	}
}

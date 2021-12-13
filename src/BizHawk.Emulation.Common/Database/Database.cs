﻿#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	public static class Database
	{
		private static readonly Dictionary<Checksum, CompactGameInfo> DB = new();

		/// <summary>
		/// blocks until the DB is done loading
		/// </summary>
		private static readonly EventWaitHandle acquire = new EventWaitHandle(false, EventResetMode.ManualReset);

		private static void LoadDatabase_Escape(string line, string path, bool silent)
		{
			if (!line.ToUpperInvariant().StartsWith("#INCLUDE"))
			{
				return;
			}

			line = line.Substring(8).TrimStart();
			var filename = Path.Combine(path, line);
			if (File.Exists(filename))
			{
				if (!silent) Util.DebugWriteLine($"loading external game database {line}");
				initializeWork(filename, silent);
			}
			else
			{
				Util.DebugWriteLine($"BENIGN: missing external game database {line}");
			}
		}

		public static void SaveDatabaseEntry(string path, CompactGameInfo gameInfo)
		{
			var sb = new StringBuilder();
			sb.Append(gameInfo.Hash)
				.Append('\t');

			sb.Append(gameInfo.Status switch
			{
				RomStatus.BadDump => "B",
				RomStatus.TranslatedRom => "T",
				RomStatus.Overdump => "O",
				RomStatus.Bios => "I",
				RomStatus.Homebrew => "D",
				RomStatus.Hack => "H",
				RomStatus.NotInDatabase => "U",
				RomStatus.Unknown => "U",
				_ => ""
			});

			sb
				.Append('\t')
				.Append(gameInfo.Name)
				.Append('\t')
				.Append(gameInfo.System)
				.Append('\t')
				.Append(gameInfo.MetaData)
				.Append(Environment.NewLine);

			File.AppendAllText(path, sb.ToString());
		}

		private static bool initialized = false;

		private static void initializeWork(string path, bool silent)
		{
			//reminder: this COULD be done on several threads, if it takes even longer
			using var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
			while (reader.EndOfStream == false)
			{
				var line = reader.ReadLine() ?? "";
				try
				{
					if (line.StartsWith(";"))
					{
						continue; // comment
					}

					if (line.StartsWith("#"))
					{
						LoadDatabase_Escape(line, Path.GetDirectoryName(path), silent);
						continue;
					}

					if (line.Trim().Length == 0)
					{
						continue;
					}

					var items = line.Split('\t');

					var game = new CompactGameInfo
					{
						Hash = Checksum.Parse(items[0], out _),
						Status = items[1].Trim()
							switch
						{
							"B" => RomStatus.BadDump,
							"V" => RomStatus.BadDump,
							"T" => RomStatus.TranslatedRom,
							"O" => RomStatus.Overdump,
							"I" => RomStatus.Bios,
							"D" => RomStatus.Homebrew,
							"H" => RomStatus.Hack,
							"U" => RomStatus.Unknown,
							_ => RomStatus.GoodDump
						},
						Name = items[2],
						System = items[3],
						MetaData = items.Length >= 6 ? items[5] : null,
						Region = items.Length >= 7 ? items[6] : "",
						ForcedCore = items.Length >= 8 ? items[7].ToLowerInvariant() : ""
					};

					if (!silent && DB.TryGetValue(game.Hash, out var dupe))
					{
						Console.WriteLine("gamedb: Multiple hash entries {0}, duplicate detected on \"{1}\" and \"{2}\"", game.Hash, game.Name, dupe.Name);
					}

					DB[game.Hash] = game;
				}
				catch
				{
					Util.DebugWriteLine($"Error parsing database entry: {line}");
				}
			}

			acquire.Set();
		}

		public static void InitializeDatabase(string path, bool silent)
		{
			if (initialized) throw new InvalidOperationException("Did not expect re-initialize of game Database");
			initialized = true;

			var stopwatch = Stopwatch.StartNew();
			ThreadPool.QueueUserWorkItem(_=> {
				initializeWork(path, silent);
				Util.DebugWriteLine("GameDB load: " + stopwatch.Elapsed + " sec");
			});
		}

		public static GameInfo CheckDatabase(Checksum hash)
		{
			acquire.WaitOne();

			DB.TryGetValue(hash, out var cgi);
			if (cgi == null)
			{
				Console.WriteLine($"DB: hash {hash} not in game database.");
				return null;
			}

			return new GameInfo(cgi);
		}

		public static GameInfo GetGameInfo(byte[] romData, string fileName)
		{
			acquire.WaitOne();

			var hashCRC32 = CRC32Checksum.Compute(romData);
			if (DB.TryGetValue(hashCRC32, out var cgi))
			{
				return new GameInfo(cgi);
			}

			var hashMD5 = MD5Checksum.Compute(romData);
			if (DB.TryGetValue(hashMD5, out cgi))
			{
				return new GameInfo(cgi);
			}

			var hashSHA1 = SHA1Checksum.Compute(romData);
			if (DB.TryGetValue(hashSHA1, out cgi))
			{
				return new GameInfo(cgi);
			}

			// rom is not in database. make some best-guesses
			var game = new GameInfo
			{
				Hash = hashSHA1,
				Status = RomStatus.NotInDatabase,
				NotInDatabase = true
			};

			Console.WriteLine($"Game was not in DB.  {hashCRC32}  {hashMD5}");

			var ext = Path.GetExtension(fileName)?.ToUpperInvariant();

			switch (ext)
			{
				case ".NES":
				case ".UNF":
				case ".FDS":
					game.System = VSystemID.Raw.NES;
					break;

				case ".SFC":
				case ".SMC":
					game.System = VSystemID.Raw.SNES;
					break;

				case ".GB":
					game.System = VSystemID.Raw.GB;
					break;
				case ".GBC":
					game.System = VSystemID.Raw.GBC;
					break;
				case ".GBA":
					game.System = VSystemID.Raw.GBA;
					break;
				case ".NDS":
					game.System = VSystemID.Raw.NDS;
					break;

				case ".SMS":
					game.System = VSystemID.Raw.SMS;
					break;
				case ".GG":
					game.System = VSystemID.Raw.GG;
					break;
				case ".SG":
					game.System = VSystemID.Raw.SG;
					break;

				case ".GEN":
				case ".MD":
				case ".SMD":
					game.System = VSystemID.Raw.GEN;
					break;

				case ".PSF":
				case ".MINIPSF":
					game.System = VSystemID.Raw.PSX;
					break;

				case ".PCE":
					game.System = VSystemID.Raw.PCE;
					break;
				case ".SGX":
					game.System = VSystemID.Raw.SGX;
					break;

				case ".A26":
					game.System = VSystemID.Raw.A26;
					break;
				case ".A78":
					game.System = VSystemID.Raw.A78;
					break;

				case ".COL":
					game.System = VSystemID.Raw.Coleco;
					break;

				case ".INT":
					game.System = VSystemID.Raw.INTV;
					break;

				case ".PRG":
				case ".D64":
				case ".T64":
				case ".G64":
				case ".CRT":
					game.System = VSystemID.Raw.C64;
					break;

				case ".TZX":
				case ".PZX":
				case ".CSW":
				case ".WAV":
					game.System = VSystemID.Raw.ZXSpectrum;
					break;

				case ".CDT":
					game.System = VSystemID.Raw.AmstradCPC;
					break;

				case ".TAP":
					byte[] head = romData.Take(8).ToArray();
					game.System = Encoding.Default.GetString(head).Contains("C64-TAPE")
						? VSystemID.Raw.C64
						: VSystemID.Raw.ZXSpectrum;
					break;

				case ".Z64":
				case ".V64":
				case ".N64":
					game.System = VSystemID.Raw.N64;
					break;

				case ".DEBUG":
					game.System = VSystemID.Raw.DEBUG;
					break;

				case ".WS":
				case ".WSC":
					game.System = VSystemID.Raw.WSWAN;
					break;

				case ".LNX":
					game.System = VSystemID.Raw.Lynx;
					break;

				case ".83P":
					game.System = VSystemID.Raw.TI83;
					break;

				case ".DSK":
					var dId = new DskIdentifier(romData);
					game.System = dId.IdentifiedSystem;
					break;

				case ".PO":
				case ".DO":
					game.System = VSystemID.Raw.AppleII;
					break;

				case ".VB":
					game.System = VSystemID.Raw.VB;
					break;

				case ".NGP":
				case ".NGC":
					game.System = VSystemID.Raw.NGP;
					break;

				case ".O2":
					game.System = VSystemID.Raw.O2;
					break;

				case ".UZE":
					game.System = VSystemID.Raw.UZE;
					break;

				case ".32X":
					game.System = VSystemID.Raw.Sega32X;
					game.AddOption("32X", "true");
					break;

				case ".VEC":
					game.System = VSystemID.Raw.VEC;
					game.AddOption("VEC", "true");
					break;

				// refactor to use mame db (output of "mame -listxml" command)
				// there's no good definition for Arcade anymore, so we might limit to coin-based machines?
				case ".ZIP":
					game.System = VSystemID.Raw.MAME;
					break;
			}

			game.Name = Path.GetFileNameWithoutExtension(fileName)?.Replace('_', ' ');

			// If filename is all-caps, then attempt to proper-case the title.
			if (!string.IsNullOrWhiteSpace(game.Name) && game.Name == game.Name.ToUpperInvariant())
			{
				game.Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(game.Name.ToLower());
			}

			return game;
		}
	}

	public class CompactGameInfo
	{
		public string Name { get; set; }
		public string System { get; set; }
		public string MetaData { get; set; }
		public Checksum Hash { get; set; }
		public string Region { get; set; }
		public RomStatus Status { get; set; }
		public string ForcedCore { get; set; }
	}
}

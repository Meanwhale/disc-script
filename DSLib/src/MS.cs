
using DiscScriptCore;
using System.Diagnostics.CodeAnalysis;

namespace DiscScript
{

	public class MS
	{
		public const int MAX_NAME_LENGTH = 128;

		public static bool IsDebug = true;
		public static bool IsVerbose = true;
		public static MOutput Printer = new MConsoleOutput();
		public static MOutput ErrorPrinter = new MConsoleOutput();

		public delegate void MAction();

		public static void Settings(bool debug, bool verbose)
		{
			IsDebug = debug;
			IsVerbose = verbose;
			if (!IsDebug) ByteAutomata.Debug = false;
		}

		public static void WriteLine(string s)
		{
			Printer.WriteLine(s);
		}
		public static void Write(string s)
		{
			Printer.Write(s);
		}
		public static void VerboseLine(string s)
		{
			if (IsVerbose) WriteLine(s);
		}
		public static void Verbose(string s)
		{
			if (IsVerbose) Write(s);
		}
		public static void Assertion(bool b, MError err, string msg)
		{
			if (b) return;
			Trap (err, "assertion failed: " + msg);
		}
		[DoesNotReturn]
		public static void Trap(MError err, string msg)
		{
			throw new MException(err, msg);
		}
		internal static bool ValidName(string data)
		{
			if (string.IsNullOrEmpty(data)) return false;
			return data.Length < MAX_NAME_LENGTH;
		}
		internal static string ToSafeChar(byte i)
		{
			// get the byte as char or code if it's a spacial character

			if (i > 127) // 127 = [DEL]
			{
				return "[#" + i + "]";
			}
			else if (i < 32) return ascii[i];
			else if (i == 127) return "[DEL]";
			else return ((char)i).ToString();
		}
		public static readonly string[] ascii = new string[]
		{
			"[NUL]",       // null
			"[SOH]",       // start of heading
			"[STX]",       // start of text
			"[ETX]",       // end of text
			"[EOT]",       // end of transmission
			"[ENQ]",       // enquiry
			"[ACK]",       // acknowledge
			"[BEL]",       // bell
			"[BS]",        // backpace
			"[HT]",        // horizontal tab
			"[LF]",        // line feed, new line
			"[VT]",        // vertical tab
			"[FF]",        // form feed, new page
			"[CR]",        // carriage return
			"[SO]",        // shift out
			"[SI]",        // shift in
			"[DLE]",       // data link escape
			"[DC1]",       // device control 1
			"[DC2]",       // device control 2
			"[DC3]",       // device control 3
			"[DC4]",       // device control 4
			"[NAK]",       // negative acknowledge
			"[SYN]",       // synchonous idle
			"[ETB]",       // end of transmission block
			"[CAN]",       // cancel
			"[EM]",        // end of medium
			"[SUB]",       // substitute
			"[ESC]",       // escape
			"[FS]",        // file separator
			"[GS]",        // group separator
			"[RS]",        // record separator
			"[US]"         // unit separator
		};
	}
}

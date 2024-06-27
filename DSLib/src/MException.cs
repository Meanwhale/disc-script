
namespace DiscScript
{
	public class MException : Exception
	{
		public readonly MError error;
		public readonly string info;
		public MException(MError err, string s) {
			error = err; info = "Error type [" + err.Title + "], " + s;
		}
		public MException(MError err) {
			error = err; info = err.Title;
		}
		override public string ToString() {
			return "\n---------------- EXCEPTION ----------------\n"
			     + info
				 + "\n-------------------------------------------\n"
			     + StackTrace
				 + "\n-------------------------------------------\n"; }
	}
	public class MError
	{
		public readonly string Title;

		private MError(string s)
		{
			Title = s;
		}

		public static readonly MError
			TEST = new("test"),
			IO = new("input/output"),
			CLI= new("command line"),
			LIST = new("list"),
			DATA = new("data"),
			DATA_TYPE = new("data type"),
			SERIALIZER = new ("serializer"),
			MAP_SERIALIZER = new ("map serializer"),
			STRUCT_SERIALIZER = new ("struct serializer"),
			DESERIALIZER = new ("deserializer"),
			BYTE_AUTOMATA = new("parse"),
			TOKEN_HANDLER = new("read line"),
			SPACE_ON_LINE_START = new("space on line start"),
			INDENTATION = new MError("indentation"),
			UNASSIGNED_NAME = new MError("unassigned name"),
			CONVERSION = new MError("conversion"),
			WRONG_KEY_TYPE = new MError("key type, only string and int are allowed");
	}
}

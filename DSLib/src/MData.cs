namespace DiscScript
{
	// args list item

	public abstract class IData
	{
		public ICSType? DataType { get; protected set; } = null;

		public static bool ENABLE_COMPACT = false;

		public abstract string GetString(); // string value for the user when quering data
		public abstract MOutput Write(MOutput sb, int depth, bool compact);

		// operator []

		// TODO less virtual functions: get and set (object)?

		public IData this[string key]
		{
			get => GetValue(key);
			set => SetValue(key, value);
		}
		public virtual void SetValue(string key, IData value)
		{
			throw new MException(MError.DATA_TYPE, "set [string] is not defined for this type: " + this);
		}
		public virtual IData GetValue(string key)
		{
			throw new MException(MError.DATA_TYPE, "get [string] is not defined for this type: " + this);
		}
		public IData this[int key]
		{
			get => GetValue(key);
			set => SetValue(key, value);
		}
		public virtual void SetValue(int key, IData value)
		{
			throw new MException(MError.DATA_TYPE, "set [int] is not defined for this type: " + this);
		}
		public virtual IData GetValue(int key)
		{
			throw new MException(MError.DATA_TYPE, "get [int] is not defined for this type: " + this);
		}

		// get primitives: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
		
		public int GetInt()
		{
			try {
				return int.Parse(GetString(), System.Globalization.CultureInfo.InvariantCulture);
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to int");
			}
		}
		public long GetLong()
		{
			try {
				return long.Parse(GetString(), System.Globalization.CultureInfo.InvariantCulture);
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to long");
			}
		}
		public float GetFloat()
		{
			try {
				return float.Parse(GetString(), System.Globalization.CultureInfo.InvariantCulture);
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to float");
			}
		}
		public double GetDouble()
		{
			try {
				return double.Parse(GetString(), System.Globalization.CultureInfo.InvariantCulture);
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to double");
			}
		}
		public decimal GetDecimal()
		{
			try {
				return decimal.Parse(GetString(), System.Globalization.CultureInfo.InvariantCulture);
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to decimal");
			}
		}
		public bool GetBool()
		{
			try {
				return bool.Parse(GetString());
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to bool");
			}
		}
		public byte GetByte()
		{
			try {
				return byte.Parse(GetString());
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to byte");
			}
		}
		public sbyte GetSByte()
		{
			try {
				return sbyte.Parse(GetString());
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to sbyte");
			}
		}
		public char GetChar()
		{
			try {
				return char.Parse(GetString());
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to char");
			}
		}
		public uint GetUInt()
		{
			try {
				return uint.Parse(GetString());
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to uint");
			}
		}
		public ulong GetULong()
		{
			try {
				return ulong.Parse(GetString());
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to ulong");
			}
		}
		public short GetShort()
		{
			try {
				return short.Parse(GetString());
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to short");
			}
		}
		public ushort GetUShort()
		{
			try {
				return ushort.Parse(GetString());
			} catch {
				throw new MException(MError.CONVERSION, ToString() + " to ushort");
			}
		}

		// 

		internal void Close()
		{
			if (this is MDataList str) str.CloseList();
		}

		internal void AssignDataType(ICSType type)
		{
			MS.VerboseLine("AssignDataType " + type + " for data: " + ToString());

			if (MS.IsVerbose)
			{
				MS.WriteLine("data content:");
				Write(MS.Printer, 0, true);
				MS.VerboseLine("");
			}

			if (this is MText text)
			{
				text.DataType = type;
				return;
			}
			else if (this is MDataList list)
			{
				list.DataType = type;
				if (type is GenericCSType gtype)
				{	
					// get type for all list values
					MS.Assertion(gtype.GenericTypeName.Equals("list"), MError.DATA, "AssignDataType"); // and array?
					var itemType = gtype.parameters[0];
					foreach(var x in list.GetValues())
					{
						x.AssignDataType(itemType);
					}
					return;
				}
				else if (type is ClassCSType st)
				{
					// serializable class data in list format
					// assign type to struct members by iterating thru member and data lists.

					var iterator = list.GetValues();
					foreach(var member in st.GetMembers())
					{
						if (iterator.MoveNext())
						{
							iterator.Current.AssignDataType(member.CsType);
						}
						else MS.Assertion(false, MError.DATA, "AssignDataType: list has too few items for struct: " + st.Name);
					}
					return;
				}
				MS.Trap(MError.DATA, "AssignDataType: MDataList, type: " + type);
			}
			else if (this is MMap map)
			{
				// assign type for all values
				
				if (type is GenericCSType gtype)
				{	
					var itemType = gtype.parameters[1];
					foreach(var leaf in map.Values())
					{
						leaf.AssignDataType(itemType);	
					}
					return;
				}
			}
			else if (this is MNull)
			{
				return;
			}
			MS.Trap(MError.DATA, "AssignDataType: MMap, type: " + type);
		}
		
	}

	public class MNull : IData
	{
		private MNull() {}	

		// hide constructor as it shouldn't be called other than for this one object:

		public static readonly MNull Value = new MNull();
		public const string SCRIPT = "%null";

		public override string GetString()
		{
			return SCRIPT;
		}
		public override MOutput Write(MOutput op, int depth, bool compact)
		{
			op.Write(SCRIPT);
			return op;
		}
	}

	public class MText : IData
	{
		private string data;
		
		public override MOutput Write(MOutput op, int depth, bool compact)
		{
			op.Write('"');
			op.Write(data);
			op.Write('"');
			return op;

		}
		public MText(string data)
		{
			this.data = data;
		}
		public override string GetString()
		{
			return data;
		}
		public override string ToString()
		{
			if (data.Length > 16) return "MText: " + data.Substring(0,16) + "...";
			return "MText: " + data;
		}
	}

	public class MDataList : IData
	{
		// flexible IData list or structured data if DataType is ClassCSType

		private MList<IData> list = new MList<IData>();

		public MDataList() {}
		public MDataList(ClassCSType cltype)
		{
			DataType = cltype;
		}
		public override MOutput Write(MOutput o, int depth, bool compact)
		{
			if (compact)
			{
				o.Write("(");
				bool first = true;
				foreach(var x in list)
				{
					if (first) first = false;
					else o.Write(", ");
					x.Write(o, depth, true);
				}
				o.Write(")");
			}
			else
			{
				depth++;
				foreach(var x in list)
				{
					o.LineBreak();
					o.Indent(depth);
					o.Write("- ");
					x.Write(o, depth, compact);
				}
			}
			return o;
		}
		public MListEnumerator<IData> GetValues()
		{
			return list.GetEnumerator();
		}
		public void Add(IData x)
		{
			if (DataType is ClassCSType str)
			{
				// adding _n_th struct data: (..., x, ...)
				// get expected type

				var member = str.GetByIndex(list.Size());

				x.AssignDataType(member.CsType);
			}
			list.AddLast(x);
		}
		public override string GetString()
		{
			throw new MException(MError.DATA, "GetString not defined: " + this);
		}
		public override IData GetValue(int i)
		{
			return list.GetAt(i);
		}
		public override IData GetValue(string key)
		{
			// list of struct data (similar to MDataStruct)
			if (DataType is ClassCSType cl)
			{
				var member = cl.GetByNameOrNull(key);
				if (member != null) return list.GetAt(member.Index);
			}
			throw new MException(MError.DATA, "Can't get value with string key: " + key);
		}
		public override string ToString()
		{
			return "MDataList [size " + list.Size() + "]";
		}

		internal int Size()
		{
			return list.Size();
		}
		internal void CloseList()
		{
			if (DataType is ClassCSType cl)
			{
				if (list.Size() != cl.NumMembers()) throw new MException(MError.DATA, "wrong number of member values for " + ToString());
			}
		}
	}
	public class MMap : IData
	{
		internal readonly Dictionary<string,IData> map = new Dictionary<string, IData>();


		public void Add(string k, IData v)
		{
			map[k] = v;
		}
		public Dictionary<string,IData>.ValueCollection Values()
		{
			return map.Values;
		}
		public Dictionary<string,IData> Entries()
		{
			return map;
		}
		public override string ToString()
		{
			return "MMap [size " + map.Count + "]";
		}
		public override IData GetValue(string key)
		{
			return map[key];
		}
		public override string GetString()
		{
			throw new NotImplementedException();
		}
		public override MOutput Write(MOutput o, int depth, bool compact)
		{
			if (compact)
			{
				bool first = true;
				foreach (var kv in map)
				{
					if (first) first = false;
					else o.Write(", ");
					o.Write(kv.Key);
					o.Write(": ");
					kv.Value.Write(o, depth, true); // change into compact
				}
			}
			else
			{
				depth++;
				foreach (var kv in map)
				{
					o.LineBreak();
					o.Indent(depth);
					o.Write(kv.Key);
					o.Write(": ");
					kv.Value.Write(o, depth, ENABLE_COMPACT); // change into compact
				}
				o.LineBreak();
			}
			return o;
		}
	}

}
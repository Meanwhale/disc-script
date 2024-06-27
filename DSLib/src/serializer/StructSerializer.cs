using System.Collections;
using System.Reflection;

namespace DiscScript
{
	public class StructSerializer
	{
		public static bool ENABLE_COMPACT = true;

		public static void Write(object obj, MOutput o)
		{
			MSerializer.Init();
			MSerializer.WriteStructDefinitions(o);
			var objType = obj.GetType();
			// check 
			var cc = MSerializer.classes[objType];
			o.WriteLine("[" + cc.Name + "] root");
			SerializeClassMembers(obj, 1, false, o, false);
			o.Close();
		}
		public static void SerializeClassMembers(object obj, int depth, bool dontIndentFirst, MOutput o, bool compact)
		{
			var objType = obj.GetType();
			
			MS.Assertion (objType.GetCustomAttributes(typeof(DSClassAttribute), true).Length > 0, MError.STRUCT_SERIALIZER, "can't serialize type: " + objType);
			
			IList<FieldInfo> fields = new List<FieldInfo>(objType.GetFields(BindingFlags.Public | BindingFlags.Instance));
			
			if (compact) o.Write("(");

			bool first = true;

			foreach (var fieldInfo in fields)
			{
				var value = fieldInfo.GetValue(obj);
				
				if (!compact && !dontIndentFirst)
				{
					o.Indent(depth);
				}
				if (compact)
				{
					if(!first) o.Write(", ");
				}
				else if (!dontIndentFirst) o.Write("- ");
				dontIndentFirst = false;

				bool turnCompact = ENABLE_COMPACT;

				SerializeField(value, fieldInfo.FieldType, true, depth, o, turnCompact);

				if (!compact && turnCompact) o.LineBreak();

				first = false;
			}
			if (compact) o.Write(")");
		}
		
		private static void SerializeKV(DictionaryEntry kv, int depth, MOutput o, bool compact)
		{
			if (!compact) o.Indent(depth);
			o.Write(kv.Key);
			if (compact) o.Write(": ");
			if (kv.Value == null) return;
			SerializeField(kv.Value, kv.Value.GetType(), false, depth, o, compact);
		}

		private static void SerializeListValue(object item, int depth, MOutput o, bool compact)
		{
			if (!compact) o.Indent(depth);
			if (!compact) o.Write("- ");
			SerializeField(item, item.GetType(), true, depth, o, compact);
		}
		
		private static void SerializeField(object? value, Type t, bool listItem, int depth, MOutput o, bool compact)
		{
			if (value == null)
			{
				o.Write(MNull.SCRIPT);
				return;
				//throw new MException(MError.STRUCT_SERIALIZER, "null value");
			}

			//if (/*depth >= 1 && */ENABLE_COMPACT) compact = true;

			if (value is IDictionary dict)
			{
				if (compact) o.Write("{");
				else o.LineBreak();
				bool first = true;
				foreach(DictionaryEntry kv in dict)
				{
					if (!first && compact) o.Write(", ");
					first = false;
					SerializeKV(kv, depth + 1, o, compact);
				}
				if (compact) o.Write("}");
			}
			else if (value is IList list)
			{
				if (compact) o.Write("(");
				else o.LineBreak();
				bool first = true;
				foreach(var item in list)
				{
					if (!first && compact) o.Write(", ");
					first = false;
					SerializeListValue(item, depth + 1, o, compact);
				}
				if (compact) o.Write(")");
			}
			else if (MSerializer.primitives.ContainsKey(t))
			{
				var csType = MSerializer.primitives[t];
				if (!listItem && !compact) o.Write(":");
				if (t.IsEnum)
				{
					// TODO: optionally save as int etc.
					o.Write(Convert.ToInt32(value));
				}
				else if (value is string)
				{
					o.Write('"').Write(value).Write('"');
				}
				else
				{
					csType.WriteValue(o, value);
				}
				if (!compact) o.LineBreak();
			}
			else if (t.GetTypeInfo().IsClass)
			{
				SerializeClassMembers(value, depth + 1, false, o, compact);
			}
			else
			{
				MS.Trap(MError.STRUCT_SERIALIZER, "can't serialize type: " + t);
			}
		}
	}
}

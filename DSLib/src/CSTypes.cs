
namespace DiscScript
{
	public abstract class ICSType
	{
		// Wraps native type (System.Type) of MData: can be
		//  1. simple (SimpleCSType) with a name like "int" or
		//  2. class (ClassCSType)
		//  3. generic (GenericCSType) without a unique name, only GenericTypeName like "list"

		public readonly Type RealType;

		protected ICSType(Type realType)
		{
			RealType = realType;
		}
		public abstract void WriteTypeName(MOutput o);

		public abstract bool Match(ICSType iCSType);
	}
	public class SimpleCSType : ICSType
	{
		public readonly string Name;

		public SimpleCSType(string name, Type realType) : base(realType)
		{
			Name = name;
		}
		public override void WriteTypeName(MOutput o)
		{
			o.Write(Name);
		}
		public override string ToString()
		{
			return "[" + RealType + "] " + Name;
		}
		public override bool Match(ICSType x)
		{
			return x is SimpleCSType cst && Name.Equals(cst.Name);
		}

		internal void WriteValue(MOutput o, object value)
		{
			o.Write(value.ToString());
		}
	}
	public class EnumCSType : SimpleCSType
	{
		public EnumCSType(string name, Type realType) : base(name, realType)
		{
		}
	}
	public class ClassCSType : ICSType
	{
		public readonly string Name;
		private MList<Member> Members = new();

		public ClassCSType(string name, Type realType) : base(realType)
		{
			Name = name;
		}

		public void Add(string name, ICSType t)
		{
			MS.VerboseLine("InStruct: add member: " + name + " TYPE: " + t.RealType);

			var m = new Member(name, t, Members.Size());
			Members.AddLast(m);
		}

		public IEnumerable<Member> GetMembers()
		{
			return Members.GetEnumerator();
		}

		public override bool Match(ICSType x)
		{
			if (x is ClassCSType c)
			{
				// compare class types. members must match, but their order (index) may be different, e.g.
				// "class P1 {int x; int y;}" matches "class P2 {int y; int x;}"

				if (Members.Size() != c.Members.Size()) return false;
				foreach (var member in Members)
				{
					var otherMember = c.GetByNameOrNull(member.Name);
					if (otherMember == null) return false;
					if (!member.CsType.Match(otherMember.CsType)) return false;
				}
				return true;
			}
			return false;
		}

		public override void WriteTypeName(MOutput o)
		{
			o.Write(Name);
		}

		internal Member GetByIndex(int i)
		{
			return Members.GetAt(i);
		}
		internal Member? GetByNameOrNull(string name)
		{
			foreach(var member in Members)
				if (member.Name.Equals(name)) return member;
			return null;
		}

		internal int NumMembers()
		{
			return Members.Size();
		}
	}
	public class Member
	{
		public readonly string Name;
		public readonly int Index;
		public readonly ICSType CsType;

		public Member(string name, ICSType type, int index)
		{
			Name = name;
			Index = index;
			CsType = type;
		}
	}
	public class GenericCSType : ICSType
	{
		public readonly ICSType [] parameters;
		public readonly string GenericTypeName; // e.g. "list" for "list [int]"

		public GenericCSType(string scriptType, Type realType, ICSType [] pars) : base(realType)
		{
			parameters = pars;
			GenericTypeName = scriptType;
		}
		public bool Match(string n, ICSType [] pars)
		{
			if (!GenericTypeName.Equals(n)) return false;
			if (parameters.Length != pars.Length) return false;
			for(int i=0; i<pars.Length; i++)
			{
				if (!parameters[i].Match(pars[i])) return false;
			}
			return true;
		}
		public override void WriteTypeName(MOutput o)
		{
			o.Write(GenericTypeName);
			o.Write("[ ");
			foreach(var p in parameters)
			{
				p.WriteTypeName(o);
				o.Write(" ");
			}
			o.Write("]");
		}
		public override bool Match(ICSType x)
		{
			return x is GenericCSType gt && Match(gt.GenericTypeName, gt.parameters);
		}
	}
}

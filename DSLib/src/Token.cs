namespace DiscScriptCore
{
	using DiscScript;

	public enum TokenType
	{
		NONE,

		PARENTHESIS,
		SQUARE_BRACKETS,
		CURLY_BRACKETS,
		TEXT,
		INTEGER,
		DECIMAL,
		ASSIGN,
		LIST_ITEM,
		CALLBACK,
		EXPRESSION_BREAK,
		BLOCK,
		REFERENCE
	}

	public class Token
	{
		internal TokenType type;
		internal int numChildren;
		internal string data;
		internal Token? next = null;
		internal Token? child = null;
		internal Token? parent = null;
		public Token(Token? _parent, TokenType _type, string _data)
		{
			data = _data;
			parent = _parent;
			type = _type;
			numChildren = 0;
		}
		public void PrintTree(bool deep)
		{
			PrintTree(this, 0, deep);
			if (!deep) MS.WriteLine("");
		}
		public void PrintTree(Token _node, int depth, bool deep)
		{
			System.Diagnostics.Debug.Assert(_node != null, "<printTree: empty node>");
			Token node = _node;
			for (int i = 0; i < depth; i++) MS.Write("  ");
			MS.Write("[" + node.data + "]");
			if (deep) MS.WriteLine("");
			if (node.child != null && deep) PrintTree(node.child, depth + 1, deep);
			if (node.next != null) PrintTree(node.next, depth, deep);
		}
		
		internal Token Next()
		{
			if (next == null) throw new MException(MError.TOKEN_HANDLER, "unexpected end of line");
			return next;
		}
		internal Token Child()
		{
			if (child == null) throw new MException(MError.TOKEN_HANDLER, "data missing");
			return child;
		}
	}
}
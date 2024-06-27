
using System.Text;

namespace DiscScript
{
	public abstract class MOutput
	{
		protected MOutput()
		{
		}
		public MOutput Indent(int depth)
		{
			while(depth-->0) Write("  ");
			return this;
		}
		public MOutput Write(object? o)
		{
			if (o != null) Write(o.ToString());
			return this;
		}
		public MOutput WriteLine(object? o)
		{
			if (o != null) WriteLine(o.ToString());
			return this;
		}
		public void WriteSafeByteChar(byte b)
		{
			Write(MS.ToSafeChar(b));
		}
		internal void LineBreak()
		{
			Write("\n");
		}

		abstract public MOutput WriteLine(string? s);
		abstract public MOutput Write(string? s);
		abstract public MOutput Write(char v);
		abstract public void Close();
	}
	public class MConsoleOutput : MOutput
	{
		public override void Close()
		{
		}
		public override MOutput Write(string? s)
		{
			Console.Write(s);
			return this;
		}
		public override MOutput Write(char c)
		{
			Console.Write(c);
			return this;
		}
		public override MOutput WriteLine(string? s)
		{
			Console.WriteLine(s);
			return this;
		}

	}
	public class MBufferOutput : MOutput
	{
		public StringBuilder Buffer { private set; get; } = new StringBuilder();
		public override void Close()
		{
		}
		public override MOutput Write(string? s)
		{
			Buffer.Append(s);
			return this;
		}
		public override MOutput Write(char c)
		{
			Buffer.Append(c);
			return this;
		}
		public override MOutput WriteLine(string? s)
		{
			Buffer.AppendLine(s);
			return this;
		}
		public string GetString()
		{
			return Buffer.ToString();
		}
	}
	public class MFileOutput : MOutput
	{
		private readonly StreamWriter writer;

		public MFileOutput(string filePath)
		{
			try
			{
				// UTF-8 text output (MInput read as binary)
				
				Stream file;
				if (File.Exists(filePath)) file = File.Open(filePath, FileMode.Create);
				else file = File.Create(filePath);
				writer = new StreamWriter(file, Encoding.UTF8);
			}
			catch (Exception e)
			{
				MS.ErrorPrinter.WriteLine(e.ToString());
				throw new MException(MError.IO, "can't write file: " + filePath);
			}
		}
		public override void Close()
		{
			writer.Flush();
			writer.Close();
		}

		public bool IsClosed()
		{
			return writer.BaseStream == null;
		}

		public override MFileOutput Write(char x)
		{
			writer.Write(x);
			return this;
		}
		
		public override MFileOutput Write(string? x)
		{
			writer.Write(x);
			return this;
		}
		public override MFileOutput WriteLine(string? x)
		{
			writer.WriteLine(x);
			return this;
		}
	}
}

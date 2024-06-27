using System.Text;

namespace DiscScript
{
	public abstract class MInput
	{
		public MInput()
		{
		}
		public abstract byte ReadByte();
		public abstract bool End();
		public abstract void Close();
	}
	public class MInputArray : MInput
	{
		private int index = 0, size;
		private byte[] bytes;

		public MInputArray(string s)
		{
			bytes = Encoding.UTF8.GetBytes(s);
			size = bytes.Length;
		}
		public MInputArray(byte[] input)
		{
			bytes = input;
			size = bytes.Length;
		}
		override public bool End()
		{
			return index >= size;
		}
		override public void Close()
		{
		}
		public override byte ReadByte()
		{
			// try-catch?

			return bytes[index++];
		}
	}
	public class MSFileInput : MInput
	{
		private readonly BinaryReader reader;

		public MSFileInput(string fileName)
		{
			try
			{
				// read raw binary

				reader = new BinaryReader(new FileStream(fileName, FileMode.Open));
			}
			catch (Exception e)
			{
				MS.ErrorPrinter.WriteLine(e.ToString());
				throw new MException(MError.IO, "can't open file: " + fileName);
			}
		}
		public override void Close()
		{
			reader.Close();
		}
		public override bool End()
		{
			return reader.BaseStream.Position >= reader.BaseStream.Length;
		}
		public override byte ReadByte()
		{
			return reader.ReadByte();
		}
	}
}

// See https://aka.ms/new-console-template for more information
using DiscScript;

Console.WriteLine("Hello!");

// turn off debug prints

MS.Settings(false, false);

const string FILE_NAME = "ds_output_compact.is";

// write and read object data

Person person1 = new ();

Console.WriteLine("Create file stream");
var fout = new MFileOutput(FILE_NAME);

Console.WriteLine("Write object data with structs");
StructSerializer.Write(person1, fout);
			
Console.WriteLine("Deserialize");
var fin = new MSFileInput(FILE_NAME);
var person2 = MDeserializer.ReadStruct<Person>(fin);

// test if the deserialized object equals the original

Console.WriteLine("Match? " + person1.Match(person2));

// try serializing without structs

Console.WriteLine("Write object data without structs");
MapSerializer.Write(person1, new MFileOutput("ds_map.is"));


Console.WriteLine("Print object data with structs\n");
StructSerializer.Write(person1, MS.Printer);

Console.WriteLine("\nPrint object data without structs\n");
MapSerializer.Write(person1, MS.Printer);
# DiscScript for C#

DiscScript (DISC for Data Instruction Stream Code) is a **data serialization scripting language**, to write and read data in human-readable format.

It's like JSON and YAML, but has optional **data typing and structures**, for **extra compact** data files.

The project is still **work-in-progress**, but is already working quite well.

## Example

This is a simple C# class:

```cs
[DSClass] // attribute for the DiscScript serializer
public class Article
{
  public Article(string title, int id)
  {
    Title = title;
    ID = id;
  }
  public string Title;
  public int ID;
}
```

Here's how to serialize it in DiscScript's **struct format**:

```cs
Article a = new Article("Example", 123);
StructSerializer.Write(a, new MFileOutput("struct_test.ds"));
```

The `Write` method serializes the object `a` to a DiscScript file `struct_test.ds` with following content:

```
$struct DiscScript.Article
  string Title
  int32 ID
[DiscScript.Article] root
  - "Example"
  - 123
```

The DiscScript file starts with a data structure (Article) definition that defines member names and data types.
After that, the actual data, i.e. class member values are written. 

Here's how to serialize the class with DiscScript in **map format** with member names and values written:

```cs
Article a = new Article("Example", 123);
MapSerializer.Write(a, new MFileOutput("map_test.ds"));
```

Output file:

```
Title: "Example"
ID: 123
```

Deserializing the object data have the same result regardless of the serializing method:

```
var article1 = MDeserializer.ReadStruct<Person>(new MFileInput("struct_test.ds"));
var article2 = MDeserializer.Read<Person>(new MFileInput("map_test.ds");
```

Struct format results much smaller script files when serializing large data sets as it writes member names only in introductions,
not every time that value is assigned like map format does, as well as JSON and YAML.

### More Complex Example

Here's another example of the same data in map and struct formats.

Class definitions:
```cs
public enum TestEnum
{
  ALPHA = 1,
  BETA = 3
}
[DSClass]
public class Article
{
  public string Title;
  public int ID;
  // ...
}
[DSClass]
public class Person
{
  public string Prop;
  public string Name;
  public double Points;
  public Article? Null = null;
  public TestEnum EnumLetter;
  public Dictionary<int,Article> Articles;
  public int[] IntArray;
  public Article[] ArticleArray;
  public Dictionary<int,string> Dic;
  public List<string> ListSample;
  public string Rank;
}
```

Map format:
```
Name: "Pöllö"
Points: 1
Articles
  -456
    Content: "Article Number 1"
    Count: 1
IntArray
  - 123
  - -456
  - 789
ArticleArray
  - Content: "Article Number 2"
    Count: 2
  - Content: "Article Number 3"
    Count: 3
Dic
  0: "zero"
  1: "one"
  2: "two"
ListSample
  - "A"
  - "B"
  - "C"
Rank: "gold"
```

Struct format:
```
$struct DSClass.Article
  string Content
  int32 Count
$struct DSClass.Person
  string Name
  float64 Points
  map[ int32 DSClass.Article ] Articles
  list[ int32 ] IntArray
  list[ DSClass.Article ] ArticleArray
  map[ int32 string ] Dic
  list[ string ] ListSample
  string Rank
```
```
[DSClass.Person] root
  - "Pöllö"
  - 1
  - {456: ("Article Number 1", 1)}
  - (123, -456, 789)
  - (("Article Number 2", 2), ("Article Number 3", 3))
  - {0: "zero", 1: "one", 2: "two"}
  - ("A", "B", "C")
  - "gold"
```

As you can see, map and struct format organizes data differently. You can write key-value pairs and list item on multiple lines using line **indentations**.
Compact but less readable way is to use inline format (one-liner) for **maps** (set of key-value pairs, "dictionaries") and **lists** by wrapping them with
**{curly brackets}** or **(parenthesis)**, respectively.

## Syntax

DiscScript uses map (dictionary) of key-value pairs to save data. Key can be an integer or string, and value a string, list, or another map.

For **primitive types** (string, numbers, etc.) the key and the value are separated with a semicolon:
```
key : value
```

Value can be text, integer, or decimal number containing basic ASCII letters and numbers (a-z, A-Z, 0-9, _ and . in the middle).
Text with spaces and special characters are between quote marks. For example:
```
bigNumber: 2147483647
decimalNumber: -1.23
simpleText: Hello
normalText: "Hyvää Päivää!"
booleanValue: true
```

A **list** can be written in multi-line or single-line format.

Multi-line, where list items are indented and start with a dash:
```
list_name
  - list_item_1
  - list_item_2
```

Single-line, where list items come after semicolon, in parenthesis, separated by comma:
```
list_name: ( list_item_1, list_item_2 )
```

Also a **map** can be written in multi-line or single-line format.

Multi-line, where where key-value pairs are indented:
```
map_name
  key1 : value1
  key2 : value2
```
Single-line, where where key-value pairs are inside curly brackets, separated by comma:
```
map_name : { key1 : value1, key2 : value2 }
```

### Supported primitive types

DiscScript supports all basic C# data types:

- integers: signed/unsigned byte/short/int/long
- decimal numbers: decimal/float/double
- bool
- string
- char

### Supported collections

- Array
- List<>
- Dictionary<,>

## Try it!

Requirements: Visual Studio (Code) with .NET support.

Open **vs/DiscScript.sln** and run the **Example** project to see how it works. Other projects in the VS solution:
- **DiscScriptLib:** core implementation.
- **DiscScriptTest:** automatic testing.
- **DiscScriptCLI:** work-in-progress command line tool.

## Design

DiscScript project aims to provide some real improvements to other similar scripting languages, namely typing and data structures.
Some design principles are:

- **Safe and robust** by typing and data structures. You'll be sure that serialized data is in correct format.
- Optionally **compact** (although less readable) data format for large data sets by using data structures.
- **Fast** by using optimal, ad-hoc parser. Avoid copying data in the serialization process, for example,
  you could write data straight to network stream, without writing it first to memory or file locally.
- **Standalone**, no external dependencies, only C#'s standard libraries.

Script code is parsed and executed **line-by-line**, not the whole file at once. After every line, the data is in "good shape"
regardless of what the next line does. The user could choose to ignore possibly errors in script, so if there were an error on some
line the rest of the script could still be read.

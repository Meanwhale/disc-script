
namespace DiscScript
{
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

		public Article() { Title = ""; }
		public Article(string content, int count)
		{
			Title = content;
			ID = count;
		}
		
		public static int counter = 1;

		public bool Match(Article x)
		{
			if (!Title.Equals(x.Title)) return false;
			if (ID != x.ID) return false;
			return true;
		}
	}
	[DSClass]
	public class Person
	{
		public string Prop { get; set; } = "property";
		public string Name = "Pöllö";
		public double Points = 1.0;
		public Article? Null = null;
		public TestEnum EnumLetter = TestEnum.BETA;
		public Dictionary<int,Article> Articles = new Dictionary<int, Article>();
		public int[] IntArray;
		public Article[] ArticleArray;
		public Dictionary<int,string> Dic = new Dictionary<int, string>();
		public List<string> ListSample = new List<string>();
		public string Rank = "gold";

		public Person()
		{
			Articles[-456] = new Article("Article Number " + Article.counter++, Article.counter);
			IntArray = new int [] { 123, -456, 789 };
			ArticleArray = new Article [] { new Article("Article Number " + Article.counter++, Article.counter), new Article("Article Number " + Article.counter++, Article.counter) };
			Dic[0] = "zero";
			Dic[1] = "one";
			Dic[2] = "two";
			ListSample.Add("A");
			ListSample.Add("B");
			ListSample.Add("C");
		}
		public bool Match(Person x)
		{
			if (!Prop.Equals(x.Prop)) return false;
			if (!Name.Equals(x.Name)) return false;
			if (!Rank.Equals(x.Rank)) return false;
			if (Points != x.Points) return false;
			if (Null != x.Null) return false;
			if (EnumLetter != x.EnumLetter) return false;

			if (Articles.Count != x.Articles.Count) return false;
			foreach(var akv in Articles)
			{
				if (!akv.Value.Match(x.Articles[akv.Key])) return false;
			}

			if (!IntArray.SequenceEqual(x.IntArray)) return false;
			
			if (ArticleArray.Length != x.ArticleArray.Length) return false;
			for (int i=0; i<ArticleArray.Length; i++)
			{
				if (!ArticleArray[i].Match(x.ArticleArray[i])) return false;
			}
			
			if (Dic.Count != x.Dic.Count) return false;
			foreach(var akv in Dic)
			{
				if (!akv.Value.Equals(x.Dic[akv.Key])) return false;
			}

			if (!ListSample.SequenceEqual(x.ListSample)) return false;

			return true;
		}
	}
}

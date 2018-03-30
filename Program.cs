using CsvHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ComicGenerator
{
    public class CrimeRecord
    {
        public string ID { get; set; }
        public string Case_Number { get; set; }
        public DateTime OccFrom { get; set; }
        public DateTime ReportedOn { get; set; }
        public string Description { get; set; }
        public string KeyWord { get; set; }
        public string Addr { get; set; }
        public string LunarCycle { get; set; }
        public string LunarPhase { get; set; }
    }

    internal class Program
    {
        private static List<ComicGen.Message> messages = new List<ComicGen.Message>();
        private static List<Dictionary<string, string[]>> scenes = new List<Dictionary<string, string[]>>();
        private static string dataFilePath = "";
        private static string dialogFilePath = "";

        public static Random rnd = new Random();

        private static void print(object o)
        {
            Console.WriteLine(o);
        }

        private static void Main(string[] a_args)
        {
            if (a_args.Length < 2)
            {
                print("usage: ComicGenerator.exe <datafile.csv path> <dialog.json path>");
                return;
            }
            //get data

            dataFilePath = a_args[0];
            dialogFilePath = a_args[1];

            scenes = JsonConvert.DeserializeObject<List<Dictionary<string, string[]>>>(File.ReadAllText(dialogFilePath));

            var currentDate = DateTime.Now;

            messages = new List<ComicGen.Message>();
            GenerateComic(currentDate.AddDays(-1));
        }

        public static void GenerateComic(DateTime currentDate)
        {
            var csv = new CsvReader(new StreamReader(dataFilePath));

            var config = new CsvHelper.Configuration.Configuration();
            config.AutoMap<CrimeRecord>();

            var records = csv.GetRecords<CrimeRecord>().Where(r => r.OccFrom.Month == currentDate.Month && r.OccFrom.Day == currentDate.Day).ToArray();

            var allowedKeywords = File.ReadAllText("comic/keywords.txt"); //must be present

            var keywords = records.Select(s => s.KeyWord).Where(s => allowedKeywords.Contains(s)).Distinct().ToArray();

            var selectedKeyword = keywords[rnd.Next(0, keywords.Count() - 1)];

            var splitChars = new[] { ';', '/' };

            var tokens = selectedKeyword.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
            selectedKeyword = tokens[0];

            print($"Theme:{selectedKeyword}");

            var crimes = records.Where(r => r.KeyWord.Contains(selectedKeyword)).ToArray();

            print($"related crimes:{crimes.Length}");
            var crime = crimes[rnd.Next(0, crimes.Length - 1)];

            foreach (var kv in scenes)
            {
                AddScene(kv, crime);
            }

            ComicGen gen = new ComicGen();
            if (!Directory.Exists("result"))
                Directory.CreateDirectory("result");
            WriteComic(gen.Generate("", messages), $"result/comic_{currentDate.Ticks}.png");
        }

        private static void AddScene(Dictionary<string, string[]> kv, CrimeRecord record)
        {
            string title = "";
            foreach (var key in kv.Keys)
            {
                if (key.Equals("title", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (kv.ContainsKey("title")) title = kv["title"][rnd.Next(kv["title"].Length)];
                }
                else if (key.ToLower().Trim().StartsWith("actor"))
                {
                    string quote = kv[key][rnd.Next(kv[key].Length)];

                    var variables = ExtractFromString(quote, "{", "}");
                    foreach (var v in variables)
                    {
                        try
                        {
                            quote = quote.Replace("{" + v + "}", "" + GetPropValue(record, v));
                        }
                        catch (Exception ex)
                        { Console.WriteLine(ex); }
                    }

                    Add(key, quote);
                }
            }
        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        public static void AddMessage(dynamic record, string actorId, string text)
        {
        }

        public static void Add(string id, string text)
        {
            messages.Add(new ComicGen.Message(id, text));
        }

        public static void WriteComic(MemoryStream ms, string filename)
        {
            try
            {
                var fs = File.Open(filename, FileMode.Create);
                ms.CopyTo(fs);
                ms.Close();
                fs.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine("ERR: Failed to write comic to disk.");
                Console.WriteLine(e);
            }

            print(filename);
        }

        private static List<string> ExtractFromString(string source, string start, string end)
        {
            var results = new List<string>();

            string pattern = string.Format(
                "{0}({1}){2}",
                Regex.Escape(start),
                ".+?",
                 Regex.Escape(end));

            foreach (Match m in Regex.Matches(source, pattern))
            {
                results.Add(m.Groups[1].Value);
            }

            return results;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace DicParser
{
    class Program
    {
        private static List<string> DailyWords = new List<string>();
        private static List<string> ShortWords = new List<string>();
        private static ShadowTrie ShortWordsTrie = new ShadowTrie();

        //private const string Path = "d:\\OneDrive\\Dev\\DicParser\\DicParser\\";
        private const string DataPath = @"Data\";
        private const string OutputPath = @"Output\";
        private static bool WriteCombined = false;

#if true
        private const int MinimumWords = 40;
        private static string MainDictionaryCombinedFilename = "combined_ru.txt";
        private const string ShortWordsBinaryName = "three.bytes";
        private const string ShortWordsTrieBinaryName = "three_trie.bytes";
        private const string DailyWordsBinaryName = "daily.bytes";
#else
        private const int MinimumWords = 20;
        private static string[] _mainDictionaryFilenames = { "english_n.txt", "nounlist.txt" };
        private static string MainDictionaryCombinedFilename = "combined_en.txt";
        private const string ShortWordsBinaryName = "three_en.bytes";
        private const string ShortWordsTrieBinaryName = "three_trie_en.bytes";
        private const string DailyWordsBinaryName = "daily_en.bytes";
#endif

        private static void FillDictionary()
        {
#if true
            var csv = ReadCsvDictionary();
            var txt = ReadDictionary();

            var dictionary = new List<string>();

            Console.WriteLine();
            Console.WriteLine("Loaded {0} words in csv", csv.Count);
            Console.WriteLine("Loaded {0} words in txt", txt.Count);
            Console.WriteLine();

            dictionary.AddRange(csv);
            dictionary.AddRange(txt);

            Console.WriteLine();
            Console.WriteLine("Dictionary has {0} words now", dictionary.Count);
            Console.WriteLine();

            Console.WriteLine("Cleaning...");
            var minLength = 10;

            var notLongEnougth = new List<string>();
            foreach (var w in dictionary)
            {
                if (w.Length > 3 && w.Length <= minLength)
                {
                    notLongEnougth.Add(w);
                }
            }

            Console.WriteLine("Found {0} words that not long enough", notLongEnougth.Count);

            dictionary = dictionary.Except(notLongEnougth).ToList();

            Console.WriteLine();
            Console.WriteLine("Dictionary has {0} words now", dictionary.Count);
            Console.WriteLine();


            Console.WriteLine("Replacing Ё to Е");
            for (var i = 0; i != dictionary.Count; ++i)
            {
                dictionary[i] = dictionary[i].ToLower();
                dictionary[i] = dictionary[i].Replace('ё', 'е');
            }

            Console.WriteLine("Removing banned words...");
            Console.WriteLine("Auto remove...");

            var bannedPart = new[]
            {
                "хуй", "пизд", "суки", "-"
            };

            var bannedEnd = new[]
            {
                "сть", "ние", "вка", "ная" , "тво",
            };

            var bannedWords1 = from word in dictionary from part in bannedPart where word.Contains(part) select word;
            var bannedWords2 = from word in dictionary from part in bannedEnd where ((word.Length > 5) && (word.Substring(word.Length - 3) == part)) select word;

            dictionary = dictionary.Except(bannedWords1).ToList();
            dictionary = dictionary.Except(bannedWords2).ToList();

            Console.WriteLine();
            Console.WriteLine("Dictionary has {0} words now", dictionary.Count);
            Console.WriteLine();


            Console.WriteLine("Manual remove...");
            var banned = new[]
            {
                "промстройбанк", "проститутка", "сперматозоид", "рав", "нет",
                "мыш", "дез", "зет", "валерьяша", "уда", "хуй", "жидовка", "бэр",
                "баш", "вуз", "дер", "дэз", "жэк", "зав", "зум", "изм", "кед", "кус",
                "лет", "нэп", "пим", "рус", "сум", "тун", "фэн", "чмо", "шоп", "лит",
                "обо", "очи", "тар", "означающее", "гит", "выя", "катедер-социализм", "икт", 
                "социал-демократия", "дуплекс-автотипия", "генерал-инспектор", "социал-демократ"
            };

            dictionary = dictionary.Except(banned).ToList();

            Console.WriteLine();
            Console.WriteLine("Dictionary has {0} words now", dictionary.Count);
            Console.WriteLine();

            Console.WriteLine("Manual Add...");

            Console.WriteLine("Finding duplicates...");
            var dups = dictionary.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).AsParallel().ToList();
            Console.WriteLine("Found {0} duplicates", dups.Count);
            foreach (var word in dups)
            {
                Console.WriteLine(word);
            }

            Console.WriteLine("Removing duplicates...");
            //dictionary = dictionary.Except(dups).ToList();
            dictionary = dictionary.Distinct().ToList();

            Console.WriteLine("Sorting...");
            dictionary.Sort();
            Console.WriteLine();
       
            Console.WriteLine("Loaded {0} words in total", dictionary.Count);
            Console.WriteLine();

            WriteDictionary(MainDictionaryCombinedFilename, dictionary.ToArray());
#else
            var dictionary= ReadCombinedDictionary(_mainDictionaryFilenames);
            Console.WriteLine("Loaded {0} words in total", dictionary.Count);

            Console.WriteLine("Finding duplicates...");
            var dups = dictionary.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).AsParallel().ToList();
            Console.WriteLine("Found {0} duplicates", dups.Count);
            Console.WriteLine("Removing duplicates...");
            dictionary = dictionary.Distinct().ToList();
#endif
            Console.WriteLine("Sorting...");
            dictionary.Sort();

            Console.WriteLine("{0} words left", dictionary.Count);

            if (WriteCombined)
            {
                WriteDictionary(MainDictionaryCombinedFilename, dictionary.ToArray());
            }

            var shortW = new List<string>();

            foreach (var word in dictionary)
            {
                if (word.Length == 3)
                {
                    shortW.Add(word);
                    ShortWordsTrie.Insert(word);
                }
            }
            shortW.Sort();

            Console.WriteLine();
            Console.WriteLine("Found {0} SHORT words in total", shortW.Count);
            Console.WriteLine();

            ShortWords = shortW;

            foreach (var word in dictionary)
            {
                var length = word.Length;
                if ((length >= 5) && (length <= 17))
                {
                    var shortWordsForWord = GetShortWordsForWord(ShortWords, word);
                    if (shortWordsForWord.Count <= 99)
                    {
                        if (shortWordsForWord.Count >= MinimumWords) DailyWords.Add(word);
                    }
                }
            }

            DailyWords.Sort();

            Console.WriteLine();
            Console.WriteLine("Found {0} DAILY words in total", DailyWords.Count);
            Console.WriteLine();
        }

        static T ReadDictionary<T>(string fileName)
        {
            var stream = new FileStream(fileName, FileMode.Open);
            var binary = new BinaryFormatter();
            var result = (T)binary.Deserialize(stream);
            stream.Close();

            return result;
        }

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Console.OutputEncoding = Encoding.UTF8;
            var dictionary = new ShadowTrie();

            FillDictionary();
            WriteBinaryDictionary();

            var seed = DateTime.Now.Millisecond;
            var three = ReadDictionary<ShadowTrie>(ShortWordsTrieBinaryName);
            var daily = ReadDictionary<string[]>(DailyWordsBinaryName);

            var random = new Random(seed++);

            while (true)
            {
                var word = daily[random.Next() % daily.Length];
                var letters = GetUniqLetters(word);
                Console.Write("Random word -- {0} [ ", word);
                foreach (var l in letters)
                {
                    Console.Write("{0}, ", l);
                }
                Console.WriteLine("]");

                three.GetWordsWithChars(letters.ToList());
                Console.ReadLine();
            }
        }

        private static char[] GetUniqLetters(string word)
        {
            var result = new List<char>();
            for (var i = 0; i != word.Length; ++i)
            {
                var c = word[i];
                if (!result.Contains(c)) result.Add(c);
            }

            var resultArray = result.ToArray();
            Array.Sort(resultArray);
            return resultArray;
        }

        private static void WriteDictionary(string name, string[] dictionary)
        {
            Console.WriteLine("Saving dictionary...");

            var fullFileName = Path.Combine(DataPath, name);
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fullFileName);
            var sWriter = new StreamWriter(path);
            foreach (var word in dictionary)
            {
                sWriter.WriteLine(word);
            }
            sWriter.Close();

            Console.WriteLine("Done written {0} with {1} words", name, dictionary.Length); 
            Console.WriteLine();
        }

        private static void WriteBinaryDictionary()
        {
            Console.WriteLine("Saving dictionaries...");

            var path = Path.Combine(DataPath, ShortWordsTrieBinaryName);
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            var stream = new FileStream(path, FileMode.Create);
            var binary = new BinaryFormatter();
            binary.Serialize(stream, ShortWordsTrie);
            stream.Close();

            path = Path.Combine(DataPath, ShortWordsBinaryName);
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            stream = new FileStream(path, FileMode.Create);
            binary = new BinaryFormatter();
            binary.Serialize(stream, ShortWords.ToArray());
            Console.WriteLine("{0} SHORT WORDS WRITTEN", ShortWords.Count);

            path = Path.Combine(DataPath, DailyWordsBinaryName);
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            stream = new FileStream(path, FileMode.Create);
            binary = new BinaryFormatter();
            binary.Serialize(stream, DailyWords.ToArray());

            Console.WriteLine("{0} DAILY WORDS WRITTEN", DailyWords.Count);

            stream.Close();
            
            var sWriter = new StreamWriter("three_en.txt");
            foreach (var word in ShortWords)
            {
                sWriter.WriteLine(word);
            }
            sWriter.Close();

            Console.WriteLine("Done");
            Console.WriteLine();
        }

        private static bool WordContains(string word, char[] chars)
        {
            var wordChars = GetLetters(word);
            return chars.Except(wordChars).ToArray().Length == 0;
        }

        /*
        private static void FindMostUniqLetters(List<string> shortWords)
        {
            var maxCount = 0;
            var word = "";
            for (var i = 0; i != LongWords.Count; ++i)
            {
                var lettersCount = GetLetters(LongWords[i]).Length;
                if (lettersCount > maxCount)
                {
                    maxCount = lettersCount;
                    word = LongWords[i];
                }
            }

            Console.WriteLine("Max uniq letters {0} in {1}", maxCount, word);
            GetShortWordsForWord(shortWords, word);
        }
        */

        private static List<string> GetShortWordsForWord(List<string> shortWords, string word)
        {
            var result = new List<string>();
            var wordChars = GetLetters(word);

            for (var i = 0; i != shortWords.Count; ++i)
            {
                var shortWord = shortWords[i];
                var shortWordChars = GetLetters(shortWord);
                var count = 0;
                for (var o = 0; o != shortWordChars.Length; ++o)
                {
                    for (var p = 0; p != wordChars.Length; ++p)
                    {
                        if (shortWordChars[o] == wordChars[p]) count++;
                        if (count == shortWordChars.Length) break;
                    }
                }
                if (count == shortWordChars.Length) result.Add(shortWord);
            }
            return result;
        }

        private static char[] GetLetters(string word)
        {
            var result = new List<char>();

            for (var i = 0; i != word.Length; ++i)
            {
                var character = word[i];
                if (character == '-') continue;
                else if (character == ' ') continue;
                if (result.Any(c => c == character)) continue;
                result.Add(character);
            }
            return result.ToArray();
        }

        private static List<string> ReadCsvDictionary()
        {
            string line;
            var words = new List<string>();

            var path = Path.Combine(DataPath, "freqrnc2011.csv");
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            var file = new StreamReader(path);
            
            while ((line = file.ReadLine()) != null)
            {
                var columns = line.Split('\t');
                if (columns.Length != 6) continue;

                var word = columns[0];
                word = word.ToLower();
                var wordType = columns[1];

                if (wordType == "s") words.Add(word);
            }
            file.Close();

            Console.WriteLine("Found {0} words in freqrnc2011.csv", words.Count);

            var lastCount = words.Count;

            path = Path.Combine(DataPath, "word_fill.csv");
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            file = new StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                var columns = line.Split('\t');
                if (columns.Length != 6) continue;

                var word = columns[1];
                word = word.ToLower();

                if (word != "")
                {
                    if (!words.Contains(word)) words.Add(word);
                }
            }
            file.Close();

            Console.WriteLine("Found {0} words in word_fill.csv", words.Count - lastCount);
            Console.WriteLine("Found {0} words in CSV total", words.Count);

            return words;
        }

        private static List<string> ReadCombinedDictionary(string[] names)
        {
            string line;
            var words = new List<string>();

            foreach (var name in names)
            {
                var path = Path.Combine(DataPath, name);
                path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);

                var file = new StreamReader(path);
                var count = 0;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.ToLower();
                    words.Add(line);
                    count++;
                }
                file.Close();
                Console.WriteLine("Found {0} words in {1}", count, name);
            }
            
            Console.WriteLine("Loaded {0} words", words.Count);

            return words;
        }

        private static List<string> ReadDictionary(int minLength = 4)
        {
            string line;

            var path = Path.Combine(DataPath, "word_rus_utf.txt");
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            var file = new StreamReader(path);
            var words = new List<string>();
            while ((line = file.ReadLine()) != null)
            {
                if (line.Length < minLength) continue;
                line = line.ToLower();
                words.Add(line);
            }
            file.Close();

            Console.WriteLine("Found {0} words in word_rus_utf.txt", words.Count);

            var lastCount = words.Count;

            path = Path.Combine(DataPath, "word_rus2.txt");
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            file = new StreamReader(path);
            words = new List<string>();
            while ((line = file.ReadLine()) != null)
            {
                if (line.Length < minLength) continue;
                line = line.ToLower();
                words.Add(line);
            }
            file.Close();

            Console.WriteLine("Found {0} words in word_rus2.txt", words.Count - lastCount);

            lastCount = words.Count;

            path = Path.Combine(DataPath, "balda_dic.txt");
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            file = new StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                line = line.ToLower();
                var readedWords = line.Split(' ').ToList();
                readedWords = readedWords.Except(words).ToList();
                words.AddRange(readedWords);
            }
            file.Close();
            Console.WriteLine("Found {0} words in balda_dic.txt", words.Count - lastCount);

            lastCount = words.Count;

            path = Path.Combine(DataPath, "3letters.txt");
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            file = new StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                line = line.ToLower();
                var readedWords = line.Split(' ').ToList();
                readedWords = readedWords.Except(words).ToList();
                words.AddRange(readedWords);
            }
            file.Close();
            Console.WriteLine("Found {0} words in 3letters.txt", words.Count - lastCount);

            Console.WriteLine("Found {0} words in TXT total", words.Count);
            Console.WriteLine();

            return words;
        }
    }
}

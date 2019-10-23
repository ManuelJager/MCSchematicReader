using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace SchemReader
{
    class Program
    {
        static string jsonString = "";
        static List<string> files = new List<string>();

        static void Main(string[] args)
        {
            try
            {
                File.ReadAllLines(@".\idMap.json").ToList().ForEach(a => jsonString += a.TrimStart());
            }
            catch
            {
                Console.WriteLine("Please provide an idMap.json next to the exe");
                Console.WriteLine("Json can be found at https://minecraft-ids.grahamedgecombe.com/items.json");
                System.Threading.Thread.Sleep(15000);
                Environment.Exit(0);
            }
            while(true)
            {
                Console.Write("Please input a schematics folder > ");
                var input = Console.ReadLine();
                if (input == "quit")
                    Environment.Exit(0);

                if (!Directory.Exists(input))
                {
                    Console.Clear();
                    Console.WriteLine("Folder was wrongly formatted");
                    continue;
                }

                files = Directory.GetFiles(input).Where(
                    (file) =>
                    Path.GetExtension(file) == ".schematic")
                    .ToList();

                if (files.Count == 0)
                {
                    Console.Clear();
                    Console.WriteLine("No files found");
                    continue;
                }

                while (true)
                {
                    Console.Write("\n");
                    foreach (var item in files.Select((value, i) => (value, i)))
                        Console.WriteLine($"{item.i + 1} {Path.GetFileNameWithoutExtension(item.value)}");
                    Console.Write("\nSelect a schematic > ");
                    input = Console.ReadLine();
                    if (input == "quit")
                        Environment.Exit(0);
                    int selector = 0;
                    if (!int.TryParse(input, out selector))
                    {
                        Console.Clear();
                        Console.WriteLine("Wrong input");
                        continue;
                    }
                    if (selector < 1 || selector > files.Count)
                    {
                        Console.Clear();
                        Console.WriteLine("Wrong input");
                        continue;
                    }
                    PrintSchematic(files[selector - 1]);
                }
            }
        }

        static void PrintSchematic(string path)
        {
            var schem = SchematicReader.LoadSchematic(path);
            var items = JsonConvert.DeserializeObject<List<Item>>(jsonString);
            var itemDic = new Dictionary<int, string>();
            foreach (var item in items)
                if (!itemDic.ContainsKey(item.type))
                    itemDic.Add(item.type, item.name);
            var blocks = schem.Blocks;
            Dictionary<int, int> dic = new Dictionary<int, int>();
            foreach (var block in blocks)
                addToList(ref dic, block);
            var ordered = dic.ToList().OrderByDescending(x => x.Value);
            var table = new List<Tuple<string, int, int>>();
            foreach (var pair in ordered)
            {
                var stacks = Floor(pair.Value, 64);
                var count = pair.Value % 64;
                table.Add(new Tuple<string, int, int>(itemDic[pair.Key], stacks, count));
            }
            Console.Clear();
            Console.WriteLine(table.ToStringTable(new[] { Path.GetFileNameWithoutExtension(path), "stacks", "count" }, a => a.Item1, a => a.Item2, a => a.Item3));

        }
        static void addToList(ref Dictionary<int, int> dic, Block block)
        {
            var id = block.BlockID;
            if (!dic.Keys.Contains(id))
                dic.Add(id, 0);
            dic[id]++;
        }


        /// <summary>
        /// Python style floor division
        /// </summary>
        /// <param name="a">left</param>
        /// <param name="b">right</param>
        static int Floor(int a, int b) => (a / b - Convert.ToInt32(((a < 0) ^ (b < 0)) && (a % b != 0)));
    }

    public static class TableParser
    {
        public static string ToStringTable<T>(
          this IEnumerable<T> values,
          string[] columnHeaders,
          params Func<T, object>[] valueSelectors)
        {
            return ToStringTable(values.ToArray(), columnHeaders, valueSelectors);
        }

        public static string ToStringTable<T>(
          this T[] values,
          string[] columnHeaders,
          params Func<T, object>[] valueSelectors)
        {
            var arrValues = new string[values.Length + 1, valueSelectors.Length];

            // Fill headers
            for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                arrValues[0, colIndex] = columnHeaders[colIndex];
            }

            // Fill table rows
            for (int rowIndex = 1; rowIndex < arrValues.GetLength(0); rowIndex++)
            {
                for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
                {
                    arrValues[rowIndex, colIndex] = valueSelectors[colIndex]
                      .Invoke(values[rowIndex - 1]).ToString();
                }
            }

            return ToStringTable(arrValues);
        }

        public static string ToStringTable(this string[,] arrValues)
        {
            int[] maxColumnsWidth = GetMaxColumnsWidth(arrValues);
            var headerSpliter = new string('-', maxColumnsWidth.Sum(i => i + 3) - 1);

            var sb = new StringBuilder();
            for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
            {
                for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
                {
                    // Print cell
                    string cell = arrValues[rowIndex, colIndex];
                    cell = cell.PadRight(maxColumnsWidth[colIndex]);
                    sb.Append(" | ");
                    sb.Append(cell);
                }

                // Print end of line
                sb.Append(" | ");
                sb.AppendLine();

                // Print splitter
                if (rowIndex == 0)
                {
                    sb.AppendFormat($" |{headerSpliter}| ");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static int[] GetMaxColumnsWidth(string[,] arrValues)
        {
            var maxColumnsWidth = new int[arrValues.GetLength(1)];
            for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
                {
                    int newLength = arrValues[rowIndex, colIndex].Length;
                    int oldLength = maxColumnsWidth[colIndex];

                    if (newLength > oldLength)
                    {
                        maxColumnsWidth[colIndex] = newLength;
                    }
                }
            }

            return maxColumnsWidth;
        }
    }
}

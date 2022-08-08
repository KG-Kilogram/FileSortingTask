using System.Diagnostics;

namespace FileSortingTask
{
    internal partial class Program
    {
        public static int MaxLineNumber = 10000;

        // Не менее 4
        public static int MinLineLength = 20;
        public static int MaxLineLength = 100;

        private static readonly string SourceFileName = "source.dat";
        private static readonly string InternalSortFileName = "internal.dat";
        private static readonly string TargetFileName = "target_icomparable.dat";
        private static readonly string TargetFileNameTree = "target_ktree.dat";

        // Размер буфера сортировок
        private static readonly int PageSize = 1024 * 1024;

        private static readonly bool UseInternalSortFile = true;
        private static readonly bool UseDefaultSort = true;
        private static readonly bool UseKTreeSort = true;

        static void Main()
        {
            /*
                 
            */

            Stopwatch stopwatch = Stopwatch.StartNew();
            new FileGenerator().Generate(SourceFileName, (long)1024 * 1024 * 100, 1);
            stopwatch.Stop();
            Console.WriteLine("Время генерации файла: {0:0.00} сек.", 0.001 * stopwatch.ElapsedMilliseconds);




            /*
                Получение / сортировка страниц 
            */

            Stopwatch defaultStopwatch = Stopwatch.StartNew();
            Stopwatch ktreeStopwatch = Stopwatch.StartNew();

            FileInfo fileinfo = new(SourceFileName);
            List<Page> pages = new((int)(fileinfo.Length / PageSize));

            if (UseInternalSortFile)
            {
                using FileStream internalSortFileStream = new(InternalSortFileName, FileMode.Create);
                PageSorter.Perform(SourceFileName, PageSize, internalSortFileStream, page => pages.Add(page));
            }
            else
                PageSorter.Perform(SourceFileName, PageSize, null, page => pages.Add(page));

            defaultStopwatch.Stop();
            ktreeStopwatch.Stop();

            Console.WriteLine("Постраничная сортировка заняла: {0:0.00} сек.\r\n", 0.001 * defaultStopwatch.ElapsedMilliseconds);




            using FileStream fs = new(UseInternalSortFile ? InternalSortFileName : SourceFileName, FileMode.Open);

            /*
                KTree sort
            */

            if (UseKTreeSort)
            {
                ktreeStopwatch.Start();

                KTree ktree = new();

                foreach (var page in pages)
                    ktree.Add(page.Clone());

                KTreeSort(ktree!, fs);

                ktreeStopwatch.Stop();
                Console.WriteLine("Сортировка через KTree заняла (включая постраничную): {0:0.00} сек.", 
                    0.001 * ktreeStopwatch.ElapsedMilliseconds);

                if (CheckFileSort(TargetFileNameTree))
                    Console.WriteLine("[+] Правильность сортировки файла \"{0}\" проверена.\r\n", TargetFileNameTree);
                else
                    Console.WriteLine("[-] Файл \"{0}\" отсортирован неправильно!\r\n", TargetFileNameTree);
            }
            else
                Console.WriteLine("Сортировка через KTree отключена\r\n");





            /*
                Default sort
            */

            if (UseDefaultSort)
            {
                defaultStopwatch.Start();
                DefaultSort(pages, fs);
                defaultStopwatch.Stop();

                Console.WriteLine("Сортировка через Array + IComparable заняла (включая постраничную): {0:0.00} сек.", 
                    0.001 * defaultStopwatch.ElapsedMilliseconds);

                if (CheckFileSort(TargetFileName))
                    Console.WriteLine("[+] Правильность сортировки файла \"{0}\" проверена.\r\n", TargetFileName);
                else
                    Console.WriteLine("[-] Файл \"{0}\" отсортирован неправильно!\r\n", TargetFileName);
            }
            else
                Console.WriteLine("Сортировка через Array + IComparable отключена\r\n");

            Console.ReadLine();
        }

        private static void KTreeSort(KTree ktree, FileStream sourceStream)
        {
            Writer writer = new();
            Writer.Run(writer, TargetFileNameTree);

            while (!ktree.Empty)
            {
                lock (writer.Locker)
                {
                    for (int i = 0; i < ktree.MinHashDataList!.Count; i++)
                    {
                        var subpage = (Page)ktree.MinHashDataList[i];
                        subpage.ExtractLinesTo(sourceStream, writer.Lines);
                    }
                }
#if DEBUG
                // string ktreeDebugString = ktree.GetDebugTreeString();
#endif                    
                ktree.RefreshTree();
            }

            Interlocked.Increment(ref writer.EndFlag);

            while (Interlocked.Read(ref writer.Compilite) == 0)
                ;
        }

        private static void DefaultSort(List<Page> pages, FileStream sourceStream)
        {
            using FileStream outfs = new(TargetFileName, FileMode.Create);

            List<Line> lines = new(100);

            while (pages.Count > 0)
            {
                pages.Sort();

                var hash = pages[0].Hash;

                int i = 0;

                while (pages[i].Hash == hash)
                {
                    pages[i++].ExtractLinesTo(sourceStream, lines);

                    if (i == pages.Count)
                        break;
                }

                if (lines.Count > 1)
                    lines.Sort();

                foreach (var line in lines)
                    outfs.Write(line.Buff, 0, line.Length);

                for (int j = i - 1; j >= 0; j--)
                {
                    if (pages[j].Empty)
                        pages.RemoveAt(j);
                }

                lines.Clear();
            }

            outfs.Close();
        }

        private static bool CheckFileSort(string filename)
        {
            string? str;
            string prevLine = new('a', MaxLineLength);

            using StreamReader sr = new(filename);
            while (!sr.EndOfStream)
            {
                str = sr.ReadLine();

                int dpos = str!.IndexOf('.');
                string s = str[(dpos + 1)..].TrimStart();

                if (prevLine.CompareTo(s) == 1)
                    return false;

                prevLine = s;
            }

            return true;
        }
    }
}
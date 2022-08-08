namespace FileSortingTask
{
    public class Writer
    {
        private readonly List<Line> LinesA = new();
        private readonly List<Line> LinesB = new();

        public List<Line> Lines => UseAToAdd ? LinesA : LinesB;

        private bool UseAToAdd = true;

        public object Locker = new();
        public long EndFlag = 0;
        public long Compilite = 0;

        private FileStream? Stream;

        public static void Run(Writer writer, string filename)
        {
            writer.Stream = new(filename, FileMode.Create);

            new Thread(() => {

                try
                {
                    while (Interlocked.Read(ref writer.EndFlag) == 0)
                    {
                        if (writer.UseAToAdd)
                            writer.SortWriteClear(writer.LinesB);
                        else
                            writer.SortWriteClear(writer.LinesA);

                        lock (writer.Locker)
                        {
                            writer.UseAToAdd = !writer.UseAToAdd;
                        }
                    }
                }
                finally
                {
                    writer.Stream.Close();
                    Interlocked.Increment(ref writer.Compilite);
                }

            }).Start();
        }

        private void SortWriteClear(List<Line> lines)
        {
            if (lines.Count > 0)
                lines.Sort();

            foreach (var line in lines)
                Stream!.Write(line.Buff, line.BuffPosition, line.Length);

            lines.Clear();
        }
    }
}
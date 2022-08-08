using System.Diagnostics;
using System.Text;

namespace FileSortingTask
{
    internal class FileGenerator
    {
        private readonly List<byte[]> Duplicates = new(100);

        private readonly byte[] _buff = new byte[Program.MaxLineLength + 1];
        private int _duplicatesCount = 0;
        private int _currentIndex = 0;

        private readonly Random Random = new();

        /// <summary>
        ///             
        /// </summary>
        /// <param name="approxSize">Приблизительный размер файла в байтах</param>
        /// <param name="approxDuplicatesPersent">Приблизительное количество дубликатов 
        /// строк в процентах от 0.0 до 100.0</param>
        public void Generate(string filename, long approxSize, double approxDuplicatesPersent)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            _currentIndex = 0;

            using FileStream fs = new(filename, FileMode.Create);
            try
            {
                int strLength = 0;

                while (fs.Position < approxSize)
                {
                    bool duplicate = _currentIndex > 0 && (100 * Random.NextDouble()) < approxDuplicatesPersent;

                    if (!duplicate)
                    {
                        strLength = Random.Next(Program.MinLineLength, Program.MaxLineLength);

                        for (int i = 0; i < strLength; i++)
                            _buff[i] = (byte)Random.Next('a', 1 + 'z');

                        _buff[strLength] = 0x0A;

                        WriteLine(fs, _buff, strLength + 1);
                    }
                    else
                        PushDuplicate(_buff, strLength);

                    if (Duplicates.Count == 100)
                        WriteDuplicates(fs);

                    if (stopwatch.ElapsedMilliseconds > 1000)
                    {
                        Console.WriteLine("Идет генерация! Всего строк: {0} ({1:0.00}%)",
                            _currentIndex, (100.0 * fs.Length / approxSize));

                        stopwatch.Restart();
                    }
                }

                WriteDuplicates(fs);

                Console.WriteLine("-------------------------------------------------------------------");
                Console.WriteLine("Размер сгенерированного файла: {0:0.00}Mb", (float)fs.Length / (1024 * 1024));
            }
            finally
            {
                fs.Close();
            }

            Console.WriteLine("Сгенерировано строк: {0}, в том числе дубликатов: {1} ({2:0.00}%)",
                _currentIndex, _duplicatesCount, 100.0 * _duplicatesCount / _currentIndex);
        }

        private void PushDuplicate(byte[] buff, int length)
        {
            byte[] duplicateBuff = new byte[length + 1];
            Array.Copy(buff, duplicateBuff, length + 1);
            Duplicates.Add(duplicateBuff);
        }

        private void WriteDuplicates(FileStream fs)
        {
            for (int i = Duplicates.Count - 1; i >= 0; i--)
                WriteLine(fs, Duplicates[i], Duplicates[i].Length);

            _duplicatesCount += Duplicates.Count;

            Duplicates.Clear();
        }

        private void WriteLine(FileStream fs, byte[] buff, int length)
        {
            fs.Write(Encoding.ASCII.GetBytes(Random.Next(Program.MaxLineNumber) + ". "));
            fs.Write(buff, 0, length);
            _currentIndex++;
        }
    }
}

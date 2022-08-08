namespace FileSortingTask
{
    public delegate void OnNewPageDelegate(Page page);

    public static class PageSorter
    {
        /// <summary>
        ///     Постраничная сортировка. Объем страницы приблизительно равен объему буфера
        /// </summary>
        /// <param name="filename">Сортируемый файл</param>
        /// <param name="bufferSize">Размер буфера</param>
        /// <param name="outFileStream">Отсортированные данные будут записаны в указанный поток. Если 
        /// данный параметр равен null, то отсортированные данные будут записаны в файл-источник</param>
        /// <param name="onNewPage">Callback, вызываемый при формировании новой страницы</param>
        public static void Perform(string filename, int bufferSize, 
            FileStream? outFileStream = null, OnNewPageDelegate? onNewPage = null)
        {
            byte[] buff = new byte[bufferSize];

            using FileStream fs = new(filename, FileMode.Open);
            try
            {
                Line[] lines = new Line[bufferSize / 
                    (Program.MinLineLength + Program.MaxLineNumber.ToString().Length + 2)];

                int readedBytes;
                                                                
                while ((readedBytes = fs.Read(buff, 0, bufferSize)) > 0)
                {
                    Page page = new();

                    int i = Program.MinLineLength + 3;
                    int prevPos = 0;
                    int linesCount = 0;

                    while (i < readedBytes)
                    {
                        i = Array.IndexOf(buff, (byte)0x0A, i - 1);
                        if (i == -1)
                            break;
                        
                        int dotpos = Array.IndexOf(buff, (byte)0x2E, prevPos) - prevPos;

                        var lineLength = i - prevPos + 1;

                        lines[linesCount++] = new Line()
                        {
                            Buff = buff,
                            BuffPosition = prevPos,
                            PrefixLength = dotpos + 2,
                            Length = lineLength,
                        };

                        page.Size += lineLength;

                        if (linesCount == lines.Length)
                            break;

                        prevPos = i + 1;
                        i += Program.MinLineLength + 3;

                        if (i >= buff.Length)
                            break;
                    }

                    Array.Sort(lines, 0, linesCount);

                    if (outFileStream is null)
                        // Запись обратно в файл-источник
                        _ = fs.Seek(-readedBytes, SeekOrigin.Current);
                    else
                        _ = fs.Seek(-(readedBytes - page.Size), SeekOrigin.Current);

                    for (int j = 0; j < linesCount; j++)
                    {
                        if (j == 0)
                        {
                            page.Hash = Page.GetHash(buff, lines[j].TextBuffPosition);
                            page.InFilePosition = (outFileStream ?? fs).Position;
                        }

                        (outFileStream ?? fs).Write(buff, lines[j].BuffPosition, lines[j].Length);
                    }

                    onNewPage?.Invoke(page);
                }
            }
            finally
            {
                fs.Close();
            }
        }
    }
}
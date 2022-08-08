namespace FileSortingTask
{
    public class Page : IKTreeElementData, IComparable<Page>
    {
        
        public UInt32 Hash { get; set; }
        public bool Empty { get; set; }



        public Int64 InFilePosition;
        public int Size;
        private int Position;

        /*
          
                Размер буфера рассчитывается исходя из необходимости рассчитать 
            hash следующей строки (т.е. необходимо читать 4 байта текстовых данных 
            следующей строки).           
         
        */
        private static readonly int BufferSize =
            // Максимальная длина строки
            Program.MaxLineLength +
            // Максимальная длина числа + точка + пробел для читаемой строки и сл. строки
            (Program.MaxLineNumber.ToString().Length + 2) * 2 +
            // Символ перевода строки 0x0A
            1 +
            // 4 байта сл. строки для расчета hash
            4;

        private static readonly byte[] Buffer = new byte[BufferSize];

        
        public void ExtractLinesTo(FileStream fs, List<Line> lines)
        {
            var initialHash = Hash;
            int bufferLength;

            fs.Seek(InFilePosition + Position, SeekOrigin.Begin);

            while ((bufferLength = fs.Read(Buffer, 0, Buffer.Length)) > 0)
            {
                var dotpos = Array.IndexOf(Buffer, (byte)0x2E);
                var i = Array.IndexOf(Buffer, (byte)0x0A, Program.MinLineLength + 3);

                Line line = new()
                {
                    BuffPosition = 0,
                    Buff = new byte[i + 1],
                    PrefixLength = dotpos,
                    Length = i + 1,
                };

                Array.Copy(Buffer, line.Buff, line.Length);

                lines.Add(line);
                Position += line.Length;

                if (Position == Size)
                {
                    Empty = true;
                    break;
                }

                // Получение hash следующей строки
                dotpos = Array.IndexOf(Buffer, (byte)0x2E, i + 1);

                if (dotpos == -1)
                {
                    Empty = true;
                    return;
                }

                Hash = GetHash(Buffer, dotpos + 2);
                if (initialHash != Hash)
                    return;

                if (line.Length < bufferLength)
                    fs.Seek(line.Length - bufferLength, SeekOrigin.Current);
            }

            return;
        } 
        

        public static UInt32 GetHash(byte[] buff, int index) =>
            (UInt32)(
                (buff[index] << 24) +
                (buff[index + 1] << 16) +
                (buff[index + 2] << 8) +
                buff[index + 3]
            );

        public Page Clone() => new()
        {
            Hash = Hash,
            InFilePosition = InFilePosition,
            Size = Size,
            Empty = Empty,
        };

        public int CompareTo(Page? other) => Hash.CompareTo(other!.Hash);
    }
}
namespace FileSortingTask
{
    public struct Line : IComparable<Line>
    {
        public int PrefixLength;
        public int BuffPosition;
        public Int64 InFilePosition;
        public int Length;
        public byte[] Buff;

        public int TextBuffPosition => BuffPosition + PrefixLength;

        public int TextLength => Length - PrefixLength - 1;

        public int CompareTo(Line line)
        {
            // Сравнение по текстовой составляющей строки
            for (int i = 0; i < Program.MaxLineLength; i++)
            {
                if (TextLength < i)
                {
                    if (line.TextLength > TextLength)
                        return 1;
                    else
                        return 0;
                } else if (line.TextLength < i)
                {
                    if (TextLength > line.TextLength)
                        return -1;
                    else
                        return 0;
                }

                if (Buff[TextBuffPosition + i] > line.Buff[line.TextBuffPosition + i])
                    return 1;
                else if (Buff[TextBuffPosition + i] < line.Buff[line.TextBuffPosition + i])
                    return -1;
            }

            // Сравнение по числовой составляющей строки (число от 0 до Program.MaxLineNumber)
            // Line prefix: "1234. "
            for (int i = 0; i < Program.MaxLineNumber.ToString().Length; i++)
            {
                if (PrefixLength < i - 2)
                {
                    if (line.PrefixLength > PrefixLength)
                        return 1;
                    else
                        return 0;
                }
                else if (line.PrefixLength < i - 2)
                {
                    if (PrefixLength > line.PrefixLength)
                        return -1;
                    else
                        return 0;
                }

                if (Buff[i] > line.Buff[i])
                    return 1;
                else if (Buff[i] < line.Buff[i])
                    return -1;
            }

            return 0;
        }
    }
}
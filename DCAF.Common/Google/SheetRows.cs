namespace DCAF.Google
{
    public sealed class SheetRows
    {
        public int First { get; set; }

        public int Last { get; set; }

        public static SheetRows One(int row) => new SheetRows(row, row);

        public static SheetRows Multiple(int first, int last) => new SheetRows(first, last);
        
        SheetRows(int first, int last)
        {
            if (first > last)
            {
                (last, first) = (first, last);
            }

            First = first;
            Last = last;
        }
    }
}
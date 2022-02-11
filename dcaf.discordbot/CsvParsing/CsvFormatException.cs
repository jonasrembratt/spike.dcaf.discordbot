using System;

namespace DCAF.DiscordBot.CsvParsing
{
    public class CsvFormatException : FormatException
    {
        public CsvFormatException(string message, int lineNo, Exception? inner = null)
        : base($"{message} @ line {lineNo.ToString()}", inner)
        {
        }
    }
}
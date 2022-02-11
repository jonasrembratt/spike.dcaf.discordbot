using System;

namespace DCAF.DiscordBot.Google
{
    public class GoogleSheetArgs
    {
        public string SheetName { get; }
        
        public string ApplicationName { get; }

        public string DocumentId { get; }
        
        public GoogleSheetArgs(string sheetName, string applicationName, string documentId)
        {
            SheetName = string.IsNullOrWhiteSpace(sheetName)
                ? throw new ArgumentNullException(nameof(sheetName))
                : sheetName.Trim();
            ApplicationName = string.IsNullOrWhiteSpace(applicationName)
                ? throw new ArgumentNullException(nameof(applicationName))
                : applicationName.Trim();
            DocumentId = string.IsNullOrWhiteSpace(documentId) 
                ? throw new ArgumentNullException(nameof(documentId)) 
                : documentId.Trim();
        }
    }
}
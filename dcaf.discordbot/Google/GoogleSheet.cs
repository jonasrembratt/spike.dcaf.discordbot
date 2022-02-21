using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using TetraPak.XP;

namespace DCAF.DiscordBot.Google
{
    /// <summary>
    ///   Represents a single Google sheet.
    /// </summary>
    public class GoogleSheet : IGoogleSheet
    {
        readonly object _syncRoot = new();
        static readonly string[] s_scopes = { SheetsService.Scope.Spreadsheets } ;
        readonly GoogleCredential _credential;
        readonly GoogleSheetArgs _args;
        SheetsService? _service;

        string SheetName => _args.SheetName;
        
        string ApplicationName => _args.ApplicationName;

        string DocumentId => _args.DocumentId;

        SheetsService Service => getService();

        /// <summary>
        ///   Reads values from one or more columns
        /// </summary>
        /// <param name="columns">
        ///   Specifies a range of columns to be read.
        /// </param>
        /// <param name="rows">
        ///   (optional; default = all)<br/>
        ///   A range of rows ro be read.
        /// </param>
        /// <returns>
        /// </returns>
        public async Task<Outcome<ValueRange>> ReadValuesAsync(SheetColumns columns, SheetRows? rows = null)
        {
            if (columns is null) throw new ArgumentNullException(nameof(columns));
            var range = rows is null
                ? $"{SheetName}!{columns.First}:{columns.Last}"
                : $"{SheetName}!{columns.First}{rows.First.ToString()}:{columns.Last}{rows.Last.ToString()}";
            var request = Service.Spreadsheets.Values.Get(DocumentId, range);
            try
            {
                var valueRange = await request.ExecuteAsync();
                return Outcome<ValueRange>.Success(valueRange);
            }
            catch (Exception ex)
            {
                return Outcome<ValueRange>.Fail(ex);
            }
        }

        public async Task<Outcome> WriteCell(string column, int row, string value)
        {
            var range = $"{SheetName}!{column}{row.ToString()}";
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { new List<object> { value } }
            };
            var update = Service.Spreadsheets.Values.Update(valueRange, DocumentId, range);
            update.ValueInputOption =
                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            try
            {
                var response = await update.ExecuteAsync();
                return response.UpdatedCells == 1
                    ? Outcome.Success()
                    : Outcome.Fail(new Exception($"Could not update sheet cell {range}"));
            }
            catch (GoogleApiException ex)
            {
                return Outcome.Fail(ex).WithGoogleApiError(ex.Error);
            }
            catch (Exception ex)
            {
                return Outcome.Fail(ex);
            }
        }

        /// <summary>
        ///   Constructs and returns a <see cref="GoogleSheet"/> object on Google credentials
        ///   from a specified JSON file.
        /// </summary>
        /// <param name="args">
        ///   Arguments needed to open a Google sheet.
        /// </param>
        /// <param name="credentialsFile">
        ///   The Google credentials JSON file.
        /// </param>
        /// <returns>
        ///   A <see cref="GoogleSheet"/> object.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        ///   The specifies Google credentials JSON file could not be found.
        /// </exception>
        public static async Task<GoogleSheet> OpenAsync(GoogleSheetArgs args, FileInfo credentialsFile)
        {
            if (!credentialsFile.Exists)
                throw new FileNotFoundException($"Credentials file not found: {credentialsFile.FullName}");
                
            await using var stream = new FileStream(credentialsFile.FullName, FileMode.Open, FileAccess.Read);
            var credential =  (await GoogleCredential.FromStreamAsync(stream, CancellationToken.None)).CreateScoped(s_scopes);
            return new GoogleSheet(args, credential);
        }
        
        SheetsService getService()
        {
            lock (_syncRoot)
            {
                return _service ??= new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = ApplicationName
                });
            }
        }

        GoogleSheet(GoogleSheetArgs args, GoogleCredential credential)
        {
            _args = args ?? throw new ArgumentNullException(nameof(args));
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        }
    }

    public static class GoogleSheetHelper
    {
        const string KeyGoogleApiError = "__googleApiError";
        
        internal static Outcome WithGoogleApiError(this Outcome outcome, object error)
        {
            return outcome.WithValue(KeyGoogleApiError, error);
        }

        internal static bool TryGetGoogleApiError(this Outcome outcome, out object? error)
        {
            return outcome.TryGetValue(KeyGoogleApiError, out error);
        }
    }
}
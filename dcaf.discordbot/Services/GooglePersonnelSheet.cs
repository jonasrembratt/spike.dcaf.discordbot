using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using DCAF.DiscordBot.Google;
using Google.Apis.Sheets.v4.Data;

namespace DCAF.DiscordBot.Services
{
    public class GooglePersonnelSheet : IGoogleSheet
    {
        readonly IGoogleSheet _internal;

        public Task<Outcome<ValueRange>> ReadValuesAsync(SheetColumns columns, SheetRows? rows = null) 
            => _internal.ReadValuesAsync(columns, rows);

        public Task<Outcome> WriteCell(string column, int row, string value) 
            => _internal.WriteCell(column, row, value);

        internal GooglePersonnelSheet(IGoogleSheet sheet)
        {
            _internal = sheet;
        }
    }
}
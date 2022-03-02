using System.Threading.Tasks;
using DCAF.Google;
using Google.Apis.Sheets.v4.Data;
using TetraPak.XP;

namespace DCAF.Services
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
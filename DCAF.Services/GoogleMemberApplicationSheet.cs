using System.Threading.Tasks;
using DCAF.Google;
using Google.Apis.Sheets.v4.Data;
using TetraPak.XP;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Services;

public sealed class GoogleMemberApplicationSheet : IGoogleSheet
{
    readonly IGoogleSheet _internal;
    public ILog? Log { get;  }

    public Task<Outcome<ValueRange>> ReadValuesAsync(SheetColumns columns, SheetRows? rows = null)
       => _internal.ReadValuesAsync(columns, rows);

    public Task<Outcome> WriteCell(string column, int row, string value)
        => _internal.WriteCell(column, row, value);
    
    internal GoogleMemberApplicationSheet(IGoogleSheet sheet, ILog? log)
    {
        _internal = sheet;
        Log = log;
    }

}
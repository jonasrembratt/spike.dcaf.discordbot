using System.Threading.Tasks;
using Google.Apis.Sheets.v4.Data;
using TetraPak.XP;

namespace DCAF.Google
{
    public interface IGoogleSheet
    {
        Task<Outcome<ValueRange>> ReadValuesAsync(SheetColumns columns, SheetRows? rows = null);
        Task<Outcome> WriteCell(string column, int row, string value);
    }
}
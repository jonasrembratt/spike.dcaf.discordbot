using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using DCAF.DiscordBot.Google;
using Google.Apis.Sheets.v4.Data;

namespace DCAF.DiscordBot
{
    public interface IGoogleSheet
    {
        Task<Outcome<ValueRange>> ReadValuesAsync(SheetColumns columns, SheetRows? rows = null);
        Task<Outcome> WriteCell(string column, int row, string value);
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DCAF.DiscordBot._lib;
using DCAF.DiscordBot.Model;
using TetraPak.XP;

namespace DCAF.DiscordBot.CsvParsing
{
    public class RollCallCollectionCsvParser
    {
        const char Separator = ',';
        const string EndIdent = "-- end --";
        const string HeaderColumns = "Name,Date,Time";
        const string RollCallColumns = "Role,Spec,Name,ID,Timestamp,Status";
        public Outcome<EventCollection> ParseCsv(string[] lines)
        {
            // string line;
            var list = new List<Event>();
            var eof = false;
            for (var i = 0; i < lines.Length && !eof; i++)
            {
                skipToAfter(HeaderColumns, out var line, out var lineNo);
                if (!parseRollCallMetadata(out var name, out var dateTime))
                {
                    if (!eof)
                        return Outcome<EventCollection>.Fail(
                            new CsvFormatException("Expected roll call meta data (name, time etc.", lineNo));
                    continue;
                }
                
                skipToAfter(RollCallColumns, out line, out lineNo);
                if (line.StartsWith(EndIdent)) // there's always a small chance no one has roll called yet 
                    continue;

                if (!parseEntries(out List<EventEntry>? entries))
                    return Outcome<EventCollection>.Fail(
                        new CsvFormatException("Expected roll entries", lineNo));
                
                list.Add(new Event(name!, dateTime!.Value, entries!));

                bool parseRollCallMetadata(out string? rcName, out DateTime? rcDateTime)
                {
                    // cells might contain commas so we need to apply som special parsing for meta data ...
                    const string DateFormat = "d-M-yyyy HH:mm";
                    rcName = null;
                    rcDateTime = null;
                    var split = line.Split(Separator);
                    if (split.Length < 3)
                    {
                        // some event names seems to allow line feeds, test next line ...
                        ++i;
                        eof = i >= lines.Length;
                        if (eof)
                            return false;
                        
                        line = lines[i];
                        lineNo = i + 1;
                        return parseRollCallMetadata(out rcName, out rcDateTime);
                    }

                    var sb = new StringBuilder(split[0]);
                    for (var j = 1; j < split.Length-1; j++)
                    {
                        var sDataTime = $"{split[j]} {split[j+1]}";
                        if (!DateTime.TryParseExact(sDataTime, DateFormat, null, DateTimeStyles.None, out var dt))
                        {
                            sb.Append(", ");
                            sb.Append(split[j]);
                            continue;
                        }

                        rcName = sb.ToString();
                        rcDateTime = dt;
                        return true;
                    }

                    return false;
                }
                
                Outcome<IEnumerable<EventEntry>> parseEntries(out List<EventEntry>? rollCallEntries)
                {
                    rollCallEntries = new List<EventEntry>(); 
                    for (; i < lines.Length; i++)
                    {
                        line = lines[i];
                        if (line == EndIdent)
                            break;
                        
                        var split = line.Split(Separator);
                        if (split.Length < 6)
                            return Outcome<IEnumerable<EventEntry>>.Fail(
                                new CsvFormatException("Expected a minimum of six columns for roll call entries", i+1));

                        if (!DateTime.TryParse(split[4], CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeStamp))
                            return Outcome<IEnumerable<EventEntry>>.Fail(
                                new CsvFormatException($"Could not parse timestamp ({split[4]})", i+1));

                        if (!split[5].TryParseMemberStatus(out var status))
                            return Outcome<IEnumerable<EventEntry>>.Fail(
                                new CsvFormatException($"Could not parse member status ({split[5]})", i+1));

                        rollCallEntries.Add(
                            new EventEntry
                            {
                                Role = split[0],
                                Spec = split[1],
                                Name = split[2],
                                Id = split[3],
                                TimeStamp = timeStamp,
                                Status = status!.Value
                            });
                    }
                    return Outcome<IEnumerable<EventEntry>>.Success(rollCallEntries);
                }

                void skipToAfter(string pattern, out string nextLine, out int newLineNo)
                {
                    for (; i < lines.Length; i++)
                    {
                        nextLine = lines[i];
                        if (!nextLine.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase)) 
                            continue;
                        
                        ++i;
                        nextLine = lines[i];
                        newLineNo = i + 1;
                        return;
                    }

                    nextLine = string.Empty;
                    newLineNo = i + 1;
                }
            }
            
            return Outcome<EventCollection>.Success(new EventCollection(list));
        }
    }
}
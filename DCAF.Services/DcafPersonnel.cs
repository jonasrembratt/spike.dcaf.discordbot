using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DCAF._lib;
using DCAF.Discord;
using DCAF.Google;
using DCAF.Model;
using Google.Apis.Requests;
using TetraPak.XP;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Services
{
    public sealed class DcafPersonnel : IPersonnel<DiscordMember>
    {
        TaskCompletionSource<List<DiscordMember>> _getMembersTcs = new();
        DateTime _lastReadMembers = DateTime.MinValue;
        readonly GooglePersonnelSheet _sheet;
        readonly Dictionary<string, int> _columnIndex = new()
        {
            [nameof(Member.DateOfApplication)] = 0,
            [nameof(Member.Callsign)] = 4,
            [nameof(Member.Grade)] = 5,
            [nameof(Member.Forename)] = 7,
            [nameof(Member.Surname)] = 8,
            [nameof(DiscordMember.DiscordName)] = 9,
            [nameof(Member.Email)] = 10,
            [nameof(Member.Status)] = 12,
            [nameof(Member.Id)] = 14,
        };
        Dictionary<DiscordMember, int> _sheetRowNoIndex = new();
        Dictionary<string, DiscordMember> _idIndex = new();
        Dictionary<string, DiscordMember> _emailIndex = new();
        List<DiscordMember>? _members;

        ILog? Log => _sheet.Log;

        public async Task<EnumOutcome<DiscordMember>> GetAllMembers()
        {
            try
            {
                await _getMembersTcs.Task;
                return EnumOutcome<DiscordMember>.Success(_members?.Any() ?? false ? _members : Array.Empty<DiscordMember>());
            }
            catch (Exception ex)
            {
                return EnumOutcome<DiscordMember>.Fail(ex);
            }
        }

        public async Task<Outcome<DiscordMember>> GetMemberWithIdAsync(string id)
        {
            try
            {
                await _getMembersTcs.Task;
                return _idIndex.TryGetValue(id, out var member)
                    ? Outcome<DiscordMember>.Success(member)
                    : Outcome<DiscordMember>.Fail(new Exception($"No member found with id '{id}'"));
            }
            catch (Exception ex)
            {
                return Outcome<DiscordMember>.Fail(ex);
            }
        }

        public async Task<Outcome<DiscordMember>> GetMemberWithEmailAsync(string email)
        {
            await _getMembersTcs.Task;
            return _emailIndex.TryGetValue(email, out var member)
                ? Outcome<DiscordMember>.Success(member)
                : Outcome<DiscordMember>.Fail(new Exception($"No member found with email {email}"));
        }

        public Task<Outcome> UpdateAsync(params DiscordMember[] members)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var countUpdated = 0; 
                    var delay  = TimeSpan.Zero;
                    for (var m = 0; m < members.Length; m++)
                    {
                        var member = members[m];
                        var updatedProps = member.GetUpdatedProperties();
                        for (var p = 0; p < updatedProps.Length; p++)
                        {
                            if (delay != TimeSpan.Zero)
                            {
                                await Task.Delay(delay);
                            }
                            var propertyName = updatedProps[p];
                            var (column, row) = getCell(member, propertyName);
                            var value = member[propertyName]?.ToString() ?? string.Empty;
                            var outcome = await _sheet.WriteCell(column, row, value);
                            var retryBecauseRateLimit = isRateLimited(outcome, ref delay);
                            while (retryBecauseRateLimit)
                            {
                                await Task.Delay(delay);
                                outcome = await _sheet.WriteCell(column, row, value);
                                retryBecauseRateLimit = isRateLimited(outcome, ref delay);
                            }
                            countUpdated += outcome ? 1 : 0;
                            if (!outcome)
                            {
                                // todo log failed write to Sheet
                            }
                        }
                        member.ResetModified();
                    }

                    return Outcome.Success($"{countUpdated} users updated");
                }
                catch (Exception ex) 
                {
                    return Outcome.Fail(ex);
                }
            });
        }

        static bool isRateLimited(Outcome outcome, ref TimeSpan delay)
        {
            if (outcome)
                return false;

            if (!outcome.TryGetGoogleApiError(out var obj) || obj is not RequestError requestError)
                return false;

            if (requestError.Code != (int)HttpStatusCode.TooManyRequests)
                return false;

            delay = delay.Add(TimeSpan.FromMilliseconds(50));
            return true;
        }

        public async Task<Outcome> ResetAsync()
        {
            _getMembersTcs = new TaskCompletionSource<List<DiscordMember>>();
            getMembersAsync(true);
            var outcome = await _getMembersTcs.GetOutcomeAsync();
            return outcome ? Outcome.Success("Personnel cache was reloaded") : outcome;
        }

        public event EventHandler? Ready;

        void getMembersAsync(bool reload)
        {
            if (_members is { } && !reload || XpDateTime.Now.Subtract(_lastReadMembers) < TimeSpan.FromSeconds(20))
            {
                Console.WriteLine($"### DCAF Personnel sheet won't reset (was last reset: {_lastReadMembers:u})"); // nisse
                _getMembersTcs.SetResult(_members!);
                return;
            }                                                      

            // read/reload members ...
            Task.Run(async () =>
            {
                var outcome = await _sheet.ReadValuesAsync(new SheetColumns("A", "Z"));
                if (!outcome)
                {
                    _getMembersTcs.SetException(
                        new Exception(
                            "Could not load DCAF members from google sheet (see inner exception)", 
                            outcome.Exception!));
                    return;
                }

                var values = outcome.Value!.Values;
                var isReadingMembers = false;
                if (values is { Count: > 0 })
                {
                    try
                    {
                        _members = buildMemberList();
                    }
                    catch (Exception ex)
                    {
                        _getMembersTcs.SetException(ex);
                        Log.Error(ex);
                        Ready?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                }
                else
                {
                    _members = new List<DiscordMember>();
                }
                _getMembersTcs.SetResult(_members!);
                Ready?.Invoke(this, EventArgs.Empty);

                List<DiscordMember> buildMemberList()
                {
                    var list = new List<DiscordMember>();
                    var rowArray = values.ToArray();
                    var sheetRowNoIndex = new Dictionary<DiscordMember, int>();
                    var idIndex = new Dictionary<string, DiscordMember>();
                    var emailIndex = new Dictionary<string, DiscordMember>();
                    for (var r = 0; r < rowArray.Length; r++)
                    {
                        var row = rowArray[r];
                        var rowNo = r + 1;
                        if (!isMember(row, out var member))
                        {
                            if (row.Count == 0 && isReadingMembers)
                                break;
                            continue;
                        }

                        isReadingMembers = true;
                        list.Add(member);
                        sheetRowNoIndex.Add(member, rowNo);
                        if (member.IsIdentifiable)
                        {
                            if (idIndex.ContainsKey(member.Id))
                            {
                                Log.Warning($"Personnel id {member.Id} already exists; Discord name='{member.DiscordName}'");
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(member.Email) && member.Email.Contains('@'))
                        {
                            try
                            {
                                emailIndex.Add(member.Email, member);
                            }
                            catch (Exception ex)
                            { // nisse
                                Console.WriteLine(ex);
                                throw;
                            }
                        }
                    }

                    _sheetRowNoIndex = sheetRowNoIndex;
                    _idIndex = idIndex;
                    _emailIndex = emailIndex;
                    _lastReadMembers = XpDateTime.Now;
                    return list;
                }
            });
        }

        bool isMember(IList<object> row, [NotNullWhen(true)] out DiscordMember? member)
        {
            member = null;
            if (row.Count < 11)
                return false;
            
            var id = row.Count >= 14 ? (string) row[_columnIndex[nameof(Member.Id)]] : Member.MissingId;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Member.MissingId;
            }
            var doaString = (string) row[_columnIndex[nameof(Member.DateOfApplication)]];
            var doa = doaString.TryParseGoogleDateTime(out var dateTime) ? dateTime : XpDateTime.Now;
            var forename = (string) row[_columnIndex[nameof(Member.Forename)]];
            var callsign = (string) row[_columnIndex[nameof(Member.Callsign)]];
            var surname = (string) row[_columnIndex[nameof(Member.Surname)]];
            if (forename == "Forename" && surname == "Surname")
                return false;

            if (string.IsNullOrWhiteSpace(forename) 
                && string.IsNullOrWhiteSpace(surname) &&
                string.IsNullOrWhiteSpace(callsign))
                return false;
                        
            var discordName = (string) row[_columnIndex[nameof(DiscordMember.DiscordName)]];
            var email = (string) row[_columnIndex[nameof(Member.Email)]];
            var status = ((string)row[_columnIndex[nameof(Member.Status)]]).TryParseMemberStatus(out var memberStatus) 
                ? memberStatus!
                : MemberStatus.Unknown;
            var grade = ((string)row[_columnIndex[nameof(Member.Grade)]]).TryParseMemberGrade(out var memberGrade) 
                ? memberGrade
                : MemberGrade.Unknown;

            member = new DiscordMember(id, 
                doa,
                forename, surname, 
                grade, 
                status.Value)
            {
                Callsign = callsign,
                Email = email,
                DiscordName = new DiscordName(discordName),
            };
            member.SetOperational();
            return true;
        }

        (string column, int row) getCell(DiscordMember member, string propertyName)
        {
            return (columnName(propertyName), _sheetRowNoIndex[member]);
        }

        string columnName(string propertyName)
        {
            var column = this.column(propertyName);
            // note This logic will break if Sheet adds columns beyond 'Z'
            return ((char)('A' + (char)column)).ToString();
        }

        int column(string propertyName) => _columnIndex[propertyName];

        public IEnumerator<DiscordMember> GetEnumerator()
        {
            var outcome = _getMembersTcs.GetOutcome();
            if (outcome)
                return _members!.GetEnumerator();

            Log.Warning($"Failed to get an enumerator for {this} (check for previous errors in log)");
            return new List<DiscordMember>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public DcafPersonnel(GooglePersonnelSheet sheet)
        {
            _sheet = sheet;
            getMembersAsync(false);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Google;
using DCAF.DiscordBot.Model;
using Google.Apis.Requests;
using TetraPak.XP;

namespace DCAF.DiscordBot.Services
{
    public class DcafPersonnel : IPersonnel
    {
        TaskCompletionSource<List<Member>> _getMembersTcs = new();
        DateTime _lastReadMembers = DateTime.MinValue;
        readonly GooglePersonnelSheet _sheet;
        readonly Dictionary<string, int> _columnIndex = new()
        {
            [nameof(Member.Id)] = 13,
            [nameof(Member.Forename)] = 6,
            [nameof(Member.Callsign)] = 3,
            [nameof(Member.Surname)] = 7,
            [nameof(Member.DiscordName)] = 8,
            [nameof(Member.Email)] = 9,
            [nameof(Member.Status)] = 11,
        };
        Dictionary<Member, int> _sheetRowNoIndex = new();
        Dictionary<string, Member> _idIndex = new();
        Dictionary<string, Member> _emailIndex = new();
        List<Member>? _members;

        public async Task<Outcome<IEnumerable<Member>>> GetAllMembers()
        {
            try
            {
                await _getMembersTcs.Task;
                return Outcome<IEnumerable<Member>>.Success(_members?.Any() ?? false ? _members : Array.Empty<Member>());
            }
            catch (Exception ex)
            {
                return Outcome<IEnumerable<Member>>.Fail(ex);
            }
        }

        public async Task<Outcome<Member>> GetMemberWithIdAsync(string id)
        {
            await _getMembersTcs.Task;
            return _idIndex.TryGetValue(id, out var member)
                ? Outcome<Member>.Success(member)
                : Outcome<Member>.Fail(new Exception($"No member found with id '{id}'"));
        }

        public async Task<Outcome<Member>> GetMemberWithEmailAsync(string email)
        {
            await _getMembersTcs.Task;
            return _emailIndex.TryGetValue(email, out var member)
                ? Outcome<Member>.Success(member)
                : Outcome<Member>.Fail(new Exception($"No member found with email {email}"));
        }

        public Task<Outcome> UpdateAsync(params Member[] members)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var countUpdated = 0; 
                    TimeSpan delay  = TimeSpan.Zero;
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

        public Task<Outcome> ResetAsync()
        {
            _getMembersTcs = new TaskCompletionSource<List<Member>>();
            getMembersAsync(true);
            TaskHelper.AwaitResult(_getMembersTcs);
            return Task.FromResult(Outcome.Success("Personnel cache was reloaded"));
        }

        public event EventHandler? Ready;

        void getMembersAsync(bool reload)
        {
            if (_members is { } && !reload || DateTime.Now.Subtract(_lastReadMembers) < TimeSpan.FromSeconds(20))
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
                    throw new Exception(
                        "Could not load DCAF members from google sheet (see inner exception)", outcome.Exception!);

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
                        Ready?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                }
                else
                {
                    _members = new List<Member>();
                }
                _getMembersTcs.SetResult(_members!);
                Ready?.Invoke(this, EventArgs.Empty);

                List<Member> buildMemberList()
                {
                    var list = new List<Member>();
                    var rowArray = values.ToArray();
                    var sheetRowNoIndex = new Dictionary<Member, int>();
                    var idIndex = new Dictionary<string, Member>();
                    var emailIndex = new Dictionary<string, Member>();
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
                            idIndex.Add(member.Id, member);
                        }
                        if (!string.IsNullOrWhiteSpace(member.Email))
                        {
                            emailIndex.Add(member.Email, member);
                        }
                    }

                    _sheetRowNoIndex = sheetRowNoIndex;
                    _idIndex = idIndex;
                    _emailIndex = emailIndex;
                    _lastReadMembers = DateTime.Now;
                    return list;
                }
            });
        }

        bool isMember(IList<object> row, [NotNullWhen(true)] out Member? member)
        {
            member = null;
            if (row.Count < 11)
                return false;
            
            var id = row.Count >= 14 ? (string) row[_columnIndex[nameof(Member.Id)]] : Member.MissingId;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Member.MissingId;
            }
            var forename = (string) row[_columnIndex[nameof(Member.Forename)]];
            var callsign = (string) row[_columnIndex[nameof(Member.Callsign)]];
            var surname = (string) row[_columnIndex[nameof(Member.Surname)]];
            if (forename == "Forename" && surname == "Surname")
                return false;

            if (string.IsNullOrWhiteSpace(forename) 
                && string.IsNullOrWhiteSpace(surname) &&
                string.IsNullOrWhiteSpace(callsign))
                return false;
                        
            var discordName = (string) row[_columnIndex[nameof(Member.DiscordName)]];
            var email = (string) row[_columnIndex[nameof(Member.Email)]];
            var status = ((string)row[_columnIndex[nameof(Member.Status)]]).TryParseMemberStatus(out var statusValue) 
                ? statusValue!
                : MemberStatus.Unknown;

            member = new Member(id, forename, surname, status.Value)
            {
                Callsign = callsign,
                Email = email,
                DiscordName = new DiscordName(discordName),
            };
            member.SetOperational();
            return true;
        }

        (string column, int row) getCell(Member member, string propertyName)
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

        public IEnumerator<Member> GetEnumerator()
        {
            TaskHelper.AwaitResult(_getMembersTcs);
            return _members!.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            TaskHelper.AwaitResult(_getMembersTcs);
            return _members!.GetEnumerator();
        }
        
        public DcafPersonnel(GooglePersonnelSheet sheet)
        {
            _sheet = sheet;
            getMembersAsync(false);
        }
    }
}
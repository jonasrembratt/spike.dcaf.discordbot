using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Google;
using DCAF.DiscordBot.Model;

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
            [nameof(Member.Status)] = 10,
        };
        Dictionary<Member, int> _sheetRowNoIndex = new();
        Dictionary<string, Member> _idIndex = new();
        Dictionary<string, Member> _emailIndex = new();
        List<Member>? _members;

        public async Task<Outcome<Member>> GetMemberWithId(string id)
        {
            await _getMembersTcs.Task;
            return _idIndex.TryGetValue(id, out var member)
                ? Outcome<Member>.Success(member)
                : Outcome<Member>.Fail(new Exception($"No member found with id '{id}'"));
        }

        public async Task<Outcome<Member>> GetMemberWithEmail(string email)
        {
            await _getMembersTcs.Task;
            return _emailIndex.TryGetValue(email, out var member)
                ? Outcome<Member>.Success(member)
                : Outcome<Member>.Fail(new Exception($"No member found with email {email}"));
        }

        public Task<Outcome> Update(params Member[] members)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var countUpdated = 0; 
                    for (var m = 0; m < members.Length; m++)
                    {
                        var member = members[m];
                        var updatedProps = member.GetUpdatedProperties();
                        for (var p = 0; p < updatedProps.Length; p++)
                        {
                            var propertyName = updatedProps[p];
                            var (column, row) = getCell(member, propertyName);
                            var value = member[propertyName]?.ToString() ?? string.Empty;
                            var outcome = await _sheet.WriteCell(column, row, value);
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

        public Task<Outcome> ResetAsync()
        {
            _getMembersTcs = new TaskCompletionSource<List<Member>>();
            getMembersAsync(true);
            _getMembersTcs.AwaitResult();
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
                        if (member.Id != Member.MissingId)
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

        static bool isMember(IList<object> row, [NotNullWhen(true)] out Member? member)
        {
            member = null;
            if (row.Count < 11)
                return false;
            
            var id = row.Count >= 14 ? (string) row[13] : Member.MissingId;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Member.MissingId;
            }
            var forename = (string) row[6];
            var callsign = (string) row[3];
            var surname = (string) row[7];
            if (forename == "Forename" && surname == "Surname")
                return false;

            if (string.IsNullOrWhiteSpace(forename) 
                && string.IsNullOrWhiteSpace(surname) &&
                string.IsNullOrWhiteSpace(callsign))
                return false;
                        
            var discordName = (string) row[8];
            var email = (string) row[9];
            var status = ((string)row[10]).TryParseMemberStatus(out var statusValue) 
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
            _getMembersTcs.AwaitResult();
            return _members!.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            _getMembersTcs.AwaitResult();
            return _members!.GetEnumerator();
        }
        
        public DcafPersonnel(GooglePersonnelSheet sheet)
        {
            _sheet = sheet;
            getMembersAsync(false);
        }
    }
}
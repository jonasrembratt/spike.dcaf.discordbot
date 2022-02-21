using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using DCAF.DiscordBot.Model;
using TetraPak.XP;

namespace DCAF.DiscordBot
{
    public interface IPersonnel : IEnumerable<Member>
    {
        event EventHandler Ready;

        Task<Outcome<IEnumerable<Member>>> GetAllMembers();

        Task<Outcome<Member>> GetMemberWithIdAsync(string id);

        Task<Outcome<Member>> GetMemberWithEmailAsync(string email);
        
        Task<Outcome> UpdateAsync(params Member[] members);

        Task<Outcome> ResetAsync();
    }
}
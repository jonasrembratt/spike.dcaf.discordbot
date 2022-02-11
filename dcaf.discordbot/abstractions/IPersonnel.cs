using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using DCAF.DiscordBot.Model;

namespace DCAF.DiscordBot
{
    public interface IPersonnel : IEnumerable<Member>
    {
        event EventHandler Ready;

        Task<Outcome<Member>> GetMemberWithId(string id);

        Task<Outcome<Member>> GetMemberWithEmail(string email);
        Task<Outcome> Update(params Member[] members);
    }
}
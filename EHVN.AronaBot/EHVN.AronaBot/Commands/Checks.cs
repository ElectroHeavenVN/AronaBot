using EHVN.AronaBot.Config;
using EHVN.ZepLaoSharp.Commands;
using System;
using System.Linq;

namespace EHVN.AronaBot.Commands
{
    internal class AdminCheck : ICommandCheck
    {
        public bool CanExecute(CommandContext ctx) => BotConfig.GetAllAdminIDs().Contains(ctx.User.ID);
    }

    internal class GroupCheck : ICommandCheck
    {
        public virtual bool CanExecute(CommandContext ctx) => ctx.Group is not null;
    }

    internal class EnabledGroupsAndUsersCheck : GroupCheck
    {
        public override bool CanExecute(CommandContext ctx)
        {
            if (!base.CanExecute(ctx))
                return false;
            if (BotConfig.GetAllAdminIDs().Contains(ctx.User.ID))
                return true;
            if (!BotConfig.WritableConfig.CommandEnabledGroupIDs.Contains(ctx.Thread.ThreadID))
                return false;
            if (BotConfig.WritableConfig.DisabledUserIDs.Contains(ctx.User.ID))
                return false;
            return true;
        }
    }
}

namespace Kulfibot.Discord
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Kulfibot.Discord.Messages;
    //TODO: come up with something better
    using static Kulfibot.Messages;

    public class ConnectionMaintainerHandler : IMessageHandler
    {
        private readonly DiscordSecrets secrets;
        private Timekeeper? timekeeper;

        public ConnectionMaintainerHandler(DiscordSecrets secrets)
        {
            this.secrets = secrets;
        }

        public MessageIntent DeclareIntent(Message message) => message switch
        {
            HelloMessage or ClockMessage => MessageIntent.Passive,
            _ => MessageIntent.Ignore
        };

        public Task<IEnumerable<Message>> HandleAsync(Message message)
        {
            if (timekeeper is not null
                && message is ClockMessage clockMessage
                && timekeeper.HitTargetTime(clockMessage.Instant))
            {
                return OneAsync(new HeartbeatMessage());
            }

            if (message is HelloMessage helloMessage)
            {
                this.timekeeper = new Timekeeper(helloMessage.HeartbeatInterval);

                return OneAsync(new IdentifyMessage(this.secrets.BotToken, 3136, "Windows", "Kulfibot", "Kulfibot"));

                //we'll send out the first heartbeat soon -- about 1sec because timekeeper's first HitTargetTime is true
            }

            return NoneAsync;
        }
    }
}

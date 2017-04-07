using System;
using System.Linq;
using System.Text;
using Client.Exceptions;
using IrcDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Client
{
    public class Client : IDisposable
    {
        private readonly IConfigurationRoot _configuration;
        private readonly ILogger _logger;

        public Client(IConfigurationRoot configuration, ILoggerFactory logger)
        {
            _configuration = configuration;
            _logger = logger.CreateLogger<Client>();

            // Init IRC Client
            IrcClient = new StandardIrcClient
            {
                TextEncoding = Encoding.UTF8,
                FloodPreventer = new IrcStandardFloodPreventer(20, 10)
            };
        }

        public StandardIrcClient IrcClient { get; }

        public void Dispose()
        {
            // We Are still connected, it is impolite to not leave a quit message...
            if (IrcClient != null && IrcClient.IsConnected)
            {
                IrcClient.Quit("Glory Be");
                IrcClient.Disconnected += (sender, args) => IrcClient?.Dispose();
                return;
            }

            IrcClient?.Dispose();
        }

        public void Connect()
        {
            // Server
            var server = _configuration.GetValue<string>("client:server:address");
            var port = _configuration.GetValue<int?>("client:server:port");
            var secure = _configuration.GetValue<bool?>("client:server:secure");

            // User
            var userName = _configuration.GetValue<string>("client:user:username");
            var nickName = _configuration.GetValue<string>("client:user:nickname");
            var realName = _configuration.GetValue<string>("client:user:realname");


            // Misc
            var debug = _configuration.GetValue<bool>("client:debug");

            // Validation of Configuration
            if (
                !port.HasValue ||
                !secure.HasValue ||
                string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(nickName) ||
                string.IsNullOrWhiteSpace(realName)
            ) throw new ConfigurationFormatException();


            // Create the userInfo Object
            var userInfo = new IrcUserRegistrationInfo
            {
                NickName = nickName,
                RealName = realName,
                UserName = userName
            };


            // Append A password if we have one
            if (_configuration.GetValue<string>("client:user:password") != null)
                userInfo.Password = _configuration.GetValue<string>("client:user:password");


            AssociateInitialEvents(server);

            // During debuging, echo results to screen
            if (debug)
            {
                IrcClient.RawMessageSent += (sender, ircArgs) => _logger.LogInformation(ircArgs.RawContent);
                IrcClient.RawMessageReceived += (sender, ircArgs) => _logger.LogInformation(ircArgs.RawContent);
            }

            IrcClient.Connect(server, port.Value, secure.Value, userInfo);
        }

        private void AssociateInitialEvents(string server)
        {
            // Assoicate Events
            AddEventLogging(server);

            // Associate events that require a connection
            IrcClient.Registered += AssociateInitialRegisteredEvents;

            // Join The channels specified in the configuration
            IrcClient.Registered += JoinChannels;
        }

        // Wireup Event Logging
        private void AddEventLogging(string server)
        {
            IrcClient.Connected += (sender, args) => _logger.LogInformation($"Connected to {server}");
            IrcClient.Registered += (sender, args) => _logger.LogInformation($"Registered to {server}");
            IrcClient.Disconnected += (sender, args) => _logger.LogInformation($"Disconnected to {server}");
            IrcClient.ConnectFailed += (sender, args) => _logger.LogInformation($"Connection to {server} failed...");
            IrcClient.NetworkInformationReceived +=
                (sender, args) => _logger.LogInformation($"Received Network Info from {server}");
            IrcClient.ConnectFailed += (sender, args) => _logger.LogInformation($"Connection to {server} failed...");
            IrcClient.MotdReceived += (sender, args) => _logger.LogInformation($"MOTD Received from {server}");
            IrcClient.ServerSupportedFeaturesReceived +=
                (sender, args) => _logger.LogInformation($"Support Features received from {server}");
        }


        // Initial Registered Events
        private void AssociateInitialRegisteredEvents(object sender, EventArgs eventArgs)
        {
            // On Channel Join
            IrcClient.LocalUser.JoinedChannel +=
                (o, args) =>
                    _logger.LogInformation(
                        $"Joined {args.Channel.Name} : Topic [{args.Channel.Topic}] : Type [{args.Channel.Type.ToString()}]");
        }


        // Join Channels
        private void JoinChannels(object sender, EventArgs e) => _configuration
            .GetSection("client:channels")
            .GetChildren()?
            .ToList()
            .ForEach(channel =>
            {
                _logger.LogInformation($"Joining {channel}");
                IrcClient.Channels.Join(channel.Value);
            });
    }
}
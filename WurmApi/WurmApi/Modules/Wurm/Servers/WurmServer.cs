using System;
using System.Threading.Tasks;
using AldurSoft.Core;
using AldurSoft.WurmApi.Modules.DataContext.DataModel.WurmServersModel;

namespace AldurSoft.WurmApi.Modules.Wurm.Servers
{
    public class WurmServer : IWurmServer
    {
        private readonly WurmServerInfo wurmServerInfo;
        private readonly LiveLogs liveLogs;
        private readonly WebFeeds webFeeds;
        private readonly LogHistory logHistory;

        internal WurmServer(
            WurmServerInfo wurmServerInfo,
            LiveLogs liveLogs,
            LogHistory logHistory,
            WebFeeds webFeeds)
        {
            if (wurmServerInfo == null) throw new ArgumentNullException("wurmServerInfo");
            if (liveLogs == null) throw new ArgumentNullException("liveLogs");
            if (webFeeds == null) throw new ArgumentNullException("webFeeds");
            if (logHistory == null) throw new ArgumentNullException("logHistory");
            this.wurmServerInfo = wurmServerInfo;
            this.liveLogs = liveLogs;
            this.webFeeds = webFeeds;
            this.logHistory = logHistory;
        }

        public virtual ServerName ServerName
        {
            get
            {
                return wurmServerInfo.Name;
            }
        }

        public ServerGroup ServerGroup
        {
            get
            {
                return wurmServerInfo.ServerGroup;
            }
        }

        public async Task<WurmDateTime?> TryGetCurrentTime()
        {
            var liveData = await liveLogs.GetForServer(ServerName);
            if (liveData.ServerDate.Stamp > DateTimeOffset.MinValue)
            {
                return AdjustedWurmDateTime(liveData.ServerDate);
            }
            var logHistoryData = await logHistory.GetForServer(ServerName);
            if (logHistoryData.ServerDate.Stamp > Time.Clock.LocalNowOffset.AddDays(-1))
            {
                return AdjustedWurmDateTime(logHistoryData.ServerDate);
            }
            var webFeedsData = await webFeeds.GetForServer(ServerName);
            if (webFeedsData.ServerDate.Stamp > DateTimeOffset.MinValue)
            {
                return AdjustedWurmDateTime(webFeedsData.ServerDate);
            }
            if (logHistoryData.ServerDate.Stamp > DateTimeOffset.MinValue)
            {
                return AdjustedWurmDateTime(logHistoryData.ServerDate);
            }

            return null;
        }

        private WurmDateTime AdjustedWurmDateTime(ServerDateStamped date)
        {
            return date.WurmDateTime + (Time.Clock.LocalNowOffset - date.Stamp);
        }

        public async Task<TimeSpan?> TryGetCurrentUptime()
        {
            var liveData = await liveLogs.GetForServer(ServerName);
            if (liveData.ServerUptime.Stamp > DateTimeOffset.MinValue)
            {
                return AdjustedUptime(liveData.ServerUptime);
            }
            var logHistoryData = await logHistory.GetForServer(ServerName);
            if (logHistoryData.ServerUptime.Stamp > Time.Clock.LocalNowOffset.AddDays(-1))
            {
                return AdjustedUptime(logHistoryData.ServerUptime);
            }
            var webFeedsData = await webFeeds.GetForServer(ServerName);
            if (webFeedsData.ServerUptime.Stamp > DateTimeOffset.MinValue)
            {
                return AdjustedUptime(webFeedsData.ServerUptime);
            }
            if (logHistoryData.ServerUptime.Stamp > DateTimeOffset.MinValue)
            {
                return AdjustedUptime(logHistoryData.ServerUptime);
            }

            return null;
        }

        private TimeSpan AdjustedUptime(ServerUptimeStamped uptime)
        {
            return uptime.Uptime + (Time.Clock.LocalNowOffset - uptime.Stamp);
        }
    }
}
using System;
using System.Threading.Tasks;
using AngryWasp.Logger;
using Discord;
using AwLog = AngryWasp.Logger.Log;

namespace Nerva.Bots.Helpers
{
    public static class Logger
    {
        public static Task WriteDebug(string msg)
		{
			AwLog.Instance.Write(Log_Severity.Info, msg);
			return Task.CompletedTask;
		}

		public static Task WriteWarning(string msg)
		{
			AwLog.Instance.Write(Log_Severity.Warning, msg);
			return Task.CompletedTask;
		}

		public static Task WriteError(string msg)
		{
			AwLog.Instance.Write(Log_Severity.Error, msg);
			return Task.CompletedTask;
		}

		public static Task HandleException(Exception ex)
		{
			AwLog.Instance.WriteNonFatalException(ex);
			return Task.CompletedTask;
		}

		public static Task HandleException(Exception ex, string message) 
		{
			AwLog.Instance.WriteNonFatalException(ex, message);
			return Task.CompletedTask;
		}

		public static Task Write(LogMessage msg)
		{
			Log_Severity ls = Log_Severity.None;

			// Map Discord.net LogSeverity to Log_Severity
			switch (msg.Severity)
			{
				case LogSeverity.Info:
					ls = Log_Severity.Info;
					break;
				case LogSeverity.Warning:
					ls = Log_Severity.Warning;
					break;
				case LogSeverity.Error:
					ls = Log_Severity.Error;
					break;
				case LogSeverity.Critical:
					ls = Log_Severity.Fatal;
					break;
			}

			if (msg.Exception == null)
			{
				AwLog.Instance.Write(msg.ToString());
			}
			else
			{
				if (ls == Log_Severity.Fatal)
				{
					// It's not loggers job, to kill the application
					//AwLog.Instance.WriteFatalException(msg.Exception, msg.Message);
					AwLog.Instance.WriteNonFatalException(msg.Exception, msg.Message);
				}
				else
				{
					AwLog.Instance.WriteNonFatalException(msg.Exception, msg.Message);
				}
			}

			return Task.CompletedTask;
		}
    }
}
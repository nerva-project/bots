using System;
using System.Threading.Tasks;
using AngryWasp.Logger;
using Discord;
using AwLog = AngryWasp.Logger.Log;

namespace Nerva.Bots.Helpers
{
    public static class Logger
    {
		public static Task HandleException(Exception ex)
		{
			LogException(ex, string.Empty);
			return Task.CompletedTask;
		}

		public static Task HandleException(Exception ex, string message) 
		{
			LogException(ex, message);
			return Task.CompletedTask;
		}

		public static Task LogException(Exception ex, string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				AwLog.Instance.Write(Log_Severity.None, "ERROR\t" + DateTime.Now.ToString("u") + "\t Ex Msg: " + ex.Message + "\nTrace: " + ex.StackTrace);
			}
			else
			{
				AwLog.Instance.Write(Log_Severity.None, "ERROR\t" + DateTime.Now.ToString("u") + "\t " + message + " | Ex Msg: " + ex.Message + "\nTrace: " + ex.StackTrace);
			}

			return Task.CompletedTask;
		}

        public static Task WriteDebug(string msg)
		{
			AwLog.Instance.Write(Log_Severity.None, "DEBUG\t" + DateTime.Now.ToString("u") + "\t" + msg);
			return Task.CompletedTask;
		}

		public static Task WriteWarning(string msg)
		{
			AwLog.Instance.Write(Log_Severity.None, "WARN\t" + DateTime.Now.ToString("u") + "\t" + msg);
			return Task.CompletedTask;
		}

		public static Task WriteError(string msg)
		{
			AwLog.Instance.Write(Log_Severity.None, "ERROR\t" + DateTime.Now.ToString("u") + "\t" + msg);
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
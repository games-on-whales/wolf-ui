using System;
using System.Globalization;
using Godot;

public interface ILoggerFactory
{
	public enum LogLevelEnum { NONE, ERROR, WARN, INFO, DEBUG }
	public void SetLogLevel(LogLevelEnum logLevel);
	public abstract static ILoggerFactory Create();
	public ILogger<T> CreateLogger<T>();
}

public sealed class LoggerFactory : ILoggerFactory
{
    readonly System.Type Implemetnation = typeof(MyTempLogger<>);
	private ILoggerFactory.LogLevelEnum logLevel = ILoggerFactory.LogLevelEnum.INFO;

	private LoggerFactory()
	{

	}

	public void SetLogLevel(ILoggerFactory.LogLevelEnum logLevel)
	{
		this.logLevel = logLevel;
	}

	public static ILoggerFactory Create()
	{
		return new LoggerFactory();
	}

	public ILogger<T> CreateLogger<T>()
	{
		var a = Implemetnation.MakeGenericType(typeof(T));
		var logger_obj = Activator.CreateInstance(a);
		if (logger_obj is ILogger<T> logger)
		{
			logger.SetLogLevel(logLevel);
			return logger;
		}
		return null;
	}
}

public interface ILogger<T>
{
	public void SetLogLevel(ILoggerFactory.LogLevelEnum logLevel);
	public void LogInformation(string text, params object[] args);
	public void LogWarning(string text, params object[] args);
	public void LogDebug(string text, params object[] args);
	public void LogError(string text, params object[] args);
}

public sealed class MyTempLogger<T> : ILogger<T>
{
	private ILoggerFactory.LogLevelEnum logLevel = ILoggerFactory.LogLevelEnum.INFO;
	public void SetLogLevel(ILoggerFactory.LogLevelEnum logLevel)
	{
		this.logLevel = logLevel;
	}

	private Action<string> Writer = Console.Write;//GD.Print;

	private void Write(string text, string level, params object[] args)
	{
		string Msg = String.Format(new CultureInfo("en-US"), text, args);
		Writer($"{DateTime.Now} ");
		Writer(level);
		Writer($": {typeof(T).FullName} {Msg}\n");
	}

	public void LogDebug(string text, params object[] args)
	{
		if (logLevel < ILoggerFactory.LogLevelEnum.DEBUG)
			return;

		Write(text, $"{(char)0x1b}[1;40;35mdebug{(char)0x1b}[0m", args);
	}

    public void LogError(string text, params object[] args)
    {
		if (logLevel < ILoggerFactory.LogLevelEnum.ERROR)
			return;

		Write(text, $"{(char)0x1b}[1;40;31merror{(char)0x1b}[0m", args);
    }

    public void LogInformation(string text, params object[] args)
    {
		if (logLevel < ILoggerFactory.LogLevelEnum.INFO)
			return;

		Write(text, $"{(char)0x1b}[1;40;32minfo{(char)0x1b}[0m", args);
    }

    public void LogWarning(string text, params object[] args)
    {
		if (logLevel < ILoggerFactory.LogLevelEnum.WARN)
			return;

		Write(text, $"{(char)0x1b}[1;40;33mwarn{(char)0x1b}[0m", args);
    }
}
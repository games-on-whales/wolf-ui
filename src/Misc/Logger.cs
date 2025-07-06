using System;
using System.Collections.Generic;
using System.Globalization;

namespace WolfUI;

public interface ILoggerFactory
{
	public enum LogLevelEnum { NONE, ERROR, WARN, INFO, DEBUG }
	public void SetLogLevel(LogLevelEnum logLevel);
	public static abstract ILoggerFactory Create();
	public ILogger<T> CreateLogger<T>();
}

public sealed class LoggerFactory : ILoggerFactory
{
	private readonly Type _implementation = typeof(MyTempLogger<>);
	private ILoggerFactory.LogLevelEnum _logLevel = ILoggerFactory.LogLevelEnum.INFO;

	private LoggerFactory()
	{

	}

	public void SetLogLevel(ILoggerFactory.LogLevelEnum logLevel)
	{
		this._logLevel = logLevel;
	}

	public static ILoggerFactory Create()
	{
		return new LoggerFactory();
	}

	public ILogger<T> CreateLogger<T>()
	{
		var a = _implementation.MakeGenericType(typeof(T));
		var loggerObj = Activator.CreateInstance(a);
        var logger = loggerObj as ILogger<T> ?? throw new TypeAccessException("Logger creation failed");
        logger.SetLogLevel(_logLevel);
		return logger;
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
	private ILoggerFactory.LogLevelEnum _logLevel = ILoggerFactory.LogLevelEnum.INFO;
	public void SetLogLevel(ILoggerFactory.LogLevelEnum logLevel)
	{
		this._logLevel = logLevel;
	}

	private readonly Action<string> _writer = Console.Write;//GD.Print;

	private void Write(string text, string level, params object[] args)
	{
		var msg = string.Format(new CultureInfo("en-US"), text, args);
		_writer($"{DateTime.Now} ");
		_writer(level);
		_writer($": {typeof(T).FullName} {msg}\n");
	}

	public void LogDebug(string text, params object[] args)
	{
		if (_logLevel < ILoggerFactory.LogLevelEnum.DEBUG)
			return;

		Write(text, $"{(char)0x1b}[1;40;35mdebug{(char)0x1b}[0m", args);
	}

	public void LogError(string text, params object[] args)
	{
		if (_logLevel < ILoggerFactory.LogLevelEnum.ERROR)
			return;

		Write(text, $"{(char)0x1b}[1;40;31merror{(char)0x1b}[0m", args);
	}

	public void LogInformation(string text, params object[] args)
	{
		if (_logLevel < ILoggerFactory.LogLevelEnum.INFO)
			return;

		Write(text, $"{(char)0x1b}[1;40;32minfo{(char)0x1b}[0m", args);
	}

	public void LogWarning(string text, params object[] args)
	{
		if (_logLevel < ILoggerFactory.LogLevelEnum.WARN)
			return;

		Write(text, $"{(char)0x1b}[1;40;33mwarn{(char)0x1b}[0m", args);
	}
}

public partial class Main
{
	private static readonly ILoggerFactory Factory;
	private static readonly ILogger<Main> Logger;

#nullable disable
	static Main()
	{
		var strToEnum = new Dictionary<string, ILoggerFactory.LogLevelEnum>
		{
			{ "NONE", ILoggerFactory.LogLevelEnum.NONE},
			{ "ERROR", ILoggerFactory.LogLevelEnum.ERROR},
			{ "WARNING", ILoggerFactory.LogLevelEnum.WARN},
			{ "WARN", ILoggerFactory.LogLevelEnum.WARN},
			{ "INFORMATION", ILoggerFactory.LogLevelEnum.INFO},
			{ "INFO", ILoggerFactory.LogLevelEnum.INFO},
			{ "DEBUG", ILoggerFactory.LogLevelEnum.DEBUG}
		};
		var logLevelEnv = Environment.GetEnvironmentVariable("LOGLEVEL") ?? "INFO";
		var logLevel = strToEnum.GetValueOrDefault(logLevelEnv.ToUpper(), ILoggerFactory.LogLevelEnum.INFO);

		Factory = LoggerFactory.Create();

		Factory.SetLogLevel(logLevel);
		Logger = Factory.CreateLogger<Main>();
		Logger.LogInformation("Wolf-UI started.");
	}

	public static ILogger<T> GetLogger<T>()
	{
		return Factory.CreateLogger<T>();
	}
#nullable enable
}
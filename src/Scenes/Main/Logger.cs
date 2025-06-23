using System;
using System.Globalization;
using Godot;

public interface ILoggerFactory
{
	public abstract static ILoggerFactory Create();
	public ILogger<T> CreateLogger<T>();
}

public sealed class LoggerFactory : ILoggerFactory
{
    readonly System.Type Implemetnation = typeof(MyTempLogger<>);

	private LoggerFactory()
	{
		
	}

	public static ILoggerFactory Create()
	{
		return new LoggerFactory();
	}

	public ILogger<T> CreateLogger<T>()
	{
		var a = Implemetnation.MakeGenericType(typeof(T));
		var logger = Activator.CreateInstance(a);
		return (ILogger<T>)logger;

	}
}

public interface ILogger<T>
{
	public void LogInformation(string text, params object[] args);
	public void LogWarning(string text, params object[] args);
	public void LogDebug(string text, params object[] args);
	public void LogError(string text, params object[] args);
}

public sealed class MyTempLogger<T> : ILogger<T>
{
    public void LogDebug(string text, params object[] args)
    {
		string Msg = String.Format(new CultureInfo("en-US"), text, args);
		GD.Print($"{DateTime.Now} debug: {typeof(T).FullName} {Msg}");
    }

    public void LogError(string text, params object[] args)
    {
		string Msg = String.Format(new CultureInfo("en-US"), text, args);
		GD.Print($"{DateTime.Now} error: {typeof(T).FullName} {Msg}");
    }

    public void LogInformation(string text, params object[] args)
    {
		string Msg = String.Format(new CultureInfo("en-US"), text, args);
		GD.Print($"{DateTime.Now} info: {typeof(T).FullName} {Msg}");
    }

    public void LogWarning(string text, params object[] args)
    {
		string Msg = String.Format(new CultureInfo("en-US"), text, args);
		GD.Print($"{DateTime.Now} warn: {typeof(T).FullName} {Msg}");
    }
}
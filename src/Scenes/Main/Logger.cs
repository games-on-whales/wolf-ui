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
	private Action<string> Writer = Console.Out.WriteLine;//GD.Print;

	private void Write(string text, string level, params object[] args)
	{
		string Msg = String.Format(new CultureInfo("en-US"), text, args);
		Writer($"{DateTime.Now} {level}: {typeof(T).FullName} {Msg}");
	}

	public void LogDebug(string text, params object[] args)
	{
		Write(text, "debug", args);
	}

    public void LogError(string text, params object[] args)
    {
		Write(text, "error", args);
    }

    public void LogInformation(string text, params object[] args)
    {
		Write(text, "info", args);
    }

    public void LogWarning(string text, params object[] args)
    {
		Write(text, "warn", args);
    }
}
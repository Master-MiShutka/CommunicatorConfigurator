namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class TextFileLoggerExtensions
{
    /// <summary>
    /// Adds a text file logger
    /// </summary>
    /// <param name="fileName">log file name</param>
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string fileName)
    {
        builder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, TextFileLoggerProvider>(
            provider => new TextFileLoggerProvider(fileName)
        ));

        return builder;
    }

    /// <summary>
    /// Adds a file logger.
    /// </summary>
    /// <param name="factory">The <see cref="ILoggerFactory"/> to use</param>
    /// <param name="fileName">log file name</param>
    public static ILoggerFactory AddFile(this ILoggerFactory factory, string fileName)
    {
        factory.AddProvider(new TextFileLoggerProvider(fileName));
        return factory;
    }
}

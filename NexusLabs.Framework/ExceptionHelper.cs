using System;
using System.Text;

namespace NexusLabs.Framework;

public static class ExceptionHelper
{
    public static string BuildExceptionMessage(Exception? exception)
    {
        if (exception == null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        BuildExceptionMessage(
            builder,
            exception);
        return builder.ToString();
    }

    public static void BuildExceptionMessage(StringBuilder builder, Exception? exception)
     => BuildExceptionMessage(builder, exception, 0);

    public static void BuildExceptionMessage(
        StringBuilder builder,
        Exception? exception,
        int indentationLevel)
    {
        if (exception == null)
        {
            return;
        }

        const int INDENT_SIZE = 4;
        builder.Append(new string(' ', INDENT_SIZE));
        builder.Append(exception.GetType().Name);
        builder.Append(": ");
        builder.AppendLine(exception.Message);

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            builder.Append(new string(' ', INDENT_SIZE));
            builder.AppendLine("Stack Trace:");
            builder.Append(new string(' ', INDENT_SIZE));
            builder.AppendLine(exception.StackTrace);
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (var child in aggregateException.InnerExceptions)
            {
                BuildExceptionMessage(builder, child, indentationLevel + 1);
            }
        }
        else if (exception.InnerException != null)
        {
            BuildExceptionMessage(builder, exception.InnerException, indentationLevel + 1);
        }
    }
}

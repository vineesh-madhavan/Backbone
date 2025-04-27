// Backbone.Infrastructure/Interceptors/SerilogDbContextInterceptor.cs
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using System.Data.Common;

//namespace Infrastructure.Interceptors
//{ 
    public class SerilogDbContextInterceptor : DbCommandInterceptor
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SerilogDbContextInterceptor>();

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Log.Debug("Executing DbCommand: {CommandText}", command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        // Override other methods as needed
    }
//}
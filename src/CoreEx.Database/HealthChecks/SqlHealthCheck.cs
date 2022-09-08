using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Configuration;
using HealthChecks.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.DataBase.HealthChecks
{

    /// <summary> Sql Server Health Check. </summary>
    public class SqlHealthCheck : IHealthCheck
    {
        private const string HEALTH_QUERY = "SELECT 1;";
        private readonly string _sqlConnectionString;
        private readonly string _connectionName;
        private IHealthCheck? _innerHealthCheck;
        private IReadOnlyDictionary<string, object>? _data;

        /// <summary> constructor. </summary>
        /// <remarks> Note that constructor takes setting NAMES not values, values are looked up from <paramref name="settings"/>. </remarks>
        public SqlHealthCheck(SettingsBase settings, string connectionName)
        {
            _sqlConnectionString = settings.GetValue<string>(connectionName);
            _connectionName = connectionName;
        }

        /// <summary> constructor. </summary>
        /// <remarks> Note that constructor takes setting NAMES not values, values are looked up from <paramref name="settings"/>. </remarks>
        public SqlHealthCheck(SettingsBase settings, string connectionName, IHealthCheck sqlCheck)
        {
            _sqlConnectionString = settings.GetValue<string>(connectionName);
            _connectionName = connectionName;
            _innerHealthCheck = sqlCheck;
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_sqlConnectionString))
            {
                return HealthCheckResult.Unhealthy($"Sql Server connection is not configured under {_connectionName} in settings");
            }

            if (_data == null)
            {
                try
                {
                    SqlConnectionStringBuilder builder = new(_sqlConnectionString);
                    _data = new Dictionary<string, object>
                    {
                        { "server", builder.DataSource },
                        { "database", builder.InitialCatalog },
                        { "timeout", builder.ConnectTimeout },
                        { "authenticationMethod", builder.Authentication}
                    };
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"Sql Server connection could not be parsed. Check the value under {_connectionName} in settings", ex);
                }
            }

            _innerHealthCheck ??= new SqlServerHealthCheck(_sqlConnectionString, HEALTH_QUERY);

            try
            {
                var result = await _innerHealthCheck.CheckHealthAsync(context, cancellationToken);
                return new HealthCheckResult(result.Status, result.Description, result.Exception, data: _data);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex, data: _data);
            }
        }
    }
}
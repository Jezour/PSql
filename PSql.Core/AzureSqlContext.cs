﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Management.Automation;

namespace PSql
{
    public class AzureSqlContext : SqlContext
    {
        public string ResourceGroupName { get; set; }

        public string ServerFullName { get; private set; }

        protected override void BuildConnectionString(SqlConnectionStringBuilder builder)
        {
            if (Credential == null || Credential == PSCredential.Empty)
                throw new NotSupportedException("A credential is required when connecting to Azure SQL Database.");

            if (!UseEncryption)
                throw new NotSupportedException("Encryption is required when connecting to Azure SQL Database.");

            if (!UseServerIdentityCheck)
                throw new NotSupportedException("Server identity check is required when connecting to Azure SQL Database.");

            base.BuildConnectionString(builder);

            builder.DataSource = ServerFullName ?? ResolveServerFullName();

            if (string.IsNullOrEmpty(DatabaseName))
                builder.InitialCatalog = MasterDatabaseName;
        }

        private string ResolveServerFullName()
        {
            var value = ScriptBlock
                .Create("param ($x) Get-AzSqlServer @x -ea Stop")
                .Invoke(new Dictionary<string, object>
                {
                    ["ResourceGroupName"] = ResourceGroupName,
                    ["ServerName"]        = ServerName,
                })
                .FirstOrDefault()
                ?.Properties["FullyQualifiedDomainName"]
                ?.Value as string;

            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException(
                    "The Get-AzSqlServer command completed without error, " +
                    "but did not yield an object with a FullyQualifiedDomainName " +
                    "property set to a non-null, non-empty string."
                );

            return ServerFullName = value;
        }
    }
}

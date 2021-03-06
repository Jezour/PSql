using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Microsoft.Data.SqlClient;

namespace PSql
{
    [Cmdlet(VerbsLifecycle.Invoke, "Sql", DefaultParameterSetName = ContextName)]
    [OutputType(typeof(PSObject[]))]
    public class InvokeSqlCommand : ConnectedCmdlet
    {
        // -Sql
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string[] Sql { get; set; }

        // -Define
        [Parameter(Position = 1)]
        public Hashtable Define { get; set; }

        // -NoPreprocessing
        [Parameter]
        [Alias("NoSqlCmdMode")]
        public SwitchParameter NoPreprocessing { get; set; }

        // -NoErrorHandling
        [Parameter]
        public SwitchParameter NoErrorHandling { get; set; }

        // -UseSqlTypes
        [Parameter]
        public SwitchParameter UseSqlTypes { get; set; }

        // -Timeout
        [Parameter]
        public TimeSpan? Timeout { get; set; }

        private SqlCmdPreprocessor _preprocessor;
        private SqlCommand         _command;

        private bool ShouldUsePreprocessing
            => !NoPreprocessing;

        private bool ShouldUseErrorHandling
            => !NoErrorHandling;

        protected override void BeginProcessing()
        {
            // Will open a connection if one is not already open
            base.BeginProcessing();

            // Clear any failures from prior command
            ConnectionInfo.Get(Connection).HasErrors = false;

            _preprocessor = new SqlCmdPreprocessor().WithVariables(Define);

            _command             = Connection.CreateCommand();
            _command.Connection  = Connection;
            _command.CommandType = CommandType.Text;

            if (Timeout.HasValue)
                _command.CommandTimeout = (int) Timeout.Value.TotalSeconds;
        }

        protected override void ProcessRecord()
        {
            // Check if scripts were provided at all
            var scripts = (IEnumerable<string>) Sql;
            if (scripts == null)
                return;

            // No need to send empty scripts to server
            scripts = ExcludeNullOrEmpty(scripts);

            // Add optional preprocessing
            if (ShouldUsePreprocessing)
                scripts = Preprocess(scripts);

            // Execute with optional error handling
            if (ShouldUseErrorHandling)
                Execute(SqlErrorHandling.Apply(scripts));
            else
                Execute(scripts);
        }

        private static IEnumerable<string> ExcludeNullOrEmpty(IEnumerable<string> scripts)
        {
            return scripts.Where(s => !string.IsNullOrEmpty(s));
        }

        private IEnumerable<string> Preprocess(IEnumerable<string> scripts)
        {
            return scripts.SelectMany(s => _preprocessor.Process(s));
        }

        private void Execute(IEnumerable<string> batches)
        {
            foreach (var batch in batches)
                Execute(batch);
        }

        private void Execute(string batch)
        {
            _command.CommandText = batch;

            foreach (var obj in _command.ExecuteAndProjectToPSObjects(UseSqlTypes))
                WriteObject(obj);
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            ReportErrors();
        }

        protected override void StopProcessing()
        {
            base.StopProcessing();

            ReportErrors();
        }

        private void ReportErrors()
        {
            if (ConnectionInfo.Get(Connection).HasErrors)
                throw new DataException("An error occurred while executing the SQL batch.");
        }

        protected override void Dispose(bool managed)
        {
            if (managed)
            {
                _command?.Dispose();
                _command = null;
            }

            base.Dispose(managed);
        }
    }
}

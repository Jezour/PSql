﻿using System;
using System.Collections;
using System.Globalization;

namespace PSql
{
    internal static class SqlCmdPreprocessorExtensions
    {
        public static SqlCmdPreprocessor WithVariables(
            this SqlCmdPreprocessor preprocessor, IDictionary entries)
        {
            if (preprocessor is null)
                throw new ArgumentNullException(nameof(preprocessor));

            if (entries == null)
                return preprocessor;

            var variables = preprocessor.Variables;

            foreach (DictionaryEntry entry in entries)
            {
                if (entry.Key is null)
                    continue;

                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);

                if (string.IsNullOrEmpty(key))
                    continue;

                var value = Convert.ToString(entry.Value, CultureInfo.InvariantCulture);

                variables[key] = value ?? string.Empty;
            }

            return preprocessor;
        }
    }
}

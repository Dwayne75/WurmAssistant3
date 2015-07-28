using System;
using System.Collections.Generic;

namespace AldurSoft.WurmApi
{
    public interface IWurmConfigDirectories
    {
        IEnumerable<string> AllDirectoryNamesNormalized { get; }

        IEnumerable<string> AllDirectoriesFullPaths { get; }

        IEnumerable<string> AllConfigNames { get; }

        /// <summary>
        /// Returns absolute gamesettings.txt file path for specified config name.
        /// </summary>
        /// <param name="directoryName">Case insensitive</param>
        /// <returns></returns>
        /// <exception cref="DataNotFoundException">Config does not exist.</exception>
        string GetGameSettingsFileFullPathForConfigName(string directoryName);
    }
}
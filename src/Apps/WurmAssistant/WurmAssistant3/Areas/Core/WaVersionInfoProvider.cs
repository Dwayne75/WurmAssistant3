﻿using System;
using AldursLab.WurmAssistant3.Areas.Config;
using JetBrains.Annotations;

namespace AldursLab.WurmAssistant3.Areas.Core
{
    [KernelBind(BindingHint.Singleton)]
    class WaVersionInfoProvider : IWaVersionInfoProvider
    {
        readonly IWurmAssistantConfig wurmAssistantConfig;
        readonly IWaVersion waVersion;

        public WaVersionInfoProvider([NotNull] IWurmAssistantConfig wurmAssistantConfig, [NotNull] IWaVersion waVersion)
        {
            if (wurmAssistantConfig == null) throw new ArgumentNullException("wurmAssistantConfig");
            if (waVersion == null) throw new ArgumentNullException("waVersion");
            this.wurmAssistantConfig = wurmAssistantConfig;
            this.waVersion = waVersion;
        }

        public string Get()
        {
            string s = string.Empty;
            if (waVersion.Known)
            {
                s += string.Format("{0}", waVersion.AsString());
            }
            else
            {
                s += "unknown version";
            }
            s += " P:" + "Windows";
            return s;
        }
    }
}
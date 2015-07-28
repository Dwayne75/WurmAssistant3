﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AldurSoft.WurmApi
{
    /// <summary>
    /// Marshalls public WurmApi event invocations to designated thread.
    /// </summary>
    public interface IPublicEventMarshaller
    {
        /// <summary>
        /// Invokes provided action on a designated thread.
        /// </summary>
        /// <param name="action"></param>
        void Marshal(Action action);
    }
}
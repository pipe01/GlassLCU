using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlassLCU
{
    public enum InitializeMethod
    {
        /// <summary>
        /// Gets the connection info through launch command line arguments. Requires LeagueClientUx to be running
        /// </summary>
        CommandLine,

        /// <summary>
        /// Gets the connection info through the lockfile. Only requires LeagueClient to be running.
        /// </summary>
        Lockfile
    }
}

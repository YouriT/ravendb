using System;
using System.Collections.Generic;
using System.ComponentModel;
using Raven.Server.Config.Attributes;
using Raven.Server.Config.Settings;

namespace Raven.Server.Config.Categories
{
    public class ServerConfiguration : ConfigurationCategory
    {
        [DefaultValue(30)]
        [TimeUnit(TimeUnit.Seconds)]
        [ConfigurationEntry("Server.MaxTimeForTaskToWaitForDatabaseToLoadInSec")]
        [LegacyConfigurationEntry("Raven/MaxSecondsForTaskToWaitForDatabaseToLoad")]
        public TimeSetting MaxTimeForTaskToWaitForDatabaseToLoad { get; set; }

        [Description("The server name")]
        [DefaultValue(null)]
        [ConfigurationEntry("Server.Name")]
        [LegacyConfigurationEntry("Raven/ServerName")]
        public string Name { get; set; }

        [Description("Prevent unsafe access to the server")]
        [DefaultValue(AnonymousUserAccessModeValues.Admin)]
        [ConfigurationEntry("AnonymousUserAccessMode")]
        public AnonymousUserAccessModeValues AnonymousUserAccessMode { get; internal set; }

        [Description("When set to true, exposes the database to the world.")]
        [DefaultValue(false)]
        [ConfigurationEntry("AllowAnonymousUserToAccessTheServer")]
        public bool AllowAnonymousUserToAccessTheServer { get; internal set; }
    }
}
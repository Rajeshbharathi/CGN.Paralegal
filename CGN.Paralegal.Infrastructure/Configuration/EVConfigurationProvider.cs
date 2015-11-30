namespace CGN.Paralegal.Infrastructure.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.Linq;

    public class EVConfigurationProvider
    {
        static List<EVConfigurationItem> _configItems;

        public EVConfigurationProvider()
        {
            if(null == _configItems)
            {
                _configItems = LoadConfigurationFromDatabase();
            }
        }

        static List<EVConfigurationItem> LoadConfigurationFromDatabase()
        {
            var configItems = new List<EVConfigurationItem>();
            var connectionString = ConfigurationManager.ConnectionStrings["EV"].ConnectionString; 
            // @"Data Source=EVDBALIAS;Initial Catalog=ConfigPoC;User Id=sa;Password=Admin@2010;";
            const string query = "SELECT * FROM dbo.EVConfiguration";
            var sqlConnection = new SqlConnection(connectionString);
            var sqlCommand = new SqlCommand(query, sqlConnection);
            sqlConnection.Open();
            using (var dr = sqlCommand.ExecuteReader())
            {
                while (dr.Read())
                {
                    var configItem = new EVConfigurationItem
                                         {
                                             Section =
                                                 (dr["Section"] != DBNull.Value)
                                                     ? dr["Section"].ToString().ToLower()
                                                     : string.Empty,
                                             Application =
                                                 (dr["Application"] != DBNull.Value)
                                                     ? dr["Application"].ToString().ToLower()
                                                     : string.Empty,
                                             SubApplication =
                                                 (dr["SubApplication"] != DBNull.Value)
                                                     ? dr["SubApplication"].ToString().ToLower()
                                                     : string.Empty,
                                             ConfigKey =
                                                 (dr["ConfigKey"] != DBNull.Value)
                                                     ? dr["ConfigKey"].ToString().ToLower()
                                                     : string.Empty,
                                             ConfigValue =
                                                 (dr["ConfigValue"] != DBNull.Value)
                                                     ? dr["ConfigValue"].ToString()
                                                     : string.Empty
                                         };
                    configItems.Add(configItem);
                }
            }
            sqlConnection.Close();
            return configItems;
        }

        public string this[string key]
        {
            get { return Get(null, null, null, key); }
        }

        public static string Get(string section, string application, string subApplication, string configKey)
        {
            if (string.IsNullOrEmpty(configKey)) return null;

            if (string.IsNullOrEmpty(section) && string.IsNullOrEmpty(application) && string.IsNullOrEmpty(subApplication))
            {
                return _configItems.First(
                    item =>
                    item.ConfigKey.Equals(configKey.ToLower())
                    ).ConfigValue;
            }
            if(string.IsNullOrEmpty(section) && string.IsNullOrEmpty(application))
            {
                return _configItems.First(
                    item => 
                    item.SubApplication.Equals(subApplication.ToLower()) &&
                    item.ConfigKey.Equals(configKey.ToLower())
                    ).ConfigValue;
            }
            if (string.IsNullOrEmpty(section))
            {
                return _configItems.First(
                    item =>
                    item.Application.Equals(application.ToLower()) &&
                    item.SubApplication.Equals(subApplication.ToLower()) &&
                    item.ConfigKey.Equals(configKey.ToLower())
                    ).ConfigValue;
            }
            return _configItems.First(
                item=>
                item.Section.Equals(section.ToLower()) && 
                item.Application.Equals(application.ToLower()) && 
                item.SubApplication.Equals(subApplication.ToLower()) && 
                item.ConfigKey.Equals(configKey.ToLower())
                ).ConfigValue;
        }

    }
    
    public class EVConfigurationItem
    {
        public string Section { get; set; }
        public string Application { get; set; }
        public string SubApplication { get; set; }
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; }
    }
}
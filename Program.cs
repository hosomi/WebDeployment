using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Web.Deployment;

namespace WebDeployment
{
    public static class MethodExtention
    {
        public static string SafeGetAttribute(this XElement node, string attribute, string defaultValue = null)
        {
            var attr = node.Attribute(attribute);
            return attr == null ? defaultValue : attr.Value;
        }
    }

    public enum ContentType
    {
        Pacakge,
        Folder,
        File
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: {0} <publishSettings> <source>", Path.GetFileName(Assembly.GetEntryAssembly().Location));
                Environment.Exit(1);
            }

            var publishSettingsPath = args[0];
            var sourcePath = args[1];
            ContentType contentType = ContentType.File;

            if (!File.Exists(publishSettingsPath))
            {
                Console.Error.WriteLine("{0}: Not found.", publishSettingsPath);
                Environment.Exit(1);
            }
            if (Directory.Exists(sourcePath))
            {
                contentType = ContentType.Folder;
            }
            else if (!File.Exists(sourcePath))
            {
                Console.Error.WriteLine("{0}: Not found.", sourcePath);
                Environment.Exit(1);
            }
            else if (Path.GetExtension(sourcePath).Equals(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                contentType = ContentType.Pacakge;
            }

            var document = XElement.Load(publishSettingsPath);
            var profile = document.XPathSelectElement("//publishProfile[@publishMethod='MSDeploy']");

            if (profile == null)
            {
                Console.Error.WriteLine("{0}: Not a valid publishing profile.", publishSettingsPath);
                Environment.Exit(1);
            }

            var publishUrl = profile.SafeGetAttribute("publishUrl");
            var destinationAppUrl = profile.SafeGetAttribute("destinationAppUrl");
            var userName = profile.SafeGetAttribute("userName");
            var password = profile.SafeGetAttribute("userPWD");
            var siteName = profile.SafeGetAttribute("msdeploySite");

            // Database related attributes
            var databaseInfo = new SqlConnectionStringBuilder();
            var databaseSection = profile.XPathSelectElements("./databases/add").FirstOrDefault();
            if (databaseSection != null)
            {
                databaseInfo.ConnectionString = databaseSection.SafeGetAttribute("connectionString");
            }
            var webDeployServer = string.Format(@"https://{0}/msdeploy.axd?site={1}", publishUrl, siteName);

            Console.WriteLine("Publishing {0} to {1}", sourcePath, destinationAppUrl);

            // Set up deployment
            var sourceProvider = contentType == ContentType.Pacakge ? DeploymentWellKnownProvider.Package : DeploymentWellKnownProvider.ContentPath;
            var destinationProvider = contentType == ContentType.Pacakge ? DeploymentWellKnownProvider.Auto : DeploymentWellKnownProvider.ContentPath;

            var sourceOptions = new DeploymentBaseOptions();
            var destinationOptions = new DeploymentBaseOptions
            {
                ComputerName = webDeployServer,
                UserName = userName,
                Password = password,
                AuthenticationType = "basic",
                IncludeAcls = true,
                TraceLevel = System.Diagnostics.TraceLevel.Info
            };
            destinationOptions.Trace += (sender, e) => Console.WriteLine(e.Message);

            var destinationPath = siteName;
            if (contentType == ContentType.File)
            {
                var filename = new FileInfo(sourcePath).Name;
                destinationPath += "/" + filename;
            }

            var syncOptions = new DeploymentSyncOptions { DoNotDelete = true };  // Please change as you want

            // Start deployment
            using (var deploy = DeploymentManager.CreateObject(sourceProvider, sourcePath, sourceOptions))
            {
                // Apply package parameters
                foreach (var p in deploy.SyncParameters)
                {
                    switch (p.Name)
                    {
                        case "IIS Web Application Name":
                        case "AppPath":
                            p.Value = siteName;
                            break;
                        case "DbServer":
                            p.Value = databaseInfo.DataSource;
                            break;
                        case "DbName":
                            p.Value = databaseInfo.InitialCatalog;
                            break;
                        case "DbUsername":
                        case "DbAdminUsername":
                            p.Value = databaseInfo.UserID;
                            break;
                        case "DbPassword":
                        case "DbAdminPassword":
                            p.Value = databaseInfo.Password;
                            break;
                    }
                }

                var changeSummary = deploy.SyncTo(destinationProvider, destinationPath, destinationOptions, syncOptions);

                Console.WriteLine("Deployment finsihed.");
                Console.WriteLine("Added: " + changeSummary.ObjectsAdded);
                Console.WriteLine("Updated: " + changeSummary.ObjectsUpdated);
                Console.WriteLine("Deleted: " + changeSummary.ObjectsDeleted);
                Console.WriteLine("Total errors: " + changeSummary.Errors);
                Console.WriteLine("Total changes: " + changeSummary.TotalChanges);

            }
        }
    }
}

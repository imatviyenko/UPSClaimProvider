using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kcell.UPSClaimProvider
{

    [Guid("acb88855-51a8-438f-9923-8a9d916e7d85")]
    public class UPSClaimProviderLogger : SPDiagnosticsServiceBase
    {

        private const string LOG_AREA = "UPSClaimProvider";


        public enum Categories
        {
            General,
            Debug
        }


        public UPSClaimProviderLogger()
        {
        }


        public UPSClaimProviderLogger(string name, SPFarm parent)
            : base(name, parent)
        {
        }


        public static UPSClaimProviderLogger Local
        {
            get
            {
                var LogSvc = SPDiagnosticsServiceBase.GetLocal<UPSClaimProviderLogger>(); 
                // if the Logging Service is registered, just return it.
                if (LogSvc != null)
                    return LogSvc;

                UPSClaimProviderLogger svc = null;
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    // otherwise instantiate and register the new instance, which requires farm administrator privileges
                    svc = new UPSClaimProviderLogger();
                    //svc.Update();
                });
                return svc;
            }
        }


        public static void Unregister()
        {
            SPFarm.Local.Services
                        .OfType<UPSClaimProviderLogger>()
                        .ToList()
                        .ForEach(s =>
                        {
                            s.Delete();
                            s.Unprovision();
                            s.Uncache();
                        });
        }


        public static void LogDebug(string message)
        {
            try
            {
                TraceSeverity severity = TraceSeverity.Verbose;
                SPDiagnosticsCategory category = Local.Areas[LOG_AREA].Categories[UPSClaimProviderLogger.Categories.Debug.ToString()];
                Local.WriteTrace(0, category, severity, message);
                Debug.WriteLine(message);
            }
            catch
            {   // Don't want to do anything if logging goes wrong, just ignore and continue
            }
        }


        public static void LogInfo(string message)
        {
            try
            {
                TraceSeverity severity = TraceSeverity.Medium;
                SPDiagnosticsCategory category = Local.Areas[LOG_AREA].Categories[UPSClaimProviderLogger.Categories.General.ToString()];
                Local.WriteTrace(0, category, severity, message);
                Debug.WriteLine(message);
            }
            catch
            {   // Don't want to do anything if logging goes wrong, just ignore and continue
            }
        }

        public static void LogError(string message)
        {
            try
            {
                SPDiagnosticsCategory category = Local.Areas[LOG_AREA].Categories[UPSClaimProviderLogger.Categories.General.ToString()];

                TraceSeverity traceSeverity = TraceSeverity.High;
                Local.WriteTrace(0, category, traceSeverity, message);

                EventSeverity eventSeverity = EventSeverity.Error;
                Local.WriteEvent(0, category, eventSeverity, message);

                Debug.WriteLine(message);
            }
            catch
            {   // Don't want to do anything if logging goes wrong, just ignore and continue
            }
        }

        protected override IEnumerable<SPDiagnosticsArea> ProvideAreas()
        {
            List<SPDiagnosticsCategory> categories = new List<SPDiagnosticsCategory>
            {
                new SPDiagnosticsCategory(
                                            Categories.General.ToString(),
                                            TraceSeverity.Medium, 
                                            EventSeverity.Error
                ),
                new SPDiagnosticsCategory(
                                            Categories.Debug.ToString(),
                                            TraceSeverity.Verbose,
                                            EventSeverity.Information
                ),

            };

            yield return new SPDiagnosticsArea(
                                                LOG_AREA, 
                                                0, 
                                                0, 
                                                false,
                                                categories);
        }

    }



}

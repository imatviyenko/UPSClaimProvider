using Microsoft.SharePoint.Administration.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;

namespace Kcell.UPSClaimProvider
{
    public class UPSClaimProviderFeatureReceiver : SPClaimProviderFeatureReceiver
    {
        private void ExecBaseFeatureActivated(Microsoft.SharePoint.SPFeatureReceiverProperties properties)
        {
            // Wrapper function for base FeatureActivated. Used because base
            // keyword can lead to unverifiable code inside lambda expression.
            base.FeatureActivated(properties);
        }

        public override string ClaimProviderAssembly
        {
            get
            {
                return typeof(UPSClaimProvider).Assembly.FullName;
            }
        }

        public override string ClaimProviderDescription
        {
            get
            {
                return "SharePoint custom claim provider which uses User Profile Service as the source of information about user accounts";
            }
        }

        public override string ClaimProviderDisplayName
        {
            get
            {
                // This is where we reuse that internal static property.
                return UPSClaimProvider.ProviderDisplayName;
            }
        }

        public override string ClaimProviderType
        {
            get
            {
                return typeof(UPSClaimProvider).FullName;
            }
        }

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            ExecBaseFeatureActivated(properties);

            // Make this custom claims provider enabled but NOT used by default
            // https://samlman.wordpress.com/2015/02/28/configuring-a-custom-claims-provider-to-be-used-only-on-select-zones-in-sharepoint-2010/
            SPClaimProviderManager cpm = SPClaimProviderManager.Local;
            foreach (SPClaimProviderDefinition cp in cpm.ClaimProviders)
            {
                if (cp.ClaimProviderType == typeof(UPSClaimProvider))
                {
                    cp.IsUsedByDefault = false;
                    cpm.Update();
                    break;
                }
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Administration.Claims;
using Microsoft.SharePoint.WebControls;

using Microsoft.Office.Server;
using Microsoft.SharePoint;
using System.Web;
using Microsoft.SharePoint.Administration;

namespace Kcell.UPSClaimProvider
{
    public class UPSClaimProvider : SPClaimProvider
    {
        #region Properties
        internal static string ProviderDisplayName
        {
            get {
                return "UPSClaimProvider";
            }
        }

        internal static string ProviderInternalName
        {
            get
            {
                return "UPSClaimProvider";
            }
        }

        public override string Name
        {
            get
            {
                return ProviderInternalName;
            }
        }



        internal string SPTrustedIdentityTokenIssuerName
        {
            //This is the same value returned from:
            //Get-SPTrustedIdentityTokenIssuer | select Name
            get {
                string valueToReturn = SPTrust.Name;
                return valueToReturn;
            }
        }

        

        private static string UPSEmailAddressClaimType
        {
            //The type of claim that we will return. Our provider only returns the email address (user identifier claim).
            get { return "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"; }
        }

        private static string UPSEmailAddressClaimValueType
        {
            //The type of value that we will return. Our provider only returns email address as a string.
            get { return System.Security.Claims.ClaimValueTypes.String; }
        }


        public override bool SupportsEntityInformation
        {
            get
            {
                return true;
            }
        }
        public override bool SupportsHierarchy
        {
            get
            {
                return false;
            }
        }
        public override bool SupportsResolve
        {
            get
            {
                return true;
            }
        }
        public override bool SupportsSearch
        {
            get
            {
                return true;
            }
        }
        #endregion Properties

        #region Field
        
        // SPTrust associated with the claims provider
        protected SPTrustedLoginProvider SPTrust;

        // Reference to DAL instance for retrvieving users
        private IUsersDAL usersDAL;

        #endregion Field

        #region CONSTRUCTOR    
        public UPSClaimProvider(string displayName): base(displayName) {
            usersDAL = new UPSUsersDAL();
            SPTrust = GetSPTrustAssociatedWithCP(ProviderInternalName);
        }
        #endregion CONSTRUCTOR    


        #region Methods

        // Get the first TrustedLoginProvider associated with current claim provider
        public static SPTrustedLoginProvider GetSPTrustAssociatedWithCP(string ProviderInternalName)
        {
            var lp = SPSecurityTokenServiceManager.Local.TrustedLoginProviders.Where(x => String.Equals(x.ClaimProviderName, ProviderInternalName, StringComparison.OrdinalIgnoreCase));

            if (lp != null && lp.Count() == 1)
                return lp.First();

            if (lp != null && lp.Count() > 1)
            {
                UPSClaimProviderLogger.LogError(String.Format("[{0}] Claims provider {0} is associated to multiple SPTrustedIdentityTokenIssuer, which is not supported because at runtime there is no way to determine what TrustedLoginProvider is currently calling", ProviderInternalName));
                return null;
            }

            UPSClaimProviderLogger.LogError(String.Format("[{0}] Claims provider {0} is not associated with any SPTrustedIdentityTokenIssuer. Set property ClaimProviderName with PowerShell cmdlet Get-SPTrustedIdentityTokenIssuer to create association.", ProviderInternalName));
            return null;
        }
        protected override void FillClaimTypes(List<string> claimTypes)
        {
            if (claimTypes == null)
            { 
                throw new ArgumentNullException("claimTypes");
            };

            // Add our claim type.
            claimTypes.Add(UPSEmailAddressClaimType);
        }

        protected override void FillClaimValueTypes(List<string> claimValueTypes)
        {
            if (claimValueTypes == null)
            {
                throw new ArgumentNullException("claimValueTypes");
            };
                
            // Add our claim value type.
            claimValueTypes.Add(UPSEmailAddressClaimValueType);
        }

        //Augment claims
        protected override void FillClaimsForEntity(Uri context, SPClaim entity, List<SPClaim> claims)
        {
        }


        protected override void FillEntityTypes(List<string> entityTypes)
        {
            if (entityTypes ==null)
            {
                throw new ArgumentNullException("entityTypes");
            };

            entityTypes.Add(SPClaimEntityTypes.User);
        }


        protected override void FillResolve(Uri context, string[] entityTypes, SPClaim resolveInput, List<Microsoft.SharePoint.WebControls.PickerEntity> resolved)
        {
            UPSClaimProviderLogger.LogDebug("FillResolve type2 invoked!");

            string outputString;
            outputString = $"resolveInput - ";
            outputString += $"ClaimType: {resolveInput.ClaimType}; ";
            outputString += $"OriginalIssuer: {resolveInput.OriginalIssuer}; ";
            outputString += $"Value: {resolveInput.Value}; ";
            outputString += $"ValueType: {resolveInput.ValueType}; ";
            UPSClaimProviderLogger.LogDebug(outputString);

            UPSClaimProviderLogger.LogDebug($"SPTrustedIdentityTokenIssuerName: {SPTrustedIdentityTokenIssuerName}");

            if (!resolveInput.OriginalIssuer.ToLower().Contains(SPTrustedIdentityTokenIssuerName.ToLower()))
            {
                return;
            }

            SPClaimProviderManager cpm = SPClaimProviderManager.Local;
            string accountName = cpm.EncodeClaim(resolveInput);
            User foundUser = usersDAL.GetUserByAccountName(accountName);
            if (foundUser == null)
            {
                UPSClaimProviderLogger.LogError($"usersDAL.GetUserByAccountName(accountName) returned null! Error performing the final resolving of the user in FillResolve type2");
                return;
            };
            UPSClaimProviderLogger.LogDebug($"foundUser.Email: {foundUser.Email}");

            PickerEntity entity = GetPickerEntity(foundUser);
            resolved.Add(entity);
            UPSClaimProviderLogger.LogDebug($"Added PickerEntity to resolved with Claim -  Claim.Value: {entity.Claim.Value}, Claim.ClaimType: {entity.Claim.ClaimType}, Claim.OriginalIssuer: {entity.Claim.OriginalIssuer}");
        }
        protected override void FillResolve(Uri context, string[] entityTypes, string resolveInput, List<Microsoft.SharePoint.WebControls.PickerEntity> resolved)
        {
            UPSClaimProviderLogger.LogDebug("FillResolve type1 invoked!");
            string outputString;

            outputString = $"resolveInput: {resolveInput}";
            UPSClaimProviderLogger.LogDebug(outputString);

            List<User> foundUsers = usersDAL.GetUsersBySearchPattern(resolveInput);
            if (foundUsers.Count > 0)
            {
                UPSClaimProviderLogger.LogDebug($"Count of users found: {foundUsers.Count} - input resolved");
                foundUsers.ForEach((foundUser) =>
                {
                    PickerEntity entity = GetPickerEntity(foundUser);
                    resolved.Add(entity);
                    UPSClaimProviderLogger.LogDebug($"Added PickerEntity to resolved with Claim -  Claim.Value: {entity.Claim.Value}, Claim.ClaimType: {entity.Claim.ClaimType}, Claim.OriginalIssuer: {entity.Claim.OriginalIssuer}");
                });
            }
            else if (foundUsers.Count == 0)
            {
                UPSClaimProviderLogger.LogDebug("No users found - input unresolved");
            };

        }

        protected override void FillSchema(Microsoft.SharePoint.WebControls.SPProviderSchema schema)
        {
            throw new NotImplementedException();
        }


        protected void LogDebugSearchTree(Microsoft.SharePoint.WebControls.SPProviderHierarchyTree searchTree)
        {
            UPSClaimProviderLogger.LogDebug($"Writing to log SPProviderHierarchyTree:");
            UPSClaimProviderLogger.LogDebug($"searchTree.Name: {searchTree.Name}");
            UPSClaimProviderLogger.LogDebug($"searchTree.ProviderName: {searchTree.ProviderName}");
            UPSClaimProviderLogger.LogDebug($"searchTree.IsRoot: {searchTree.IsRoot}");
            UPSClaimProviderLogger.LogDebug($"searchTree.IsLeaf: {searchTree.IsLeaf}");
            UPSClaimProviderLogger.LogDebug($"searchTree.HierarchyNodeID: {searchTree.HierarchyNodeID}");
            UPSClaimProviderLogger.LogDebug($"searchTree.HasChildren: {searchTree.HasChildren}");
            UPSClaimProviderLogger.LogDebug($"searchTree.Count: {searchTree.Count}");

        }

        protected override void FillSearch(Uri context, string[] entityTypes, string searchPattern, string hierarchyNodeID, int maxCount, Microsoft.SharePoint.WebControls.SPProviderHierarchyTree searchTree)
        {
            UPSClaimProviderLogger.LogDebug("FillSearch invoked!");

            LogDebugSearchTree(searchTree);

            string outputString;
            outputString = $"searchPattern: {searchPattern}, hierarchyNodeID: {hierarchyNodeID}, maxCount: {maxCount}";
            UPSClaimProviderLogger.LogDebug(outputString);

            List<User> foundUsers = usersDAL.GetUsersBySearchPattern(searchPattern);
            if (foundUsers.Count > 0)
            {
                UPSClaimProviderLogger.LogDebug($"Count of users found: {foundUsers.Count}");

                foundUsers.ForEach((foundUser) =>
                {
                    PickerEntity entity = GetPickerEntity(foundUser);
                    searchTree.AddEntity(entity);
                    UPSClaimProviderLogger.LogDebug($"Added PickerEntity with Claim -  Claim.Value: {entity.Claim.Value}, Claim.ClaimType: {entity.Claim.ClaimType}, Claim.OriginalIssuer: {entity.Claim.OriginalIssuer}");
                });
            }
            else if (foundUsers.Count == 0)
            {
                UPSClaimProviderLogger.LogDebug("No users found");
            };

        }

        protected override void FillHierarchy(Uri context, string[] entityTypes, string hierarchyNodeID, int numberOfLevels, SPProviderHierarchyTree hierarchy)
        {
            throw new NotImplementedException();
        }


        private PickerEntity GetPickerEntity(User user)
        {
            UPSClaimProviderLogger.LogDebug("GetPickerEntity invoked!");

            PickerEntity entity = CreatePickerEntity();

            string originalIssuer = SPOriginalIssuers.Format(SPOriginalIssuerType.TrustedProvider, SPTrustedIdentityTokenIssuerName);
            UPSClaimProviderLogger.LogDebug($"originalIssuer: {originalIssuer}");
            entity.Claim = new SPClaim(UPSEmailAddressClaimType, user.Email, UPSEmailAddressClaimValueType, originalIssuer);
            string claimAsString = entity.Claim.ToEncodedString();
            UPSClaimProviderLogger.LogDebug($"claimAsString: {claimAsString}");


            entity.Description = user.Username;
            entity.DisplayText = user.Username;
            entity.EntityData[PeopleEditorEntityDataKeys.DisplayName] = user.Username;
            entity.EntityData[PeopleEditorEntityDataKeys.Email] = user.Email;
            entity.EntityData[PeopleEditorEntityDataKeys.AccountName] = user.Email;
            entity.EntityData[PeopleEditorEntityDataKeys.Department] = user.Department;
            entity.EntityData[PeopleEditorEntityDataKeys.JobTitle] = user.JobTitle;
            entity.EntityType = SPClaimEntityTypes.User;
            entity.IsResolved = true;
            return entity;
        }

        #endregion Methods
    }
}

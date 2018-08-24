using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.SharePoint;
using Microsoft.Office.Server;
using Microsoft.SharePoint.Administration;
using Microsoft.Office.Server.UserProfiles;



namespace Kcell.UPSClaimProvider
{
    class UPSUsersDAL : IUsersDAL
    {
        public List<User> GetUsersBySearchPattern(string searchPattern)
        {
            UPSClaimProviderLogger.LogDebug("UPSUsersDAL.GetUsersBySearchPattern invoked!");
            string outputString;
            List<User> foundUsers = new List<User>();

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    UPSClaimProviderLogger.LogDebug("Running with elevated privileges");
                    // Access the User Profile Service
                    try
                    {
                        SPServiceContext serviceContext = SPServiceContext.GetContext(SPServiceApplicationProxyGroup.Default, SPSiteSubscriptionIdentifier.Default);
                        UPSClaimProviderLogger.LogDebug("Reference to SPServiceContext obtained");
                        UserProfileManager userProfileManager = new UserProfileManager(serviceContext);
                        UPSClaimProviderLogger.LogDebug("Reference to UserProfileManager obtained");
                        ProfileBase[] searchResults = userProfileManager.Search(searchPattern);
                        UPSClaimProviderLogger.LogDebug($"searchResults.Length: {searchResults.Length}");
                        outputString = searchResults.Aggregate("", (result, item) => String.Concat(result, "User display name: ", item.DisplayName, "; "));
                        UPSClaimProviderLogger.LogDebug(outputString);

                        
                        Array.ForEach(searchResults, (profileBaseItem) =>
                        {
                            UserProfile item = (UserProfile)profileBaseItem;
                            User user = UserProfileToUser(item);
                            outputString = $"Retrieved user properties - Email: {user.Email}, Username: {user.Username}, Firstname: {user.Firstname}, Lastname: {user.Lastname}, Department: {user.Department}, JobTitle: {user.JobTitle}";
                            UPSClaimProviderLogger.LogDebug(outputString);
                            foundUsers.Add(user);
                        });
                        
                    }
                    catch (System.Exception e)
                    {
                        UPSClaimProviderLogger.LogError(e.Message);
                    }
                });
            }
            catch (System.Exception e)
            {
                UPSClaimProviderLogger.LogError($"Error while trying to elevate privileges: {e.Message}");
            };

            return foundUsers;
        }


        public User GetUserByAccountName(string accountName)
        {
            UPSClaimProviderLogger.LogDebug("UPSUsersDAL.GetUserByAccountName invoked!");
            UPSClaimProviderLogger.LogDebug($"accountName: {accountName}");
            User foundUser = null;

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    UPSClaimProviderLogger.LogDebug("Running with elevated privileges");
                    // Access the User Profile Service
                    try
                    {
                        SPServiceContext serviceContext = SPServiceContext.GetContext(SPServiceApplicationProxyGroup.Default, SPSiteSubscriptionIdentifier.Default);
                        UPSClaimProviderLogger.LogDebug("Reference to SPServiceContext obtained");
                        UserProfileManager userProfileManager = new UserProfileManager(serviceContext);
                        UPSClaimProviderLogger.LogDebug("Reference to UserProfileManager obtained");

                        UserProfile userProfile = userProfileManager.GetUserProfile(accountName);
                        UPSClaimProviderLogger.LogDebug($"userProfile: {userProfile}");
                        foundUser = UserProfileToUser(userProfile);
                    }
                    catch (System.Exception e)
                    {
                        UPSClaimProviderLogger.LogError(e.Message);
                    }
                });
            }
            catch (System.Exception e)
            {
                UPSClaimProviderLogger.LogError($"Error while trying to elevate privileges: {e.Message}");
            };

            return foundUser;
        }

        private Kcell.UPSClaimProvider.User UserProfileToUser(UserProfile userProfile)
        {
            User user = null;
            if (userProfile != null)
            {
                user = new Kcell.UPSClaimProvider.User
                {
                    Email = (string)userProfile[PropertyConstants.WorkEmail].Value,
                    Username = userProfile.DisplayName,
                    Firstname = (string)userProfile[PropertyConstants.FirstName].Value,
                    Lastname = (string)userProfile[PropertyConstants.LastName].Value,
                    Department = (string)userProfile[PropertyConstants.Department].Value,
                    JobTitle = (string)userProfile[PropertyConstants.JobTitle].Value
                };
            }
            return user;
        }

    }
}

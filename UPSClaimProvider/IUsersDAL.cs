using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kcell.UPSClaimProvider
{
    interface IUsersDAL
    {
        List<User> GetUsersBySearchPattern(string searchPattern);
        User GetUserByAccountName(string accountName);
    }
}

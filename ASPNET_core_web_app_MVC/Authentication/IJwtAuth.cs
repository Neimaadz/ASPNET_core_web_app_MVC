using System;
using Microsoft.IdentityModel.Tokens;

namespace ASPNET_core_web_app_MVC.Authentication
{
    public interface IJwtAuth
    {
        SecurityToken Authentication(string username, string password);
    }
}

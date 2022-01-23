using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;
using ASPNET_core_web_app_MVC.Models;
using Microsoft.IdentityModel.Tokens;

namespace ASPNET_core_web_app_MVC.Authentication
{
    public class Auth : IJwtAuth
    {
        private readonly string key;

        public Auth(string key)
        {
            this.key = key;
        }
        public SecurityToken Authentication(string username, string password)
        {
            User CurrentUser = new User();

            if (username.Equals(null) || password.Equals(null))
            {
                return null;
            }

            var XMLFile = XElement.Load($@"{Directory.GetCurrentDirectory()}/Data/Users.xml");
            var XMLUsers = from element in XMLFile.Descendants("User")
                           where element.Element("Username").Value.ToLower().Contains(username) &&
                                 element.Element("Password").Value.ToLower().Contains(password)
                           select new User()
                           {
                               Id = Convert.ToInt32(element.Element("Id").Value),
                               Username = element.Element("Username").Value,
                               Password = element.Element("Password").Value,
                               Role = element.Element("Role").Value
                           };

            foreach (var user in XMLUsers)
            {
                CurrentUser.Id = user.Id;
                CurrentUser.Username = user.Username;
                CurrentUser.Password = user.Password;
                CurrentUser.Role = user.Role;
            }


            if (!username.Equals(CurrentUser.Username) || !password.Equals(CurrentUser.Password))
            {
                return null;
            }
            if (username.Equals(CurrentUser.Username) && password.Equals(CurrentUser.Password))
            {
                // 1. Create Security Token Handler
                var tokenHandler = new JwtSecurityTokenHandler();

                // 2. Create Private Key to Encrypted
                var tokenKey = Encoding.ASCII.GetBytes(key);

                //3. Create JETdescriptor
                var tokenDescriptor = new SecurityTokenDescriptor()
                {
                    Subject = new ClaimsIdentity(
                        new Claim[]
                        {
                            // Customing the token
                            new Claim("id", CurrentUser.Id.ToString()),
                            new Claim("username", username),
                            new Claim(ClaimTypes.Role, CurrentUser.Role)    // add a role
                        }),
                    Expires = DateTime.UtcNow.AddMinutes(15),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
                };
                //4. Create Token
                var token = tokenHandler.CreateToken(tokenDescriptor);

                // 6. Return Token from method
                //return tokenHandler.WriteToken(token);
                return token;
            }

            return null;

        }
    }
}

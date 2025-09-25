using System.Collections;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Data;
using Org.BouncyCastle.Ocsp;
using Newtonsoft.Json;

public class loginServices
{
    private readonly Dictionary<string, string> jwt_config = new Dictionary<string, string>();
    IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();


    public loginServices(IHttpContextAccessor httpContextAccessor)
    {
        jwt_config["Key"] = appsettings["jwt_config:Key"].ToString();
        jwt_config["Issuer"] = appsettings["jwt_config:Issuer"].ToString();
        jwt_config["Audience"] = appsettings["jwt_config:Audience"].ToString();
        jwt_config["Subject"] = appsettings["jwt_config:Subject"].ToString();
        jwt_config["ExpiryDuration_app"] = appsettings["jwt_config:ExpiryDuration_app"].ToString();
        jwt_config["ExpiryDuration_web"] = appsettings["jwt_config:ExpiryDuration_web"].ToString();

    }
    dbServices ds = new dbServices();
    public async Task<responseData> getRoles(requestData reqData)
    {
        responseData resData = new responseData();
        resData.eventID = reqData.eventID;
        resData.rData["rCode"] = 0;
        try
        {

            var sq = "select ROLE_ID, ROLE_NAME from m_com_roles order by ROLE_ID desc;";
            var dbdata = ds.ExecuteSQLName(sq, null);

            if (dbdata[0].Count() == 0)
            {
                resData.rData["rCode"] = 1;
                resData.rStatus = 102;
                resData.rData["rMessage"] = "No Data Found";
            }
            else
            {
                resData.rData["rData"] = dbdata;
            }
        }
        catch (System.Exception ex)
        {

            resData.rData["rCode"] = 105;
            resData.rData["rData"] = errors.err[105] + Environment.NewLine + "database connection error ERROR" + ex.Message;

        }

        return resData;
    }
    public async Task<responseData> userRegistration(requestData reqData)
    {
        responseData resData = new responseData();
        resData.rData["rCode"] = 0;
        resData.eventID = reqData.eventID;
        resData.rData["rMessage"] = "Registered Successfully";
        try
        {

            MySqlParameter[] myParams = new MySqlParameter[] {
      new MySqlParameter("@uName",reqData.addInfo["uName"].ToString()),
      new MySqlParameter("@firstName",reqData.addInfo["firstName"].ToString()) ,
      new MySqlParameter("@lastName",reqData.addInfo["lastName"].ToString()) ,
      new MySqlParameter("@emailId",reqData.addInfo["emailId"].ToString()) ,
      new MySqlParameter("@password",reqData.addInfo["password"].ToString()),
      new MySqlParameter("@mobileNo",reqData.addInfo["mobileNo"].ToString()),
      new MySqlParameter("@role",2),
      new MySqlParameter("@isActive",1),
      };
            var sq = "select U_NAME from m_com_users where U_NAME='" + reqData.addInfo["uName"] + "'  OR EMAIL_ID = '" + reqData.addInfo["emailId"] + "';";
            var dbData1 = ds.ExecuteSQLName(sq, myParams);
            if (dbData1[0].Count() > 0)
            {
                resData.rData["rCode"] = 1;
                resData.eventID = reqData.eventID;
                resData.rData["rMessage"] = "User Name Already Exists";
                return resData;
            }
            var sql = @"Insert into m_com_users(U_NAME,FIRST_NAME,LAST_NAME,PASSWORD,EMAIL_ID,MOBILE_NO,ROLE_ID, IS_ACTIVE) values(@uName,@firstName,@lastName,@password,@emailId,@mobileNo,@role,@isActive);";
            var dbData = ds.executeSQL(sql, myParams);
            if (dbData != null)
            {
                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Registered Successfully";
            }
            else
            {
                resData.rData["rCode"] = 1;
                resData.eventID = reqData.eventID;
                resData.rData["rMessage"] = "Error In Registration";
            }
        }
        catch (Exception ex)
        {
            resData.rData["rCode"] = 1;
            resData.eventID = reqData.eventID;
            resData.rData["rMessage"] = "Error In Registration: +" + ex.Message;
        }
        return resData;
    }
    public async Task<responseData> AuthenticateUser(requestData reqData)
    {
        responseData resData = new responseData();
        resData.eventID = reqData.eventID;

        try
        {
            if (!reqData.addInfo.ContainsKey("U_ID") || !reqData.addInfo.ContainsKey("Password"))
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "UserID and Password are required.";
                resData.rStatus = 400;
                return resData;
            }

            string loginQuery = @"
                    SELECT * FROM users WHERE U_ID = @U_ID AND PASSWORD = @PASSWORD AND Is_Active = 1";

            MySqlParameter[] parameters = new MySqlParameter[]
            {
                    new MySqlParameter("@U_ID", reqData.addInfo["U_ID"].ToString()),
                    new MySqlParameter("@PASSWORD", reqData.addInfo["PASSWORD"].ToString()) // In production, use hashed passwords
            };

            var result = ds.ExecuteSQLName(loginQuery, parameters);

            if (result[0].Count() == 0)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "Invalid credentials or user inactive.";
                resData.rStatus = 401;
            }
            else
            {
                var claims = new[]
              {
                        new Claim("uid", result[0][0]["U_ID"].ToString())
                    };
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt_config["Key"]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
                var tokenDescriptor = new JwtSecurityToken(issuer: jwt_config["Issuer"], audience: jwt_config["Audience"], claims: claims,
                    expires: DateTime.Now.AddMinutes(Int32.Parse(jwt_config["ExpiryDuration_app"])), signingCredentials: credentials);
                var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);


                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Login successful.";
                resData.rData["user"] = result[0][0];
                resData.rData["jwtToken"] = token;
            }
        }
        catch (Exception ex)
        {
            resData.rData["rCode"] = 1;
            resData.rData["rMessage"] = $"Authentication error: {ex.Message}";
            resData.rStatus = 500;
        }

        return resData;
    }

    public async Task<responseData> ChangePassword(requestData reqData)
    {
        responseData resData = new responseData();
        resData.eventID = reqData.eventID;

        try
        {
            if (!reqData.addInfo.ContainsKey("Email"))
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "Email is required for password reset.";
                resData.rStatus = 400;
                return resData;
            }

            string searchQuery = "";
            MySqlParameter[] parameters;

            searchQuery = "SELECT * FROM users WHERE EMAIL = @Email AND Active = 1";
            parameters = new MySqlParameter[] { new MySqlParameter("@Email", reqData.addInfo["Email"].ToString()) };


            var result = ds.ExecuteSQLName(searchQuery, parameters);

            if (result[0].Count() == 0)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "User not found or inactive.";
                resData.rStatus = 404;
            }
            else
            {
                string updateQuery = "UPDATE users SET Password = @Password WHERE U_ID = @U_ID";
                MySqlParameter[] updateParams = new MySqlParameter[]
                {
                        new MySqlParameter("@Password", reqData.addInfo["NewPassword"].ToString()),
                        new MySqlParameter("@U_ID", result[0][0]["U_ID"])
                };

                ds.executeSQL(updateQuery, updateParams);

                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Password reset successful. Check your email for the temporary password.";
            }
        }
        catch (Exception ex)
        {
            resData.rData["rCode"] = 1;
            resData.rData["rMessage"] = $"Password reset error: {ex.Message}";
            resData.rStatus = 500;
        }
        return resData;
    }
}
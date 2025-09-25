using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using D_API.services;

IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

WebHost.CreateDefaultBuilder().
ConfigureServices(s =>
{
  s.AddHttpClient();
  s.AddSingleton<loginServices>();
  s.AddSingleton<customerService>();
  s.AddHttpContextAccessor();
  s.AddSingleton<dbServices>();
  s.AddAuthorization();
  s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidIssuer = appsettings["jwt_config:Issuer"].ToString(),
      ValidAudience = appsettings["jwt_config:Audience"],
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appsettings["jwt_config:Key"])),
    };
  });
  s.AddCors();
  s.AddControllers();

}).

Configure(app =>
 {
   app.UseStaticFiles();
   app.UseRouting();
   app.UseAuthentication();
   app.UseAuthorization();

   app.UseCors(options =>
       options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

   app.UseEndpoints(e =>
   {
     var loginServices = e.ServiceProvider.GetRequiredService<loginServices>();
     var customerService = e.ServiceProvider.GetRequiredService<customerService>();

     try
     {

       e.MapPost("/login",
       [AllowAnonymous] async (HttpContext http) =>
              {
                var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
                requestData rData = JsonSerializer.Deserialize<requestData>(body);
                try
                {
                  if (rData.eventID == "1001")
                    await http.Response.WriteAsJsonAsync(await loginServices.userRegistration(rData));
                  else if (rData.eventID == "1002")
                    await http.Response.WriteAsJsonAsync(await loginServices.getRoles(rData));
                  else if (rData.eventID == "1003")
                    await http.Response.WriteAsJsonAsync(await loginServices.AuthenticateUser(rData));
                  else if (rData.eventID == "1004")
                    await http.Response.WriteAsJsonAsync(await loginServices.ChangePassword(rData));
                }
                catch (System.Exception ex)
                {
                  Console.WriteLine(ex);
                }

              });
       e.MapPost("/customer",
       [AllowAnonymous] async (HttpContext http) =>
              {
                var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
                requestData rData = JsonSerializer.Deserialize<requestData>(body);
                try
                {
                  if (rData.eventID == "1001")
                    await http.Response.WriteAsJsonAsync(await customerService.GetCustomerSubscriptions(rData));
                  else if (rData.eventID == "1002")
                    await http.Response.WriteAsJsonAsync(await customerService.AddCustomerSubscription(rData));
                  else if (rData.eventID == "1003")
                    await http.Response.WriteAsJsonAsync(await customerService.UpdateCustomerSubscription(rData));
                  else if (rData.eventID == "1004")
                    await http.Response.WriteAsJsonAsync(await customerService.DeleteCustomerSubscription(rData));
                  else if (rData.eventID == "1005")
                    await http.Response.WriteAsJsonAsync(await customerService.UpdateSubscriptionCount(rData));
                }
                catch (System.Exception ex)
                {
                  Console.WriteLine(ex);
                }

              });

       e.MapGet("/",

             async c => await c.Response.WriteAsJsonAsync("Hello Ajay!.."));
       e.MapGet("/bing",
         async c => await c.Response.WriteAsJsonAsync("{'Name':'Ajay','Age':'26','Project':'Assignment'}"));

     }
     catch (Exception ex)
     {
       Console.Write(ex);
     }

   });
 }).Build().Run();




public record requestData
{
  [Required]
  public string eventID { get; set; }
  [Required]
  public IDictionary<string, object> addInfo { get; set; } // request data .. previously addInfo 
}

public record responseData
{ //response data
  public responseData()
  { // set default values here
    eventID = "";
    rStatus = 0;
    rData = new Dictionary<string, object>();

  }
  [Required]
  public int rStatus { get; set; } = 0; // this will be defaulted 0 fo success and other numbers for failures
  [Required]
  public string eventID { get; set; } //  response ID this is the ID of entity requesting the
  public IDictionary<string, object> addInfo { get; set; } // request data .. previously addInfo 
  public Dictionary<string, object> rData { get; set; }
  //public ArrayList rData {get;set;}
}

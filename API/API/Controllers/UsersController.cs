using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models;
using System.Security.Cryptography;
using System.Text;
using SendGrid;
using SendGrid.Helpers.Mail;
using API.Utils;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using API.DTO;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly IConfiguration _configuration;
        #region Constructor
        public UsersController(UserDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        #endregion

        #region Actions

        // route: "/api/Users/Register"
        [HttpPost(nameof(Register))]
        public async Task<ActionResult> Register([FromBody]User user)
        {
            //If user is registered already
            if (IsEmailRegistered(user.Email))
            {
                throw new Exception("Email id is already registered");
            }
            try
            {
                var password = user.Password;

                // ONE WAY ENCRYPTION: Salted hashing and MD5 encryption algorithm
                var salt = GenerateSalt();
                var hashedPassword = HashPasswordWithSalt(Encoding.UTF8.GetBytes(password), salt);

                user.Password = Convert.ToBase64String(hashedPassword);
                user.Salt = Convert.ToBase64String(salt);

                //create the user
                var CreatedUser = await CreateUser(user, password);
                if (CreatedUser.UserId > 0)
                {
                    var htmlContent = GetHtmlContent(CreatedUser.UserId);
                    var plainTextContent = htmlContent.StripHTML();
                    var ToEmailAddress = new EmailAddress(CreatedUser.Email, "Example User");

                    var emailSent = await SendEmailAsync(htmlContent, plainTextContent, ToEmailAddress);
                }
                return Ok(new { email = user.Email, id = user.UserId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet(nameof(ConfirmEmail))]
        public async Task<ActionResult> ConfirmEmail([FromQuery] string Email, [FromQuery] string Token)
        {
            //1. Get User by email
            User user = GetUserByEmail(Email);
            if (user == null)
            {
                return BadRequest("User is not register");
            }

            //2. Decrypt the token and get expriry date
            var decryptedValue = CryptograpyHelper.Decrypt(Email, user.Salt, Token);

            var urlExpiryDateTime = DateTime.ParseExact(decryptedValue, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var isUrlValid = urlExpiryDateTime > DateTime.UtcNow ? true : false;

            if (!isUrlValid)
            {
                return BadRequest("Email confirmation url is expired");
            }

            var updatedFlagStatus = await UpdateEmailConfirmationFlag(user);

            return Ok("Updated successfully");

        }

        [HttpPost(nameof(Login))]
        public ActionResult Login([FromBody] LoginRequest request)
        {
            //2. Get user using email
            var user = GetUserByEmail(request.Email);

            if (user == null)
            {
                return BadRequest("Email is not registered");
            }

            if (!user.EmailConfirmed)
            {
                return BadRequest("Email is not confirmed");
            }
            //3. Encrypt password using salt s
            var encryptedPassword = HashPasswordWithSalt(Encoding.UTF8.GetBytes(request.Password),
                                                            Convert.FromBase64String(user.Salt));
            var base64Password = Convert.ToBase64String(encryptedPassword);
            //4. match with existing password; if matches then generate access token else bad request
            if (base64Password == user.Password)
            {
                //Serialize JSON object :ExpiryDateTime and UserId
                var LoginRes = new LoginResponse();
                var stringObj = Newtonsoft.Json.JsonConvert.SerializeObject(
                                      new
                                      {
                                          ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyyMMddHHmmss"),
                                          UserId = user.UserId
                                      });

                var encrypt = CryptograpyHelper.Encrypt(user.Email, user.Salt, stringObj);
                var decrypt = CryptograpyHelper.Decrypt(user.Email, user.Salt, encrypt);
                return Ok(true);
            }
            return Unauthorized("Not Authorized to login");

           
            //5. AccessToken Generate

        }

        #endregion

        #region Private functions
        private string GetHtmlContent(int userId)
        {
            var url = GenerateEmailConfirmationURl(userId);
            var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>" +
                "               <h1>Email Confirm URL : <a href=" + url + "> </a></h1>";

            return htmlContent;
        }

        private string GenerateEmailConfirmationURl(int userId)
        {
            User user = GetUser(userId);
            var encryptToken = "";
            if (user != null)
            {
                encryptToken = CryptograpyHelper.Encrypt(user.Email, user.Salt,
                                                    DateTime.UtcNow.AddDays(1).ToString("yyyyMMddHHmmss"));
            }

            var baseUrl = _configuration.GetValue<string>("AppBaseUrl");

            var EmailConfirmUrl = baseUrl + "/api/Users/ConfirmEmail?Email=" + user.Email + "&Token=" + encryptToken;
            return EmailConfirmUrl;
        }

        private User GetUser(int userId)
        {
            return _context.Users.FirstOrDefault(u => u.UserId == userId);
        }

        private User GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        private async Task<User> CreateUser(User user, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password is required");

            if (_context.Users.Any(x => x.Email == user.Email))
                throw new Exception("Email Id \"" + user.Email + "\" is already taken");

            _context.Users.Add(user);
            _context.SaveChanges();

            await _context.SaveChangesAsync();


            return user;
        }
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        private bool IsEmailRegistered(String email)
        {
            return _context.Users.Any(e => e.Email == email);
        }

        private byte[] GenerateSalt()
        {
            const int saltLength = 16; //32;

            using (var randomNumberGenerator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[saltLength];
                randomNumberGenerator.GetBytes(randomNumber);

                return randomNumber;
            }
        }

        private byte[] HashPasswordWithSalt(byte[] toBeHashed, byte[] salt)
        {
            using (var MD51 = MD5.Create())
            {
                byte[] combinedHash = Combine(toBeHashed, salt);

                return MD51.ComputeHash(combinedHash);
            }
        }
        private byte[] Combine(byte[] first, byte[] second)
        {
            var ret = new byte[first.Length + second.Length];

            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);

            return ret;
        }

        private async Task<bool> SendEmailAsync(string htmlContent, string plainContent, EmailAddress to)
        {
            var apiKey = _configuration.GetValue<string>("SENDGRID_API_KEY");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("poonamdhimate@gmail.com", "Example User");
            var subject = "Sending with SendGrid is Fun";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted ||
                                                        response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            return false;

        }

        private async Task<bool> UpdateEmailConfirmationFlag(User user)
        {
            user.EmailConfirmed = true;
            _context.Entry(user).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw ex;
            }
        }


        #endregion

    }


}

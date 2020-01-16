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

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDbContext _context;

        public UsersController(UserDbContext context)
        {
            _context = context;
        }

      //  [Route("api/Users/Register")]
        [HttpPost("Register")]
        public async Task<ActionResult>Register([FromBody]User user)
        {
            //If user is registered already
            if (IsEmailRegistered(user.Email))
            {
                throw new Exception("Email id is already registered");
            }
            try
            {
                var password = user.Password;

                //Salted hashing and MD5 encryption algorithm
                var salt = GenerateSalt();
                var hashedPassword = HashPasswordWithSalt(Encoding.UTF8.GetBytes(password), salt);
                user.Password = BitConverter.ToString(hashedPassword);

                //create the user
                var CreatedUser = CreateUser(user, password);
                if (CreatedUser.UserId>0)
                {
                   await SendEmailAsync();
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        #region Private functions

        private User CreateUser(User user, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password is required");

            if (_context.Users.Any(x => x.Email == user.Email))
                throw new Exception("Email Id \"" + user.Email + "\" is already taken");

            _context.Users.Add(user);
            _context.SaveChanges();

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
            const int saltLength = 32;

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



        private async Task SendEmailAsync()
        {
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("test@example.com", "Example User");
            var subject = "Sending with SendGrid is Fun";
            var to = new EmailAddress("poonamdhimate@gmail.com", "Example User");
            var plainTextContent = "and easy to do anywhere, even with C#";
            var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            
        }

        #endregion

    }


}

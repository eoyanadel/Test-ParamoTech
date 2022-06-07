using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Sat.Recruitment.Api.Models;

namespace Sat.Recruitment.Api.Controllers
{    
    [ApiController]
    [Route("[controller]")]
    public partial class UsersController : ControllerBase
    {
        private readonly List<User> _users = new List<User>();

        public UsersController()
        {
        }

        [HttpPost]
        [Route("/create-user")]
        public async Task<Result> CreateUser(string name, string email, string address, string phone, string userType, string money)
        {
            var errors = "";

            try
            {
                ValidateErrors(name, email, address, phone, ref errors);

                if (errors != null && errors != "")
                    return new Result()
                    {
                        IsSuccess = false,
                        Errors = errors
                    };

                var newUser = new User
                {
                    Name = name,
                    Email = NormalizeEmail(email),
                    Address = address,
                    Phone = phone,
                    UserType = userType,
                    Money = CalculateMoney(decimal.Parse(money), userType)
                };

                getUsersFromFile();            

                var isDuplicated = userIsDuplicated(newUser);

                return (!isDuplicated) ? generateResponse(true, "User Created") : generateResponse(false, "The user is duplicated");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Error creating user - " + ex.Message);
                return new Result()
                {
                    IsSuccess = false,
                    Errors = "Error creating user - " + ex.Message
                };
            }            
        }

        //Validate errors
        private void ValidateErrors(string name, string email, string address, string phone, ref string errors)
        {
            if (name == null)
                //Validate if Name is null
                errors = "The name is required";
            if (email == null)
                //Validate if Email is null
                errors = errors + " The email is required";
            if (address == null)
                //Validate if Address is null
                errors = errors + " The address is required";
            if (phone == null)
                //Validate if Phone is null
                errors = errors + " The phone is required";
        }

        private decimal CalculateMoney(decimal money, string userType)
        {
            decimal percentage = 0;            

            switch(userType)
            {
                case "Normal":
                    if (money > 100) percentage = Convert.ToDecimal(0.12);
                    if (money < 100 && money > 10) percentage = Convert.ToDecimal(0.8);
                    break;
                case "SuperUser":
                    if (money > 100) percentage = Convert.ToDecimal(0.20);
                    break;
                case "Premium":
                    if (money > 100) percentage = 2;
                    break;
                default:                    
                    break;
            }
            
            decimal gif = money * percentage;
            decimal userMoney = money + gif;

            return userMoney;
        }

        private string NormalizeEmail(string email)
        {
            string pattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

            if (!regex.IsMatch(email))
                throw new Exception("Format email invalid");

            return email;            
        }

        private void getUsersFromFile()
        {
            var reader = ReadUsersFromFile();

            while (reader.Peek() >= 0)
            {
                var line = reader.ReadLineAsync().Result;
                var user = new User
                {
                    Name = line.Split(',')[0].ToString(),
                    Email = line.Split(',')[1].ToString(),
                    Phone = line.Split(',')[2].ToString(),
                    Address = line.Split(',')[3].ToString(),
                    UserType = line.Split(',')[4].ToString(),
                    Money = decimal.Parse(line.Split(',')[5].ToString()),
                };
                _users.Add(user);
            }
            reader.Close();
        }

        private bool userIsDuplicated(User newUser)
        {
            if (_users.FindIndex(user => user.Email == newUser.Email || user.Phone == newUser.Phone) != -1)
                return true;

            if (_users.FindIndex(user => user.Name == newUser.Name && user.Address == newUser.Address) != -1)                
                return true;

            return false;
        }

        private Result generateResponse(bool isSuccess, string message)
        {
            Debug.WriteLine(message);

            return new Result()
            {
                IsSuccess = isSuccess,
                Errors = message
            };
        }
    }
}

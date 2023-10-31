using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Simbir_GO_Api.Helpers;
using System.Data;
using static Simbir_GO_Api.MyDBContext;

namespace Simbir_GO_Api.Controllers
{
    [ApiController]
    [Route("api/Admin")]
    public class AdminAccountController : ControllerBase
    {
        private readonly MyDBContext _dbContext;
        public AdminAccountController(MyDBContext dbContext)
        {
            _dbContext = dbContext;
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet("Account")]
        [Authorize(Roles = "admin")]
        public IActionResult GetAccounts(int start, int count)
        {
            if (start < 0 || count <= 0)
            {
                return BadRequest("Некорректные параметры запроса.");
            }

            var accounts = _dbContext.Users
                .Skip(start)
                .Take(count)
                .ToList();

            return Ok(accounts);
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("Account/{id}")]
        [Authorize(Roles = "admin")] 
        public IActionResult GetAccountById(long id)
        {
            var account = _dbContext.Users.FirstOrDefault(u => u.Id == id);

            if (account == null)
            {
                return NotFound("Аккаунт не найден.");
            }

            return Ok(account);
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpPost("Account")]
        [Authorize(Roles = "admin")]
        public IActionResult SignUp([FromBody] User user)
        {
            var existingUser = _dbContext.Users.FirstOrDefault(u => u.UserName == user.UserName);

            if (existingUser != null)
            {
                return BadRequest("Пользователь с таким именем уже существует");
            }

            var newUser = new User
            {
                UserName = user.UserName,
                Password = HashForPass.HashPassword(user.Password),
                IsAdmin = user.IsAdmin,
                Balance = user.Balance
            };

            _dbContext.Users.Add(newUser);
            _dbContext.SaveChanges();

            return Ok("Пользователь успешно зарегистрирован");
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("Account/{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult UpdateAccount(long id, [FromBody] User user)
        {
            var account = _dbContext.Users.FirstOrDefault(u => u.Id == id);

            if (account == null)
            {
                return NotFound("Аккаунт не найден.");
            }

            var existingUser = _dbContext.Users.FirstOrDefault(u => u.UserName == user.UserName);
            if (existingUser != null && existingUser.Id != id)
            {
                return BadRequest("Пользователь с таким именем уже существует.");
            }

            account.UserName = user.UserName;
            account.Password = HashForPass.HashPassword(user.Password);
            account.IsAdmin = user.IsAdmin;
            account.Balance = user.Balance;

            _dbContext.SaveChanges();

            return Ok("Аккаунт успешно обновлен.");
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("Account/{id}")]
        [Authorize(Roles = "admin")] 
        public IActionResult DeleteAccount(long id)
        {
            var account = _dbContext.Users.FirstOrDefault(u => u.Id == id);

            if (account == null)
            {
                return NotFound("Аккаунт не найден.");
            }

            _dbContext.Users.Remove(account);
            _dbContext.SaveChanges();

            return Ok("Аккаунт успешно удален.");
        }
    }
}

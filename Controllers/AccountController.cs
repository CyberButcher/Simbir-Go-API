using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Simbir_GO_Api.Helpers;
using static Simbir_GO_Api.MyDBContext;
using Newtonsoft.Json.Linq;

namespace Simbir_GO_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase 
    {
        private readonly MyDBContext _dbContext;
        public AccountController(MyDBContext dbContext)
        {
            _dbContext = dbContext;
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("Me")]
        [Authorize]
        public IActionResult GetAccountInfo()
        {
            // Возвращает данные о текущем авторизованном аккаунте
            return Ok(new { Username = User.Identity.Name });
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("SignIn")]
        [AllowAnonymous]
        public IActionResult SignIn([FromForm] string username, [FromForm] string password)
        {
            //_dbContext.Database.EnsureCreated();
            var user = _dbContext.Users.SingleOrDefault(u => (u.UserName == username));

            if (user != null && HashForPass.VerifyHashedPassword(user.Password, password))
            {
                return Ok(new { Token = JWTService.GenerateJWT(user) });
            }

            return BadRequest("Неверное имя пользователя или пароль");
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("SignUp")]
        [AllowAnonymous]
        public IActionResult SignUp([FromForm] string username, [FromForm] string password)
        {
            var existingUser = _dbContext.Users.FirstOrDefault(u => u.UserName == username);

            if (existingUser != null)
            {
                return BadRequest("Пользователь с таким именем уже существует");
            }

            var newUser = new User
            {
                UserName = username,
                Password = HashForPass.HashPassword(password),
                IsAdmin = false 
            };

            _dbContext.Users.Add(newUser);
            _dbContext.SaveChanges();

            return Ok("Пользователь успешно зарегистрирован");
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("SignOut")]
        [Authorize]
        public IActionResult SignOut()
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            var newBannedJWT = new BannedJWT
            {
                Token = jwtToken
            };

            _dbContext.BannedJWTs.Add(newBannedJWT);
            _dbContext.SaveChanges();

            return Ok("Выход из аккаунта выполнен успешно.");
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("Update")]
        [Authorize]
        public IActionResult UpdateAccount(
            [FromForm(Name = "oldUsername")] string? oldUsername = null,
            [FromForm(Name = "newUsername")] string? newUsername = null,
            [FromForm(Name = "oldPassword")] string? oldPassword = null,
            [FromForm(Name = "newPassword")] string? newPassword = null)
        {
            var currentUsername = oldUsername;

            // Проверить, не пытается ли пользователь использовать имя, которое уже существует
            var existingUser = _dbContext.Users.FirstOrDefault(u => u.UserName == oldUsername);
            if (existingUser != null && existingUser.UserName != currentUsername)
            {
                return BadRequest("Пользователь с таким именем уже существует.");
            }

            // Найти текущего пользователя в базе данных
            var currentUser = _dbContext.Users.FirstOrDefault(u => u.UserName == currentUsername);

            if (currentUser == null)
            {
                return NotFound("Пользователь не найден.");
            }

            // Обновить данные пользователя, если они были предоставлены
            if (!string.IsNullOrWhiteSpace(oldPassword) && HashForPass.VerifyHashedPassword(currentUser.Password, oldPassword))
            {
                if (!string.IsNullOrWhiteSpace(newUsername))
                {
                    currentUser.UserName = newUsername;
                }

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    currentUser.Password = HashForPass.HashPassword(newPassword);
                }
            }
            else 
            { 
                return BadRequest("Неверный текущий пароль.");
            }

            _dbContext.SaveChanges();

            return Ok(new { Msg = "Аккаунт успешно обновлен.", Token = JWTService.GenerateJWT(currentUser) });
        }
    }
}

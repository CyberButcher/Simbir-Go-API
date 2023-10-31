using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Simbir_GO_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly MyDBContext _dbContext;
        public PaymentController(MyDBContext dbContext) 
        {
            _dbContext = dbContext;
        }


        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPost("Hesoyam/{accountId}")]
        [Authorize]
        public IActionResult AddMoneyToAccount(long accountId)
        {
            var currentUser = _dbContext.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            var account = _dbContext.Users.FirstOrDefault(u => u.Id == accountId);

            if (account == null)
            {
                return NotFound("Аккаунт не найден.");
            }

            // Проверка, может ли текущий пользователь добавить баланс.
            if (User.IsInRole("admin") || (User.Identity.Name == account.UserName))
            {
                account.Balance += 250000;
                _dbContext.SaveChanges();

                return Ok($"Добавлено 250,000 денежных единиц на баланс аккаунта {account.UserName}.");
            }

            return Forbid("Bearer");
        }
    }
}

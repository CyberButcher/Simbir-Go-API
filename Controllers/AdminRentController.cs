using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Simbir_GO_Api.Helpers;
using System.Globalization;
using System.Security.Claims;
using static Simbir_GO_Api.MyDBContext;

namespace Simbir_GO_Api.Controllers
{
    [Route("api/Admin")]
    [ApiController]
    public class AdminRentController : ControllerBase
    {
        private readonly MyDBContext _dbContext;
        public AdminRentController(MyDBContext dbContext) 
        {
            _dbContext = dbContext;
        }

        [HttpGet("Rent/{rentId}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetRentInfo(long rentId)
        {
            var rental = _dbContext.Rentals.Include(r => r.Transport).FirstOrDefault(r => r.Id == rentId);

            if (rental == null)
            {
                return NotFound("Аренда не найдена.");
            }

            return Ok(rental);
        }


        [HttpGet("UserHistory/{userId}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetUserRentHistory(long userId)
        {
            var account = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
            if (account == null)
            {
                return NotFound($"Аккаунт с id {userId} не найден.");
            }

            var userRentals = _dbContext.Rentals.Where(r => r.UserId == userId).ToList();

            return Ok(userRentals);
        }


        [HttpGet("TransportHistory/{transportId}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetTransportRentHistory(long transportId) 
        {
            var currentTransport = _dbContext.Transports.FirstOrDefault(t => t.Id == transportId);
            if (currentTransport == null)
            {
                return NotFound($"Транспорт c id {transportId} не найден.");
            }

            var transportRentals = _dbContext.Rentals.Where(r => r.TransportId == transportId).ToList();

            return Ok(transportRentals);
        }


        [HttpPost("Rent")]
        [Authorize(Roles = "admin")]
        public IActionResult RentTransport([FromBody] Rental rental)
        {
            DateTime parsedDate;
            bool TimeStartDateTypeIsCorrect = DateTime.TryParseExact(rental.TimeStart, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
            bool TimeEndDateTypeIsCorrect = DateTime.TryParseExact(rental.TimeEnd, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);

            var account = _dbContext.Users.FirstOrDefault(u => u.Id == rental.UserId); 
            if (account == null)
            {
                return NotFound($"Аккаунт с id {rental.UserId} не найден.");
            }

            var transport = _dbContext.Transports.FirstOrDefault(t => t.Id == rental.TransportId);
            if (transport == null)
            {
                return NotFound($"Транспорт c id {rental.TransportId} не найден.");
            }

            if (string.IsNullOrWhiteSpace(rental.PriceType) ||
                (rental.PriceType != "Minutes" && rental.PriceType != "Days"))
            {
                return BadRequest("Неверный тип аренды. Должно быть 'Minutes' или 'Days'.");
            }

            if (TimeStartDateTypeIsCorrect == false || (!string.IsNullOrWhiteSpace(rental.TimeEnd) && TimeEndDateTypeIsCorrect == false))
            {
                return BadRequest("Неверный формат даты. Формат даты: yyyy-MM-dd HH:mm:ss");
            }

            var newRental = new Rental
            {
                UserId = rental.UserId,
                TransportId = rental.TransportId,
                TimeStart = rental.TimeStart,
                TimeEnd = rental.TimeEnd,
                PriceOfUnit = rental.PriceOfUnit,
                PriceType = rental.PriceType,
                FinalPrice = rental.FinalPrice
            };

            transport.CanBeRented = false;
            _dbContext.Rentals.Add(newRental);
            _dbContext.SaveChanges();

            return Ok("Новая аренда успешно создана.");
        }


        [HttpPost("Rent/End/{rentId}")]
        [Authorize(Roles = "admin")]
        public IActionResult EndRent(long rentId, [FromForm] double lat, [FromForm] double lon)
        {
            var rental = _dbContext.Rentals.Include(r => r.Transport).FirstOrDefault(r => r.Id == rentId);

            if (rental == null)
            {
                return NotFound("Аренда не найдена.");
            }

            if (lat < -90 || lat > 90)
            {
                return BadRequest("Недопустимое значение для Latitude.");
            }
            if (lon < -180 || lon > 180)
            {
                return BadRequest("Недопустимое значение для Longitude.");
            }

            rental.TimeEnd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            rental.Transport.Latitude = lat;
            rental.Transport.Longitude = lon;
            rental.FinalPrice = PriceCalculations.CalculateFinalRentalPrice(rental);

            rental.Transport.CanBeRented = true;
            _dbContext.Rentals.Update(rental);
            _dbContext.SaveChanges();

            return Ok("Аренда успешно завершена.");
        }


        [HttpPut("Rent/{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult UpdateRent(long id, [FromBody] Rental rental)  
        {
            DateTime parsedDate;
            bool TimeStartDateTypeIsCorrect = DateTime.TryParseExact(rental.TimeStart, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
            bool TimeEndDateTypeIsCorrect = DateTime.TryParseExact(rental.TimeEnd, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);

            var currentRental = _dbContext.Rentals.FirstOrDefault(r => r.Id == id);

            if (currentRental == null)
            {
                return NotFound("Аренда не найдена.");
            }

            if (string.IsNullOrWhiteSpace(rental.PriceType) ||
                (rental.PriceType != "Minutes" && rental.PriceType != "Days"))
            {
                return BadRequest("Неверный тип аренды. Должно быть 'Minutes' или 'Days'.");
            }

            if (TimeStartDateTypeIsCorrect == false || (!string.IsNullOrWhiteSpace(rental.TimeEnd) && TimeEndDateTypeIsCorrect == false))
            {
                return BadRequest("Неверный формат даты. Формат даты: yyyy-MM-dd HH:mm:ss");
            }

            var account = _dbContext.Users.FirstOrDefault(u => u.Id == rental.UserId);
            if (account == null)
            {
                return NotFound($"Аккаунт с id {rental.UserId} не найден.");
            }

            var transport = _dbContext.Transports.FirstOrDefault(t => t.Id == rental.TransportId);
            if (transport == null)
            {
                return NotFound($"Транспорт c id {rental.TransportId} не найден.");
            }

            foreach (var propertyInfo in rental.GetType().GetProperties())
            {
                var updatedValue = propertyInfo.GetValue(rental);

                var currentProperty = currentRental.GetType().GetProperty(propertyInfo.Name);

                if (currentProperty != null && currentProperty.Name != "Id")
                {
                    if (updatedValue is string stringValue)
                    {
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            currentProperty.SetValue(currentRental, stringValue);
                        }
                    }
                    else if (updatedValue is double doubleValue)
                    {
                        if (doubleValue != -1)
                        {
                            currentProperty.SetValue(currentRental, doubleValue);
                        }
                    }
                    else if (updatedValue is long longValue)
                    {
                        if (longValue != -1)
                        {
                            currentProperty.SetValue(currentRental, longValue);
                        }
                    }
                }
            }

            _dbContext.SaveChanges();

            return Ok("Аренда успешно обновлена.");
        }


        [HttpDelete("Rent/{rentId}")]
        [Authorize(Roles = "admin")]
        public IActionResult DeleteRent(long rentId)  
        {
            var rental = _dbContext.Rentals.FirstOrDefault(t => t.Id == rentId);

            if (rental == null)
            {
                return NotFound("Аренда не найдена.");
            }

            _dbContext.Rentals.Remove(rental);
            _dbContext.SaveChanges();

            return Ok("Аренда успешно удалена.");
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using static Simbir_GO_Api.MyDBContext;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Simbir_GO_Api.Helpers;

namespace Simbir_GO_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RentController : ControllerBase
    {
        private readonly MyDBContext _dbContext;
        public RentController(MyDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("Transport")]
        public IActionResult GetAvailableTransport(
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] double radius,
            [FromQuery] string type)
        {
            if (type != "Car" && type != "Bike" && type != "Scooter" && type != "All")
            {
                return BadRequest("Ошибка в заполненных данных! Такого типа транспорта не существует.");
            }

            if (lat < -90 || lat > 90)
            {
                return BadRequest("Недопустимое значение для Latitude.");
            }
            if (lon < -180 || lon > 180)
            {
                return BadRequest("Недопустимое значение для Longitude.");
            }

            if (radius < 0)
            {
                return BadRequest("Радиус не может быть меньше 0.");
            }

            double lon1 = lon - radius / Math.Abs(Math.Cos(lat * Math.PI / 180.0) * 111.0); // 1 градус широты = 111 км
            double lon2 = lon + radius / Math.Abs(Math.Cos(lat * Math.PI / 180.0) * 111.0);
            double lat1 = lat - (radius / 111.0);
            double lat2 = lat + (radius / 111.0);

            var query = _dbContext.Transports
                .Where(t =>
                    t.CanBeRented == true &&
                    t.Latitude >= lat1 &&
                    t.Latitude <= lat2 &&
                    t.Longitude >= lon1 &&
                    t.Longitude <= lon2);

            if (!string.IsNullOrWhiteSpace(type) && type != "All")
            {
                query = query.Where(t => t.TransportType == type);
            }

            var availableTransport = query.ToList();
            return Ok(availableTransport);
        }


        [HttpGet("{rentId}")]
        [Authorize]
        public IActionResult GetRentInfo(long rentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var rental = _dbContext.Rentals.Include(r => r.Transport).FirstOrDefault(r => r.Id == rentId);

            if (rental == null)
            {
                return NotFound("Аренда не найдена.");
            }

            if (rental.UserId != long.Parse(userId) && rental.Transport.OwnerId != long.Parse(userId))
            {
                return Unauthorized("У вас нет доступа к информации об этой аренде.");
            }

            return Ok(rental);
        }


        [HttpGet("MyHistory")]
        [Authorize]
        public IActionResult GetMyRentHistory()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userRentals = _dbContext.Rentals.Where(r => r.UserId == long.Parse(userId)).ToList();

            return Ok(userRentals);
        }


        [HttpGet("TransportHistory/{transportId}")]
        [Authorize]
        public IActionResult GetTransportRentHistory(long transportId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var transport = _dbContext.Transports.FirstOrDefault(t => t.Id == transportId);

            if (transport == null)
            {
                return NotFound("Транспорт не найден.");
            }

            if (transport.OwnerId != long.Parse(userId))
            {
                return Unauthorized("Вы не являетесь владельцем этого транспорта.");
            }

            var transportRentals = _dbContext.Rentals.Where(r => r.TransportId == transportId).ToList();

            return Ok(transportRentals);
        }


        [HttpPost("New/{transportId}")]
        [Authorize]
        public IActionResult RentTransport(long transportId, [FromForm] string rentType)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var transport = _dbContext.Transports.FirstOrDefault(t => t.Id == transportId);

            if (transport == null)
            {
                return NotFound("Транспорт не найден.");
            }

            if (transport.OwnerId == long.Parse(userId))
            {
                return BadRequest("Нельзя брать в аренду собственный транспорт.");
            }

            if (transport.CanBeRented == false) 
            {
                return BadRequest("Этот транспорт нельзя арендовать.");
            }

            if (string.IsNullOrWhiteSpace(rentType) ||
                (rentType != "Minutes" && rentType != "Days"))
            {
                return BadRequest("Неверный тип аренды. Должно быть 'Minutes' или 'Days'.");
            }

            double rentalPrice = 0.0;
            if (rentType == "Minutes")
            {
                rentalPrice = (double)transport.MinutePrice;
            }
            if (rentType == "Days")
            {
                rentalPrice = (double)transport.DayPrice;
            }

            var rental = new Rental
            {
                UserId = long.Parse(userId),
                TransportId = transportId,
                TimeStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                PriceOfUnit = rentalPrice,
                PriceType = rentType
            };

            transport.CanBeRented = false;
            _dbContext.Rentals.Add(rental);
            _dbContext.SaveChanges();

            return Ok("Транспорт успешно арендован.");
        }


        [HttpPost("End/{rentId}")]
        [Authorize]
        public IActionResult EndRent(long rentId, [FromForm] double lat, [FromForm] double lon)   
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var rental = _dbContext.Rentals.Include(r => r.Transport).FirstOrDefault(r => r.Id == rentId);

            if (rental == null)
            {
                return NotFound("Аренда не найдена.");
            }

            if (rental.UserId != long.Parse(userId))
            {
                return Unauthorized("У вас нет доступа для завершения этой аренды.");
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
    }
}

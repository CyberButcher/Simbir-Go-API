using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;
using System.Security.Claims;
using static Simbir_GO_Api.MyDBContext;

namespace Simbir_GO_Api.Controllers
{
    [Route("api/Admin")]
    [ApiController]
    public class AdminTransportController : ControllerBase
    {
        private readonly MyDBContext _dbContext;
        public AdminTransportController(MyDBContext dbContext) 
        {
            _dbContext = dbContext;
        }


        [HttpGet("Transport")]
        [Authorize(Roles = "admin")]
        public IActionResult GetTransports(int start, int count, string transportType) 
        {
            if (start < 0 || count <= 0)
            {
                return BadRequest("Некорректные параметры запроса start и count.");
            }
            if (transportType != "Car" && transportType != "Bike" && transportType != "Scooter" && transportType != "All")
            {
                return BadRequest("Ошибка в заполненных данных! Такого типа транспорта не существует.");
            }

            var query = _dbContext.Transports.AsQueryable(); 

            if (transportType != "All") 
            {
                query = query.Where(t => t.TransportType == transportType);
            }

            var transports = query
                .Skip(start)
                .Take(count)
                .ToList();

            return Ok(transports);
        }


        [HttpGet("Transport/{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetTransportById(long id)
        {
            var transport = _dbContext.Transports.FirstOrDefault(t => t.Id == id);

            if (transport == null)
            {
                return NotFound("Транспорт не найден.");
            }

            return Ok(transport);
        }


        [HttpPost("Transport")]
        [Authorize(Roles = "admin")]
        public IActionResult AddTransport([FromBody] Transport transport)
        {
            if (transport.TransportType != "Car" && transport.TransportType != "Bike" && transport.TransportType != "Scooter")
            {
                return BadRequest("Ошибка в заполненных данных! Такого типа транспорта не существует.");
            }

            var account = _dbContext.Users.FirstOrDefault(u => u.Id == transport.OwnerId);
            if (account == null)
            {
                return NotFound($"Аккаунт с id {transport.OwnerId} не найден.");
            }

            PropertyInfo[] properties = transport.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(transport);
                if (value == null 
                    || (value is string && string.IsNullOrEmpty((string)value)) 
                    && (property.Name != "Description" && property.Name != "MinutePrice" && property.Name != "DayPrice"))
                {
                    return BadRequest($"Ошибка в заполненных данных! Поле {property.Name} не заполнено.");
                }
            }

            if (transport.Latitude < -90 || transport.Latitude > 90)
            {
                return BadRequest("Недопустимое значение для Latitude.");
            }
            if (transport.Longitude < -180 || transport.Longitude > 180)
            {
                return BadRequest("Недопустимое значение для Longitude.");
            }

            var newTransport = new Transport
            {
                CanBeRented = transport.CanBeRented,
                TransportType = transport.TransportType,
                Model = transport.Model,
                Color = transport.Color,
                Identifier = transport.Identifier,
                Description = transport.Description,
                Latitude = transport.Latitude,
                Longitude = transport.Longitude,
                MinutePrice = transport.MinutePrice,
                DayPrice = transport.DayPrice,
                OwnerId = transport.OwnerId
            };

            _dbContext.Transports.Add(newTransport);
            _dbContext.SaveChanges();

            return Ok("Транспорт успешно добавлен.");
        }


        [HttpPut("Transport/{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult UpdateTransport(long id, [FromBody] Transport transport)
        {
            var currentTransport = _dbContext.Transports.FirstOrDefault(t => t.Id == id);

            if (currentTransport == null)
            {
                return NotFound("Транспорт не найден.");
            }

            if (transport.TransportType != "Car" && transport.TransportType != "Bike" && transport.TransportType != "Scooter")
            {
                return BadRequest("Ошибка в заполненных данных! Такого типа транспорта не существует.");
            }

            if ((transport.Latitude < -90 || transport.Latitude > 90) && transport.Latitude != 91)
            {
                return BadRequest("Недопустимое значение для Latitude.");
            }
            if ((transport.Longitude < -180 || transport.Longitude > 180) && transport.Longitude != 181)
            {
                return BadRequest("Недопустимое значение для Longitude.");
            }

            var account = _dbContext.Users.FirstOrDefault(u => u.Id == transport.OwnerId);
            if (account == null)
            {
                return NotFound($"Аккаунт с id {transport.OwnerId} не найден.");
            }

            foreach (var propertyInfo in transport.GetType().GetProperties())
            {
                var updatedValue = propertyInfo.GetValue(transport);

                // Проверяем, если значение свойства не равно null
                if (updatedValue != null && (updatedValue is string && !string.IsNullOrEmpty((string)updatedValue)))
                {
                    // Получаем соответствующее свойство текущего транспорта по имени
                    var currentProperty = currentTransport.GetType().GetProperty(propertyInfo.Name);

                    if (currentProperty != null && currentProperty.Name != "Id")
                    {
                        if ((currentProperty.Name == "Latitude" || currentProperty.Name == "Longitude"))
                        {
                            if ((double)updatedValue != 181 && (double)updatedValue != 91)
                            {
                                currentProperty.SetValue(currentTransport, updatedValue);
                            }
                        }
                        else if (currentProperty.Name == "OwnerId") 
                        {
                            if ((long)updatedValue != -1)
                            {
                                currentProperty.SetValue(currentTransport, updatedValue);
                            }
                        }
                        else
                        {
                            currentProperty.SetValue(currentTransport, updatedValue);
                        }
                    }
                }
            }

            _dbContext.SaveChanges();
            return Ok("Транспорт успешно обновлен.");
        }


        [HttpDelete("Transport/{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult DeleteTransport(long id) 
        {
            var transport = _dbContext.Transports.FirstOrDefault(t => t.Id == id);

            if (transport == null)
            {
                return NotFound("Транспорт не найден.");
            }

            _dbContext.Transports.Remove(transport);
            _dbContext.SaveChanges();

            return Ok("Транспорт успешно удален.");
        }
    }
}

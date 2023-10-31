using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Security.Claims;
using static Simbir_GO_Api.MyDBContext;

namespace Simbir_GO_Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class TransportController : ControllerBase
    {
        private readonly MyDBContext _dbContext;
        public TransportController(MyDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetTransportById(long id)
        {
            var transport = _dbContext.Transports.FirstOrDefault(t => t.Id == id);

            if (transport == null)
            {
                return NotFound("Транспорт не найден.");
            }

            return Ok(transport);
        }

        [HttpPost]
        [Authorize]
        public IActionResult AddTransport([FromBody] Transport transport)
        {
            if (transport.TransportType != "Car" && transport.TransportType != "Bike" && transport.TransportType != "Scooter")
            {
                return BadRequest("Ошибка в заполненных данных! Такого типа транспорта не существует.");
            }

            PropertyInfo[] properties = transport.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(transport);
                if (value == null || (value is string && string.IsNullOrEmpty((string)value)) && (property.Name != "Description" && property.Name != "MinutePrice" && property.Name != "DayPrice"))
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

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
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
                OwnerId = long.Parse(userId)
            };

            _dbContext.Transports.Add(newTransport);
            _dbContext.SaveChanges();

            return Ok("Транспорт успешно добавлен.");
        }

        [HttpPut("{id}")]
        [Authorize]
        public IActionResult UpdateTransport(long id, [FromBody] Transport transport)
        {
            var currentTransport = _dbContext.Transports.FirstOrDefault(t => t.Id == id);
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (currentTransport == null)
            {
                return NotFound("Транспорт не найден.");
            }

            if (currentTransport.OwnerId != long.Parse(userId))
            {
                return Unauthorized("Вы не являетесь владельцем этого транспорта.");
            }

            if (transport.TransportType != "Car" && transport.TransportType != "Bike" && transport.TransportType != "Scooter")
            {
                return BadRequest("Ошибка в заполненных данных! Такого типа транспорта не существует.");
            }

            foreach (var propertyInfo in transport.GetType().GetProperties())
            {
                var updatedValue = propertyInfo.GetValue(transport);

                // Проверяем, если значение свойства не равно null
                if (updatedValue != null && (updatedValue is string && !string.IsNullOrEmpty((string)updatedValue)))
                {
                    // Получаем соответствующее свойство текущего транспорта по имени
                    var currentProperty = currentTransport.GetType().GetProperty(propertyInfo.Name);

                    if (currentProperty != null && currentProperty.Name != "Id" && currentProperty.Name != "OwnerId")
                    {
                        if ((currentProperty.Name == "Latitude" || currentProperty.Name == "Longitude"))
                        {
                            if ((double)updatedValue != 181 && (double)updatedValue != 91)
                            {
                                double newValue = (double)updatedValue;

                                if (currentProperty.Name == "Latitude" && (newValue < -90 || newValue > 90))
                                {
                                    return BadRequest("Недопустимое значение для Latitude.");
                                }
                                else if (currentProperty.Name == "Longitude" && (newValue < -180 || newValue > 180))
                                {
                                    return BadRequest("Недопустимое значение для Longitude.");
                                }
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


        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult DeleteTransport(long id) 
        {
            var transport = _dbContext.Transports.FirstOrDefault(u => u.Id == id);
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (transport == null)
            {
                return NotFound("Транспорт не найден.");
            }

            if (transport.OwnerId != long.Parse(userId))
            {
                return Unauthorized("Вы не являетесь владельцем этого транспорта.");
            }

            _dbContext.Transports.Remove(transport);
            _dbContext.SaveChanges();

            return Ok("Транспорт успешно удален.");
        }
    }
}
